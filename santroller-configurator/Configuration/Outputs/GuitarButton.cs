using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Input;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class GuitarButton : OutputButton
{
    public readonly InstrumentButtonType Type;

    public GuitarButton(ConfigViewModel model, bool enabled, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte[] ledIndicesPeripheral, byte[] ledIndicesMpr121, int debounce,
        InstrumentButtonType type, bool outputEnabled, bool outputPeripheral, bool outputInverted, int outputPin,
        bool childOfCombined) : base(model, enabled, input, ledOn, ledOff, ledIndices, ledIndicesPeripheral, ledIndicesMpr121, debounce,
        outputEnabled, outputInverted, outputPeripheral, outputPin,
        childOfCombined)
    {
        Type = type;
        UpdateDetails();
    }
    
    private readonly Dictionary<InstrumentButtonType, Key> _fortniteKeys = new()
    {
        {InstrumentButtonType.Green, Key.D},
        {InstrumentButtonType.Red, Key.F},
        {InstrumentButtonType.Yellow, Key.J},
        {InstrumentButtonType.Blue, Key.K},
        {InstrumentButtonType.Orange, Key.L},
        {InstrumentButtonType.StrumUp, Key.Up},
        {InstrumentButtonType.StrumDown, Key.Down},
    };

    private readonly Dictionary<InstrumentButtonType, object> _fortniteProKeys = new()
    {
        {InstrumentButtonType.Green, Key.D1},
        {InstrumentButtonType.Red, Key.D2},
        {InstrumentButtonType.Yellow, Key.D3},
        {InstrumentButtonType.Blue, Key.D4},
        {InstrumentButtonType.Orange, Key.D5},
        {InstrumentButtonType.StrumUp, Key.RightShift},
        {InstrumentButtonType.StrumDown, "enter"},
    };

    public override string LedOnLabel => Resources.LedColourActiveButtonColour;
    public override string LedOffLabel => Resources.LedColourInactiveButtonColour;

    public override bool IsKeyboard => false;


    public override bool IsStrum => Type is InstrumentButtonType.StrumDown or InstrumentButtonType.StrumUp;

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {

        return EnumToStringConverter.Convert(Type);
    }

    public override Enum GetOutputType()
    {
        return Type;
    }
    public override string GenerateOutput(ConfigField mode)
    {
        return Type switch
        {
            InstrumentButtonType.StrumDown => GetReportField(StandardButtonType.DpadDown),
            InstrumentButtonType.StrumUp => GetReportField(StandardButtonType.DpadUp),
            _ => base.GenerateOutput(mode)
        };
    }

    public override string Generate(ConfigField mode, int debounceIndex, int ledIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        if (!Model.Branded && !Enabled)
        {
            return "";
        }
        if (mode is not ConfigField.Shared) return "";
        // If combined debounce is on, then additionally generate extra logic to ignore this input if the opposite debounce flag is active
        if (Type is InstrumentButtonType.StrumDown or InstrumentButtonType.StrumUp)
        {
            combinedExtra = string.Join(" && ",
                strumIndexes.Distinct().Where(s => s != debounceIndex).Select(x => $"!debounce[{x}]"));
            if (!string.IsNullOrEmpty(combinedExtra))
            {
                combinedExtra = "((!COMBINED_DEBOUNCE) || (" + combinedExtra + "))";
            }
        }
        
        return base.Generate(mode, debounceIndex, ledIndex, extra, combinedExtra, strumIndexes, combinedDebounce, macros, writer);
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedGuitarButton(Input!.Serialise(), Enabled, LedOn, LedOff, LedIndices.ToArray(),
            LedIndicesPeripheral.ToArray(), Debounce, Type, OutputEnabled, OutputPin, OutputInverted, PeripheralOutput,
            ChildOfCombined, LedIndicesMpr121.ToArray());
    }
}
