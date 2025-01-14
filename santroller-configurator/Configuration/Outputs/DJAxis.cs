using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public partial class DjAxis : OutputAxis
{
    public DjAxis(ConfigViewModel model, bool enabled, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte[] ledIndicesPeripheral, byte[] ledIndicesMpr121, int min, int max,
        int deadZone, DjAxisType type, bool outputEnabled, bool outputPeripheral, bool outputInverted, int outputPin,
        bool childOfCombined) : base(model, enabled, input, ledOn, ledOff, ledIndices, ledIndicesPeripheral,
        ledIndicesMpr121,
        min, max, false,
        deadZone,
        false, outputEnabled, outputInverted, outputPeripheral, outputPin, childOfCombined)
    {
        Multiplier = 0;
        Type = type;
        UpdateDetails();
    }

    public DjAxis(ConfigViewModel model, bool enabled, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte[] ledIndicesPeripheral, byte[] ledIndicesMpr121, int multiplier, int ledMultiplier,
        DjAxisType type, bool outputEnabled, bool outputPeripheral, bool outputInverted, int outputPin,
        bool childOfCombined) : base(model, enabled, input, ledOn, ledOff, ledIndices, ledIndicesPeripheral,
        ledIndicesMpr121, 0,
        0, false,
        0,
        false, outputEnabled, outputInverted, outputPeripheral, outputPin, childOfCombined)
    {
        if (type == DjAxisType.EffectsKnob)
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
    protected override OutputAxisCalibrationState MaxState => Type is DjAxisType.EffectsKnob ? OutputAxisCalibrationState.Max : OutputAxisCalibrationState.DeadZone;
    
    [Reactive] private int _multiplier;
    [Reactive] private int _ledMultiplier;

    [Reactive] private bool _invert;

    protected override int Calculate(bool enabled, int value, int min, int max, int center, int deadZone, bool trigger,
        DeviceControllerType
            deviceControllerType)
    {
        return Type switch
        {
            DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity when Input.IsUint => (value -
                short.MaxValue) * Multiplier,
            DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity => value * Multiplier,

            _ => base.Calculate(enabled, value, min, max, center, deadZone, trigger, deviceControllerType)
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

    public bool SupportsMinMax => !IsDigitalToAnalog && !IsVelocity;

    public bool SupportsDeadzone => SupportsMinMax && !IsEffectsKnob;

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
            return new SerializedDjAxis(Input.Serialise(), Enabled, Type, LedOn, LedOff, LedIndices.ToArray(),
                LedIndicesPeripheral.ToArray(), Multiplier, LedMultiplier, OutputEnabled, OutputPin, OutputInverted,
                PeripheralOutput,
                ChildOfCombined, LedIndicesMpr121.ToArray());
        }

        if (IsEffectsKnob)
        {
            return new SerializedDjAxis(Input.Serialise(), Enabled, Type, LedOn, LedOff, LedIndices.ToArray(),
                LedIndicesPeripheral.ToArray(), Invert ? -1 : 1, LedMultiplier, OutputEnabled, OutputPin,
                OutputInverted, PeripheralOutput,
                ChildOfCombined, LedIndicesMpr121.ToArray());
        }

        return new SerializedDjAxis(Input.Serialise(), Enabled, Type, LedOn, LedOff, LedIndices.ToArray(),
            LedIndicesPeripheral.ToArray(), Min, Max, DeadZone, OutputEnabled, OutputPin, OutputInverted,
            PeripheralOutput,
            ChildOfCombined, LedIndicesMpr121.ToArray());
    }

    public override string GenerateOutput(ConfigField mode)
    {
        return GetReportField(Type);
    }

    public override string Generate(ConfigField mode, int debounceIndex, int ledIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        if (Model is {Branded: false, Builder: false} && !Enabled)
        {
            return "";
        }
        if (mode == ConfigField.Shared)
            return base.Generate(mode, debounceIndex, ledIndex, extra, combinedExtra, strumIndexes, combinedDebounce,
                macros,
                writer);
        if (mode is not (ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.XboxOne or ConfigField.Xbox360
            or ConfigField.Universal or ConfigField.Xbox)) return "";

        // The crossfader and effects knob on ps3 controllers are shoved into the accelerometer data
        var accelerometer = mode is ConfigField.Ps3 or ConfigField.Ps3WithoutCapture &&
                            Type is DjAxisType.Crossfader or DjAxisType.EffectsKnob;
        // PS3 needs uint, xb360 needs int
        // So convert to the right method for that console, and then shift for ps3
        var generated = $"({Input.Generate(writer)})";

        if (InputIsUint)
        {
            // xinput needs int, uint -> int
            generated = $"({generated} - INT16_MAX)";
        }

        // Table just applies a multiplier to the value
        // This is the one instance where even PS3 uses int values, because it makes the math easier
        var generatedTable = $"handle_calibration_turntable_360({GenerateOutput(mode)},{generated}, {Multiplier})";
        var generatedTablePs3 = $"handle_calibration_turntable_ps3({GenerateOutput(mode)},{generated}, {Multiplier})";

        if (writer != null && Type is DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity)
        {
            var multiplierBlob = ConfigViewModel.WriteBlob(writer, Multiplier);
            generatedTable = $"handle_calibration_turntable_360({GenerateOutput(mode)},{generated}, {multiplierBlob})";
            generatedTablePs3 =
                $"handle_calibration_turntable_ps3({GenerateOutput(mode)},{generated}, {multiplierBlob})";
        }

        var gen = Type switch
        {
            DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity when mode is ConfigField.Ps3
                    or ConfigField.Ps3WithoutCapture or ConfigField.Universal
                => generatedTablePs3,
            DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity
                => generatedTable,
            _ => GenerateAssignment(GenerateOutput(mode), mode, accelerometer, false, false, false, writer)
        };
        return Type switch
        {
            DjAxisType.Crossfader => $"{GenerateOutput(mode)} = {gen};",
            _ => $"if ({Input.Generate(writer)}){{{GenerateOutput(mode)} = {gen};}}"
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
        return Type is DjAxisType.Crossfader or DjAxisType.EffectsKnob;
    }
}