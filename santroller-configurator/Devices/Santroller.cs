using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using LibUsbDotNet;
using ProtoBuf;

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
        CommandSetDetect,
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
    }

    private readonly Dictionary<int, int> _analogRaw = new();
    private readonly Dictionary<int, bool> _digitalRaw = new();
    private readonly Dictionary<int, bool> _digitalRawPeripheral = new();
    private readonly Dictionary<byte, TimeSpan> _ledTimers = new();
    private readonly Stopwatch _sw = Stopwatch.StartNew();

    private DeviceControllerType _deviceControllerType;
    private Microcontroller _microcontroller;
    private ConfigViewModel? _model;
    private bool _picking;
    private readonly DispatcherTimer _timer;
    private ReadOnlyObservableCollection<Output>? _bindings;
    private readonly ConsoleType _currentMode;
    public string Product { get; }
    public string Manufacturer { get; }

    public bool IsSantroller => Product == "Santroller";

    public Santroller(string path, UsbDevice device, string serial,
        ushort version, string product, string manufacturer) : base(
        device, path, serial, version)
    {
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Background, Tick);
        _microcontroller = new Pico(Board.Generic);
        _deviceControllerType = (DeviceControllerType) (version >> 8);
        _currentMode = (ConsoleType) (serial[^3] - '0');
        Product = product;
        Manufacturer = manufacturer;
        if (device is IUsbDevice usbDevice) usbDevice.ClaimInterface(2);
