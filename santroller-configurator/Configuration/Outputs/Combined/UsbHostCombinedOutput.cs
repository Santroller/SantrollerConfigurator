using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public class UsbHostCombinedOutput : CombinedOutput
{
    public UsbHostCombinedOutput(ConfigViewModel model) : base(
        model)
    {
        Outputs.Clear();
        _usbHostDm = model.WhenAnyValue(x => x.UsbHostDm).ToProperty(this, x => x.UsbHostDm);
        _usbHostDp = model.WhenAnyValue(x => x.UsbHostDp).ToProperty(this, x => x.UsbHostDp);
        Outputs.Connect().Filter(x => x is OutputAxis)
            .Filter(s => s.IsVisible)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Any())
            .Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton or JoystickToDpad)
            .Filter(s => s.IsVisible)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Any())
            .Bind(out var digitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
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
        {DrumAxisType.YellowCymbal, UsbHostInputType.YellowCymbalVelocity},
        {DrumAxisType.BlueCymbal, UsbHostInputType.BlueCymbalVelocity},
        {DrumAxisType.GreenCymbal, UsbHostInputType.GreenCymbalVelocity},
        {DrumAxisType.Green, UsbHostInputType.GreenVelocity},
        {DrumAxisType.Red, UsbHostInputType.RedVelocity},
        {DrumAxisType.Yellow, UsbHostInputType.YellowVelocity},
        {DrumAxisType.Blue, UsbHostInputType.BlueVelocity},
        {DrumAxisType.Orange, UsbHostInputType.OrangeVelocity},
        {GuitarAxisType.Pickup, UsbHostInputType.Pickup},
        {GuitarAxisType.Tilt, UsbHostInputType.Tilt},
        {GuitarAxisType.Whammy, UsbHostInputType.Whammy},
        {GuitarAxisType.Slider, UsbHostInputType.Slider},
        {DjAxisType.LeftTableVelocity, UsbHostInputType.LeftTableVelocity},
        {DjAxisType.RightTableVelocity, UsbHostInputType.RightTableVelocity},
        {DjAxisType.EffectsKnob, UsbHostInputType.EffectsKnob},
        {DjAxisType.Crossfader, UsbHostInputType.Crossfader},
    };

    private static readonly Dictionary<object, UsbHostInputType> MappingsDrumGh = new()
    {
        {DrumAxisType.Kick, UsbHostInputType.KickVelocity},
    };

    private static readonly Dictionary<object, UsbHostInputType> MappingsDrumRb = new()
    {
        {DrumAxisType.Kick, UsbHostInputType.Kick1},
        {DrumAxisType.Kick2, UsbHostInputType.Kick2},
    };

    [Reactive] public string UsbHostInfo { get; set; } = "";

    [Reactive] public int ConnectedDevices { get; set; }

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

    public override void SetOutputsOrDefaults(IReadOnlyCollection<Output> outputs)
    {
        Outputs.Clear();
        if (outputs.Any())
            Outputs.AddRange(outputs);
        else
            CreateDefaults();
    }

    private void LoadMatchingFromDict(IReadOnlySet<object> valid, Dictionary<object, UsbHostInputType> dict)
    {
        foreach (var (key, value) in dict)
        {
            if (!valid.Contains(key))
            {
                continue;
            } 

            var input = new UsbHostInput(value, Model, true);
            int min = input.IsUint ? ushort.MinValue : short.MinValue;
            int max = input.IsUint ? ushort.MaxValue : short.MaxValue;
            Output? output = key switch
            {
                StandardAxisType standardAxisType => new ControllerAxis(Model,
                    input, Colors.Black, Colors.Black, Array.Empty<byte>(),
                    min, max, 0, ushort.MaxValue, standardAxisType, true),
                StandardButtonType standardButtonType => new ControllerButton(Model,
                    input, Colors.Black,
                    Colors.Black, Array.Empty<byte>(), 5,
                    standardButtonType, true),
                InstrumentButtonType standardButtonType => new GuitarButton(Model,
                    input, Colors.Black,
                    Colors.Black, Array.Empty<byte>(), 5,
                    standardButtonType, true),
                DrumAxisType drumAxisType => new DrumAxis(Model,
                    input, Colors.Black, Colors.Black, Array.Empty<byte>(),
                    min, max, 0, 10, drumAxisType, true),
                Ps3AxisType ps3AxisType => new Ps3Axis(Model,
                    input, Colors.Black, Colors.Black, Array.Empty<byte>(),
                    min, max, 0, ps3AxisType, true),
                GuitarAxisType guitarAxisType and not GuitarAxisType.Slider => new GuitarAxis(Model,
                    input, Colors.Black, Colors.Black, Array.Empty<byte>(),
                    min, max, 0, guitarAxisType, true),
                GuitarAxisType.Slider => new GuitarAxis(Model,
                    input, Colors.Black, Colors.Black, Array.Empty<byte>(),
                    min, max, 0, GuitarAxisType.Slider, true),
                DjAxisType.LeftTableVelocity => new DjAxis(Model,
                    input, Colors.Black, Colors.Black, Array.Empty<byte>(),
                    1, DjAxisType.LeftTableVelocity, true),
                DjAxisType.RightTableVelocity => new DjAxis(Model,
                    input, Colors.Black, Colors.Black, Array.Empty<byte>(),
                    1, DjAxisType.RightTableVelocity, true),
                DjAxisType.EffectsKnob => new DjAxis(Model,
                    input, Colors.Black, Colors.Black, Array.Empty<byte>(),
                    1, DjAxisType.EffectsKnob, true),
                DjAxisType djAxisType => new DjAxis(Model,
                    input, Colors.Black, Colors.Black, Array.Empty<byte>(),
                    min, max, 0, djAxisType, true),
                DjInputType djInputType => new DjButton(Model,
                    input, Colors.Black, Colors.Black, Array.Empty<byte>(), 10,
                    djInputType, true),
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
        switch (Model.DeviceControllerType)
        {
            case DeviceControllerType.GuitarHeroDrums:
                LoadMatchingFromDict(valid, MappingsDrumGh);
                break;
            case DeviceControllerType.RockBandDrums:
                LoadMatchingFromDict(valid, MappingsDrumRb);
                break;
        }
    }

    public override void UpdateBindings()
    {
        CreateDefaults();
    }

    private readonly ObservableAsPropertyHelper<int> _usbHostDm;
    private readonly ObservableAsPropertyHelper<int> _usbHostDp;

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


    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw, ReadOnlySpan<byte> wiiRaw,
        ReadOnlySpan<byte> djLeftRaw, ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw,
        ReadOnlySpan<byte> ghWtRaw, ReadOnlySpan<byte> ps2ControllerType,
        ReadOnlySpan<byte> wiiControllerType, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> bluetoothRaw,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> peripheralWtRaw, Dictionary<int, bool> digitalPeripheral,
        Dictionary<int, int> analogPeripheral)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw, digitalPeripheral, analogPeripheral);
        
        var buffer = "";
        if (usbHostRaw.IsEmpty)
        {
            UsbHostInfo = "";
            ConnectedDevices = 0;
            return;
        }
        for (var i = 0; i < usbHostRaw.Length; i += 2)
        {
            var consoleType = (ConsoleType) usbHostRaw[i];
            string subType;
            if (consoleType == ConsoleType.Xbox360)
            {
                var xInputSubType = (XInputSubType) usbHostRaw[i + 1];
                subType = EnumToStringConverter.Convert(xInputSubType);
            }
            else
            {
                var deviceType = (DeviceControllerType) usbHostRaw[i + 1];
                subType = EnumToStringConverter.Convert(deviceType);
            }

            buffer += $"{consoleType} {subType}\n";
        }

        ConnectedDevices = usbHostRaw.Length / 2;

        UsbHostInfo = buffer.Trim();
        UpdateDetails();
    }
}