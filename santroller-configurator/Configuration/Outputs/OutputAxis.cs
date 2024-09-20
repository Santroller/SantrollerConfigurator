using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using static GuitarConfigurator.NetCore.ViewModels.ConfigViewModel;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public enum OutputAxisCalibrationState
{
    None,
    Min,
    Max,
    DeadZone,
    Last
}

public abstract partial class OutputAxis : Output
{
    protected internal const float ProgressWidth = 400;

    private OutputAxisCalibrationState _calibrationState = OutputAxisCalibrationState.None;

    protected OutputAxis(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte[] ledIndicesPeripheral,
        byte[] ledIndicesMpr121,
        int min, int max,
        int deadZone, bool trigger, bool outputEnabled, bool outputInverted, bool outputPeripheral, int outputPin,
        bool childOfCombined) : base(model, input, ledOn, ledOff,
        ledIndices, ledIndicesPeripheral, ledIndicesMpr121, outputEnabled, outputInverted, outputPeripheral, outputPin, childOfCombined)
    {
        Trigger = trigger;
        LedOn = ledOn;
        LedOff = ledOff;
        Max = max;
        Min = min;
        DeadZone = deadZone;
        _inputIsUintHelper = this.WhenAnyValue(x => x.Input).Select(i => i is {IsUint: true})
            .ToProperty(this, x => x.InputIsUint);
        var calibrationWatcher = this.WhenAnyValue(x => x.Input.RawValue);
        calibrationWatcher.Subscribe(ApplyCalibration);
        _valueRawLowerHelper = this.WhenAnyValue(x => x.ValueRaw).Select(s => s < 0 ? -s : 0)
            .ToProperty(this, x => x.ValueRawLower);
        _valueRawUpperHelper = this.WhenAnyValue(x => x.ValueRaw).Select(s => s > 0 ? s : 0)
            .ToProperty(this, x => x.ValueRawUpper);

        _sliderMaxHelper = this.WhenAnyValue(x => x.InputIsUint).Select(s => s ? (int)ushort.MaxValue : short.MaxValue).ToProperty(this, x => x.SliderMax);
        _sliderMinHelper = this.WhenAnyValue(x => x.InputIsUint).Select(s => s ? (int)ushort.MinValue : short.MinValue).ToProperty(this, x => x.SliderMin);

        _valueHelper = this
            .WhenAnyValue(x => x.Enabled, x => x.ValueRaw, x => x.Min, x => x.Max, x => x.DeadZone, x => x.Trigger,
                x => x.Model.DeviceControllerType).Select(Calculate).ToProperty(this, x => x.Value);
        _valueLowerHelper = this.WhenAnyValue(x => x.Value).Select(s => s < 0 ? -s : 0).ToProperty(this, x => x.ValueLower);
        _valueUpperHelper = this.WhenAnyValue(x => x.Value).Select(s => s > 0 ? s : 0).ToProperty(this, x => x.ValueUpper);
        _computedDeadZoneMarginHelper = this
            .WhenAnyValue(x => x.Min, x => x.Max, x => x.Trigger, x => x.InputIsUint, x => x.DeadZone)
            .Select(ComputeDeadZoneMargin).ToProperty(this, x => x.ComputedDeadZoneMargin);
        _calibrationMinMaxMarginHelper = this.WhenAnyValue(x => x.Min, x => x.Max, x => x.InputIsUint)
            .Select(ComputeMinMaxMargin).ToProperty(this, x => x.CalibrationMinMaxMargin);
        _isDigitalToAnalogHelper = this.WhenAnyValue(x => x.Input).Select(s => s is DigitalToAnalog)
            .ToProperty(this, x => x.IsDigitalToAnalog);
        _isDigitalToAnalogOrConstantHelper = this.WhenAnyValue(x => x.Input).Select(s => s is DigitalToAnalog or ConstantInput)
            .ToProperty(this, x => x.IsDigitalToAnalogOrConstant);
    }
    public override bool UsesPwm => true;