#if Windows
        var isXinput = (serial[^1] - '0') != 0;
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
            WriteData(0, (byte) Commands.CommandJumpBootloaderUno, Array.Empty<byte>());
        }
        else
            WriteData(0, (byte) Commands.CommandJumpBootloader, Array.Empty<byte>());

        Device.Close();
    }


    public override void Revert()
    {
        // Reverting just needs to go to dfu mode, thats good enough
        WriteData(0, (byte) Commands.CommandJumpBootloader, Array.Empty<byte>());
        Device.Close();
    }


    public override Microcontroller GetMicrocontroller(ConfigViewModel model)
    {
        return _microcontroller;
    }

    private void Tick(object? sender, EventArgs e)
    {
        if (_model == null || _bindings == null) return;
        if (!Device.IsOpen || _model.Main.Working)
        {
            _timer.Stop();
            return;
        }

        foreach (var (led, elapsed) in _ledTimers)
        {
            if (_sw.Elapsed - elapsed <= TimeSpan.FromSeconds(2)) continue;
            ClearLed(led);
            _ledTimers.Remove(led);
        }

        try
        {
            var direct = _model.Bindings.Items.Select(s => s.Input.InnermostInput())
                .OfType<DirectInput>().ToList();
            var digital = direct.Where(s => s is {IsAnalog: false, Peripheral: false}).SelectMany(s => s.Pins)
                .Distinct().Where(s => s.Pin != -1);
            var digitalPeripheral = direct.Where(s => s is {IsAnalog: false, Peripheral: true}).SelectMany(s => s.Pins)
                .Distinct().Where(s => s.Pin != -1);
            var analog = direct.Where(s => s is {IsAnalog: true, Peripheral: false}).SelectMany(s => s.Pins).Distinct()
                .Where(s => s.Pin != -1);
            var analogPeripheral = direct.Where(s => s is {IsAnalog: true, Peripheral: true}).SelectMany(s => s.Pins)
                .Distinct().Where(s => s.Pin != -1);
            var ports = _model.Microcontroller.GetPortsForTicking(digital);
            var portsPeripheral = _model.Microcontroller.GetPortsForTicking(digitalPeripheral);

            foreach (var (port, mask) in ports)
            {
                var wValue = (ushort) (port | (mask << 8));
                var data = ReadData(wValue, (byte) Commands.CommandReadDigital, sizeof(byte));
                if (data.Length == 0) return;

                var pins = data[0];
                _model.Microcontroller.PinsFromPortMask(port, mask, pins, _digitalRaw);
            }

            foreach (var (port, mask) in portsPeripheral)
            {
                var wValue = (ushort) (port | (mask << 8));
                var data = ReadData(wValue, (byte) Commands.CommandReadPeripheralDigital, sizeof(byte));
                if (data.Length == 0) return;

                var pins = data[0];
                _model.Microcontroller.PinsFromPortMask(port, mask, pins, _digitalRawPeripheral);
            }

            foreach (var devicePin in analog)
            {
                var mask = _model.Microcontroller.GetAnalogMask(devicePin);
                var wValue = (ushort) (_model.Microcontroller.GetChannel(devicePin.Pin, false) | (mask << 8));
                var val = BitConverter.ToUInt16(ReadData(wValue, (byte) Commands.CommandReadAnalog,
                    sizeof(ushort)));
                _analogRaw[devicePin.Pin] = val;
            }

            var ps2Raw = ReadData(0, (byte) Commands.CommandReadPs2, 9);
            var wiiRaw = ReadData(0, (byte) Commands.CommandReadWii, 8);
            var djLeftRaw = ReadData(0, (byte) Commands.CommandReadDjLeft, 3);
            var djRightRaw = ReadData(0, (byte) Commands.CommandReadDjRight, 3);
            var gh5Raw = ReadData(0, (byte) Commands.CommandReadGh5, 2);
            var ghWtRaw = ReadData(0, (byte) Commands.CommandReadGhWt, 5 * sizeof(int));
            var ps2ControllerType = ReadData(0, (byte) Commands.CommandGetExtensionPs2, 1);
            var wiiControllerType = ReadData(0, (byte) Commands.CommandGetExtensionWii, sizeof(short));
            var cloneRaw = ReadData(0, (byte) Commands.CommandReadClone, 4);
            var usbHostRaw = Array.Empty<byte>();
            var usbHostInputsRaw = Array.Empty<byte>();
            var peripheralWtRaw = Array.Empty<byte>();
            var peripheralConnected = false;
            if (_model.HasPeripheral)
            {
                peripheralWtRaw = ReadData(0, (byte) Commands.CommandReadPeripheralWt, 5 * sizeof(int));
                peripheralConnected = ReadData(0, (byte) Commands.CommandReadPeripheralValid, 1).Any(x => x != 0);
            }
            if (_model.UsbHostEnabled)
            {
                usbHostRaw = ReadData(0, (byte) Commands.CommandReadUsbHost, 24);
                usbHostInputsRaw = ReadData(0, (byte) Commands.CommandReadUsbHostInputs, 100);
            }

            var bluetoothRaw = Array.Empty<byte>();
            if (IsPico()) bluetoothRaw = ReadData(0, (byte) Commands.CommandGetBtState, 1);


            _model.Update(bluetoothRaw, peripheralConnected);
            foreach (var output in _bindings)
                output.Update(_analogRaw, _digitalRaw, ps2Raw, wiiRaw, djLeftRaw,
                    djRightRaw, gh5Raw,
                    ghWtRaw, ps2ControllerType, wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw, _digitalRawPeripheral, cloneRaw);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void Load()
    {
        var fCpuStr = GetString(ReadData(0, (byte) Commands.CommandReadFCpu, 32)).Replace("L", "").Trim();
        if (!fCpuStr.Any()) return;

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
            if (!chunk.Any()) break;
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
        _timer.Start();
        return true;
    }


    public void StartTicking(ConfigViewModel model)
    {
        _model = model;

        _deviceControllerType = model.DeviceControllerType;
        model.Bindings.Connect().Bind(out var allOutputs).Subscribe();
        _bindings = allOutputs;
        _timer.Start();
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
                ReadData(wValue,
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
                    var response = ReadData(wValue,
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
            ReadData(wValue, (byte) (peripheral ? Commands.CommandReadPeripheralDigital : Commands.CommandReadDigital),
                sizeof(byte));
        }
        Dictionary<int, byte> tickedPorts = new();
        while (_picking)
        {
            foreach (var (port, mask) in ports)
            {
                var wValue = (ushort) (port | (mask << 8));
                var pins = (byte) (ReadData(wValue,
                                       (byte) (peripheral
                                           ? Commands.CommandReadPeripheralDigital
                                           : Commands.CommandReadDigital), sizeof(byte)).FirstOrDefault() &
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
        if (InvalidDevice)
        {
            var ret2 = Resources.ErrorNotPCMode;
            if (_currentMode is not (ConsoleType.Universal or ConsoleType.Windows))
            {
                ret2 += $" (Currently {EnumToStringConverter.Convert(_currentMode)})";
            }

            return ret2;
        }

        var ret = $"{Product} - {Board.Name}";
        ret += $" - {EnumToStringConverter.Convert(_deviceControllerType)}";

        return ret;
    }

    public void ClearLed(byte led)
    {
        WriteData(0, (byte) Commands.CommandSetLeds, new byte[] {led, 0, 0, 0});
    }

    public void SetLed(byte led, byte[] color)
    {
        _ledTimers[led] = _sw.Elapsed;
        WriteData(0, (byte) Commands.CommandSetLeds, new[] {led}.Concat(color).ToArray());
    }

    public void StartScan()
    {
        WriteData(0, (byte) Commands.CommandStartBtScan, Array.Empty<byte>());
    }

    public IReadOnlyList<string> GetBtScanResults()
    {
        if (!IsPico()) return Array.Empty<string>();
        var data = ReadData(0, (byte) Commands.CommandGetBtDevices);
        var addressesAsStrings = new List<string>();
        for (var i = 0; i < data.Length; i += BtAddressLength)
        {
            addressesAsStrings.Add(GetString(data[i..(i + BtAddressLength)]));
        }

        return addressesAsStrings;
    }

    public string GetBluetoothAddress()
    {
        return !IsPico() ? "" : GetString(ReadData(0, (byte) Commands.CommandGetBtAddress));
    }

    public override void Disconnect()
    {
        _picking = false;
        _timer.Stop();

        base.Disconnect();
    }

    public void StopTicking()
    {
        _timer.Stop();
    }
}