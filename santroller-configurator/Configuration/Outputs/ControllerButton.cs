using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class ControllerButton : OutputButton
{
    public ControllerButton(ConfigViewModel model, bool enabled, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte[] ledIndicesPeripheral, byte[] ledIndicesMpr121, int debounce, StandardButtonType type, bool outputEnabled,
        bool outputPeripheral, bool outputInverted, int outputPin, bool childOfCombined) : base(model, enabled, input, ledOn,
        ledOff, ledIndices, ledIndicesPeripheral, ledIndicesMpr121, debounce, outputEnabled, outputInverted,
        outputPeripheral, outputPin, childOfCombined)
    {
        Type = type;
        UpdateDetails();
    }

    public StandardButtonType Type { get; }

    public override bool IsKeyboard => false;

    public override bool IsStrum => Type is StandardButtonType.DpadUp or StandardButtonType.DpadDown;

    public override bool IsCombined => false;
    public override string LedOnLabel => Resources.LedColourActiveButtonColour;
    public override string LedOffLabel => Resources.LedColourInactiveButtonColour;

    private readonly Dictionary<StandardButtonType, Key> _fortniteKeys = new()
    {
        {StandardButtonType.Back, Key.Space},
        {StandardButtonType.DpadUp, Key.Up},
        {StandardButtonType.DpadDown, Key.Down},
    };

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return ControllerEnumConverter.Convert(Type, deviceControllerType, legendType, swapSwitchFaceButtons);
    }

    public override Enum GetOutputType()
    {
        return Type;
    }

    public override string GenerateOutput(ConfigField mode)
    {
        if (Model.IsFortniteFestivalPro && mode is ConfigField.Keyboard)
        {
            switch (Type)
            {
                case StandardButtonType.Back:
                    return GetReportField(Key.PageDown);
                case StandardButtonType.Start:
                    return GetReportField(Key.Escape);
                case StandardButtonType.DpadLeft:
                    return GetReportField(Key.Left);
                case StandardButtonType.DpadRight:
                    return GetReportField(Key.Right);
            }
        }

        return mode is ConfigField.Shared
            ? GetReportField(Type)
            : "";
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedControllerButton(Input.Serialise(), Enabled, LedOn, LedOff, LedIndices.ToArray(),
            LedIndicesPeripheral.ToArray(), Debounce, Type, OutputEnabled, OutputPin, OutputInverted, PeripheralOutput,
            ChildOfCombined, LedIndicesMpr121.ToArray());
    }
}
