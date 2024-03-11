using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public partial class DjAxis : OutputAxis
{
    public DjAxis(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte[] ledIndicesPeripheral, int min, int max,
        int deadZone, DjAxisType type, bool outputEnabled, bool outputPeripheral, bool outputInverted, int outputPin,
        bool childOfCombined) : base(model, input, ledOn, ledOff, ledIndices, ledIndicesPeripheral, min, max,
        deadZone,
        false, outputEnabled, outputInverted, outputPeripheral, outputPin, childOfCombined)
    {
        Multiplier = 0;
        Type = type;
        UpdateDetails();
    }

    public DjAxis(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte[] ledIndicesPeripheral, int multiplier, int ledMultiplier,
        DjAxisType type, bool outputEnabled, bool outputPeripheral, bool outputInverted, int outputPin,
        bool childOfCombined) : base(model, input, ledOn, ledOff, ledIndices, ledIndicesPeripheral, 0,
        0,
        0,
        false, outputEnabled, outputInverted, outputPeripheral, outputPin, childOfCombined)
    {
        if (type == DjAxisType.Crossfader)
        {
            Invert = multiplier == -1;
        }
        else
        {
            Multiplier = multiplier;
        }

        LedMultiplier = ledMultiplier;

        Type = type;
        UpdateDetails();
    }

    [Reactive] public int Multiplier { get; set; }
    [Reactive] public int LedMultiplier { get; set; }

    [Reactive] public bool Invert { get; set; }

    protected override int Calculate(
        (bool enabled, int value, int min, int max, int deadZone, bool trigger, DeviceControllerType
            deviceControllerType) values)
    {
        return Type switch
        {
            DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity when Input.IsUint => (values.value -
                short.MaxValue) * Multiplier,
            DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity => values.value * Multiplier,

            DjAxisType.EffectsKnob when Input.IsUint => (values.value - short.MaxValue) * (Invert ? -1 : 1),
            DjAxisType.EffectsKnob => values.value * (Invert ? -1 : 1),
            _ => base.Calculate(values)
        };
    }

    public DjAxisType Type { get; }

    public override bool IsKeyboard => false;

    public override string LedOnLabel
    {
        get
        {
            return Type switch
            {
                DjAxisType.Crossfader => Resources.LedColourActiveAxisX,
                DjAxisType.EffectsKnob => Resources.LedColourActiveAxisX,
                DjAxisType.LeftTableVelocity => Resources.LedColourActiveDjVelocity,
                DjAxisType.RightTableVelocity => Resources.LedColourActiveDjVelocity,
                _ => ""
            };
        }
    }

    public override string LedOffLabel
    {
        get
        {
            return Type switch
            {
                DjAxisType.Crossfader => Resources.LedColourInactiveAxisX,
                DjAxisType.EffectsKnob => Resources.LedColourInactiveAxisX,
                DjAxisType.LeftTableVelocity => Resources.LedColourInactiveDjVelocity,
                DjAxisType.RightTableVelocity => Resources.LedColourInactiveDjVelocity,
                _ => ""
            };
        }
    }

    public bool IsVelocity => Type is DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity;

    public bool SupportsMinMax => !IsDigitalToAnalog && !IsVelocity && !IsEffectsKnob;

    public bool IsFader => Type is DjAxisType.Crossfader;
    public bool IsEffectsKnob => Type is DjAxisType.EffectsKnob;

    public override bool ShouldFlip(ConfigField mode)
    {
        return mode is ConfigField.Xbox360 && Type is DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity;
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

    public override SerializedOutput Serialize()
    {
        if (IsVelocity)
        {
            return new SerializedDjAxis(Input.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(),
                LedIndicesPeripheral.ToArray(), Multiplier, LedMultiplier, OutputEnabled, OutputPin, OutputInverted, PeripheralOutput,
                ChildOfCombined);
        }

        if (IsEffectsKnob)
        {
            return new SerializedDjAxis(Input.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(),
                LedIndicesPeripheral.ToArray(), Invert ? -1 : 1, LedMultiplier, OutputEnabled, OutputPin, OutputInverted, PeripheralOutput,
                ChildOfCombined);
        }

        return new SerializedDjAxis(Input.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(),
            LedIndicesPeripheral.ToArray(), Min, Max, DeadZone, OutputEnabled, OutputPin, OutputInverted, PeripheralOutput,
            ChildOfCombined);
    }

    public override string GenerateOutput(ConfigField mode)
    {
        return GetReportField(Type);
    }

    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        if (mode == ConfigField.Shared)
            return base.Generate(mode, debounceIndex, extra, combinedExtra, strumIndexes, combinedDebounce, macros,
                writer);
        if (mode is not (ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.XboxOne or ConfigField.Xbox360
            or ConfigField.Universal)) return "";

        // The crossfader and effects knob on ps3 controllers are shoved into the accelerometer data
        var accelerometer = mode is ConfigField.Ps3 or ConfigField.Ps3WithoutCapture &&
                            Type is DjAxisType.Crossfader or DjAxisType.EffectsKnob;
        // PS3 needs uint, xb360 needs int
        // So convert to the right method for that console, and then shift for ps3
        var generated = $"({Input.Generate()})";
        var generatedPs3 = generated;

        var tableCommand = "handle_calibration_turntable_ps3";
        var tableCommand360 = "handle_calibration_turntable_360";

        if (InputIsUint)
        {
            // xinput needs int, uint -> int
            generated = $"({generated} - INT16_MAX)";
        }
        else
        {
            // ps3 needs int, int -> uint
            generatedPs3 = $"({generated} + INT16_MAX)";
        }

        // Table just applies a multiplier to the value
        // This is the one instance where even PS3 uses int values, because it makes the math easier
        var generatedTable = $"{tableCommand360}({GenerateOutput(mode)},{generated}, {Multiplier})";
        var generatedTablePs3 = $"{tableCommand}({GenerateOutput(mode)},{generated}, {Multiplier})";

        if (writer != null && Type is DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity)
        {
            var multiplierBlob = ConfigViewModel.WriteBlob(writer, Multiplier);
            generatedTable = $"{tableCommand360}({GenerateOutput(mode)},{generated}, {multiplierBlob})";
            generatedTablePs3 = $"{tableCommand}({GenerateOutput(mode)},{generated}, {multiplierBlob})";
        }

        var gen = Type switch
        {
            DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity when mode is ConfigField.Ps3
                    or ConfigField.Ps3WithoutCapture or ConfigField.Universal
                => generatedTablePs3,
            DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity
                => generatedTable,
            DjAxisType.EffectsKnob when mode is ConfigField.Ps3 or ConfigField.Ps3WithoutCapture
                => $"(({generatedPs3} >> 6))",
            DjAxisType.EffectsKnob when mode is ConfigField.Universal
                => $"(({generatedPs3} >> 8))",
            DjAxisType.EffectsKnob => generated,
            _ => GenerateAssignment(GenerateOutput(mode), mode, accelerometer, false, false, false, writer)
        };
        return Type switch
        {
            DjAxisType.Crossfader or DjAxisType.EffectsKnob => $"{GenerateOutput(mode)} = {gen};",
            _ => $"if ({Input.Generate()}){{{GenerateOutput(mode)} = {gen};}}"
        };
    }

    protected override string MinCalibrationText()
    {
        return Type switch
        {
            DjAxisType.Crossfader => Resources.AxisCalibraitonMinDjCrossfader,
            DjAxisType.EffectsKnob => Resources.AxisCalibrationMinEffectsKnob,
            DjAxisType.LeftTableVelocity => Resources.AxisCalibrationMinDjVelocityLeft,
            DjAxisType.RightTableVelocity => Resources.AxisCalibrationMinDjVelocityRight,
            _ => ""
        };
    }

    protected override string MaxCalibrationText()
    {
        return Type switch
        {
            DjAxisType.Crossfader => Resources.AxisCalibrationMaxDjCrossfader,
            DjAxisType.EffectsKnob => Resources.AxisCalibrationMaxDjEffectsKnob,
            DjAxisType.LeftTableVelocity => Resources.AxisCalibrationMaxDjVelocityLeft,
            DjAxisType.RightTableVelocity => Resources.AxisCalibrationMaxDjVelocityRight,
            _ => ""
        };
    }


    protected override bool SupportsCalibration()
    {
        return Type is DjAxisType.Crossfader;
    }
}