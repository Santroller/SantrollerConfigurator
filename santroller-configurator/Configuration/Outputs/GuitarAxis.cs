using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Input;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public partial class GuitarAxis : OutputAxis
{
    public static readonly Dictionary<int, int> PickupSelectorRangesPS = new()
    {
        {1, 0x19},
        {2, 0x4C},
        {3, 0x96},
        {4, 0xB2},
        {5, 0xE5},
    };

    public static readonly Dictionary<int, int> PickupSelectorRangesXb1 = new()
    {
        {1, 0x0},
        {2, 0x10},
        {3, 0x20},
        {4, 0x30},
        {5, 0x40},
    };

    public static readonly Dictionary<int, int> WiiGh5Mappings = new()
    {
        {0x15, 0x04},
        {0x30, 0x07},
        {0x4D, 0x0a},
        {0x65, 0x0c},
        {0x66, 0x0c},
        {0x80, 0x0f},
        {0x99, 0x12},
        {0x9A, 0x12},
        {0xAC, 0x14},
        {0xAD, 0x14},
        {0xAE, 0x14},
        {0xAF, 0x14},
        {0xC6, 0x17},
        {0xC7, 0x17},
        {0xC8, 0x17},
        {0xC9, 0x17},
        {0xDF, 0x1A},
        {0xE0, 0x1A},
        {0xE1, 0x1A},
        {0xE2, 0x1A},
        {0xE3, 0x1A},
        {0xE4, 0x1A},
        {0xE5, 0x1A},
        {0xE6, 0x1A},
        {0xF8, 0x1F},
        {0xF9, 0x1F},
        {0xFA, 0x1F},
        {0xFB, 0x1F},
        {0xFC, 0x1F},
        {0xFD, 0x1F},
        {0xFE, 0x1F},
        {0xFF, 0x1F},
    };
    public int PickupSelectorNotch2 { get; set; }
    public int PickupSelectorNotch3 { get; set; }
    public int PickupSelectorNotch4 { get; set; }
    public int PickupSelectorNotch5 { get; set; }


    public GuitarAxis(ConfigViewModel model, bool enabled, Input input, Color ledOn, Color ledOff,
        byte[] ledIndices, byte[] ledIndicesPeripheral, byte[] ledIndicesMpr121, int min, int max, int deadZone,
        bool invert,
        GuitarAxisType type, bool outputEnabled, bool outputPeripheral, bool outputInverted, int outputPin,
        bool childOfCombined) : base(model,enabled,
        input, ledOn,
        ledOff, ledIndices, ledIndicesPeripheral, ledIndicesMpr121, min, max, 0,deadZone,
        type is GuitarAxisType.Slider or GuitarAxisType.Whammy, outputEnabled, outputInverted, outputPeripheral,
        outputPin, childOfCombined)
    {
        Type = type;
        Inverted = invert;
        UpdateDetails();
        _namedAxisInfoHelper = this.WhenAnyValue(x => x.Value).Select(GetNamedAxisInfo).ToProperty(this, x => x.NamedAxisInfo);
    }

    public GuitarAxis(ConfigViewModel model, bool enabled, Input input, int pickupSelectorNotch2,
        int pickupSelectorNotch3, int pickupSelectorNotch4, int pickupSelectorNotch5, Color ledOn, Color ledOff,
        byte[] ledIndices, byte[] ledIndicesPeripheral, byte[] ledIndicesMpr121, int min, int max, int deadZone,
        bool invert,
        GuitarAxisType type, bool outputEnabled, bool outputPeripheral, bool outputInverted, int outputPin,
        bool childOfCombined) : base(model, enabled,
        input, ledOn,
        ledOff, ledIndices, ledIndicesPeripheral, ledIndicesMpr121, min, max,0, deadZone,
        true, outputEnabled, outputInverted, outputPeripheral,
        outputPin, childOfCombined)
    {
        PickupSelectorNotch2 = pickupSelectorNotch2;
        PickupSelectorNotch3 = pickupSelectorNotch3;
        PickupSelectorNotch4 = pickupSelectorNotch4;
        PickupSelectorNotch5 = pickupSelectorNotch5;
        Type = type;
        Inverted = invert;
        UpdateDetails();
        _namedAxisInfoHelper = this.WhenAnyValue(x => x.Value).Select(GetNamedAxisInfo).ToProperty(this, x => x.NamedAxisInfo);
    }

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] private string _namedAxisInfo = "";

    [Reactive] private bool _inverted;

    public GuitarAxisType Type { get; }
    public bool HasNamedAxis => Type is GuitarAxisType.Slider or GuitarAxisType.Pickup;
    public bool IsPickup => Type is GuitarAxisType.Pickup;

    public bool AllowInvert => Type is GuitarAxisType.Pickup;

    public override bool IsKeyboard => false;

    public override string LedOnLabel
    {
        get
        {
            return Type switch
            {
                GuitarAxisType.Tilt => Resources.LEDColourActiveTilt,
                GuitarAxisType.Whammy => Resources.LEDColourActiveWhammy,
                GuitarAxisType.Pickup => Resources.LEDColourActivePickup,
                GuitarAxisType.Slider => Resources.LEDColourActiveSlider,
                _ => ""
            };
        }
    }

    protected override int Calculate(bool enabled, int value, int min, int max, int center, int deadZone, bool trigger, DeviceControllerType
        deviceControllerType)
    {
        return Type switch
        {
            GuitarAxisType.Slider or GuitarAxisType.Pickup => value,
            _ => base.Calculate(enabled, value, min, max, center, deadZone, trigger, deviceControllerType)
        };
    }

    public override string LedOffLabel
    {
        get
        {
            return Type switch
            {
                GuitarAxisType.Tilt => Resources.LEDColourInctiveTilt,
                GuitarAxisType.Whammy => Resources.LEDColourInctiveWhammy,
                GuitarAxisType.Pickup => Resources.LEDColourInctivePickup,
                GuitarAxisType.Slider => Resources.LEDColourInctiveSlider,
                _ => ""
            };
        }
    }

    private int GetPickupSelectorValue(int val)
    {
        if (Input is DigitalToAnalog or FixedInput)
        {
            return Math.Min(val / (ushort.MaxValue / 5) + 1, 5);
        }

        if (val < PickupSelectorNotch2)
        {
            return 1;
        }

        if (val < PickupSelectorNotch3)
        {
            return 2;
        }

        if (val < PickupSelectorNotch4)
        {
            return 3;
        }

        return val < PickupSelectorNotch5 ? 4 : 5;
    }

    private string GetNamedAxisInfo(int val)
    {
        if (Type is GuitarAxisType.Pickup)
        {
            if (!Input.IsUint)
            {
                val += short.MaxValue;
            }

            if (Inverted)
            {
                val = ushort.MaxValue - val;
            }

            return $"Notch {GetPickupSelectorValue(val)}";
        }

        var ret = "";
        if (!ChildOfCombined)
        {
            ret = Resources.TapBarCurrentFrets;
        }

        val &= 0xFF;
        if (Type is not GuitarAxisType.Slider || !Gh5NeckInput.Gh5Mappings.TryGetValue(val, out var info))
            return ret + Resources.TapBarCurrentFretsNone;
        if (info.HasFlag(BarButton.Green)) ret += $"{Resources.TapBarCurrentFretsGreen} ";
        if (info.HasFlag(BarButton.Red)) ret += $"{Resources.TapBarCurrentFretsRed} ";
        if (info.HasFlag(BarButton.Yellow)) ret += $"{Resources.TapBarCurrentFretsYellow} ";
        if (info.HasFlag(BarButton.Blue)) ret += $"{Resources.TapBarCurrentFretsBlue} ";
        if (info.HasFlag(BarButton.Orange)) ret += $"{Resources.TapBarCurrentFretsOrange} ";
        return ret.Trim();
    }

    public override bool ShouldFlip(ConfigField mode)
    {
        return false;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedGuitarAxis(Input!.Serialise(), Enabled, Type, PickupSelectorNotch2, PickupSelectorNotch3,
            PickupSelectorNotch4, PickupSelectorNotch5, LedOn, LedOff, LedIndices.ToArray(),
            LedIndicesPeripheral.ToArray(), Inverted, Min, Max,
            DeadZone, OutputEnabled, OutputPin, OutputInverted, PeripheralOutput, ChildOfCombined,
            LedIndicesMpr121.ToArray());
    }

    public override Enum GetOutputType()
    {
        return Type;
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        if (deviceControllerType is DeviceControllerType.RockBandGuitar && Type is GuitarAxisType.Slider)
        {
            return "Solo Frets";
        }

        return EnumToStringConverter.Convert(Type);
    }

    protected override string MinCalibrationText()
    {
        return Type switch
        {
            GuitarAxisType.Tilt => Resources.AxisMinCalibrationTilt,
            GuitarAxisType.Whammy => Resources.AxisMinCalibrationWhammy,
            _ => ""
        };
    }

    protected override string MaxCalibrationText()
    {
        return Type switch
        {
            GuitarAxisType.Tilt => Resources.AxisMaxCalibrationTilt,
            GuitarAxisType.Whammy => Resources.AxisMaxCalibrationWhammy,
            _ => ""
        };
    }

    protected override bool SupportsCalibration()
    {
        return Type is not GuitarAxisType.Slider;
    }
}