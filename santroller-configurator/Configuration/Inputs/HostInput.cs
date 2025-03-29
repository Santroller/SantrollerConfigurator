using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Input;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public abstract partial class HostInput : Input
{
    public HostInput(UsbHostInputType input, ConfigViewModel model, bool combined = false) : base(model)
    {
        Key = Key.A;
        MouseButtonType = MouseButtonType.Left;
        MouseAxisType = MouseAxisType.X;
        Combined = combined;
        Input = input;
        IsAnalog = input is (>= UsbHostInputType.LeftTrigger and < UsbHostInputType.GenericButton1
            or >= UsbHostInputType.GenericAxisX) and not UsbHostInputType.KeyboardInput
            and not UsbHostInputType.MouseButton;
    }

    public HostInput(Key key, ConfigViewModel model, bool combined = false) : base(model)
    {
        Key = key;
        MouseButtonType = MouseButtonType.Left;
        MouseAxisType = MouseAxisType.X;
        Combined = combined;
        Input = UsbHostInputType.KeyboardInput;
        ProKeyType = ProKeyType.Key1;
        IsAnalog = false;
    }

    public HostInput(MouseButtonType mouseButtonType, ConfigViewModel model, bool combined = false) : base(model)
    {
        Key = Key.A;
        MouseButtonType = mouseButtonType;
        MouseAxisType = MouseAxisType.X;
        Combined = combined;
        Input = UsbHostInputType.MouseButton;
        ProKeyType = ProKeyType.Key1;
        IsAnalog = false;
    }

    public HostInput(MouseAxisType mouseAxisType, ConfigViewModel model, bool combined = false) : base(model)
    {
        Key = Key.A;
        MouseButtonType = MouseButtonType.Left;
        MouseAxisType = mouseAxisType;
        Input = UsbHostInputType.MouseAxis;
        ProKeyType = ProKeyType.Key1;
        Combined = combined;
        IsAnalog = true;
    }
    public HostInput(ProKeyType proKeyType, ConfigViewModel model, bool combined = false) : base(model)
    {
        Key = Key.A;
        MouseButtonType = MouseButtonType.Left;
        MouseAxisType = MouseAxisType.X;
        ProKeyType = proKeyType;
        Input = UsbHostInputType.ProKey;
        Combined = combined;
        IsAnalog = proKeyType is ProKeyType.PedalAnalog or ProKeyType.TouchPad or <= ProKeyType.Key25;
    }

    public bool Combined { get; }
    public bool ShouldShowPins => !Combined && !Model.Branded;
    public override bool Peripheral => false;

    public UsbHostInputType Input { get; }

    public Key Key { get; }

    public MouseAxisType MouseAxisType { get; }

    public MouseButtonType MouseButtonType { get; }

    public ProKeyType ProKeyType { get; }

    public override bool IsUint => Input is not (UsbHostInputType.LeftStickX or UsbHostInputType.LeftStickY
        or UsbHostInputType.RightStickX or UsbHostInputType.RightStickY or UsbHostInputType.Crossfader
        or UsbHostInputType.LeftTableVelocity or UsbHostInputType.RightTableVelocity
        or UsbHostInputType.EffectsKnob or UsbHostInputType.Tilt or UsbHostInputType.MouseAxis);

    public override string Title =>
        Input switch
        {
            UsbHostInputType.KeyboardInput => EnumToStringConverter.Convert(Key),
            UsbHostInputType.MouseAxis => EnumToStringConverter.Convert(MouseAxisType),
            UsbHostInputType.MouseButton => EnumToStringConverter.Convert(MouseButtonType),
            UsbHostInputType.ProKey => EnumToStringConverter.Convert(ProKeyType),
            _ => EnumToStringConverter.Convert(Input)
        };

    [Reactive] private string _usbHostInfo = "";
    [Reactive] private int _connectedDevices;

    public abstract string Field { get; }
    
    public override string Generate(BinaryWriter? writer)
    {
        var ret = (Input switch
        {
            UsbHostInputType.KeyboardInput => Output.GetReportField(Key, $"{Field}.keyboard", false),
            UsbHostInputType.MouseAxis => Output.GetReportField(MouseAxisType, $"{Field}.mouse", false),
            UsbHostInputType.MouseButton => Output.GetReportField(MouseButtonType, $"{Field}.mouse", false),
            UsbHostInputType.ProKey when ProKeyType is ProKeyType.TouchPad or ProKeyType.PedalAnalog or ProKeyType.PedalDigital or ProKeyType.Overdrive => Output.GetReportField(ProKeyType, Field, false),
            UsbHostInputType.ProKey => Output.GetReportField($"proKeyVelocities[{(int) ProKeyType}]", Field, false),
            _ => Output.GetReportField(Input, Field, false)
        });

        if (ByteBased.Contains(Input) && IsAnalog)
        {
            ret = "(" + ret + " << 8)";
        }

        return ret;
    }

    public void Update(ReadOnlySpan<byte> usbHostInputsRaw)
    {
        Console.WriteLine(Marshal.SizeOf<UsbHostInputs>());
        if (usbHostInputsRaw.Length < Marshal.SizeOf<UsbHostInputs>()) return;
        var inputs = StructTools.RawDeserialize<UsbHostInputs>(usbHostInputsRaw, 0);
        RawValue = inputs.RawValue(Input, Key, MouseAxisType, MouseButtonType, ProKeyType);
    }

    public override void Update(Dictionary<int, int> analogRaw, Dictionary<int, bool> digitalRaw,
        ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw,
        ReadOnlySpan<byte> mpr121Raw, ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw,
        bool peripheralConnected)
    {
        
        var buffer = "";
        // When combined, the combined output renders this, so we don't need to calculate it
        if (!Combined && !usbHostRaw.IsEmpty)
        {
            var devices = 0;
            bool seen360W = false;
            for (var i = 0; i < usbHostRaw.Length; i += 2)
            {
                var consoleType = (ConsoleType) usbHostRaw[i];
                string subType;
            
                if (consoleType == ConsoleType.Xbox360W && !seen360W)
                {
                    seen360W = true;
                    devices += 1;
                    buffer += $"{Resources.Xbox360W}\n";
                }
                switch (consoleType)
                {
                    case ConsoleType.Xbox360W when usbHostRaw[i + 1] == 0xFF:
                        continue;
                    case ConsoleType.Xbox360 or ConsoleType.Xbox360W:
                    {
                        var xInputSubType = (XInputSubType) usbHostRaw[i + 1];
                        subType = EnumToStringConverter.Convert(xInputSubType);
                        break;
                    }
                    case ConsoleType.StreamDeck:
                    {
                        var streamDeckType = (StreamDeckType) usbHostRaw[i + 1];
                        subType = EnumToStringConverter.Convert(streamDeckType);
                        break;
                    }
                    default:
                    {
                        if (usbHostRaw[i + 1] == 13)
                        {
                            subType = Resources.DeviceControllerTypeGuitarHeroWtGuitar;
                        }
                        else
                        {
                            var deviceType = (DeviceControllerType) usbHostRaw[i + 1];
                            subType = EnumToStringConverter.Convert(deviceType);
                        }

                        break;
                    }
                }

                buffer += consoleType switch
                {
                    ConsoleType.Generic => string.Format(Resources.GenericGamepadLabel, subType),
                    ConsoleType.Keyboard or ConsoleType.Mouse or ConsoleType.Xbox360BigButton =>
                        $"{EnumToStringConverter.Convert(consoleType)}\n",
                    _ => $"{EnumToStringConverter.Convert(consoleType)} {subType}\n"
                };
                devices += 1;
            }

            ConnectedDevices = devices;
            UsbHostInfo = buffer.Trim();
        }
        Update(usbHostInputsRaw);
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings, ConfigField mode)
    {
        return string.Join("\n", bindings.Select(binding => binding.Item2));
    }

    private static readonly HashSet<UsbHostInputType> ByteBased =
    [
        UsbHostInputType.PressureDpadUp,
        UsbHostInputType.PressureDpadRight,
        UsbHostInputType.PressureDpadLeft,
        UsbHostInputType.PressureDpadDown,
        UsbHostInputType.PressureL1,
        UsbHostInputType.PressureR1,
        UsbHostInputType.PressureTriangle,
        UsbHostInputType.PressureCircle,
        UsbHostInputType.PressureCross,
        UsbHostInputType.PressureSquare,
        UsbHostInputType.Whammy,
        UsbHostInputType.Pickup,
        UsbHostInputType.MouseAxis,
        UsbHostInputType.ProKey
    ];


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct UsbHostInputs
    {
        private readonly uint buttons;

        private bool ButtonPressed(UsbHostInputTypeReal inputType)
        {
            switch (inputType)
            {
                case >= UsbHostInputTypeReal.LeftTrigger
                    and (< UsbHostInputTypeReal.GenericButton1 or > UsbHostInputTypeReal.GenericButton16):
                    return false;
                default:
                {
                    var val = (uint) inputType;
                    switch (val)
                    {
                        case >= (uint) UsbHostInputTypeReal.GenericButton1:
                            val -= (uint) UsbHostInputTypeReal.GenericButton1;
                            return (genericButtons & (1 << (int) val)) != 0;
                        default:
                            return (buttons & (1 << (int) val)) != 0;
                    }
                }
            }
        }

        private readonly ushort leftTrigger;
        private readonly ushort rightTrigger;
        private readonly short leftStickX;
        private readonly short leftStickY;
        private readonly short rightStickX;
        private readonly short rightStickY;
        private readonly byte pressureDpadUp;
        private readonly byte pressureDpadRight;
        private readonly byte pressureDpadLeft;
        private readonly byte pressureDpadDown;
        private readonly byte pressureL1;
        private readonly byte pressureR1;
        private readonly byte pressureTriangle;
        private readonly byte pressureCircle;
        private readonly byte pressureCross;
        private readonly byte pressureSquare;
        private readonly byte whammy;
        private readonly byte pickup;
        private readonly short tilt;
        private readonly byte slider;
        private readonly short leftTableVelocity;
        private readonly short rightTableVelocity;
        private readonly short effectsKnob;
        private readonly short crossfader;
        private readonly ushort accelX;
        private readonly ushort accelZ;
        private readonly ushort accelY;
        private readonly ushort gyro;
        private readonly ushort genericButtons;
        private readonly ushort genericX;
        private readonly ushort genericY;
        private readonly ushort genericZ;
        private readonly ushort genericRX;
        private readonly ushort genericRY;
        private readonly ushort genericRZ;
        private readonly ushort genericSlider;
        private readonly byte reserved1;
        private readonly UInt128 keys;
        private readonly byte mouseButtons;
        private readonly sbyte mouseX;
        private readonly sbyte mouseY;
        private readonly sbyte scrollY;
        private readonly sbyte scrollX;
        private readonly byte lowEFret;
        private readonly byte aFret;
        private readonly byte dFret;
        private readonly byte gFret;
        private readonly byte bFret;
        private readonly byte highEFret;
        private readonly byte lowEFretVelocity;
        private readonly byte aFretVelocity;
        private readonly byte dFretVelocity;
        private readonly byte gFretVelocity;
        private readonly byte bFretVelocity;
        private readonly byte highEFretVelocity;

        public unsafe int RawValue(UsbHostInputType inputType, Key key, MouseAxisType mouseAxisType,
            MouseButtonType mouseButtonType, ProKeyType proKeyType)
        {
            UsbHostInputTypeReal real = Enum.Parse<UsbHostInputTypeReal>(inputType.ToString());
            var val = real switch
            {
                UsbHostInputTypeReal.LeftTrigger => leftTrigger,
                UsbHostInputTypeReal.RightTrigger => rightTrigger,
                UsbHostInputTypeReal.LeftStickX => leftStickX,
                UsbHostInputTypeReal.LeftStickY => leftStickY,
                UsbHostInputTypeReal.RightStickX => rightStickX,
                UsbHostInputTypeReal.RightStickY => rightStickY,
                UsbHostInputTypeReal.PressureDpadUp => pressureDpadUp,
                UsbHostInputTypeReal.PressureDpadRight => pressureDpadRight,
                UsbHostInputTypeReal.PressureDpadLeft => pressureDpadLeft,
                UsbHostInputTypeReal.PressureDpadDown => pressureDpadDown,
                UsbHostInputTypeReal.PressureL1 => pressureL1,
                UsbHostInputTypeReal.PressureR1 => pressureR1,
                UsbHostInputTypeReal.PressureTriangle => pressureTriangle,
                UsbHostInputTypeReal.PressureCircle => pressureCircle,
                UsbHostInputTypeReal.PressureCross => pressureCross,
                UsbHostInputTypeReal.PressureSquare => pressureSquare,
                UsbHostInputTypeReal.Whammy => whammy,
                UsbHostInputTypeReal.Tilt => tilt,
                UsbHostInputTypeReal.Pickup => pickup,
                UsbHostInputTypeReal.Slider => slider,
                UsbHostInputTypeReal.LeftTableVelocity => leftTableVelocity,
                UsbHostInputTypeReal.RightTableVelocity => rightTableVelocity,
                UsbHostInputTypeReal.EffectsKnob => effectsKnob,
                UsbHostInputTypeReal.Crossfader => crossfader,
                UsbHostInputTypeReal.AccelX => accelX,
                UsbHostInputTypeReal.AccelZ => accelZ,
                UsbHostInputTypeReal.AccelY => accelY,
                UsbHostInputTypeReal.Gyro => gyro,
                UsbHostInputTypeReal.GenericAxisX => genericX,
                UsbHostInputTypeReal.GenericAxisY => genericY,
                UsbHostInputTypeReal.GenericAxisZ => genericZ,
                UsbHostInputTypeReal.GenericAxisRx => genericRX,
                UsbHostInputTypeReal.GenericAxisRy => genericRY,
                UsbHostInputTypeReal.GenericAxisRz => genericRZ,
                UsbHostInputTypeReal.GenericAxisSlider => genericSlider,
                UsbHostInputTypeReal.KeyboardInput when key is Key.LeftAlt or Key.LeftCtrl or Key.LeftShift or Key.RightAlt
                    or Key.RightCtrl
                    or Key.RightShift => (keys &
                                          ((UInt128) 1 <<
                                           KeyboardButton.Keys.IndexOf(key))) != 0
                    ? 1
                    : 0,
                UsbHostInputTypeReal.KeyboardInput => (keys &
                                                       ((UInt128) 1 <<
                                                        (KeyboardButton.KeyCodes.IndexOf(Output.GetReportField(key)) +
                                                         8))) != 0
                    ? 1
                    : 0,
                UsbHostInputTypeReal.MouseAxis => mouseAxisType switch
                {
                    MouseAxisType.X => mouseX,
                    MouseAxisType.Y => mouseY,
                    MouseAxisType.ScrollY => scrollY,
                    MouseAxisType.ScrollX => scrollX,
                    _ => 0
                },
                UsbHostInputTypeReal.MouseButton => mouseButtonType switch
                {
                    MouseButtonType.Left => (mouseButtons & 1 << 1) != 0 ? 1 : 0,
                    MouseButtonType.Right => (mouseButtons & 1 << 2) != 0 ? 1 : 0,
                    MouseButtonType.Middle => (mouseButtons & 1 << 3) != 0 ? 1 : 0,
                    _ => 0
                },
                _ => ButtonPressed(real) ? 1 : 0
            };
            if (ByteBased.Contains(inputType))
            {
                val <<= 8;
            }

            return val;
        }
    }
}