    public float FullProgressWidth => ProgressWidth;
    public float HalfProgressWidth => ProgressWidth / 2; 
    [ObservableAsProperty] private int _valueRawLower;

    [ObservableAsProperty] private int _valueRawUpper;

    [ObservableAsProperty] private int _value;

    [ObservableAsProperty] private int _valueLower;

    [ObservableAsProperty] private int _valueUpper;

    [ObservableAsProperty] private bool _inputIsUint;
    [ObservableAsProperty] private bool _isDigitalToAnalog;
    [ObservableAsProperty] private bool _isDigitalToAnalogOrConstant;

    [ObservableAsProperty] private Thickness _computedDeadZoneMargin;
    [ObservableAsProperty] private Thickness _calibrationMinMaxMargin;

    [ObservableAsProperty] private int _sliderMax;

    [ObservableAsProperty] private int _sliderMin;

    [Reactive] private int _min;

    [Reactive] private int _max;

    [Reactive] private int _deadZone;


    public bool Trigger { get; }
    public override bool IsCombined => false;
    public override bool IsStrum => false;

    public string CalibrationButtonText => GetCalibrationButtonText();
    public string? CalibrationText => GetCalibrationText();
    public string? CalibrationStatus => GetCalibrationStatus();

    private static Thickness ComputeDeadZoneMargin((int min, int max, bool trigger, bool inputIsUint, int deadZone) s)
    {
        float min = Math.Min(s.min, s.max);
        float max = Math.Max(s.min, s.max);
        var inverted = s.min > s.max;
        if (s.trigger)
        {
            if (inverted)
                min = max - s.deadZone;
            else
                max = min + s.deadZone;
        }
        else
        {
            var mid = (max + min) / 2;
            min = mid - s.deadZone;
            max = mid + s.deadZone;
        }

        if (!s.inputIsUint)
        {
            min += short.MaxValue;
            max += short.MaxValue;
        }

        var left = Math.Min(min / ushort.MaxValue * ProgressWidth, ProgressWidth);

        var right = ProgressWidth - Math.Min(max / ushort.MaxValue * ProgressWidth, ProgressWidth);
        ;
        return new Thickness(left, 0, right, 0);
    }


    private static Thickness ComputeMinMaxMargin((int min, int max, bool isUint) s)
    {
        if (!s.isUint)
        {
            s.min += short.MaxValue;
            s.max += short.MaxValue;
        }

        float min = Math.Min(s.min, s.max);
        float max = Math.Max(s.min, s.max);

        var left = Math.Min(min / ushort.MaxValue * ProgressWidth, ProgressWidth);

        var right = ProgressWidth - Math.Min(max / ushort.MaxValue * ProgressWidth, ProgressWidth);
        left = Math.Max(0, left);
        right = Math.Max(0, right);
        return new Thickness(left, 0, right, 0);
    }

    private int _tempMin;
    private void ApplyCalibration(int rawValue)
    {
        switch (_calibrationState)
        {
            case OutputAxisCalibrationState.Min:
                Min = rawValue;
                _tempMin = rawValue;
                break;
            case OutputAxisCalibrationState.Max:
                Max = rawValue;
                if (this is GuitarAxis {Type: GuitarAxisType.Tilt})
                {
                    Min = _tempMin - Max;
                }
                break;
            case OutputAxisCalibrationState.DeadZone:
                var min = Math.Min(Min, Max);
                var max = Math.Max(Min, Max);
                rawValue = Math.Min(Math.Max(min, rawValue), max);

                if (Trigger)
                {
                    if (Min < Max)
                        DeadZone = rawValue - min;
                    else
                        DeadZone = max - rawValue;
                }
                else
                {
                    // For non triggers, deadzone starts in the middle and grows in both directions
                    DeadZone = Math.Abs((max + min) / 2 - rawValue);
                }

                break;
        }
    }

