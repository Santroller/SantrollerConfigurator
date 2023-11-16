using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Other;

public class Rumble : Output
{
    private int _pin;

    private RumbleMotorType _rumbleMotorType;

    public Rumble(ConfigViewModel model, int pin, bool peripheral, RumbleMotorType rumbleMotorType) : base(model,
        new FixedInput(model, 0, false), Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), false)
    {
        PinConfig = new DirectPinConfig(model, Guid.NewGuid().ToString(), pin, peripheral, DevicePinMode.Output);
        Pin = pin;
        RumbleMotorType = rumbleMotorType;
        this.WhenAnyValue(x => x.PinConfig.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Pin)));
    }

    public List<int> AvailablePins => Model.Microcontroller.PwmPins;

    public DirectPinConfig PinConfig { get; }

    public int Pin
    {
        get => _pin;
        set
        {
            this.RaiseAndSetIfChanged(ref _pin, value);
            PinConfig.Pin = value;
        }
    }

    public RumbleMotorType RumbleMotorType
    {
        get => _rumbleMotorType;
        set
        {
            this.RaiseAndSetIfChanged(ref _rumbleMotorType, value);
            UpdateDetails();
        }
    }

    public IEnumerable<RumbleMotorType> RumbleMotorTypes => Enum.GetValues<RumbleMotorType>();

    public override bool IsCombined => false;
    public override bool IsStrum => false;
    public override string LedOnLabel => "";

    public override string LedOffLabel => "";

    public override bool SupportsLedOff => false;

    public override bool IsKeyboard => false;
    public virtual bool IsController => false;

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return string.Format(Resources.RumbleCommandTitle, EnumToStringConverter.Convert(RumbleMotorType));
    }

    protected override IEnumerable<PinConfig> GetOwnPinConfigs()
    {
        return new[] {PinConfig};
    }

    protected override IEnumerable<DevicePin> GetOwnPins()
    {
        return new List<DevicePin>
        {
            new(Pin, DevicePinMode.Output)
        };
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedRumble(RumbleMotorType, Pin, PinConfig.Peripheral);
    }

    public override Enum GetOutputType()
    {
        return RumbleMotorType;
    }

    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        return mode is not ConfigField.RumbleLed
            ? ""
            : Model.Microcontroller.GenerateAnalogWrite(Pin,
                RumbleMotorType == RumbleMotorType.Left ? "rumble_left" : "rumble_right", PinConfig.Peripheral);
    }

    public override void UpdateBindings()
    {
    }
}