using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using LibUsbDotNet;
using Version = SemanticVersioning.Version;

namespace GuitarConfigurator.NetCore.Devices;

public class Ardwiino : ConfigurableUsbDevice
{
    public static readonly Guid DeviceGuid = Guid.Parse("{DF59037D-7C92-4155-AC12-7D700A313D78}");

    public enum InputControllerType
    {
        None,
        Wii,
        Direct,
        Ps2
    }

    private const int XboxBtnCount = 16;
    private const int XboxAxisCount = 6;
    private const int XboxTriggerCount = 2;

    private const ControllerAxisType XboxWhammy = ControllerAxisType.XboxRx;
    private const ControllerAxisType XboxTilt = ControllerAxisType.XboxRy;

    public static readonly List<(int vendorId, int productId)> HardwareIds = new()
    {
        // Main IDs
        (0x1209, 0x2882),

        // PS3 IDs
        (0x12BA, 0x0100),
        (0x12BA, 0x0120),
        (0x12BA, 0x0140),
        (0x12BA, 0x0200),
        (0x12BA, 0x0210),
        (0x12BA, 0x074B),

        // Wii IDs
        (0x1BAD, 0x0004),
        (0x1BAD, 0x0005),
        (0x1BAD, 0x074B), // Copy-paste error present in the old firmware, included just in case
        (0x1BAD, 0x3010),
        (0x1BAD, 0x3110),
        (0x0112, 0x0F0D), // IDs from unintended Wii Live Guitar configuration in old firmware

        // Switch IDs
        (0x0F0D, 0x0092),
    };

    public const ushort SerialArdwiinoRevision = 0x3122;

    // On 6.0.0 and above READ_CONFIG is 59
    // On 7.0.3 and above READ_CONFIG is 60
    // And with 8.0.7 and above READ_CONFIG is 62
    private const ushort CpuInfoCommand = 50;
    private const ushort JumpBootloaderCommand = 49;
    private const ushort JumpBootloaderCommandUno = 50;
    private const ushort ReadConfigCommand = 62;
    private const ushort ReadConfigPre807Command = 60;
    private const ushort ReadConfigPre703Command = 59;
    private const byte RequestHidGetReport = 0x01;
    private const byte RequestHidSetReport = 0x09;

    private const byte NotUsed = 0xFF;
    private static readonly Version OldCpuInfoVersion = new(8, 8, 4);

    private static readonly Version UsbControlRequestApi = new(4, 3, 7);

    private static readonly Dictionary<ControllerAxisType, StandardAxisType> AxisToStandard =
        new()
        {
            {ControllerAxisType.XboxLx, StandardAxisType.LeftStickX},
            {ControllerAxisType.XboxLy, StandardAxisType.LeftStickY},
            {ControllerAxisType.XboxRx, StandardAxisType.RightStickX},
            {ControllerAxisType.XboxRy, StandardAxisType.RightStickY},
            {ControllerAxisType.XboxLt, StandardAxisType.LeftTrigger},
            {ControllerAxisType.XboxRt, StandardAxisType.RightTrigger}
        };

    private static readonly Dictionary<ControllerButtons, StandardButtonType> ButtonToStandard =
        new()
        {
            {ControllerButtons.XboxDpadUp, StandardButtonType.DpadUp},
            {ControllerButtons.XboxDpadDown, StandardButtonType.DpadDown},
            {ControllerButtons.XboxDpadLeft, StandardButtonType.DpadLeft},
            {ControllerButtons.XboxDpadRight, StandardButtonType.DpadRight},
            {ControllerButtons.XboxStart, StandardButtonType.Start},
            {ControllerButtons.XboxBack, StandardButtonType.Back},
            {ControllerButtons.XboxLeftStick, StandardButtonType.LeftThumbClick},
            {ControllerButtons.XboxRightStick, StandardButtonType.RightThumbClick},
            {ControllerButtons.XboxLb, StandardButtonType.LeftShoulder},
            {ControllerButtons.XboxRb, StandardButtonType.RightShoulder},
            {ControllerButtons.XboxHome, StandardButtonType.Guide},
            {ControllerButtons.XboxUnused, StandardButtonType.Capture},
            {ControllerButtons.XboxA, StandardButtonType.A},
            {ControllerButtons.XboxB, StandardButtonType.B},
            {ControllerButtons.XboxX, StandardButtonType.X},
            {ControllerButtons.XboxY, StandardButtonType.Y}
        };