    [RelayCommand]
    private void Calibrate()
    {
        if (!SupportsCalibration()) return;

        _calibrationState++;
        if (_calibrationState == OutputAxisCalibrationState.Last) _calibrationState = OutputAxisCalibrationState.None;

        ApplyCalibration(ValueRaw);

        this.RaisePropertyChanged(nameof(CalibrationButtonText));
        this.RaisePropertyChanged(nameof(CalibrationText));
        this.RaisePropertyChanged(nameof(CalibrationStatus));
    }

    protected virtual int Calculate(
        (bool enabled, int value, int min, int max, int deadZone, bool trigger, DeviceControllerType
            deviceControllerType) values)
    {
        if (!values.enabled) return 0;
        double val = values.value;

        var min = (float) values.min;
        var max = (float) values.max;
        var deadZone = (float) values.deadZone;
        var trigger = values.trigger;
        var inverted = min > max;
        if (trigger)
        {
            // Trigger is uint, so if the input is not, shove it forward to put it into the right range
            if (!InputIsUint)
            {
                val += short.MaxValue;
                min += short.MaxValue;
                max += short.MaxValue;
            }

            if (inverted)
            {
                min -= deadZone;
                if (val > min) return 0;
                if (val < max) val = max;
            }
            else
            {
                min += deadZone;
                if (val < min) return 0;
                if (val > max) val = max;
            }
        }
        else
        {
            // Standard axis is int, so if the input is not, then subtract to get it within the right range
            if (InputIsUint)
            {
                val -= short.MaxValue;
                max -= short.MaxValue;
                min -= short.MaxValue;
            }

            var deadZoneCalc = val - (max + min) / 2;
            if (deadZoneCalc < deadZone && deadZoneCalc > -deadZone) return 0;

            val -= Math.Sign(val) * deadZone;
            if (max > min)
            {
                min += deadZone;
                max -= deadZone;
            }
            else
            {
                min -= deadZone;
                max += deadZone;
            }
        }

        if (trigger)
        {
            val = (val - min) / (max - min) * ushort.MaxValue;
            if (val > ushort.MaxValue) val = ushort.MaxValue;
            if (val < 0) val = 0;
        }
        else
        {
            val = (val - min) / (max - min) * (short.MaxValue - short.MinValue) + short.MinValue;
            if (val > short.MaxValue) val = short.MaxValue;
            if (val < short.MinValue) val = short.MinValue;
        }

        return (int) val;
    }

    public abstract bool ShouldFlip(ConfigField mode);

    protected abstract string MinCalibrationText();
    protected abstract string MaxCalibrationText();
    protected abstract bool SupportsCalibration();

    private string? GetCalibrationText()
    {
        return _calibrationState switch
        {
            OutputAxisCalibrationState.Min => MinCalibrationText(),
            OutputAxisCalibrationState.Max => MaxCalibrationText(),
            OutputAxisCalibrationState.DeadZone => Resources.AxisCalibrationSetDeadzone,
            _ => null
        };
    }

    private string GetCalibrationButtonText()
    {
        return _calibrationState == OutputAxisCalibrationState.None
            ? Resources.AxisCalibrationCalibrate
            : Resources.AxisCalibrationNext;
    }

    private string? GetCalibrationStatus()
    {
        return _calibrationState switch
        {
            OutputAxisCalibrationState.Min => Resources.AxisCalibrationMinStatus,
            OutputAxisCalibrationState.Max => Resources.AxisCalibrationMaxStatus,
            OutputAxisCalibrationState.DeadZone => Resources.AxisCalibrationDeadzoneStatus,
            _ => null
        };
    }

