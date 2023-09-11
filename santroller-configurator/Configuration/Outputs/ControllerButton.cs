using System;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class ControllerButton : OutputButton
{

    public ControllerButton(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte debounce, StandardButtonType type, bool childOfCombined) : base(model, input, ledOn, ledOff, ledIndices,
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
        return new SerializedControllerButton(Input.Serialise(), LedOn, LedOff, LedIndices.ToArray(), Debounce, Type,
            ChildOfCombined);
    }
}