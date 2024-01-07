using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Conversions;

public class AnalogToDigital : Input
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
        this.WhenAnyValue(x => x.Child.RawValue).ObserveOn(RxApp.MainThreadScheduler)
            .ToPropertyEx(this, s => s.RawAnalogValue);
        this.WhenAnyValue(x => x.Child.RawValue)
            .Select(s => s < 0 ? -s : 0).ToPropertyEx(this, x => x.ValueLower);
        this.WhenAnyValue(x => x.Child.RawValue)
            .Select(s => s > 0 ? s : 0).ToPropertyEx(this, x => x.ValueUpper);
        _displayThreshold = this.WhenAnyValue(x => x.Threshold, x => x.AnalogToDigitalType)
            .Select(s => CalculateThreshold(s)).ToProperty(this, x => x.DisplayThreshold);
        Threshold = threshold;
    }

    public override bool Peripheral => Child.Peripheral;
    public float FullProgressWidth => OutputAxis.ProgressWidth;
    public float HalfProgressWidth => OutputAxis.ProgressWidth / 2;
    public Input Child { get; }

    [ObservableAsProperty] public int RawAnalogValue { get; }
    [ObservableAsProperty] public int ValueLower { get; }

    [ObservableAsProperty] public int ValueUpper { get; }

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

    [Reactive] public int Threshold { get; set; }
    private readonly ObservableAsPropertyHelper<int> _displayThreshold;


    private int CalculateThreshold((int threshold, AnalogToDigitalType type) type)
    {
        if (!IsUint)
        {
            return type.threshold;
        }
        switch (type.type)
        {
            case AnalogToDigitalType.Trigger:
            case AnalogToDigitalType.JoyHigh:
                return short.MaxValue + type.threshold;
            case AnalogToDigitalType.JoyLow:
                return short.MaxValue - type.threshold;
        }

        return 0;
    }
    public int DisplayThreshold
    {
        get => _displayThreshold.Value;
        set
        {
            if (!Child.IsUint)
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
    public IEnumerable<AnalogToDigitalType> AnalogToDigitalTypes => Enum.GetValues<AnalogToDigitalType>();

    public override IList<DevicePin> Pins => Child.Pins;
    public override IList<PinConfig> PinConfigs => Child.PinConfigs;
    public override bool IsUint => Child.IsUint;

    public override string Title => Child.Title;


    public override string Generate()
    {
        if (Child.IsUint)
            switch (AnalogToDigitalType)
            {
                case AnalogToDigitalType.Trigger:
                case AnalogToDigitalType.JoyHigh:
                    return $"({Child.Generate()}) > {short.MaxValue + Threshold}";
                case AnalogToDigitalType.JoyLow:
                    return $"({Child.Generate()}) < {short.MaxValue - Threshold}";
            }
        else
            switch (AnalogToDigitalType)
            {
                case AnalogToDigitalType.Trigger:
                case AnalogToDigitalType.JoyHigh:
                    return $"({Child.Generate()}) > {Math.Abs(Threshold)}";
                case AnalogToDigitalType.JoyLow:
                    return $"({Child.Generate()}) < {-Math.Abs(Threshold)}";
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
                case AnalogToDigitalType.Trigger:
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
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw)
    {
        Child.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType, usbHostInputsRaw, usbHostRaw, peripheralWtRaw, digitalPeripheral, cloneRaw);
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