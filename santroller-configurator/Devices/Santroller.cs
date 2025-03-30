using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Devices;

public class Santroller : ConfigurableUsbDevice
{
    public static readonly Guid DeviceGuid = Guid.Parse("{DF59037D-7C92-4155-AC12-7D700A313D79}");
    public const int BtAddressLength = 18;

    public enum Commands
    {
        CommandReboot = 0x30,
        CommandJumpBootloader,
        CommandJumpBootloaderUno,
        CommandJumpBootloaderUnoUsbThenSerial,
        CommandReadConfig,
        CommandReadFCpu,
        CommandReadBoard,
        CommandReadDigital,
        CommandReadAnalog,
        CommandReadPs2,
        CommandReadWii,
        CommandReadDjLeft,
        CommandReadDjRight,
        CommandReadGh5,
        CommandReadGhWt,
        CommandGetExtensionWii,
        CommandGetExtensionPs2,
        CommandSetLeds,
        CommandDisableMultiplexer,
        CommandReadSerial,
        CommandReadRf,
        CommandReadUsbHost,
        CommandStartBtScan,
        CommandStopBtScan,
        CommandGetBtDevices,
        CommandGetBtState,
        CommandGetBtAddress,
        CommandReadUsbHostInputs,
        CommandReadPeripheralDigital,
        CommandReadPeripheralWt,
        CommandReadPeripheralValid,
        CommandReadClone,
        CommandSetLedsPeripheral,
        CommandWriteAnalog,
        CommandWriteDigital,
        CommandSetBrightness,
        CommandReadAdxl,
        CommandReadMpr121,
        CommandReadMpr121Valid,
        CommandSetLedsMpr121,
        CommandReadMax1270X,
        CommandReadMax1270XValid,
        CommandReadMidi,
        CommandSetAccelFilter,
        CommandReadAccelValid,
        CommandReadBluetoothInputs,
        CommandReadWtDrumConnected
    }

    private readonly Dictionary<byte, TimeSpan> _ledTimers = new();
    private readonly Dictionary<byte, TimeSpan> _ledTimersPeripheral = new();
    private readonly Dictionary<byte, TimeSpan> _ledTimersMpr121 = new();
    private readonly Stopwatch _sw = Stopwatch.StartNew();

    private DeviceControllerType _deviceControllerType;
    private Microcontroller _microcontroller;
    private ConfigViewModel? _model;
    private bool _picking;
    private bool _keyboard = false;
    private bool _running = true;
    private ReadOnlyObservableCollection<Output>? _bindings;
    private readonly ConsoleType _currentMode;
    public bool IsSantroller => Product == "Santroller";

    public Santroller(IUsbDevice device) : base(device)
    {
        _microcontroller = new Pico(Board.Generic);
        _deviceControllerType = (DeviceControllerType) Version.Major;
        _currentMode = (ConsoleType) (Serial[^3] - '0');
        if (Serial[^2] >= 'K')
        {
            _deviceControllerType = (DeviceControllerType) (Serial[^2] - 'K');
        }

        _keyboard = Serial[^2] == 'K';
        device.Claim();
#if Windows
        var isXinput = (Serial[^1] - '0') != 0;
        if (isXinput && _currentMode != ConsoleType.Windows)
        {
            InvalidDevice = true;
            return;
        }
#endif
        if (_currentMode is not (ConsoleType.Universal or ConsoleType.Windows))
        {
            InvalidDevice = true;
            return;
        }
        Load();
        if (Board.Name == Board.Generic.Name)
        {
            InvalidDevice = true;
        }
    }

    private string GetString(byte[] str)
    {
        return Encoding.UTF8.GetString(str).Replace("\0", "");
    }

    private bool InvalidDevice { get; }

    public override bool MigrationSupported => true;

    public override void Bootloader()
    {
        if (Board.HasUsbmcu)
        {
            WriteData(0, (byte) Commands.CommandJumpBootloaderUno, []);
        }
        else
            WriteData(0, (byte) Commands.CommandJumpBootloader, []);

        UsbDevice.Close();
    }


    public override void Revert()
    {
        // Reverting just needs to go to dfu mode, thats good enough
        WriteData(0, (byte) Commands.CommandJumpBootloader, []);
        UsbDevice.Close();
    }


    public override Microcontroller GetMicrocontroller(ConfigViewModel model)
    {
        return _microcontroller;
    }

