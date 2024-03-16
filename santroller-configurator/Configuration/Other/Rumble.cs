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
    private RumbleMotorType _rumbleMotorType;

    public Rumble(ConfigViewModel model, int pin, bool peripheral, RumbleMotorType rumbleMotorType) : base(model,
        new FixedInput(model, 0, false), Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), true, false, peripheral, pin, false)
    {
        RumbleMotorType = rumbleMotorType;
    }

    public override bool UsesPwm => true;

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

    public override SerializedOutput Serialize()
    {
        return new SerializedRumble(RumbleMotorType, OutputPin, PeripheralOutput);
    }

    public override Enum GetOutputType()
    {
        return RumbleMotorType;
    }

    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        return mode is not ConfigField.RumbleLed
            ? ""
            : Model.Microcontroller.GenerateAnalogWrite(OutputPin,
                RumbleMotorType == RumbleMotorType.Left ? "rumble_left" : "rumble_right", PeripheralOutput);
    }

    public override void UpdateBindings()
    {
    }
}