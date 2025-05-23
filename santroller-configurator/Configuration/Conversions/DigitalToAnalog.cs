using System;
using System.Collections.Generic;
using System.IO;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Conversions;

public class DigitalToAnalog : Input
{
    private readonly bool _trigger;
    public override bool Peripheral => Child.Peripheral;

    public DigitalToAnalog(Input child, int on, ConfigViewModel model, DigitalToAnalogType type) : base(model)
    {
        Type = type;
        _trigger = type is DigitalToAnalogType.Trigger;
        Child = child;
        On = type is DigitalToAnalogType.Tilt ? 32767: on;
        this.WhenAnyValue(x => x.Child.RawValue).Subscribe(s => RawValue = s > 0 ? On : 0);
        if (_trigger)
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

    public DigitalToAnalogType Type { get; }
    public Input Child { get; }
    public int On { get; set; }
    public bool Tilt => Type is DigitalToAnalogType.Tilt;

    public bool TapBar => Type is DigitalToAnalogType.TapBar;
    public bool RbPickup => Type is DigitalToAnalogType.Pickup;
    
    public bool Normal => Type is DigitalToAnalogType.Normal;

    public int PickupSelectorType
    {
        get => Math.Min((On / (ushort.MaxValue / 5)) + 1, 5);
        set
        {
            On = (value-1) * (ushort.MaxValue / 5);
            this.RaisePropertyChanged();
        }
    }

    private BarButton BarButton => Gh5NeckInput.Gh5Mappings.GetValueOrDefault(On, (BarButton) 0);

    private int CalculateValue(BarButton input, bool on)
    {
        var current = BarButton;
        if (on)
        {
            current |= input;
        }
        else
        {
            current &= ~input;
        }

        return current == 0 ? 0 : Gh5NeckInput.Gh5MappingsReversed[current];
    }
    
    public bool Green
    {
        get => BarButton.HasFlag(BarButton.Green);
        set
        {
            if (TapBar)
            {
                On = CalculateValue(BarButton.Green, value);
            }
        }
    }
    
    public bool Red
    {
        get => BarButton.HasFlag(BarButton.Red);
        set
        {
            if (TapBar)
            {
                On = CalculateValue(BarButton.Red, value);
            }
        }
    }
    
    public bool Yellow
    {
        get => BarButton.HasFlag(BarButton.Yellow);
        set
        {
            if (TapBar)
            {
                On = CalculateValue(BarButton.Yellow, value);
            }
        }
    }
    
    public bool Blue
    {
        get => BarButton.HasFlag(BarButton.Blue);
        set
        {
            if (TapBar)
            {
                On = CalculateValue(BarButton.Blue, value);
            }
        }
    }
    
    public bool Orange
    {
        get => BarButton.HasFlag(BarButton.Orange);
        set
        {
            if (TapBar)
            {
                On = CalculateValue(BarButton.Orange, value);
            }
        }
    }

    public bool ConfigurableValue => !Tilt && !TapBar && !RbPickup;
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
        return new SerializedDigitalToAnalog(Child.Serialise(), On, _trigger, Type);
    }

    public override IEnumerable<Input> InnermostInputs()
    {
        return Child.InnermostInputs();
    }
    
    public override IList<Input> Inputs()
    {
        return new List<Input> {this, Child};
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw,
        ReadOnlySpan<byte> mpr121Raw, ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw,
        bool peripheralConnected)
    {
        Child.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType, usbHostInputsRaw, usbHostRaw, peripheralWtRaw, digitalPeripheral, cloneRaw, adxlRaw, mpr121Raw, midiRaw, bluetoothInputsRaw, peripheralConnected);
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