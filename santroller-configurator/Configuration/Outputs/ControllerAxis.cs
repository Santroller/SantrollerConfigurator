using System;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class ControllerAxis : OutputAxis
{

    public ControllerAxis(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices, int min,
        int max,
        int deadZone, int threshold, StandardAxisType type, bool childOfCombined) : base(model, input, ledOn, ledOff, ledIndices, min,
        max,
        deadZone, IsTrigger(type), childOfCombined)
    {
        Type = type;
        Threshold = threshold;
        UpdateDetails();
    }

    [Reactive] public int Threshold { get; set; }
    public StandardAxisType Type { get; }

    public override bool IsCombined => false;

    public override string LedOnLabel
    {
        get
        {
            switch (Type)
            {
                case StandardAxisType.LeftStickX:
                case StandardAxisType.RightStickX:
                    return Resources.LedColourActiveAxisX;
                case StandardAxisType.LeftStickY:
                case StandardAxisType.RightStickY:
                    return Resources.LedColourActiveAxisY;
                case StandardAxisType.LeftTrigger:
                case StandardAxisType.RightTrigger:
                    return Resources.LedColourActiveAxisTrigger;
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
                case StandardAxisType.LeftStickX:
                case StandardAxisType.RightStickX:
                    return Resources.LedColourInactiveAxisX;
                case StandardAxisType.LeftStickY:
                case StandardAxisType.RightStickY:
                    return Resources.LedColourInactiveAxisY;
                case StandardAxisType.LeftTrigger:
                case StandardAxisType.RightTrigger:
                    return Resources.LedColourInactiveAxisTrigger;
                default:
                    return "";
            }
        }
    }

    public override bool IsKeyboard => false;

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return ControllerEnumConverter.Convert(Type, deviceControllerType, legendType, swapSwitchFaceButtons);
    }

    public override Enum GetOutputType()
    {
        return Type;
    }

    private static bool IsTrigger(StandardAxisType type)
    {
        return type is StandardAxisType.LeftTrigger or StandardAxisType.RightTrigger;
    }

    public override string GenerateOutput(ConfigField mode)
    {
        return mode is not (ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4 or ConfigField.XboxOne or ConfigField.Xbox360 or ConfigField.Universal)
            ? ""
            : GetReportField(Type);
    }

    protected override string MinCalibrationText()
    {
        switch (Type)
        {
            case StandardAxisType.LeftStickX:
            case StandardAxisType.RightStickX:
                return Resources.AxisCalibrationMinX;
            case StandardAxisType.LeftStickY:
            case StandardAxisType.RightStickY:
                return Resources.AxisCalibrationMinY;
            case StandardAxisType.LeftTrigger:
            case StandardAxisType.RightTrigger:
                return Resources.AxisCalibrationTrigger;
            default:
                return "";
        }
    }

    protected override string MaxCalibrationText()
    {
        switch (Type)
        {
            case StandardAxisType.LeftStickX:
            case StandardAxisType.RightStickX:
                return Resources.AxisCalibrationMaxX;
            case StandardAxisType.LeftStickY:
            case StandardAxisType.RightStickY:
                return Resources.AxisCalibrationMaxY;
            case StandardAxisType.LeftTrigger:
            case StandardAxisType.RightTrigger:
                return Resources.AxisCalibraitonMaxTrigger;
            default:
                return "";
        }
    }

    public override bool ShouldFlip(ConfigField mode)
    {
        // Need to flip y axis on PS3/4
        return mode is ConfigField.Ps4 or ConfigField.Ps3 or ConfigField.Ps3WithoutCapture && Type is StandardAxisType.LeftStickY or StandardAxisType.RightStickY;
    }

    protected override bool SupportsCalibration()
    {
        return true;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedControllerAxis(Input.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), Min, Max,
            DeadZone,Threshold, ChildOfCombined);
    }

    public override void UpdateBindings()
    {
    }
}