    private readonly bool _failed = false;

    private readonly List<StandardButtonType> _frets = new()
    {
        StandardButtonType.A, StandardButtonType.B, StandardButtonType.X, StandardButtonType.Y,
        StandardButtonType.LeftShoulder,
        StandardButtonType.RightShoulder
    };

    public Ardwiino(string path, UsbDevice device, string serial, ushort versionNumber)
        : base(device, path, serial, versionNumber)
    {
        if (Version < new Version(6, 0, 0))
        {
            var buffer = ReadData(6, RequestHidGetReport);
            var cpuFreq = uint.Parse(StructTools.RawDeserializeStr(buffer));
            buffer = ReadData(7, RequestHidGetReport);
            var board = StructTools.RawDeserializeStr(buffer);
            Board = Board.FindBoard(board, cpuFreq);
            MigrationSupported = false;
            return;
        }

        MigrationSupported = true;
        // Version 6.0.0 started at config version 6, so we don't have to support anything earlier than that
        var data = ReadData(CpuInfoCommand, 1);
        if (Version < OldCpuInfoVersion)
        {
            var info = StructTools.RawDeserialize<CpuInfoOld>(data, 0);
            var cpuFreq = info.cpu_freq;
            Board = Board.FindBoard(info.board, cpuFreq);
        }
        else
        {
            var info = StructTools.RawDeserialize<CpuInfo>(data, 0);
            var cpuFreq = info.cpu_freq;
            Board = Board.FindBoard(info.board, cpuFreq);
        }
    }

    public override bool MigrationSupported { get; }

    public override string ToString()
    {
        if (_failed) return Resources.ErrorArdwiinoReading;

        return $"Ardwiino - {Board.Name} - {Version}";
    }

    public override void Bootloader()
    {
        WriteData(JumpBootloaderCommand, RequestHidSetReport, Array.Empty<byte>());
    }

    public override void Revert()
    {
    }

