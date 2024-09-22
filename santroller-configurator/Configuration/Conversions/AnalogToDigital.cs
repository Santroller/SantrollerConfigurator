using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using static GuitarConfigurator.NetCore.ViewModels.ConfigViewModel;

namespace GuitarConfigurator.NetCore.Configuration.Conversions;

public partial class AnalogToDigital : Input
{
    private AnalogToDigitalType _analogToDigitalType;

    public AnalogToDigital(Input child, AnalogToDigitalType analogToDigitalType, int threshold,
        ConfigViewModel model) : base(model)
    {
        Child = child;
        _analogToDigitalType = analogToDigitalType;
        IsAnalog = false;
        this.WhenAnyValue(x => x.Child.RawValue, x => x.Threshold).ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(s => RawValue = Calculate(s));
        _rawAnalogValueHelper =  this.WhenAnyValue(x => x.Child.RawValue).ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, s => s.RawAnalogValue);
        _valueLowerHelper = this.WhenAnyValue(x => x.Child.RawValue)
            .Select(s => s < 0 ? -s : 0).ToProperty(this, x => x.ValueLower);
        _valueUpperHelper = this.WhenAnyValue(x => x.Child.RawValue)
            .Select(s => s > 0 ? s : 0).ToProperty(this, x => x.ValueUpper);
        _displayThreshold = this.WhenAnyValue(x => x.Threshold, x => x.AnalogToDigitalType)
            .Select(CalculateThreshold).ToProperty(this, x => x.DisplayThreshold);
        Threshold = threshold;
    }

    public override bool Peripheral => Child.Peripheral;
    public float FullProgressWidth => OutputAxis.ProgressWidth;
    public float HalfProgressWidth => OutputAxis.ProgressWidth / 2;
    public Input Child { get; }

    [ObservableAsProperty] private int _rawAnalogValue;
    [ObservableAsProperty] private int _valueLower;

    [ObservableAsProperty] private int _valueUpper;

    public AnalogToDigitalType AnalogToDigitalType
    {
        get => _analogToDigitalType;
        set
        {
            this.RaiseAndSetIfChanged(ref _analogToDigitalType, value);
            Threshold = _analogToDigitalType switch
            {
                AnalogToDigitalType.JoyLow => short.MaxValue / 2,
                AnalogToDigitalType.JoyHigh => short.MaxValue / 2,
                AnalogToDigitalType.Trigger => ushort.MaxValue / 2,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public int Max => IsUint ? ushort.MaxValue : short.MaxValue;
    public int Min => IsUint ? ushort.MinValue : short.MinValue;

    [Reactive] private int _threshold;
    private readonly ObservableAsPropertyHelper<int> _displayThreshold;


    private int CalculateThreshold((int threshold, AnalogToDigitalType type) type)
    {
        if (!IsUint || type.type is AnalogToDigitalType.Drum or AnalogToDigitalType.Trigger)
        {
            return type.threshold;
        }

        return type.type switch
        {
            AnalogToDigitalType.JoyHigh => short.MaxValue + type.threshold,
            AnalogToDigitalType.JoyLow => short.MaxValue - type.threshold,
            _ => 0
        };
    }
    public int DisplayThreshold
    {
        get => _displayThreshold.Value;
        set
        {
            if (!Child.IsUint || AnalogToDigitalType is AnalogToDigitalType.Trigger or AnalogToDigitalType.Drum)
            {
                Threshold = value;
            }
            else if (AnalogToDigitalType is AnalogToDigitalType.JoyLow)
            {
                Threshold = short.MaxValue - value;
            }
            else
            {
                Threshold = value - short.MaxValue;
            }
        }
    }

    public override InputType? InputType => Child.InputType;
    public IEnumerable<AnalogToDigitalType> AnalogToDigitalTypes => Enum.GetValues<AnalogToDigitalType>().Where(s => s != AnalogToDigitalType.Drum);

    public override IList<DevicePin> Pins => Child.Pins;
    public override IList<PinConfig> PinConfigs => Child.PinConfigs;
    public override bool IsUint => Child.IsUint;

    public override string Title => Child.Title;

    private string? _tresholdBlob;
    private BinaryWriter? _writer;


    public override string Generate(BinaryWriter? writer)
    {
        var threshold = Threshold;
        if (Child.IsUint)
        {
            threshold = Math.Abs(threshold);
        }
        var thresholdVal = $"{threshold}";
        if (writer != null)
        {
            if (_writer != writer)
            {
                _writer = writer;
                _tresholdBlob = WriteBlob(writer, threshold);
            }

            thresholdVal = _tresholdBlob;
        }
        if (Child.IsUint)
            switch (AnalogToDigitalType)
            {
                case AnalogToDigitalType.Drum:
                case AnalogToDigitalType.Trigger:
                    return $"({Child.Generate(writer)}) > ({thresholdVal})";
                case AnalogToDigitalType.JoyHigh:
                    return $"({Child.Generate(writer)}) > ({short.MaxValue} + ({thresholdVal}))";
                case AnalogToDigitalType.JoyLow:
                    return $"({Child.Generate(writer)}) < ({short.MaxValue} - ({thresholdVal}))";
            }
        else
            switch (AnalogToDigitalType)
            {
                case AnalogToDigitalType.Drum:
                case AnalogToDigitalType.Trigger:
                case AnalogToDigitalType.JoyHigh:
                    return $"({Child.Generate(writer)}) > ({thresholdVal})";
                case AnalogToDigitalType.JoyLow:
                    return $"({Child.Generate(writer)}) < (-({thresholdVal}))";
            }

        return "";
    }

    public override SerializedInput Serialise()
    {
        return new SerializedAnalogToDigital(Child.Serialise(), AnalogToDigitalType, Threshold);
    }


    private int Calculate((int raw, int threshold) val)
    {
        if (Child.IsUint)
        {
            switch (AnalogToDigitalType)
            {
                case AnalogToDigitalType.Drum:
                case AnalogToDigitalType.Trigger:
                    return val.raw > val.threshold ? 1 : 0;
                case AnalogToDigitalType.JoyHigh:
                    return val.raw > short.MaxValue + val.threshold ? 1 : 0;
                case AnalogToDigitalType.JoyLow:
                    return val.raw < short.MaxValue - val.threshold ? 1 : 0;
            }
        }
        else
        {
            switch (AnalogToDigitalType)
            {
                case AnalogToDigitalType.Drum:
                case AnalogToDigitalType.Trigger:
                case AnalogToDigitalType.JoyHigh:
                    return val.raw > Math.Abs(val.threshold) ? 1 : 0;
                case AnalogToDigitalType.JoyLow:
                    return val.raw < -Math.Abs(val.threshold) ? 1 : 0;
            }
        }

        return 0;
    }

    public override IEnumerable<Input> InnermostInputs()
    {
        return new []{Child};
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw,
        ReadOnlySpan<byte> mpr121Raw, ReadOnlySpan<byte> midiRaw)
    {
        Child.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType, usbHostInputsRaw, usbHostRaw, peripheralWtRaw, digitalPeripheral, cloneRaw, adxlRaw, mpr121Raw, midiRaw);
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        throw new InvalidOperationException("Never call GenerateAll on AnalogToDigital, call it on its children");
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return Child.RequiredDefines();
    }
}