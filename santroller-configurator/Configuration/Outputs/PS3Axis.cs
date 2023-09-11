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

public class Ps3Axis : OutputAxis
{
    public Ps3Axis(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices, int min,
        int max,
        int deadZone, Ps3AxisType type, bool childOfCombined=false) : base(model, input, ledOn, ledOff, ledIndices, min, max, deadZone, true, childOfCombined)
    {
        Type = type;
        UpdateDetails();
    }

    public Ps3AxisType Type { get; }

    public override bool IsCombined => false;

    public override bool IsKeyboard => false;
    public override string LedOnLabel => Resources.LedColourActiveButtonColour;
    public override string LedOffLabel => Resources.LedColourInactiveButtonColour;

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return EnumToStringConverter.Convert(Type);
    }

    public override Enum GetOutputType()
    {
        return Type;
    }

    public override string GenerateOutput(ConfigField mode)
    {
        return mode is ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4 ? GetReportField(Type) : "";
    }

    public override bool ShouldFlip(ConfigField mode)
    {
        return false;
    }

    protected override string MinCalibrationText()
    {
        return Resources.AxisCalibrationButtonMin;
    }

    protected override string MaxCalibrationText()
    {
        return Resources.AxisCalibrationButtonMax;
    }


    protected override bool SupportsCalibration()
    {
        return true;
    }

    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        return mode is not (ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps3WithoutCapture or ConfigField.Shared or ConfigField.Universal)
            ? ""
            : base.Generate(mode, debounceIndex, extra, combinedExtra, combinedDebounce, macros, writer);
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedPs3Axis(Input.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), Min, Max,
            DeadZone, ChildOfCombined);
    }

    public override void UpdateBindings()
    {
    }
}