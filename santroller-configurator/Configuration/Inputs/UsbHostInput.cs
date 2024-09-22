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

public partial class UsbHostInput : Input
{
    public UsbHostInput(UsbHostInputType input, ConfigViewModel model, bool combined = false) : base(model)
    {
        Key = Key.A;
        MouseButtonType = MouseButtonType.Left;
        MouseAxisType = MouseAxisType.X;
        Combined = combined;
        Input = input;
        _usbHostDm = model.WhenAnyValue(x => x.UsbHostDm).ToProperty(this, x => x.UsbHostDm);
        _usbHostDp = model.WhenAnyValue(x => x.UsbHostDp).ToProperty(this, x => x.UsbHostDp);
        IsAnalog = input is (>= UsbHostInputType.LeftTrigger and < UsbHostInputType.GenericButton1
            or >= UsbHostInputType.GenericAxisX) and not UsbHostInputType.KeyboardInput
            and not UsbHostInputType.MouseButton;
    }

    public UsbHostInput(Key key, ConfigViewModel model, bool combined = false) : base(model)
    {
        Key = key;
        MouseButtonType = MouseButtonType.Left;
        MouseAxisType = MouseAxisType.X;
        Combined = combined;
        Input = UsbHostInputType.KeyboardInput;
        ProKeyType = ProKeyType.Key1;
        _usbHostDm = model.WhenAnyValue(x => x.UsbHostDm).ToProperty(this, x => x.UsbHostDm);
        _usbHostDp = model.WhenAnyValue(x => x.UsbHostDp).ToProperty(this, x => x.UsbHostDp);
        IsAnalog = false;
    }

    public UsbHostInput(MouseButtonType mouseButtonType, ConfigViewModel model, bool combined = false) : base(model)
    {
        Key = Key.A;
        MouseButtonType = mouseButtonType;
        MouseAxisType = MouseAxisType.X;
        Combined = combined;
        Input = UsbHostInputType.MouseButton;
        ProKeyType = ProKeyType.Key1;
        _usbHostDm = model.WhenAnyValue(x => x.UsbHostDm).ToProperty(this, x => x.UsbHostDm);
        _usbHostDp = model.WhenAnyValue(x => x.UsbHostDp).ToProperty(this, x => x.UsbHostDp);
        IsAnalog = false;
    }

