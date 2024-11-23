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
    public Ps3Axis(ConfigViewModel model, bool enabled, Input input, Color ledOn, Color ledOff, byte[] ledIndices, byte[] ledIndicesPeripheral, byte[] ledIndicesMpr121, int min,
        int max,
        int deadZone, Ps3AxisType type, bool outputEnabled, bool outputPeripheral, bool outputInverted, int outputPin, bool childOfCombined=false) : base(model, enabled, input, ledOn, ledOff, ledIndices, ledIndicesPeripheral, ledIndicesMpr121, min, max, 0, deadZone, true, outputEnabled, outputInverted, outputPeripheral, outputPin, childOfCombined)
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
    public override SerializedOutput Serialize()
    {
        return new SerializedPs3Axis(Input.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), LedIndicesPeripheral.ToArray(), Min, Max,
            DeadZone, OutputEnabled, OutputPin, OutputInverted, PeripheralOutput, ChildOfCombined, LedIndicesMpr121.ToArray());
    }

    public override void UpdateBindings()
    {
    }
}