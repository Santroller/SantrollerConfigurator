using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public abstract partial class HostCombinedOutput : CombinedOutput
{
    public HostCombinedOutput(ConfigViewModel model) : base(
        model)
    {
        Outputs.Clear();
        Outputs.Connect().Filter(x => x is OutputAxis)
            .Filter(s => s.IsVisible)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Length != 0)
            .Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton or JoystickToDpad)
            .Filter(s => s.IsVisible)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Length != 0)
            .Bind(out var digitalOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton or JoystickToDpad or {Input.IsAnalog: false})
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Length != 0)
            .Bind(out var allDigitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
        AllDigitalOutputs = allDigitalOutputs;
        UpdateDetails();
    }

    private static readonly Dictionary<object, UsbHostInputType> Mappings = new()
    {
        {StandardButtonType.X, UsbHostInputType.X},
        {StandardButtonType.A, UsbHostInputType.A},
        {StandardButtonType.B, UsbHostInputType.B},
        {StandardButtonType.Y, UsbHostInputType.Y},
        {StandardButtonType.Start, UsbHostInputType.Start},
        {StandardButtonType.Back, UsbHostInputType.Back},
        {StandardButtonType.LeftShoulder, UsbHostInputType.LeftShoulder},
        {StandardButtonType.RightShoulder, UsbHostInputType.RightShoulder},
        {StandardButtonType.LeftThumbClick, UsbHostInputType.LeftThumbClick},
        {StandardButtonType.RightThumbClick, UsbHostInputType.RightThumbClick},
        {StandardButtonType.Guide, UsbHostInputType.Guide},
        {StandardButtonType.Capture, UsbHostInputType.Capture},
        {StandardButtonType.DpadUp, UsbHostInputType.DpadUp},
        {StandardButtonType.DpadDown, UsbHostInputType.DpadDown},
        {StandardButtonType.DpadLeft, UsbHostInputType.DpadLeft},
        {StandardButtonType.DpadRight, UsbHostInputType.DpadRight},
        {StandardAxisType.LeftTrigger, UsbHostInputType.LeftTrigger},
        {StandardAxisType.RightTrigger, UsbHostInputType.RightTrigger},
        {StandardAxisType.LeftStickX, UsbHostInputType.LeftStickX},
        {StandardAxisType.LeftStickY, UsbHostInputType.LeftStickY},
        {StandardAxisType.RightStickX, UsbHostInputType.RightStickX},
        {StandardAxisType.RightStickY, UsbHostInputType.RightStickY},
        {Ps3AxisType.PressureDpadUp, UsbHostInputType.PressureDpadUp},
        {Ps3AxisType.PressureDpadRight, UsbHostInputType.PressureDpadRight},
        {Ps3AxisType.PressureDpadLeft, UsbHostInputType.PressureDpadLeft},
        {Ps3AxisType.PressureDpadDown, UsbHostInputType.PressureDpadDown},
        {Ps3AxisType.PressureL1, UsbHostInputType.PressureL1},
        {Ps3AxisType.PressureR1, UsbHostInputType.PressureR1},
        {Ps3AxisType.PressureTriangle, UsbHostInputType.PressureTriangle},
        {Ps3AxisType.PressureCircle, UsbHostInputType.PressureCircle},
        {Ps3AxisType.PressureCross, UsbHostInputType.PressureCross},
        {Ps3AxisType.PressureSquare, UsbHostInputType.PressureSquare},
        {InstrumentButtonType.Green, UsbHostInputType.Green},
        {InstrumentButtonType.Red, UsbHostInputType.Red},
        {InstrumentButtonType.Yellow, UsbHostInputType.Yellow},
        {InstrumentButtonType.Blue, UsbHostInputType.Blue},
        {InstrumentButtonType.Orange, UsbHostInputType.Orange},
        {InstrumentButtonType.SoloGreen, UsbHostInputType.SoloGreen},
        {InstrumentButtonType.SoloRed, UsbHostInputType.SoloRed},
        {InstrumentButtonType.SoloYellow, UsbHostInputType.SoloYellow},
        {InstrumentButtonType.SoloBlue, UsbHostInputType.SoloBlue},
        {InstrumentButtonType.SoloOrange, UsbHostInputType.SoloOrange},
        {InstrumentButtonType.StrumUp, UsbHostInputType.DpadUp},
        {InstrumentButtonType.StrumDown, UsbHostInputType.DpadDown},
        {InstrumentButtonType.Black1, UsbHostInputType.A},
        {InstrumentButtonType.Black2, UsbHostInputType.B},
        {InstrumentButtonType.Black3, UsbHostInputType.Y},
        {InstrumentButtonType.White1, UsbHostInputType.X},
        {InstrumentButtonType.White2, UsbHostInputType.LeftShoulder},
        {InstrumentButtonType.White3, UsbHostInputType.RightShoulder},
        {DjInputType.LeftBlue, UsbHostInputType.LeftBlue},
        {DjInputType.LeftRed, UsbHostInputType.LeftRed},
        {DjInputType.LeftGreen, UsbHostInputType.LeftGreen},
        {DjInputType.RightBlue, UsbHostInputType.RightBlue},
        {DjInputType.RightRed, UsbHostInputType.RightRed},
        {DjInputType.RightGreen, UsbHostInputType.RightGreen},
        {GuitarAxisType.Pickup, UsbHostInputType.Pickup},
        {GuitarAxisType.Tilt, UsbHostInputType.Tilt},
        {GuitarAxisType.Whammy, UsbHostInputType.Whammy},
        {GuitarAxisType.Slider, UsbHostInputType.Slider},
        {DjAxisType.LeftTableVelocity, UsbHostInputType.LeftTableVelocity},
        {DjAxisType.RightTableVelocity, UsbHostInputType.RightTableVelocity},
        {DjAxisType.EffectsKnob, UsbHostInputType.EffectsKnob},
        {DjAxisType.Crossfader, UsbHostInputType.Crossfader},
    };


    [Reactive] private string _usbHostInfo = "";

    [Reactive] private int _connectedDevices;

    // Since DM and DP need to be next to eachother, you cannot use pins at the far ends
    public List<int> AvailablePinsDm => Model.AvailablePinsDigital.Skip(1).ToList();
    public List<int> AvailablePinsDp => Model.AvailablePinsDigital.Where(s => AvailablePinsDm.Contains(s + 1)).ToList();

    public override SerializedOutput Serialize()
    {
        return new SerializedCombinedUsbHostOutput(Outputs.Items.ToList());
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return Resources.UsbHostCombinedTitle;
    }

    public override Enum GetOutputType()
    {
        return SimpleType.UsbHost;
    }

    public override void SetOutputsOrDefaults(IEnumerable<Output> outputs)
    {
        Outputs.Clear();
        Outputs.AddRange(outputs.Where(s => s.Input.InnermostInputs().Any(hiss => hiss is HostInput hi && Enum.IsDefined(typeof(UsbHostInputTypeReal), hi.Input.ToString()))));
        if (Outputs.Count == 0)
        {
            CreateDefaults();
        }
    }

    public abstract HostInput MakeInput(UsbHostInputType type);

    private void LoadMatchingFromDict(IReadOnlySet<object> valid, Dictionary<object, UsbHostInputType> dict)
    {
        foreach (var (key, value) in dict)
        {
            if (!valid.Contains(key))
            {
                continue;
            }

            var input = MakeInput(value);
            int min = input.IsUint ? ushort.MinValue : short.MinValue;
            int max = input.IsUint ? ushort.MaxValue : short.MaxValue;
            Output? output = key switch
            {
                StandardAxisType.RightTrigger or StandardAxisType.LeftTrigger => new ControllerAxis(Model,true,
                    input, Colors.Black, Colors.Black, [], [], [],
                    min, max, 0,(max + min) / 2, 0, 0, (StandardAxisType) key, false, false, false, -1, true),
                StandardAxisType standardAxisType => new ControllerAxis(Model,true,
                    input, Colors.Black, Colors.Black, [], [], [],
                    min, max, 0,(max + min) / 2, 0, ushort.MaxValue, standardAxisType, false, false, false, -1, true),
                StandardButtonType standardButtonType => new ControllerButton(Model,true,
                    input, Colors.Black,
                    Colors.Black, [], [], [], 5,
                    standardButtonType, false, false, false, -1, true),
                InstrumentButtonType standardButtonType => new GuitarButton(Model,true,
                    input, Colors.Black,
                    Colors.Black, [], [], [], 5,
                    standardButtonType, false, false, false, -1, true),
                DrumAxisType drumAxisType => new DrumAxis(Model,true,
                    input, null, Colors.Black, Colors.Black, [], [], [],
                    min, max, 0, 10, drumAxisType, false, false, false, -1, true),
                Ps3AxisType ps3AxisType => new Ps3Axis(Model,true,
                    input, Colors.Black, Colors.Black, [], [], [],
                    min, max, 0, ps3AxisType, false, false, false, -1, true),
                GuitarAxisType.Pickup => new GuitarAxis(Model,true,
                    input, GuitarAxis.PickupSelectorRangesPS[1] << 8, GuitarAxis.PickupSelectorRangesPS[2] << 8,
                    GuitarAxis.PickupSelectorRangesPS[3] << 8, GuitarAxis.PickupSelectorRangesPS[4] << 8, Colors.Black,
                    Colors.Black, [], [], [],
                    min, max, 0, false, GuitarAxisType.Pickup, false, false, false, -1, true),
                GuitarAxisType.Slider => new GuitarAxis(Model,true,
                    input, Colors.Black, Colors.Black, [], [], [],
                    min, max, 0, false, GuitarAxisType.Slider, false, false, false, -1, true),
                GuitarAxisType guitarAxisType => new GuitarAxis(Model,true,
                    input, Colors.Black, Colors.Black, [], [], [],
                    min, max, 0, false, guitarAxisType, false, false, false, -1, true),
                DjAxisType.LeftTableVelocity => new DjAxis(Model,true,
                    input, Colors.Black, Colors.Black, [], [], [],
                    1, 1, DjAxisType.LeftTableVelocity, false, false, false, -1, true),
                DjAxisType.RightTableVelocity => new DjAxis(Model,true,
                    input, Colors.Black, Colors.Black, [], [], [],
                    1, 1, DjAxisType.RightTableVelocity, false, false, false, -1, true),
                DjAxisType.EffectsKnob => new DjAxis(Model,true,
                    input, Colors.Black, Colors.Black, [], [], [],
                    1, 1, DjAxisType.EffectsKnob, false, false, false, -1, true),
                DjAxisType djAxisType => new DjAxis(Model,true,
                    input, Colors.Black, Colors.Black, [], [], [],
                    min, max, 0, djAxisType, false, false, false, -1, true),
                DjInputType djInputType => new DjButton(Model,true,
                    input, Colors.Black, Colors.Black, [], [], [],
                    10,
                    djInputType, false, false, false, -1, true),
                _ => null
            };
            if (output != null)
            {
                Outputs.Add(output);
            }
        }
    }

    public void CreateDefaults()
    {
        Outputs.Clear();
        var valid = ControllerEnumConverter.GetTypes(Model.DeviceControllerType).ToHashSet();
        if (Model.DeviceControllerType == DeviceControllerType.Turntable)
        {
            valid.UnionWith(Enum.GetValues<DjInputType>().Cast<object>());
        }

        LoadMatchingFromDict(valid, Mappings);
    }

    public override void UpdateBindings()
    {
        CreateDefaults();
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw, ReadOnlySpan<byte> wiiRaw,
        ReadOnlySpan<byte> djLeftRaw, ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw,
        ReadOnlySpan<byte> ghWtRaw, ReadOnlySpan<byte> ps2ControllerType,
        ReadOnlySpan<byte> wiiControllerType, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> bluetoothRaw,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral,
        ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw, ReadOnlySpan<byte> mpr121Raw,
        ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw, bool peripheralConnected, byte[] crkdRaw)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw,
            digitalPeripheral, cloneRaw, adxlRaw, mpr121Raw, midiRaw, bluetoothInputsRaw, peripheralConnected, crkdRaw);

        var buffer = "";
        if (usbHostRaw.IsEmpty)
        {
            UsbHostInfo = "";
            ConnectedDevices = 0;
            return;
        }

        var devices = 0;
        bool seen360W = false;
        for (var i = 0; i < usbHostRaw.Length; i += 2)
        {
            var consoleType = (ConsoleType) usbHostRaw[i];
            
            if (consoleType == ConsoleType.Xbox360W && !seen360W)
            {
                seen360W = true;
                devices += 1;
                buffer += $"{Resources.Xbox360W}\n";
            }
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

            devices += 1;
        }

        ConnectedDevices = devices;

        UsbHostInfo = buffer.Trim();
        UpdateDetails();
    }
}