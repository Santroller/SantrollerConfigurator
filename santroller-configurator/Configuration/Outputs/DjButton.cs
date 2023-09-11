using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class DjButton : OutputButton
{
    public readonly DjInputType Type;

    public Dictionary<DjInputType, DjInputType> DualTypes = new()
    {
        {DjInputType.RightGreen, DjInputType.LeftGreen},
        {DjInputType.RightRed, DjInputType.LeftRed},
        {DjInputType.RightBlue, DjInputType.LeftBlue},
    };

    public DjButton(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices, byte debounce,
        DjInputType type, bool childOfCombined) : base(model, input, ledOn, ledOff, ledIndices, debounce,
        childOfCombined)
    {
        Type = type;
        UpdateDetails();
    }

    public override string LedOnLabel => Resources.LedColourActiveButtonColour;
    public override string LedOffLabel => Resources.LedColourInactiveButtonColour;

    public override bool IsKeyboard => false;
    public override bool IsStrum => false;

    public override string GenerateOutput(ConfigField mode)
    {
        if (Model.DjDual && DualTypes.TryGetValue(Type, out var type))
        {
            return GetReportField(type);
        }
        return GetReportField(Type);
    }

    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        if (mode is not (ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Shared or ConfigField.XboxOne or ConfigField.Xbox360 or ConfigField.Ps4 or ConfigField.Universal))
            return "";
        if (mode is not ConfigField.Shared) {
            // Turntables also hit the standard buttons when you push each button
            switch (Type)
            {
                case DjInputType.LeftGreen:
                case DjInputType.RightGreen:
                    extra = "report->a = true;";
                    break;
                case DjInputType.LeftRed:
                case DjInputType.RightRed:
                    extra = "report->b = true;";
                    break;
                case DjInputType.LeftBlue:
                case DjInputType.RightBlue:
                    extra = "report->x = true;";
                    break;
                default:
                    return "";
            }
        }

        return base.Generate(mode, debounceIndex, extra, combinedExtra, combinedDebounce, macros, writer);
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return EnumToStringConverter.Convert(Type);
    }

    public override Enum GetOutputType()
    {
        return Type;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedDjButton(Input.Serialise(), LedOn, LedOff, LedIndices.ToArray(), Debounce, Type,
            ChildOfCombined);
    }
}