    public string GenerateAssignment(string prev, ConfigField mode, bool forceAccel, bool forceTrigger, bool whammy,
        bool drum,
        BinaryWriter? writer)
    {
        var trigger = Trigger || forceTrigger;
        switch (Input)
        {
            case DigitalToAnalog:
                return Input.Generate(writer);
            case FixedInput when this is GuitarAxis {Type: GuitarAxisType.Pickup}:
                return $"({Input.Generate(writer)} >> 8) & 0xff";
        }

        string function;
        var intBased = false;
        var singleByte = false;

        switch (mode)
        {
            // Don't use ps3 whammy hacks on PC, use a more normal whammy instead.
            // XB1 also uses a uint8 for whammy, so we can handle that here too
            case ConfigField.Shared or ConfigField.Universal or ConfigField.XboxOne or ConfigField.Wii when whammy:
                singleByte = true;
                function = "handle_calibration_ps3_360_trigger";
                break;
            case ConfigField.XboxOne when trigger:
                function = "handle_calibration_xbox_one_trigger";
                break;
            case ConfigField.XboxOne when forceAccel:
                singleByte = true;
                function = "handle_calibration_ps3_360_trigger";
                break;
            case ConfigField.XboxOne:
                intBased = true;
                function = "handle_calibration_xbox";
                break;
            case ConfigField.Xbox360 or ConfigField.Xbox when whammy:
                function = "handle_calibration_xbox_whammy";
                break;
            case ConfigField.Xbox360 or ConfigField.Xbox when trigger:
                singleByte = true;
                function = "handle_calibration_ps3_360_trigger";
                break;
            case ConfigField.Xbox360 or ConfigField.Xbox:
                intBased = true;
                function = "handle_calibration_xbox";
                break;
            case ConfigField.Mouse:
                intBased = true;
                function = "handle_calibration_mouse";
                break;
            // For LED stuff (Shared), we can use the standard handle_calibration_ps3 instead.
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture when forceAccel:
                intBased = true;
                function = "handle_calibration_ps3_accel";
                break;
            case ConfigField.Ps2 when whammy:
                function = "-handle_calibration_ps3_whammy";
                singleByte = true;
                break;
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Wii when whammy:
                function = "handle_calibration_ps3_whammy";
                singleByte = true;
                break;
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4 or ConfigField.Shared
                or ConfigField.Universal or ConfigField.Wii or ConfigField.Ps2 when trigger:
                singleByte = true;
                function = "handle_calibration_ps3_360_trigger";
                break;
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4 or ConfigField.Shared
                or ConfigField.Universal or ConfigField.Wii or ConfigField.Ps2:
                singleByte = true;
                intBased = true;
                function = "handle_calibration_ps3";
                break;
            default:
                return "";
        }

        if (drum)
        {
            function = "handle_calibration_drum";
        }

        if (this is PianoKey)
        {
            singleByte = true;
            function = "handle_calibration_ps3_360_trigger";
        }

        var min = Min;
        var max = Max;
        var inverted = Min > Max;
        float multiplier;
        if (intBased)
        {
            if (InputIsUint)
            {
                max -= short.MaxValue;
                min -= short.MaxValue;
            }

            if (inverted)
            {
                min -= DeadZone;
                max += DeadZone;
            }
            else
            {
                min += DeadZone;
                max -= DeadZone;
            }

            if (min < short.MinValue)
            {
                min = short.MinValue;
            }

            if (max > short.MaxValue)
            {
                max = short.MaxValue;
            }

            multiplier = 1f / (max - min) * (short.MaxValue - short.MinValue);
        }
        else
        {
            if (!InputIsUint)
            {
                max += short.MaxValue;
                min += short.MaxValue;
            }

            if (inverted)
                min -= DeadZone;
            else
                min += DeadZone;

            if (min < 0)
            {
                min = 0;
            }

            if (max > ushort.MaxValue)
            {
                max = ushort.MaxValue;
            }

            multiplier = 1f / (max - min) * ushort.MaxValue;
        }

        var generated = "(" + Input.Generate(writer);
        if (this is GuitarAxis {Type: GuitarAxisType.Tilt} && mode is ConfigField.XboxOne)
        {
            // XB1 tilt is special. it centers at 0 but is a uint, so we need to strip away negative values
            generated += ")";
            if (!InputIsUint)
            {
                generated = $"abs({generated}) << 1";
            }
        }
        else
        {
            generated += intBased switch
            {
                false when !InputIsUint => ") + INT16_MAX",
                true when InputIsUint => ") - INT16_MAX",
                _ => ")"
            };
        }
        
        if (ShouldFlip(mode))
        {
            generated = intBased ? $"(-({generated}))" : $"(UINT16_MAX-({generated}))";
        }

        if (Input is FixedInput)
        {
            return singleByte ? $"({generated}) >> 8" : generated;
        }
        
        var mulInt = (short) (multiplier * 512);
        if (writer == null)
            return intBased
                ? $"{function}({prev}, {generated}, {(max + min) / 2}, {min}, {mulInt}, {DeadZone})"
                : $"{function}({prev}, {generated}, {min}, {mulInt}, {DeadZone})";

        return intBased
            ? $"{function}({prev}, {generated}, {WriteBlob(writer, (max + min) / 2)}, {WriteBlob(writer, min)}, {WriteBlob(writer, mulInt)}, {WriteBlob(writer, DeadZone)})"
            : $"{function}({prev}, {generated}, {WriteBlob(writer, (uint) min)}, {WriteBlob(writer, mulInt)}, {WriteBlob(writer, (uint) DeadZone)})";
    }