    public override bool LoadConfiguration(ConfigViewModel model, bool merge)
    {
        if (!MigrationSupported) return false;
        var readConfig = ReadConfigCommand;
        if (Version < new Version(8, 0, 7))
            readConfig = ReadConfigPre807Command;
        else if (Version < new Version(7, 0, 3)) readConfig = ReadConfigPre703Command;

        var data = new byte[Marshal.SizeOf<ArdwiinoConfiguration>()];
        var sizeOfAll = Marshal.SizeOf<FullArdwiinoConfiguration>();
        var offset = 0;
        var offsetId = 0;
        var maxSize = data.Length;
        uint version = 0;
        // Set the defaults for things that arent in every version
        var config = new ArdwiinoConfiguration();
        config.neck = new NeckConfig();
        config.axisScale = new AxisScaleConfig();
        config.axisScale.axis = new AxisScale[XboxAxisCount];
        foreach (int axis in Enum.GetValues<ControllerAxisType>())
        {
            config.axisScale.axis[axis].multiplier = 1;
            config.axisScale.axis[axis].offset = short.MinValue;
            config.axisScale.axis[axis].deadzone = short.MaxValue;
        }

        config.debounce.buttons = 5;
        config.debounce.strum = 20;
        config.debounce.combinedStrum = 0;
        config.rf.id = 0;
        config.rf.rfInEnabled = 0;
        while (offset < maxSize)
        {
            var data2 = ReadData((ushort) (readConfig + offsetId), RequestHidGetReport);
            Array.Copy(data2, 0, data, offset, data2.Length);
            offset += data2.Length;
            offsetId++;
            if (offset > sizeOfAll)
            {
                config.all = StructTools.RawDeserialize<FullArdwiinoConfiguration>(data, 0);
                version = config.all.main.version;
                maxSize = version switch
                {
                    > 17 => Marshal.SizeOf<ArdwiinoConfiguration>(),
                    > 15 => Marshal.SizeOf<Configuration16>(),
                    > 13 => Marshal.SizeOf<Configuration14>(),
                    > 12 => Marshal.SizeOf<Configuration13>(),
                    > 11 => Marshal.SizeOf<Configuration12>(),
                    > 10 => Marshal.SizeOf<Configuration11>(),
                    > 8 => Marshal.SizeOf<Configuration10>(),
                    8 => Marshal.SizeOf<Configuration8>(),
                    _ => sizeOfAll
                };
            }
        }

        // Patches to all
        if (version < 9)
        {
            // For versions below version 9, r_x is inverted from how we use it now
            config.all.pins.axis![(byte) ControllerAxisType.XboxRx].inverted =
                (byte) (config.all.pins.axis[(int) ControllerAxisType.XboxRx].inverted == 0 ? 1 : 0);
        }

        // Read in the rest of the data, in the format that it is in
        if (version is 16 or 17)
        {
            config = StructTools.RawDeserialize<ArdwiinoConfiguration>(data, 0);
        }
        else if (version > 13)
        {
            var configOld = StructTools.RawDeserialize<Configuration14>(data, 0);
            config.axisScale = configOld.axisScale;
            config.pinsSP = configOld.pinsSP;
            config.rf = configOld.rf;
            config.debounce = configOld.debounce;
        }
        else if (version > 12)
        {
            var configOld = StructTools.RawDeserialize<Configuration13>(data, 0);
            config.axisScale = configOld.axisScale;
            config.pinsSP = configOld.pinsSP;
            config.rf = configOld.rf;
            config.debounce.buttons = configOld.debounce.buttons;
            config.debounce.strum = configOld.debounce.strum;
            config.debounce.combinedStrum = 0;
        }
        else if (version > 11)
        {
            var configOld = StructTools.RawDeserialize<Configuration12>(data, 0);
            foreach (int axis in Enum.GetValues<ControllerAxisType>())
            {
                config.axisScale.axis[axis].multiplier = configOld.axisScale.axis[axis].multiplier;
                config.axisScale.axis[axis].offset = configOld.axisScale.axis[axis].offset;
                config.axisScale.axis[axis].deadzone = short.MaxValue;
            }

            config.pinsSP = configOld.pinsSP;
            config.rf = configOld.rf;
            config.debounce.buttons = 5;
            config.debounce.strum = 20;
            config.debounce.combinedStrum = 0;
        }
        else if (version > 10)
        {
            var configOld = StructTools.RawDeserialize<Configuration11>(data, 0);
            config.axisScale.axis[(int) ControllerAxisType.XboxRx].multiplier = configOld.whammy.multiplier;
            config.axisScale.axis[(int) ControllerAxisType.XboxRx].offset = (short) configOld.whammy.offset;
            config.pinsSP = configOld.pinsSP;
            config.rf = configOld.rf;
        }
        else if (version > 8)
        {
            var configOld = StructTools.RawDeserialize<Configuration10>(data, 0);
            config.pinsSP = configOld.pinsSP;
            config.rf = configOld.rf;
        }

        if (version < 18)
        {
            model.Deque = false;
        }
        else
        {
            model.Deque = config.deque != 0;
        }

        if (version < 17 && config.all.main.subType > (int) SubType.XinputArcadePad)
        {
            config.all.main.subType += SubType.XinputTurntable - SubType.XinputArcadePad;
            if (config.all.main.subType > (int) SubType.Ps3Gamepad) config.all.main.subType += 2;

            if (config.all.main.subType > (int) SubType.WiiRockBandDrums) config.all.main.subType += 1;
        }

        if (version < 18)
        {
            config.debounce.buttons *= 10;
            config.debounce.strum *= 10;
        }

        var controller = Board.FindMicrocontroller(Board);
        var bindings = new List<Output>();
        var colors = new Dictionary<int, Color>();
        var ledIndexes = new Dictionary<int, byte>();
        for (byte index = 0; index < config.all.leds!.Length; index++)
        {
            var led = config.all.leds[index];
            if (led.pin != 0)
            {
                colors[led.pin - 1] = Color.FromRgb(led.red, led.green, led.blue);
                ledIndexes[led.pin - 1] = (byte) (index + 1);
            }
        }

        var ledType = LedType.None;
        DeviceControllerType deviceType;
        var emulationType = EmulationType.Controller;
        if (config.all.main.fretLEDMode == 2) ledType = LedType.Apa102Bgr;

        if ((config.all.main.subType >= (int) SubType.KeyboardGamepad &&
             config.all.main.subType <= (int) SubType.KeyboardRockBandDrums) ||
            config.all.main.subType == (int) SubType.Mouse)
            emulationType = EmulationType.KeyboardMouse;

        if (config.all.main.subType >= (int) SubType.MidiGamepad)
            //TODO if we get around to midi, this
            emulationType = EmulationType.Controller;

        var xinputOnWindows = (SubType) config.all.main.subType <= SubType.XinputTurntable;
        switch ((SubType) config.all.main.subType)
        {
            case SubType.XinputGamepad:
            case SubType.XinputLiveGuitar:
            case SubType.XinputRockBandDrums:
            case SubType.XinputGuitarHeroDrums:
            case SubType.XinputRockBandGuitar:
            case SubType.XinputGuitarHeroGuitar:
            case SubType.XinputTurntable:
                xinputOnWindows = true;
                break;
        }

        deviceType = (SubType) config.all.main.subType switch
        {
            SubType.XinputTurntable => DeviceControllerType.Turntable,
            SubType.Ps3Turntable => DeviceControllerType.Turntable,
            SubType.XinputGamepad => DeviceControllerType.Gamepad,
            SubType.Ps3Gamepad => DeviceControllerType.Gamepad,
            SubType.SwitchGamepad => DeviceControllerType.Gamepad,
            SubType.MidiGamepad => DeviceControllerType.Gamepad,
            SubType.KeyboardGamepad => DeviceControllerType.Gamepad,
            SubType.XinputArcadePad => DeviceControllerType.Gamepad,
            SubType.XinputWheel => DeviceControllerType.Gamepad,
            SubType.XinputArcadeStick => DeviceControllerType.Gamepad,
            SubType.XinputFlightStick => DeviceControllerType.Gamepad,
            SubType.XinputDancePad => DeviceControllerType.DancePad,
            SubType.WiiLiveGuitar => DeviceControllerType.LiveGuitar,
            SubType.Ps3LiveGuitar => DeviceControllerType.LiveGuitar,
            SubType.MidiLiveGuitar => DeviceControllerType.LiveGuitar,
            SubType.XinputLiveGuitar => DeviceControllerType.LiveGuitar,
            SubType.KeyboardLiveGuitar => DeviceControllerType.LiveGuitar,
            SubType.Ps3RockBandDrums => DeviceControllerType.RockBandDrums,
            SubType.WiiRockBandDrums => DeviceControllerType.RockBandDrums,
            SubType.MidiRockBandDrums => DeviceControllerType.RockBandDrums,
            SubType.XinputRockBandDrums => DeviceControllerType.RockBandDrums,
            SubType.KeyboardRockBandDrums => DeviceControllerType.RockBandDrums,
            SubType.Ps3GuitarHeroDrums => DeviceControllerType.GuitarHeroDrums,
            SubType.MidiGuitarHeroDrums => DeviceControllerType.GuitarHeroDrums,
            SubType.XinputGuitarHeroDrums => DeviceControllerType.GuitarHeroDrums,
            SubType.KeyboardGuitarHeroDrums => DeviceControllerType.GuitarHeroDrums,
            SubType.Ps3RockBandGuitar => DeviceControllerType.RockBandGuitar,
            SubType.WiiRockBandGuitar => DeviceControllerType.RockBandGuitar,
            SubType.MidiRockBandGuitar => DeviceControllerType.RockBandGuitar,
            SubType.XinputRockBandGuitar => DeviceControllerType.RockBandGuitar,
            SubType.KeyboardRockBandGuitar => DeviceControllerType.RockBandGuitar,
            SubType.Ps3GuitarHeroGuitar => DeviceControllerType.GuitarHeroGuitar,
            SubType.MidiGuitarHeroGuitar => DeviceControllerType.GuitarHeroGuitar,
            SubType.XinputGuitarHeroGuitar => DeviceControllerType.GuitarHeroGuitar,
            SubType.KeyboardGuitarHeroGuitar => DeviceControllerType.GuitarHeroGuitar,
            _ => DeviceControllerType.Gamepad
        };

        model.LedType = ledType;
        model.SetDeviceTypeAndRhythmTypeWithoutUpdating(deviceType, emulationType);
        model.Debounce = config.debounce.buttons;
        model.StrumDebounce = config.debounce.strum;
        var sda = 18;
        var scl = 19;
        var mosi = 3;
        var miso = 4;
        var sck = 6;
        var att = 0;
        var ack = 0;
        switch (controller)
        {
            case Micro:
            case Pico:
                att = 10;
                ack = 7;
                break;
            case Uno:
            case Mega:
                att = 10;
                ack = 2;
                break;
        }

        switch (config.all.main.inputType)
        {
            case (int) InputControllerType.Wii:
            {
                var wii = new WiiCombinedOutput(model, false, sda, scl);
                model.Bindings.Add(wii);
                wii.SetOutputsOrDefaults(Array.Empty<Output>());
                if (config.all.main.mapNunchukAccelToRightJoy != 0)
                    foreach (var output in wii.Outputs.Items.Where(output => output is
                             {
                                 Input: WiiInput
                                 {
                                     Input: WiiInputType.NunchukRotationRoll or WiiInputType.NunchukRotationPitch
                                 }
                             }))
                        output.Enabled = false;

                bindings.Add(wii);
                break;
            }
            case (int) InputControllerType.Ps2:
                var ps2 = new Ps2CombinedOutput(model, false, miso, mosi, sck, att, ack);
                model.Bindings.Add(ps2);
                ps2.SetOutputsOrDefaults(Array.Empty<Output>());
                bindings.Add(ps2);
                break;
        }

        if (config.all.main.inputType == (int) InputControllerType.Direct)
        {
            if (deviceType.Is5FretGuitar())
            {
                if (config.neck.gh5Neck != 0 || config.neck.gh5NeckBar != 0)
                {
                    var output = new Gh5CombinedOutput(model, false, sda, scl);
                    model.Bindings.Add(output);
                    output.SetOutputsOrDefaults(Array.Empty<Output>());
                    bindings.Add(output);
                }

                if (config.neck.wtNeck != 0)
                {
                    var output = new GhwtCombinedOutput(model, false, 9);
                    model.Bindings.Add(output);
                    output.SetOutputsOrDefaults(Array.Empty<Output>());
                    bindings.Add(output);
                }
            }

            if (deviceType == DeviceControllerType.Turntable)
            {
                var output = new DjCombinedOutput(model, false, sda, scl);
                model.Bindings.Add(output);
                output.SetOutputsOrDefaults(Array.Empty<Output>());
                bindings.Add(output);
            }

            foreach (int axis in Enum.GetValues<ControllerAxisType>())
            {
                var pin = config.all.pins.axis![axis];
                if (pin.pin == NotUsed) continue;

                var genAxis = AxisToStandard[(ControllerAxisType) axis];
                var scale = config.axisScale.axis[axis];
                var isTrigger = axis is (int) ControllerAxisType.XboxLt or (int) ControllerAxisType.XboxRt ||
                                (deviceType.IsGuitar() &&
                                 ((ControllerAxisType) axis ==
                                     XboxWhammy || (ControllerAxisType) axis == XboxTilt));

                var on = Color.FromRgb(0, 0, 0);
                if (colors.ContainsKey(axis + XboxBtnCount)) on = colors[axis + XboxBtnCount];

                var ledIndex = Array.Empty<byte>();
                if (ledIndexes.ContainsKey(axis + XboxBtnCount)) ledIndex = new[] {ledIndexes[axis + XboxBtnCount]};

                var off = Color.FromRgb(0, 0, 0);
                if (deviceType.IsGuitar() &&
                    (ControllerAxisType) axis == XboxTilt &&
                    config.all.main.tiltType == 2)
                {
                    bindings.Add(new GuitarAxis(model,
                        new DigitalToAnalog(new DirectInput(pin.pin, false, false, DevicePinMode.PullUp, model), model),
                        on,
                        off, ledIndex, Array.Empty<byte>(), ushort.MinValue, ushort.MaxValue,
                        0, GuitarAxisType.Tilt, false));
                }
                else
                {
                    var axisMultiplier = scale.multiplier / 1024.0f * (pin.inverted > 0 ? -1 : 1);
                    var axisOffset = scale.offset;
                    var axisDeadzone = (isTrigger ? 32768 : 0) + scale.deadzone;
                    int min = axisOffset;
                    var max = (int) (axisOffset + ushort.MaxValue / axisMultiplier);
                    if (isTrigger)
                    {
                        min += short.MaxValue;
                        max += short.MaxValue;
                    }

                    if (deviceType.IsGuitar() &&
                        (ControllerAxisType) axis == XboxWhammy)
                    {
                        bindings.Add(new GuitarAxis(model,
                            new DirectInput(pin.pin, false, false, DevicePinMode.Analog, model), on,
                            off,
                            ledIndex, Array.Empty<byte>(), min, max, axisDeadzone, GuitarAxisType.Whammy, false));
                    }
                    else
                    {
                        bindings.Add(new ControllerAxis(model,
                            new DirectInput(pin.pin, false, false, DevicePinMode.Analog, model), on, off,
                            ledIndex, Array.Empty<byte>(), min, max, axisDeadzone, ushort.MaxValue, genAxis, false));
                    }
                }
            }

            foreach (int button in Enum.GetValues<ControllerButtons>())
            {
                var pin = config.all.pins.pins![button];
                if (pin == NotUsed) continue;

                var on = Color.FromRgb(0, 0, 0);
                if (colors.TryGetValue(button, out var color)) on = color;

                var ledIndex = Array.Empty<byte>();
                if (ledIndexes.TryGetValue(button, out var index)) ledIndex = new[] {index};

                var off = Color.FromRgb(0, 0, 0);
                var genButton = ButtonToStandard[(ControllerButtons) button];
                var pinMode = DevicePinMode.PullUp;
                if (config.all.main.fretLEDMode == 1 && deviceType.IsGuitar() &&
                    _frets.Contains(genButton))
                    pinMode = DevicePinMode.Floating;

                var debounce = config.debounce.buttons;
                switch (deviceType)
                {
                    case DeviceControllerType.GuitarHeroGuitar or DeviceControllerType.RockBandGuitar
                        or DeviceControllerType.LiveGuitar when
                        genButton is StandardButtonType.DpadUp or StandardButtonType.DpadDown:
                        debounce = config.debounce.strum;
                        break;
                    case DeviceControllerType.Turntable when genButton == StandardButtonType.LeftThumbClick:
                        genButton = StandardButtonType.Y;
                        break;
                }

                bindings.Add(new ControllerButton(model, new DirectInput(pin, false, false, pinMode, model), on, off,
                    ledIndex, Array.Empty<byte>(), debounce, genButton, false));
            }

            if (config.all.main.mapStartSelectToHome != 0)
            {
                var start = config.all.pins.pins![(int) ControllerButtons.XboxStart];
                var select = config.all.pins.pins![(int) ControllerButtons.XboxBack];
                if (start != NotUsed && select != NotUsed)
                {
                    bindings.Add(new ControllerButton(model,
                        new MacroInput(new DirectInput(start, false, false, DevicePinMode.PullUp, model),
                            new DirectInput(select, false, false, DevicePinMode.PullUp, model), model), Colors.Black,
                        Colors.Black,
                        Array.Empty<byte>(), Array.Empty<byte>(),
                        config.debounce.buttons, StandardButtonType.Guide, false));
                }
            }
        }
        else if (config.all.main.tiltType == 2)
        {
            if (deviceType.IsGuitar())
            {
                var pin = config.all.pins.axis![(int) XboxTilt];
                if (pin.pin != NotUsed)
                {
                    var on = Color.FromRgb(0, 0, 0);
                    if (colors.TryGetValue((int) (XboxTilt + XboxBtnCount), out var color))
                        on = color;

                    var off = Color.FromRgb(0, 0, 0);
                    var ledIndex = Array.Empty<byte>();
                    if (ledIndexes.TryGetValue((int) (XboxTilt + XboxBtnCount), out var index))
                        ledIndex = new[] {index};

                    bindings.Add(new GuitarAxis(model,
                        new DigitalToAnalog(new DirectInput(pin.pin, false, false, DevicePinMode.PullUp, model), model),
                        on,
                        off, ledIndex, Array.Empty<byte>(), ushort.MinValue, ushort.MaxValue,
                        0, GuitarAxisType.Tilt, false));
                }
            }
        }

        if (config.all.main.mapLeftJoystickToDPad > 0)
        {
            ControllerAxis? lx = null;
            ControllerAxis? ly = null;
            var threshold = config.all.axis.joyThreshold << 8;
            foreach (var binding in bindings)
                if (binding is ControllerAxis axis)
                    switch (axis.Type)
                    {
                        case StandardAxisType.LeftStickX:
                            lx = axis;
                            break;
                        case StandardAxisType.LeftStickY:
                            ly = axis;
                            break;
                    }

            if (lx != null)
            {
                var ledOn = lx.LedOn;
                var ledOff = lx.LedOff;
                bindings.Add(new ControllerButton(model,
                    new AnalogToDigital(lx.Input, AnalogToDigitalType.JoyLow, threshold, model), ledOn, ledOff,
                    Array.Empty<byte>(), Array.Empty<byte>(), config.debounce.buttons, StandardButtonType.DpadLeft,
                    false));
                bindings.Add(new ControllerButton(model,
                    new AnalogToDigital(lx.Input, AnalogToDigitalType.JoyHigh, threshold, model), ledOn, ledOff,
                    Array.Empty<byte>(), Array.Empty<byte>(), config.debounce.buttons, StandardButtonType.DpadRight,
                    false));
            }

            if (ly != null)
            {
                var ledOn = ly.LedOn;
                var ledOff = ly.LedOff;
                bindings.Add(new ControllerButton(model,
                    new AnalogToDigital(ly.Input, AnalogToDigitalType.JoyLow, threshold, model), ledOn, ledOff,
                    Array.Empty<byte>(), Array.Empty<byte>(), config.debounce.buttons, StandardButtonType.DpadDown,
                    false));
                bindings.Add(new ControllerButton(model,
                    new AnalogToDigital(ly.Input, AnalogToDigitalType.JoyHigh, threshold, model), ledOn, ledOff,
                    Array.Empty<byte>(), Array.Empty<byte>(), config.debounce.buttons, StandardButtonType.DpadUp,
                    false));
            }
        }


        if (model.IsApa102)
        {
            model.Apa102Mosi = 3;
            model.Apa102Sck = 6;
            model.LedCount = ledIndexes.Values.Max();
        }

        model.CombinedStrumDebounce = config.debounce.combinedStrum != 0;
        model.XInputOnWindows = xinputOnWindows;
        model.MouseMovementType = MouseMovementType.Relative;
        model.Bindings.Clear();
        model.Bindings.AddRange(bindings);
        model.UpdateBindings();
        model.UpdateErrors();
        model.Main.Write(model);
        return true;
    }