    public UsbHostInput(MouseAxisType mouseAxisType, ConfigViewModel model, bool combined = false) : base(model)
    {
        Key = Key.A;
        MouseButtonType = MouseButtonType.Left;
        MouseAxisType = mouseAxisType;
        Input = UsbHostInputType.MouseAxis;
        ProKeyType = ProKeyType.Key1;
        Combined = combined;
        _usbHostDm = model.WhenAnyValue(x => x.UsbHostDm).ToProperty(this, x => x.UsbHostDm);
        _usbHostDp = model.WhenAnyValue(x => x.UsbHostDp).ToProperty(this, x => x.UsbHostDp);
        IsAnalog = true;
    }
    public UsbHostInput(ProKeyType proKeyType, ConfigViewModel model, bool combined = false) : base(model)
    {
        Key = Key.A;
        MouseButtonType = MouseButtonType.Left;
        MouseAxisType = MouseAxisType.X;
        ProKeyType = proKeyType;
        Input = UsbHostInputType.ProKey;
        Combined = combined;
        _usbHostDm = model.WhenAnyValue(x => x.UsbHostDm).ToProperty(this, x => x.UsbHostDm);
        _usbHostDp = model.WhenAnyValue(x => x.UsbHostDp).ToProperty(this, x => x.UsbHostDp);
        IsAnalog = proKeyType is ProKeyType.Pedal or ProKeyType.TouchPad;
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

    public override IList<DevicePin> Pins => Array.Empty<DevicePin>();
    public override IList<PinConfig> PinConfigs => Model.UsbHostPinConfigs();
    public override InputType? InputType => Types.InputType.UsbHostInput;

    public override string Title =>
        Input switch
        {
            UsbHostInputType.KeyboardInput => EnumToStringConverter.Convert(Key),
            UsbHostInputType.MouseAxis => EnumToStringConverter.Convert(MouseAxisType),
            UsbHostInputType.MouseButton => EnumToStringConverter.Convert(MouseButtonType),
            UsbHostInputType.ProKey => EnumToStringConverter.Convert(ProKeyType),
            _ => EnumToStringConverter.Convert(Input)
        };

    // Since DM and DP need to be next to eachother, you cannot use pins at the far ends
    public List<int> AvailablePinsDm => Model.AvailablePinsDigital.Skip(1).ToList();
    public List<int> AvailablePinsDp => Model.AvailablePinsDigital.Where(s => AvailablePinsDm.Contains(s + 1)).ToList();
    private readonly ObservableAsPropertyHelper<int> _usbHostDm;
    private readonly ObservableAsPropertyHelper<int> _usbHostDp;

    [Reactive] private string _usbHostInfo = "";
    [Reactive] private int _connectedDevices;

    public int UsbHostDm
    {
        get => _usbHostDm.Value;
        set => Model.UsbHostDm = value;
    }

    public int UsbHostDp
    {
        get => _usbHostDp.Value;
        set => Model.UsbHostDp = value;
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[] {"INPUT_USB_HOST"};
    }

    public override string Generate(BinaryWriter? writer)
    {
        var ret = (Input switch
        {
            UsbHostInputType.KeyboardInput => Output.GetReportField(Key, "usb_host_data.keyboard"),
            UsbHostInputType.MouseAxis => Output.GetReportField(MouseAxisType, "usb_host_data.mouse"),
            UsbHostInputType.MouseButton => Output.GetReportField(MouseButtonType, "usb_host_data.mouse"),
            _ => Output.GetReportField(Input, "usb_host_data", false)
        });

        if (ByteBased.Contains(Input))
        {
            ret = "(" + ret + " << 8)";
        }

        return ret;
    }

    public override SerializedInput Serialise()
    {
        return new SerializedUsbHostInput(Input, Key, MouseButtonType, MouseAxisType, ProKeyType, Combined);
    }

    public override void Update(Dictionary<int, int> analogRaw, Dictionary<int, bool> digitalRaw,
        ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw,
        ReadOnlySpan<byte> mpr121Raw, ReadOnlySpan<byte> midiRaw)
    {
        var buffer = "";
        // When combined, the combined output renders this, so we don't need to calculate it
        if (!Combined && !usbHostRaw.IsEmpty)
        {
            for (var i = 0; i < usbHostRaw.Length; i += 2)
            {
                var consoleType = (ConsoleType) usbHostRaw[i];
                string subType;
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
            }

            ConnectedDevices = usbHostRaw.Length / 2;
            UsbHostInfo = buffer.Trim();
        }
        if (usbHostInputsRaw.Length < Marshal.SizeOf<UsbHostInputs>()) return;
        var inputs = StructTools.RawDeserialize<UsbHostInputs>(usbHostInputsRaw, 0);
        RawValue = inputs.RawValue(Input, Key, MouseAxisType, MouseButtonType, ProKeyType);
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
        UsbHostInputType.RedVelocity,
        UsbHostInputType.YellowVelocity,
        UsbHostInputType.BlueVelocity,
        UsbHostInputType.GreenVelocity,
        UsbHostInputType.OrangeVelocity,
        UsbHostInputType.BlueCymbalVelocity,
        UsbHostInputType.YellowCymbalVelocity,
        UsbHostInputType.GreenCymbalVelocity,
        UsbHostInputType.KickVelocity,
        UsbHostInputType.Whammy,
        UsbHostInputType.Pickup,
        UsbHostInputType.MouseAxis
    ];


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct UsbHostInputs
    {
        private readonly uint buttons;
        private readonly byte buttons2;

        private bool ButtonPressed(UsbHostInputType inputType)
        {
            if (inputType is >= UsbHostInputType.LeftTrigger
                and (< UsbHostInputType.GenericButton1 or > UsbHostInputType.GenericButton16)) return false;
            var val = (uint) inputType;
            switch (val)
            {
                case >= (uint) UsbHostInputType.GenericButton1:
                    val -= (uint) UsbHostInputType.GenericButton1;
                    return (genericButtons & (1 << (int) val)) != 0;
                case >= 32:
                    val -= 32;
                    return (buttons2 & (1 << (int) val)) != 0;
                default:
                    return (buttons & (1 << (int) val)) != 0;
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
        private readonly byte redVelocity;
        private readonly byte yellowVelocity;
        private readonly byte blueVelocity;
        private readonly byte greenVelocity;
        private readonly byte orangeVelocity;
        private readonly byte blueCymbalVelocity;
        private readonly byte yellowCymbalVelocity;
        private readonly byte greenCymbalVelocity;
        private readonly byte kickVelocity;
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
        private readonly UInt32 proKeys;
        private readonly byte pedal;
        private readonly byte touchPad;

        public int RawValue(UsbHostInputType inputType, Key key, MouseAxisType mouseAxisType,
            MouseButtonType mouseButtonType, ProKeyType proKeyType)
        {
            var val = inputType switch
            {
                UsbHostInputType.LeftTrigger => leftTrigger,
                UsbHostInputType.RightTrigger => rightTrigger,
                UsbHostInputType.LeftStickX => leftStickX,
                UsbHostInputType.LeftStickY => leftStickY,
                UsbHostInputType.RightStickX => rightStickX,
                UsbHostInputType.RightStickY => rightStickY,
                UsbHostInputType.PressureDpadUp => pressureDpadUp,
                UsbHostInputType.PressureDpadRight => pressureDpadRight,
                UsbHostInputType.PressureDpadLeft => pressureDpadLeft,
                UsbHostInputType.PressureDpadDown => pressureDpadDown,
                UsbHostInputType.PressureL1 => pressureL1,
                UsbHostInputType.PressureR1 => pressureR1,
                UsbHostInputType.PressureTriangle => pressureTriangle,
                UsbHostInputType.PressureCircle => pressureCircle,
                UsbHostInputType.PressureCross => pressureCross,
                UsbHostInputType.PressureSquare => pressureSquare,
                UsbHostInputType.RedVelocity => redVelocity,
                UsbHostInputType.YellowVelocity => yellowVelocity,
                UsbHostInputType.BlueVelocity => blueVelocity,
                UsbHostInputType.GreenVelocity => greenVelocity,
                UsbHostInputType.OrangeVelocity => orangeVelocity,
                UsbHostInputType.BlueCymbalVelocity => blueCymbalVelocity,
                UsbHostInputType.YellowCymbalVelocity => yellowCymbalVelocity,
                UsbHostInputType.GreenCymbalVelocity => greenCymbalVelocity,
                UsbHostInputType.KickVelocity => kickVelocity,
                UsbHostInputType.Whammy => whammy,
                UsbHostInputType.Tilt => tilt,
                UsbHostInputType.Pickup => pickup,
                UsbHostInputType.Slider => slider,
                UsbHostInputType.LeftTableVelocity => leftTableVelocity,
                UsbHostInputType.RightTableVelocity => rightTableVelocity,
                UsbHostInputType.EffectsKnob => effectsKnob,
                UsbHostInputType.Crossfader => crossfader,
                UsbHostInputType.AccelX => accelX,
                UsbHostInputType.AccelZ => accelZ,
                UsbHostInputType.AccelY => accelY,
                UsbHostInputType.Gyro => gyro,
                UsbHostInputType.GenericAxisX => genericX,
                UsbHostInputType.GenericAxisY => genericY,
                UsbHostInputType.GenericAxisZ => genericZ,
                UsbHostInputType.GenericAxisRx => genericRX,
                UsbHostInputType.GenericAxisRy => genericRY,
                UsbHostInputType.GenericAxisRz => genericRZ,
                UsbHostInputType.GenericAxisSlider => genericSlider,
                UsbHostInputType.ProKey when proKeyType is ProKeyType.Pedal => pedal,
                UsbHostInputType.ProKey when proKeyType is ProKeyType.TouchPad => touchPad,
                UsbHostInputType.ProKey => (proKeys &
                                            ((uint) 1 <<
                                             (int) proKeyType)) != 0
                    ? 1
                    : 0,
                UsbHostInputType.KeyboardInput when key is Key.LeftAlt or Key.LeftCtrl or Key.LeftShift or Key.RightAlt
                    or Key.RightCtrl
                    or Key.RightShift => (keys &
                                          ((UInt128) 1 <<
                                           KeyboardButton.Keys.IndexOf(key))) != 0
                    ? 1
                    : 0,
                UsbHostInputType.KeyboardInput => (keys &
                                                   ((UInt128) 1 <<
                                                    (KeyboardButton.KeyCodes.IndexOf(Output.GetReportField(key)) +
                                                     8))) != 0
                    ? 1
                    : 0,
                UsbHostInputType.MouseAxis => mouseAxisType switch
                {
                    MouseAxisType.X => mouseX,
                    MouseAxisType.Y => mouseY,
                    MouseAxisType.ScrollY => scrollY,
                    MouseAxisType.ScrollX => scrollX,
                    _ => 0
                },
                UsbHostInputType.MouseButton => mouseButtonType switch
                {
                    MouseButtonType.Left => (mouseButtons & 1 << 1) != 0 ? 1 : 0,
                    MouseButtonType.Right => (mouseButtons & 1 << 2) != 0 ? 1 : 0,
                    MouseButtonType.Middle => (mouseButtons & 1 << 3) != 0 ? 1 : 0,
                    _ => 0
                },
                _ => ButtonPressed(inputType) ? 1 : 0
            };
            if (ByteBased.Contains(inputType))
            {
                val <<= 8;
            }

            return val;
        }
    }
}