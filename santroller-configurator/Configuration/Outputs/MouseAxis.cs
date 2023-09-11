using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class MouseAxis : OutputAxis
{
    public MouseAxis(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices, int min,
        int max, int deadZone, MouseAxisType type) : base(model, input, ledOn, ledOff, ledIndices, min, max,
        deadZone, false, false)
    {
        Type = type;
        UpdateDetails();
    }

    public override bool IsKeyboard => true;
    public virtual bool IsController => false;

    public MouseAxisType Type { get; }

    public override bool IsCombined => false;

    public override string LedOnLabel
    {
        get
        {
            switch (Type)
            {
                case MouseAxisType.X:
                case MouseAxisType.ScrollX:
                    return Resources.LedColourActiveMouseX;
                case MouseAxisType.Y:
                case MouseAxisType.ScrollY:
                    return Resources.LedColourActiveMouseY;
                default:
                    return "";
            }
        }
    }

    public override string LedOffLabel
    {
        get
        {
            switch (Type)
            {
                case MouseAxisType.X:
                case MouseAxisType.ScrollX:
                    return Resources.LedColourInactiveMouseX;
                case MouseAxisType.Y:
                case MouseAxisType.ScrollY:
                    return Resources.LedColourInactiveMouseY;
                default:
                    return "";
            }
        }
    }

    public override bool ShouldFlip(ConfigField mode)
    {
        return false;
    }

    public override void UpdateBindings()
    {
    }

    public override string GenerateOutput(ConfigField mode)
    {
        if (mode != ConfigField.Mouse) return "";
        return GetReportField(Type);
    }

    protected override string MinCalibrationText()
    {
        switch (Type)
        {
            case MouseAxisType.X:
            case MouseAxisType.ScrollX:
                return Resources.AxisCalibrationMinMouseX;
            case MouseAxisType.Y:
            case MouseAxisType.ScrollY:
                return Resources.AxisCalibrationMinMouseY;
            default:
                return "";
        }
    }

    protected override string MaxCalibrationText()
    {
        switch (Type)
        {
            case MouseAxisType.X:
            case MouseAxisType.ScrollX:
                return Resources.AxisCalibrationMaxMouseX;
            case MouseAxisType.Y:
            case MouseAxisType.ScrollY:
                return Resources.AxisCalibrationMaxMouseY;
            default:
                return "";
        }
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return EnumToStringConverter.Convert(Type);
    }

    public override Enum GetOutputType()
    {
        return Type;
    }

    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        return mode is not (ConfigField.Mouse or ConfigField.Shared)
            ? ""
            : base.Generate(mode, debounceIndex, extra, combinedExtra, combinedDebounce, macros, writer);
    }

    protected override bool SupportsCalibration()
    {
        return true;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedMouseAxis(Input.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), Min, Max,
            DeadZone);
    }
}