    public override string Generate(ConfigField mode, int debounceIndex, int ledIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        if (mode == ConfigField.Shared)
        {
            return "";
        }

        var output = GenerateOutput(mode);
        if (output.Length == 0) return "";

        if (Input is not DigitalToAnalog dta)
        {
            var extraTrigger = "";
            if (this is ControllerAxis axis)
            {
                if (mode is ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4 or ConfigField.Ps2 or ConfigField.Universal or ConfigField.Wii &&
                    axis.Type is StandardAxisType.LeftTrigger or StandardAxisType.RightTrigger)
                {
                    var trigger = axis.Type == StandardAxisType.LeftTrigger ? "l2" : "r2";
                    extraTrigger = $$"""
                                     if ({{Input.Generate(writer)}} > {{axis.Threshold}}) {
                                         report->{{trigger}} = true;
                                     }
                                     """;
                }
            }

            return $"""
                    {output} = {GenerateAssignment(output, mode, false, false, false, false, writer)};
                    {extraTrigger}
                    """;
        }

        // Digital to Analog stores values based on uint16_t for trigger, and int16_t for sticks
        var val = dta.On;

        switch (mode)
        {
            // x360 triggers are int16_t
            case ConfigField.Xbox360 or ConfigField.Xbox when !Trigger:
                break;
            // xb1 triggers and axis are already of the above form
            case ConfigField.XboxOne:
                break;
            // 360 triggers, and ps3 and ps4 triggers are uint8_t
            case ConfigField.Xbox360 or ConfigField.Xbox or ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4 or ConfigField.Universal or ConfigField.Wii  or ConfigField.Ps2
                when Trigger:
                val >>= 8;
                break;
            // ps3 and ps4 axis are uint8_t, so we both need to shift and add 128
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4 or ConfigField.Universal or ConfigField.Wii or ConfigField.Ps2 when !Trigger:
                val = (val >> 8) + 128;
                break;
            // Mouse is always not a trigger, and is int8_t
            case ConfigField.Mouse:
                val >>= 8;
                break;
            default:
                return "";
        }

        // On the PS3, we need to convert triggers from analog to digital
        if (mode is ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4 or ConfigField.Universal or ConfigField.Wii && this is ControllerAxis
            {
                Type: StandardAxisType.LeftTrigger or StandardAxisType.RightTrigger
            })
        {
            var trigger = this is ControllerAxis {Type: StandardAxisType.LeftTrigger} ? "l2" : "r2";
            return $$"""
                     if ({{Input.Generate(writer)}}) {
                         {{output}} = {{val}};
                         report->{{trigger}} = true;
                     }
                     """;
        }

        return $$"""
                 if ({{Input.Generate(writer)}}) {
                    {{output}} = {{val}};
                 }
                 """;
    }

    public override void UpdateBindings()
    {
    }
}