    public override Microcontroller GetMicrocontroller(ConfigViewModel model)
    {
        return Board.FindMicrocontroller(Board);
    }

    private enum SubType
    {
        XinputGamepad = 1,
        XinputWheel,
        XinputArcadeStick,
        XinputFlightStick,
        XinputDancePad,
        XinputLiveGuitar = 9,
        XinputRockBandDrums = 12,
        XinputGuitarHeroDrums,
        XinputRockBandGuitar,
        XinputGuitarHeroGuitar,
        XinputArcadePad = 19,
        XinputTurntable = 23,
        KeyboardGamepad,
        KeyboardGuitarHeroGuitar,
        KeyboardRockBandGuitar,
        KeyboardLiveGuitar,
        KeyboardGuitarHeroDrums,
        KeyboardRockBandDrums,
        SwitchGamepad,
        Ps3GuitarHeroGuitar,
        Ps3GuitarHeroDrums,
        Ps3RockBandGuitar,
        Ps3RockBandDrums,
        Ps3Gamepad,
        Ps3Turntable,
        Ps3LiveGuitar,
        WiiRockBandGuitar,
        WiiRockBandDrums,
        WiiLiveGuitar,
        Mouse,
        MidiGamepad,
        MidiGuitarHeroGuitar,
        MidiRockBandGuitar,
        MidiLiveGuitar,
        MidiGuitarHeroDrums,
        MidiRockBandDrums
    }

