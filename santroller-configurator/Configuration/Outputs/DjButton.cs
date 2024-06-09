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

    public DjButton(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte[] ledIndicesPeripheral, byte[] ledIndicesMpr121, int debounce,
        DjInputType type, bool outputEnabled, bool outputPeripheral, bool outputInverted, int outputPin,
        bool childOfCombined) : base(model, input, ledOn, ledOff, ledIndices, ledIndicesPeripheral, ledIndicesMpr121, debounce,
        outputEnabled, outputInverted, outputPeripheral, outputPin,
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
        return GetReportField(Type);
    }

    public override string Generate(ConfigField mode, int debounceIndex, int ledIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        if (mode is not (ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Shared or ConfigField.XboxOne
            or ConfigField.Xbox360 or ConfigField.Ps4 or ConfigField.Universal or ConfigField.Reset or ConfigField.Xbox))
            return "";

        if (mode is ConfigField.Shared)
            return base.Generate(mode, debounceIndex, ledIndex, extra, combinedExtra, strumIndexes, combinedDebounce, macros,
                writer);
        // Turntables also hit the standard buttons when you push each button
        switch (Type)
        {
            case DjInputType.LeftGreen:
            case DjInputType.RightGreen:
                extra = mode == ConfigField.Xbox ? "report->a = 0xFF;" : "report->a = true;";
                break;
            case DjInputType.LeftRed:
            case DjInputType.RightRed:
                extra = mode == ConfigField.Xbox ? "report->b = 0xFF;" : "report->b = true;";
                break;
            case DjInputType.LeftBlue:
            case DjInputType.RightBlue:
                extra = mode == ConfigField.Xbox ? "report->x = 0xFF;" : "report->x = true;";
                break;
            default:
                return "";
        }

        return base.Generate(mode, debounceIndex, ledIndex, extra, combinedExtra, strumIndexes, combinedDebounce, macros, writer);
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
        return new SerializedDjButton(Input.Serialise(), LedOn, LedOff, LedIndices.ToArray(),
            LedIndicesPeripheral.ToArray(), Debounce, Type, OutputEnabled, OutputPin, OutputInverted, PeripheralOutput,
            ChildOfCombined, LedIndicesMpr121.ToArray());
    }
}