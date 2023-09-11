using System;
using System.Collections.Generic;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class ConstantInput : FixedInput
{
    public ConstantInput(ConfigViewModel model, int value, bool analog, int min, int max, bool tapBar, bool rbPickup) : base(model, value, analog)
    {
        Min = min;
        Max = max;
        TapBar = tapBar;
        RbPickup = rbPickup;
        this.WhenAnyValue(x => x.Value).Subscribe(v => RawValue = v);
    }
    
    public IEnumerable<PickupSelectorType> PickupSelectorTypes => Enum.GetValues<PickupSelectorType>();
    
    public bool TapBar { get; }
    public bool RbPickup { get; }

    public bool ValueBool
    {
        get => Value != 0;
        set
        {
            if (!IsAnalog)
            {
                Value = value ? 1 : 0;
            }
        }
    }

    public PickupSelectorType PickupSelectorType
    {
        get => GuitarAxis.GetPickupSelectorValue(Value);
        set
        {
            Value = ((int) (value + 1) * 51) << 8;
            this.RaisePropertyChanged();
        }
    }

    private BarButton BarButton => Gh5NeckInput.Gh5Mappings.GetValueOrDefault(Value, (BarButton) 0);

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
                Value = CalculateValue(BarButton.Green, value);
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
                Value = CalculateValue(BarButton.Red, value);
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
                Value = CalculateValue(BarButton.Yellow, value);
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
                Value = CalculateValue(BarButton.Blue, value);
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
                Value = CalculateValue(BarButton.Orange, value);
            }
        }
    }

    public bool Normal => !TapBar && !RbPickup && IsAnalog;
    
    public int Min { get; }
    public int Max { get; }
    public override SerializedInput Serialise()
    {
        return new SerializedConstantInput(Value, IsAnalog, Min, Max, TapBar, RbPickup);
    }
}