    private enum ControllerButtons
    {
        XboxDpadUp,
        XboxDpadDown,
        XboxDpadLeft,
        XboxDpadRight,
        XboxStart,
        XboxBack,
        XboxLeftStick,
        XboxRightStick,

        XboxLb,
        XboxRb,
        XboxHome,
        XboxUnused,
        XboxA,
        XboxB,
        XboxX,
        XboxY
    }

    private enum ControllerAxisType
    {
        XboxLt,
        XboxRt,
        XboxLx,
        XboxLy,
        XboxRx,
        XboxRy
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct CpuInfoOld
    {
        public readonly uint cpu_freq;
        public readonly byte multi;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
        public readonly string board;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct CpuInfo
    {
        public readonly uint cpu_freq;
        public readonly byte multi;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
        public readonly string board;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Led
    {
        public readonly byte pin;
        public readonly byte red;
        public readonly byte green;
        public readonly byte blue;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Config
    {
        public readonly byte inputType;
        public readonly byte subType;
        public readonly byte tiltType;
        public readonly byte pollRate;
        public readonly byte fretLEDMode;
        public readonly byte mapLeftJoystickToDPad;
        public readonly byte mapStartSelectToHome;
        public readonly byte mapNunchukAccelToRightJoy;
        public readonly uint signature;
        public readonly uint version;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AnalogPin
    {
        public readonly byte pin;
        public byte inverted;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Pins
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxBtnCount)]
        public readonly byte[] pins;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxAxisCount)]
        public readonly AnalogPin[] axis;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AnalogKey
    {
        public readonly byte neg;
        public readonly byte pos;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Keys
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxBtnCount + XboxTriggerCount)]
        public readonly byte[] pins;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxAxisCount - XboxTriggerCount)]
        public readonly AnalogKey[] axis;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct MainConfig
    {
        public readonly byte inputType;
        public byte subType;
        public readonly byte tiltType;
        public readonly byte pollRate;
        public readonly byte fretLEDMode;
        public readonly byte mapLeftJoystickToDPad;
        public readonly byte mapStartSelectToHome;
        public readonly byte mapNunchukAccelToRightJoy;
        public readonly uint signature;
        public readonly uint version;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AxisConfig
    {
        public readonly byte triggerThreshold;
        public readonly byte joyThreshold;
        public readonly byte drumThreshold;

        public readonly byte mpu6050Orientation;
        public readonly short tiltSensitivity;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct MidiConfig
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxAxisCount + XboxBtnCount)]
        public readonly byte[] type;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxAxisCount + XboxBtnCount)]
        public readonly byte[] note;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxAxisCount + XboxBtnCount)]
        public readonly byte[] channel;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct RfConfig
    {
        public byte rfInEnabled;
        public uint id;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AxisScale
    {
        public short multiplier;
        public short offset;
        public short deadzone;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AxisScale12
    {
        public readonly short multiplier;
        public readonly short offset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Version11AxisWhammyConfig
    {
        public readonly byte multiplier;
        public readonly ushort offset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AxisScaleConfig
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxAxisCount)]
        public AxisScale[] axis;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AxisScaleConfig12
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxAxisCount)]
        public readonly AxisScale12[] axis;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct DebounceConfig13
    {
        public readonly byte buttons;
        public readonly byte strum;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct DebounceConfig
    {
        public byte buttons;
        public byte strum;
        public byte combinedStrum;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct ArdwiinoConfiguration
    {
        public FullArdwiinoConfiguration all;
        public RfConfig rf;
        public byte pinsSP;
        public AxisScaleConfig axisScale;
        public DebounceConfig debounce;
        public NeckConfig neck;
        public byte deque;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Configuration16
    {
        public FullArdwiinoConfiguration all;
        public RfConfig rf;
        public byte pinsSP;
        public AxisScaleConfig axisScale;
        public DebounceConfig debounce;
        public NeckConfig neck;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Configuration14
    {
        public readonly FullArdwiinoConfiguration all;
        public readonly RfConfig rf;
        public readonly byte pinsSP;
        public readonly AxisScaleConfig axisScale;
        public readonly DebounceConfig debounce;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Configuration13
    {
        public readonly FullArdwiinoConfiguration all;
        public readonly RfConfig rf;
        public readonly byte pinsSP;
        public readonly AxisScaleConfig axisScale;
        public readonly DebounceConfig13 debounce;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Configuration12
    {
        public readonly FullArdwiinoConfiguration all;
        public readonly RfConfig rf;
        public readonly byte pinsSP;
        public readonly AxisScaleConfig12 axisScale;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Configuration11
    {
        public readonly FullArdwiinoConfiguration all;
        public readonly RfConfig rf;
        public readonly byte pinsSP;

        public readonly Version11AxisWhammyConfig whammy;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Configuration10
    {
        public readonly FullArdwiinoConfiguration all;
        public readonly RfConfig rf;
        public readonly byte pinsSP;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Configuration8
    {
        public readonly FullArdwiinoConfiguration all;
        public readonly RfConfig rf;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct NeckConfig
    {
        public readonly byte wtNeck;
        public readonly byte gh5Neck;
        public readonly byte gh5NeckBar;
        public readonly byte wiiNeck;
        public readonly byte ps2Neck;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct FullArdwiinoConfiguration
    {
        public MainConfig main;
        public readonly Pins pins;
        public readonly AxisConfig axis;
        public readonly Keys keys;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxAxisCount + XboxBtnCount)]
        public readonly Led[] leds;

        public readonly MidiConfig midi;
    }
}