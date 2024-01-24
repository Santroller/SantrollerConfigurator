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

    public ControllerButton(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices, byte[] ledIndicesPeripheral,
        int debounce, StandardButtonType type, bool childOfCombined) : base(model, input, ledOn, ledOff, ledIndices, ledIndicesPeripheral,
        debounce, childOfCombined)
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
        if (deviceControllerType.IsFortnite() && !_fortniteKeys.ContainsKey(Type))
        {
            return "";
        }
        return ControllerEnumConverter.Convert(Type, deviceControllerType, legendType, swapSwitchFaceButtons);
    }

    public override Enum GetOutputType()
    {
        return Type;
    }

    public override string GenerateOutput(ConfigField mode)
    {
        if (Model.EmulationType is EmulationType.FortniteFestival && mode is ConfigField.Keyboard && _fortniteKeys.TryGetValue(Type, out var fortniteKey))
        {
            return GetReportField(fortniteKey);
        }
        if (mode is not ConfigField.Ps3 && Type is StandardButtonType.Capture)
        {
            return "";
        }
        return mode is ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4 or ConfigField.Shared or ConfigField.XboxOne
            or ConfigField.Xbox360 or ConfigField.Universal
            ? GetReportField(Type)
            : "";
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedControllerButton(Input.Serialise(), LedOn, LedOff, LedIndices.ToArray(), LedIndicesPeripheral.ToArray(), Debounce, Type,
            ChildOfCombined);
    }
}