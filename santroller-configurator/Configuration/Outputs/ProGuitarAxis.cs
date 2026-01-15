using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public partial class ProGuitarAxis : OutputAxis
{
    public readonly ProGuitarType Type;

    public ProGuitarAxis(ConfigViewModel model, bool enabled, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte[] ledIndicesPeripheral, byte[] ledIndicesMpr121, int min, int max,
        int deadZone, ProGuitarType type, bool outputEnabled, bool outputPeripheral,
        bool outputInverted,
        int outputPin, bool childOfCombined) : base(model, enabled, input, ledOn,
        ledOff, ledIndices, ledIndicesPeripheral, ledIndicesMpr121,
        min, max, 0, false, deadZone, true, outputEnabled, outputInverted, outputPeripheral, outputPin, childOfCombined)
    {
        Type = type;
        UpdateDetails();
    }

    private bool VelocityBased => Type >= ProGuitarType.LowEFretVelocity;

    public override string LedOnLabel => Resources.LedColourActiveButtonColour;
    public override string LedOffLabel => Resources.LedColourInactiveButtonColour;
 
    public override bool IsKeyboard => false;


    public override bool IsStrum => false;

    public override string GenerateOutput(ConfigField mode)
    {
        return mode is not (ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Xbox360
            or ConfigField.Universal or ConfigField.Shared)
            ? ""
            : GetReportField(Type);
    }

    public override bool ShouldFlip(ConfigField mode)
    {
        return false;
    }

    protected override string MinCalibrationText()
    {
        return "";
    }

    protected override string MaxCalibrationText()
    {
        return "";
    }

    protected override bool SupportsCalibration()
    {
        return false;
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

    protected override int Calculate(bool enabled, int value, int min, int max, int center, int deadZone, bool trigger,
        DeviceControllerType
            deviceControllerType)
    {
        if (!enabled) return 0;
        var val = base.Calculate(enabled, value, min, max, center, deadZone, trigger, deviceControllerType);

        if (VelocityBased)
        {
            val = Math.Abs(val - short.MaxValue);
        }
        else
        {
            val >>= 1;
        }

        return val;
    }

    public override string Generate(ConfigField mode, int debounceIndex, int ledIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        if (Model is { Branded: false, Builder: false } && !Enabled)
        {
            return "";
        }

        if (mode == ConfigField.Shared)
        {
            return "";
        }

        var output = GenerateOutput(mode);
        if (output.Length == 0) return "";
        if (mode is ConfigField.Xbox360 or ConfigField.Xbox or ConfigField.Ps3 or ConfigField.Universal && VelocityBased)
        {
            if (Input is DigitalToAnalog)
                return base.Generate(mode, debounceIndex, ledIndex, extra, combinedExtra, strumIndexes, combinedDebounce,
                    macros, writer);
            return
                $"{GenerateOutput(mode)} = abs({GenerateAssignment(GenerateOutput(mode), mode, false, true, false, false, writer)} - INT16_MAX);\n";
        }

        if (Input is DigitalToAnalog)
            return base.Generate(mode, debounceIndex, ledIndex, extra, combinedExtra, strumIndexes, combinedDebounce,
                macros, writer);
        return
            $"{GenerateOutput(mode)} = {GenerateAssignment("0", mode, false, true, false, false, writer)} >> 1;\n";
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedProGuitarAxis(Input!.Serialise(), LedOn, LedOff, LedIndices.ToArray(),
            LedIndicesPeripheral.ToArray(), Min, Max, DeadZone, Type, OutputEnabled, OutputPin, OutputInverted,
            PeripheralOutput,
            ChildOfCombined, LedIndicesMpr121.ToArray());
    }
}