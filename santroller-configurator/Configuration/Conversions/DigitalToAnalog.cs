using System;
using System.Collections.Generic;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Conversions;

public class DigitalToAnalog : Input
{
    private readonly bool _trigger;

    public DigitalToAnalog(Input child, int on, bool trigger, ConfigViewModel model) : base(model)
    {
        _trigger = trigger;
        Child = child;
        On = on;
        Tilt = false;
        this.WhenAnyValue(x => x.Child.RawValue).Subscribe(s => RawValue = s > 0 ? On : 0);
        if (trigger)
        {
            Minimum = ushort.MinValue;
            Maximum = ushort.MaxValue;
        }
        else
        {
            Minimum = short.MinValue;
            Maximum = short.MaxValue;
        }

        IsAnalog = Child.IsAnalog;
    }
    public override bool Peripheral => Child.Peripheral;

    public DigitalToAnalog(Input child, ConfigViewModel model) : base(model)
    {
        _trigger = false;
        Child = child;
        On = 32767;
        Tilt = true;
        this.WhenAnyValue(x => x.Child.RawValue).Subscribe(s => RawValue = s > 0 ? On : 0);

        Minimum = short.MinValue;
        Maximum = short.MaxValue;
        IsAnalog = Child.IsAnalog;
    }

    public Input Child { get; }
    public int On { get; set; }
    public bool Tilt { get; }
    public int Minimum { get; }
    public int Maximum { get; }

    public override IList<DevicePin> Pins => Child.Pins;
    public override IList<PinConfig> PinConfigs => Child.PinConfigs;
    public override InputType? InputType => Child.InputType;
    public override bool IsUint => _trigger;

    public override string Title => Child.Title;

    public override string Generate()
    {
        return Child.Generate();
    }

    public override SerializedInput Serialise()
    {
        return new SerializedDigitalToAnalog(Child.Serialise(), On, _trigger, Tilt);
    }

    public override Input InnermostInput()
    {
        return Child;
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, Dictionary<int, int> analogPeripheral, ReadOnlySpan<byte> cloneRaw)
    {
        Child.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType, usbHostInputsRaw, usbHostRaw, peripheralWtRaw, digitalPeripheral, analogPeripheral, cloneRaw);
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        throw new InvalidOperationException("Never call GenerateAll on DigitalToAnalog, call it on its children");
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return Child.RequiredDefines();
    }
}