    private async Task Tick()
    {
        if (_model == null || _bindings == null) return;
        _running = true;
        while (UsbDevice.IsOpen && !_model.Main.Working && _running)
        {

            foreach (var (led, elapsed) in _ledTimers)
            {
                if (_sw.Elapsed - elapsed <= TimeSpan.FromSeconds(2)) continue;
                ClearLed(led);
                _ledTimers.Remove(led);
            }

            foreach (var (led, elapsed) in _ledTimersPeripheral)
            {
                if (_sw.Elapsed - elapsed <= TimeSpan.FromSeconds(2)) continue;
                ClearLedPeripheral(led);
                _ledTimersPeripheral.Remove(led);
            }

            foreach (var (led, elapsed) in _ledTimersMpr121)
            {
                if (_sw.Elapsed - elapsed <= TimeSpan.FromSeconds(2)) continue;
                ClearLedMpr121(led);
                _ledTimersMpr121.Remove(led);
            }

            try
            {
                var direct = _model.Bindings.Items.Where(s => string.IsNullOrEmpty(s.ErrorText))
                    .SelectMany(s => s.Input.InnermostInputs())
                    .OfType<DirectInput>().ToList();
                var digital = direct.Where(s => s is {IsAnalog: false, Peripheral: false}).SelectMany(s => s.Pins)
                    .Distinct().Where(s => s.Pin != -1).ToList();
                var digitalPeripheral = direct.Where(s => s is {IsAnalog: false, Peripheral: true})
                    .SelectMany(s => s.Pins)
                    .Distinct().Where(s => s.Pin != -1);
                var analog = direct.Where(s => s is {IsAnalog: true, Peripheral: false}).SelectMany(s => s.Pins)
                    .Distinct()
                    .Where(s => s.Pin != -1);
                var ports = _model.Microcontroller.GetPortsForTicking(digital);
                var portsPeripheral = _model.Microcontroller.GetPortsForTicking(digitalPeripheral);
                Dictionary<int, int> analogRaw = new();
                Dictionary<int, bool> digitalRaw = new();
                Dictionary<int, bool> digitalRawPeripheral = new();

                foreach (var (port, mask) in ports)
                {
                    var wValue = (ushort) (port | (mask << 8));
                    var data = await ReadDataAsync(wValue, (byte) Commands.CommandReadDigital, sizeof(byte));
                    if (data.Length == 0) return;

                    var pins = data[0];
                    _model.Microcontroller.PinsFromPortMask(port, mask, pins, digitalRaw);
                }

                foreach (var (port, mask) in portsPeripheral)
                {
                    var wValue = (ushort) (port | (mask << 8));
                    var data = await ReadDataAsync(wValue, (byte) Commands.CommandReadPeripheralDigital, sizeof(byte));
                    if (data.Length == 0) return;

                    var pins = data[0];
                    _model.Microcontroller.PinsFromPortMask(port, mask, pins, digitalRawPeripheral);
                }

                foreach (var devicePin in analog)
                {
                    var mask = _model.Microcontroller.GetAnalogMask(devicePin);
                    var wValue = (ushort) (_model.Microcontroller.GetChannel(devicePin.Pin, false) | (mask << 8));
                    var val = BitConverter.ToUInt16(await ReadDataAsync(wValue, (byte) Commands.CommandReadAnalog,
                        sizeof(ushort)));
                    analogRaw[devicePin.Pin] = val;
                }

                var ps2Raw = Array.Empty<byte>();
                var wiiRaw = Array.Empty<byte>();
                var djLeftRaw = Array.Empty<byte>();
                var djRightRaw = Array.Empty<byte>();
                var gh5Raw = Array.Empty<byte>();
                var ghWtRaw = Array.Empty<byte>();
                var ps2ControllerType = Array.Empty<byte>();
                var wiiControllerType = Array.Empty<byte>();
                var cloneRaw = Array.Empty<byte>();
                var usbHostRaw = Array.Empty<byte>();
                var usbHostInputsRaw = Array.Empty<byte>();
                var bluetoothInputsRaw = Array.Empty<byte>();
                var peripheralWtRaw = Array.Empty<byte>();
                var adxlRaw = Array.Empty<byte>();
                var mpr121Raw = Array.Empty<byte>();
                var peripheralConnected = false;
                var mpr121Connected = false;
                var accelConnected = false;
                var wtDrumConnected = false;
                var max1270XRaw = Array.Empty<byte>();
                var max1270XConnected = false;
                var midiRaw = Array.Empty<byte>();

                if (_model.GetTwiForType(WiiInput.WiiTwiType, false) != null)
                {
                    wiiRaw = await ReadDataAsync(0, (byte) Commands.CommandReadWii, 8);
                    wiiControllerType = await ReadDataAsync(0, (byte) Commands.CommandGetExtensionWii, sizeof(short));
                }
                if (_model.GetSpiForType(Ps2Input.Ps2SpiType, false) != null)
                {
                    ps2Raw = await ReadDataAsync(0, (byte) Commands.CommandReadPs2, 32);
                    ps2ControllerType = await ReadDataAsync(0, (byte) Commands.CommandGetExtensionPs2, 1);
                }
                if (_model.GetTwiForType(DjInput.DjTwiType, false) != null)
                {
                    djLeftRaw = await ReadDataAsync(0, (byte) Commands.CommandReadDjLeft, 3);
                    djRightRaw = await ReadDataAsync(0, (byte) Commands.CommandReadDjRight, 3);
                }
                if (_model.GetTwiForType(Gh5NeckInput.Gh5TwiType, false) != null)
                {
                    gh5Raw = await ReadDataAsync(0, (byte) Commands.CommandReadGh5, 2);
                }
                if (_model.GetTwiForType(CloneNeckInput.CloneTwiType, false) != null)
                {
                    cloneRaw = await ReadDataAsync(0, (byte) Commands.CommandReadClone, 4);
                }

                if (_model.Bindings.Items.Any(s => s.Input.InnermostInputs().Any(x => x is GhWtTapInput)))
                {
                    ghWtRaw = await ReadDataAsync(0, (byte) Commands.CommandReadGhWt, 5 * sizeof(int));
                }

                if (_model.HasPeripheral)
                {
                    peripheralWtRaw = await ReadDataAsync(0, (byte) Commands.CommandReadPeripheralWt, 5 * sizeof(int));
                    peripheralConnected =
                        (await ReadDataAsync(0, (byte) Commands.CommandReadPeripheralValid, 1)).Any(x => x != 0);
                }

                if (_model.UsbHostEnabled)
                {
                    usbHostRaw = await ReadDataAsync(0, (byte) Commands.CommandReadUsbHost, 24);
                    usbHostInputsRaw = await ReadDataAsync(0, (byte) Commands.CommandReadUsbHostInputs, 97);
                }
                if (_model.IsBluetoothRx)
                {
                    bluetoothInputsRaw = await ReadDataAsync(0, (byte) Commands.CommandReadBluetoothInputs, 101);
                }

                
                if (_model.HasAccel)
                {
                    accelConnected = (await ReadDataAsync(0, (byte) Commands.CommandReadAccelValid, 1)).Any(x => x != 0);
                    adxlRaw = await ReadDataAsync(0, (byte) Commands.CommandReadAdxl, 6 * sizeof(short));
                }

                if (_model.HasMpr121)
                {
                    mpr121Raw = await ReadDataAsync(0, (byte) Commands.CommandReadMpr121, sizeof(short));
                    mpr121Connected = (await ReadDataAsync(0, (byte) Commands.CommandReadMpr121Valid, 1)).Any(x => x != 0);
                }

                if (_model.HasMax1704X)
                {
                    max1270XRaw = await ReadDataAsync(0, (byte) Commands.CommandReadMax1270X, sizeof(byte));
                    max1270XConnected = (await ReadDataAsync(0, (byte) Commands.CommandReadMax1270XValid, 1)).Any(x => x != 0);
                }

                if (_model.HasWtDrumInput)
                {
                    wtDrumConnected = (await ReadDataAsync(0, (byte) Commands.CommandReadWtDrumConnected, 1)).Any(x => x != 0);
                }

                var bluetoothRaw = Array.Empty<byte>();
                if (IsPico) bluetoothRaw = await ReadDataAsync(0, (byte) Commands.CommandGetBtState, 1);
                if (!UsbDevice.IsOpen || _model.Main.Working)
                {
                    return;
                }
                midiRaw = await ReadDataAsync(0, (byte) Commands.CommandReadMidi, 132);
                _model.Update(bluetoothRaw, peripheralConnected, mpr121Connected, max1270XConnected, max1270XRaw, accelConnected, wtDrumConnected);
                foreach (var output in _bindings)
                    output.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw,
                        djRightRaw, gh5Raw,
                        ghWtRaw, ps2ControllerType, wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw,
                        peripheralWtRaw, digitalRawPeripheral, cloneRaw, adxlRaw, mpr121Raw, midiRaw, bluetoothInputsRaw, peripheralConnected);
                // If nothing is being ticked, then give the UI some time to process data instead of polling at max speed
                if (analogRaw.Count == 0 && digitalRaw.Count == 0 && digitalRawPeripheral.Count == 0 && midiRaw.Length == 0)
                {
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    private void Load()
    {
        var fCpuStr = GetString(ReadData(0, (byte) Commands.CommandReadFCpu, 32)).Replace("L", "").Trim();
        if (fCpuStr.Length == 0) return;

        var fCpu = uint.Parse(fCpuStr);
        var board = GetString(ReadData(0, (byte) Commands.CommandReadBoard, 32));
        var m = Board.FindMicrocontroller(Board.FindBoard(board, fCpu));
        Board = m.Board;
        _microcontroller = m;
    }


    public override bool LoadConfiguration(ConfigViewModel model, bool merge)
    {
        ushort start = 0;
        using var inputStream = new MemoryStream();
        while (true)
        {
            var chunk = ReadData(start, (byte) Commands.CommandReadConfig, 64);
            if (chunk.Length == 0) break;
            inputStream.Write(chunk);
            start += 64;
        }

        inputStream.Seek(0, SeekOrigin.Begin);
        using var decompressor = new BrotliStream(inputStream, CompressionMode.Decompress);
        try
        {
            var config = Serializer.Deserialize<SerializedConfiguration>(decompressor);
            if (merge)
            {
                config.Merge(model);
            }
            else
            {
                config.LoadConfiguration(model);
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.StackTrace);
            Console.WriteLine(ex.StackTrace);
            return false;
        }

        _deviceControllerType = model.DeviceControllerType;

        _model = model;
        model.Bindings.Connect().Bind(out var bindings).Subscribe();
        _bindings = bindings;
        _ = Tick();
        return true;
    }


    public void StartTicking(ConfigViewModel model)
    {
        _model = model;

        _deviceControllerType = model.DeviceControllerType;
        model.Bindings.Connect().Bind(out var allOutputs).Subscribe();
        _bindings = allOutputs;
        _ = Tick();
    }

    public void CancelDetection()
    {
        _picking = false;
    }

    public async Task<int> DetectPinAsync(bool analog, int original, Microcontroller microcontroller, bool peripheral)
    {
        _picking = true;
        var importantPins = new List<int>();
        foreach (var config in _model!.GetPinConfigs())
            switch (config)
            {
                case SpiConfig spi when spi.Peripheral == peripheral:
                    importantPins.AddRange(spi.Pins.Where(s => s != -1));
                    break;
                case TwiConfig twi when twi.Peripheral == peripheral:
                    importantPins.AddRange(twi.Pins.Where(s => s != -1));
                    break;
                case DirectPinConfig direct when direct.Peripheral == peripheral:
                    if (!direct.Type.Contains("-")) importantPins.AddRange(direct.Pins.Where(s => s != -1));
                    break;
            }

        // Its important here that we do not tick bluetooth related pins if the user has previously programmed bluetooth and then disabled it, as that will lock up the pico.
        if (analog)
        {
            var pins = _microcontroller.GetAllPins()
                .Where(s => _microcontroller.FilterPin(true, _model.IsOrWasBluetooth, false, s)).Except(importantPins)
                .ToList();
            var analogVals = new Dictionary<int, int>();
            foreach (var pin in pins)
            {
                var devicePin = new DevicePin(pin, DevicePinMode.PullUp);
                var mask = microcontroller.GetAnalogMask(devicePin);
                var wValue = (ushort) (microcontroller.GetChannel(pin, true) | (mask << 8));
                await ReadDataAsync(wValue,
                    (byte) Commands.CommandReadAnalog,
                    sizeof(ushort));
            }

            while (_picking)
            {
                foreach (var pin in pins)
                {
                    var devicePin = new DevicePin(pin, DevicePinMode.PullUp);
                    var mask = microcontroller.GetAnalogMask(devicePin);
                    var wValue = (ushort) (microcontroller.GetChannel(pin, true) | (mask << 8));
                    var response = await ReadDataAsync(wValue,
                        (byte) Commands.CommandReadAnalog,
                        sizeof(ushort));
                    if (response.Length < 2)
                    {
                        return original;
                    }

                    var val = BitConverter.ToUInt16(response);
                    if (analogVals.TryGetValue(pin, out var analogVal))
                    {
                        if (Math.Abs(analogVal - val) <= 3000) continue;
                        _picking = false;
                        return pin;
                    }

                    analogVals[pin] = val;
                }

                await Task.Delay(100);
            }

            return original;
        }

        var allPins = _microcontroller.GetAllPins()
            .Where(s => _microcontroller.FilterPin(false, _model.IsOrWasBluetooth, false, s)).Except(importantPins)
            .Select(s => new DevicePin(s, DevicePinMode.PullUp)).ToList();
        var ports = microcontroller.GetPortsForTicking(allPins);
        foreach (var (port, mask) in ports)
        {
            var wValue = (ushort) (port | (mask << 8));
            await ReadDataAsync(wValue, (byte) (peripheral ? Commands.CommandReadPeripheralDigital : Commands.CommandReadDigital),
                sizeof(byte));
        }

        Dictionary<int, byte> tickedPorts = new();
        while (_picking)
        {
            foreach (var (port, mask) in ports)
            {
                var wValue = (ushort) (port | (mask << 8));
                var pins = (byte) ((await ReadDataAsync(wValue,
                                       (byte) (peripheral
                                           ? Commands.CommandReadPeripheralDigital
                                           : Commands.CommandReadDigital), sizeof(byte))).FirstOrDefault() &
                                   mask);
                if (tickedPorts.ContainsKey(port))
                {
                    if (tickedPorts[port] != pins)
                    {
                        Dictionary<int, bool> outPins = new();
                        // Xor the old and new values to work out what changed, and then return the first changed bit
                        // Note that we also need to invert this, as pinsFromPortMask is configured assuming a pull up is in place,
                        // Which would then be expecting a zero for a active pin and a 1 for a inactive pin.
                        microcontroller.PinsFromPortMask(port, mask, (byte) ~(pins ^ tickedPorts[port]), outPins);
                        _picking = false;
                        return outPins.First(s => !s.Value).Key;
                    }
                }

                tickedPorts[port] = pins;
            }

            await Task.Delay(100);
        }

        return original;
    }

    public override string ToString()
    {
        if (!InvalidDevice)
        {
            var type = EnumToStringConverter.Convert(_deviceControllerType);
            if (_keyboard)
            {
                type = Resources.ConsoleTypeKeyboardMouse;
            }
            
            return IsSantroller
                ? $"{Product} - {Board.Name} - {type}"
                : Product;
        }

        var ret2 = Resources.ErrorNotPCMode;
        if (_currentMode is not (ConsoleType.Universal or ConsoleType.Windows))
        {
            ret2 += $" (Currently {EnumToStringConverter.Convert(_currentMode)})";
        }

        return ret2;
    }

    public void ClearLed(byte led)
    {
        WriteData(0, (byte) Commands.CommandSetLeds, [led, 0, 0, 0]);
    }
    
    public void ClearLedMpr121(byte led)
    {
        WriteData(0, (byte) Commands.CommandSetLedsMpr121, [led, 0, 0, 0]);
    }

    public void ClearLedPeripheral(byte led)
    {
        WriteData(0, (byte) Commands.CommandSetLedsPeripheral, [led, 0, 0, 0]);
    }

    public void AnalogWrite(int pin, int value)
    {
        WriteData(0, (byte) Commands.CommandWriteAnalog, [(byte) pin, (byte) value, 0, 0]);
    }

    public void DigitalWrite(int pin, bool value)
    {
        var ports = _microcontroller.GetPortsForTicking([new DevicePin(pin, DevicePinMode.Output)]);
        foreach (var (port, mask) in ports)
        {
            WriteData(0, (byte) Commands.CommandWriteDigital,
                [(byte) port, (byte) mask, (byte) (value ? mask : 0), 0]);
        }
    }
    
    public void SetBrightness(int brightness)
    {
        WriteData(0, (byte) Commands.CommandSetBrightness,
            [(byte)brightness]);
    }

    public void SetLed(byte led, Color color, byte brightness)
    {
        if (_model == null) return;
        _ledTimers[led] = _sw.Elapsed;
        var bytes = _model.LedType.GetLedBytes(color, brightness);
        
        // If the user changes led colour order, translate the colours so that they can see the effects of the led type being changed live
        if (!_model.Branded && _model.LastLedType != LedType.None)
        {
            if (_model.LastLedType != _model.LedType)
            {
                bytes = _model.LastLedType.TranslateLedBytes(bytes);
            }
        }

        WriteData(0, (byte) Commands.CommandSetLeds, new[] {led}.Concat(bytes).ToArray());
    }

    public void SetLedPeripheral(byte led, Color color, byte brightness)
    {
        if (_model == null) return;
        _ledTimersPeripheral[led] = _sw.Elapsed;
        
        var bytes = _model.LedTypePeripheral.GetLedBytes(color, brightness);
        
        // If the user changes led colour order, translate the colours so that they can see the effects of the led type being changed live
        if (!_model.Branded && _model.LastLedTypePeripheral != LedType.None)
        {
            if (_model.LastLedTypePeripheral != _model.LedTypePeripheral)
            {
                bytes = _model.LastLedTypePeripheral.TranslateLedBytes(bytes);
            }
        }

        WriteData(0, (byte) Commands.CommandSetLedsPeripheral, new[] {led}.Concat(bytes).ToArray());
    }

    public void SetAccelFilter(double filter)
    {
        WriteData(0, (byte) Commands.CommandSetAccelFilter, BitConverter.GetBytes(filter));
    }

    public void SetLedStp(byte led, bool state)
    {
        _ledTimers[led] = _sw.Elapsed;
        WriteData(0, (byte) Commands.CommandSetLeds, [led, (byte) (state ? 1 : 0)]);
    }
    
    public void SetLedMpr121(byte led, bool state)
    {
        _ledTimersMpr121[led] = _sw.Elapsed;
        WriteData(0, (byte) Commands.CommandSetLedsMpr121, [led, (byte) (state ? 1 : 0)]);
    }

    public void SetLedStpPeripheral(byte led, bool state)
    {
        _ledTimersPeripheral[led] = _sw.Elapsed;
        WriteData(0, (byte) Commands.CommandSetLedsPeripheral, [led, (byte) (state ? 1 : 0)]);
    }

    public void StartScan()
    {
        WriteData(0, (byte) Commands.CommandStartBtScan, []);
    }

    public IReadOnlyList<string> GetBtScanResults()
    {
        if (!IsPico) return Array.Empty<string>();
        var data = ReadData(0, (byte) Commands.CommandGetBtDevices);
        return Encoding.UTF8.GetString(data).Split("\0").Where(s => s.Length != 0).ToList();
    }

    public string GetBluetoothAddress()
    {
        return !IsPico ? "" : GetString(ReadData(0, (byte) Commands.CommandGetBtAddress));
    }

    public int AnalogRead(int pin)
    {
        if (_model == null) return 0;
        var devicePin = new DevicePin(pin, DevicePinMode.Analog);
        var mask = _model.Microcontroller.GetAnalogMask(devicePin);
        var wValue = (ushort) (_model.Microcontroller.GetChannel(devicePin.Pin, false) | (mask << 8));
        var val = BitConverter.ToUInt16(ReadData(wValue, (byte) Commands.CommandReadAnalog,
            sizeof(ushort)));
        return val;
    }

    public override void Disconnect()
    {
        _picking = false;
        _running = false;

        base.Disconnect();
    }

    public void StopTicking()
    {
        _running = false;
    }

    public int MultiplexerRead(int pinS0, int pinS1, int pinS2, int pinS3, int pin, int channel, bool isSixteenChannel)
    {
        // Disable standard multiplexer reads otherwise standard controller polls will set digital pins in the background
        WriteData(0, (byte) Commands.CommandDisableMultiplexer,
            [1]);
        DigitalWrite(pinS0, (channel & (1 << 0)) != 0);
        DigitalWrite(pinS1, (channel & (1 << 1)) != 0);
        DigitalWrite(pinS2, (channel & (1 << 2)) != 0);
        if (isSixteenChannel)
        {
            DigitalWrite(pinS3, (channel & (1 << 3)) != 0);
        }
        var ret = AnalogRead(pin);
        // Enable them again
        WriteData(0, (byte) Commands.CommandDisableMultiplexer,
            [0]);
        return ret;
    }
}