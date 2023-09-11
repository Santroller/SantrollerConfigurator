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

public class GuitarButton : OutputButton
{
    public readonly InstrumentButtonType Type;

    public GuitarButton(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices, byte debounce,
        InstrumentButtonType type, bool childOfCombined) : base(model, input, ledOn, ledOff, ledIndices, debounce,
        childOfCombined)
    {
        Type = type;
        UpdateDetails();
    }

    public override string LedOnLabel => Resources.LedColourActiveButtonColour;
    public override string LedOffLabel => Resources.LedColourInactiveButtonColour;

    public override bool IsKeyboard => false;


    public override bool IsStrum => Type is InstrumentButtonType.StrumDown or InstrumentButtonType.StrumUp;

    public override string GenerateOutput(ConfigField mode)
    {
        // PS3 and 360 just set the standard buttons, and rely on the solo flag
        // XB1 however has things broken out
        // For the universal report, we only put standard frets on nav, not solo
        
        var requiresFaceButtons = mode is not (ConfigField.XboxOne or ConfigField.Universal);
        return Type switch
        {
            InstrumentButtonType.StrumUp => GetReportField(StandardButtonType.DpadUp),
            InstrumentButtonType.StrumDown => GetReportField(StandardButtonType.DpadDown),
            
            InstrumentButtonType.Green when mode is ConfigField.Universal => GetReportField(StandardButtonType.A),
            InstrumentButtonType.Red when mode is ConfigField.Universal => GetReportField(StandardButtonType.B),
            InstrumentButtonType.Yellow when mode is ConfigField.Universal => GetReportField(StandardButtonType.Y),
            InstrumentButtonType.Blue when mode is ConfigField.Universal => GetReportField(StandardButtonType.X),
            InstrumentButtonType.Orange when mode is ConfigField.Universal => GetReportField(StandardButtonType.LeftShoulder),
            
            InstrumentButtonType.SoloGreen or InstrumentButtonType.Green when requiresFaceButtons =>
                GetReportField(StandardButtonType.A),
            InstrumentButtonType.SoloRed or InstrumentButtonType.Red when requiresFaceButtons =>
                GetReportField(StandardButtonType.B),
            InstrumentButtonType.SoloYellow or InstrumentButtonType.Yellow when requiresFaceButtons =>
                GetReportField(StandardButtonType.Y),
            InstrumentButtonType.SoloBlue or InstrumentButtonType.Blue when requiresFaceButtons =>
                GetReportField(StandardButtonType.X),
            InstrumentButtonType.SoloOrange or InstrumentButtonType.Orange when requiresFaceButtons =>
                GetReportField(StandardButtonType.LeftShoulder),
            
            InstrumentButtonType.Black1 => GetReportField(StandardButtonType.A),
            InstrumentButtonType.Black2 => GetReportField(StandardButtonType.B),
            InstrumentButtonType.White1 => GetReportField(StandardButtonType.X),
            InstrumentButtonType.Black3 => GetReportField(StandardButtonType.Y),
            InstrumentButtonType.White2 => GetReportField(StandardButtonType.LeftShoulder),
            InstrumentButtonType.White3 => GetReportField(StandardButtonType.RightShoulder),
            _ => GetReportField(Type)
        };
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

    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        if (mode is not (ConfigField.Shared or ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4 or ConfigField.Xbox360
            or ConfigField.Universal
            or ConfigField.XboxOne)) return "";
        // If combined debounce is on, then additionally generate extra logic to ignore this input if the opposite debounce flag is active
        if (combinedDebounce.Any() && Type is InstrumentButtonType.StrumDown or InstrumentButtonType.StrumUp)
            combinedExtra = string.Join(" && ",
                combinedDebounce.Where(s => s != debounceIndex).Select(x => $"!debounce[{x}]"));
        // GHL Guitars map strum up and strum down to dpad up and down, and also the stick
        if (Model.DeviceControllerType is DeviceControllerType.LiveGuitar &&
            Type is InstrumentButtonType.StrumDown or InstrumentButtonType.StrumUp &&
            mode is ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4 or ConfigField.Xbox360)
            return base.Generate(mode, debounceIndex,
                $"report->strumBar={(Type is InstrumentButtonType.StrumDown ? "0xFF" : "0x00")};", combinedExtra,
                combinedDebounce, macros, writer);

        if (Model is not {DeviceControllerType: DeviceControllerType.RockBandGuitar})
            return base.Generate(mode, debounceIndex, "", combinedExtra, combinedDebounce, macros, writer);

        //This stuff is only relevant for rock band guitars
        // Set solo flag (not relevant for universal)
        if (Type is InstrumentButtonType.SoloBlue or InstrumentButtonType.SoloGreen
                or InstrumentButtonType.SoloOrange or InstrumentButtonType.SoloRed
                or InstrumentButtonType.SoloYellow && mode is not (ConfigField.Shared or ConfigField.Universal))
            extra = "report->solo=true;";
        // XB1 also needs to set the normal face buttons, which can conveniently be done using the PS3 format
        if (mode is ConfigField.XboxOne && Type is not (InstrumentButtonType.StrumUp or InstrumentButtonType.StrumDown))
            extra += $"{GenerateOutput(ConfigField.Ps3)}=true;";


        return base.Generate(mode, debounceIndex, extra, combinedExtra, combinedDebounce, macros, writer);
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedRbButton(Input!.Serialise(), LedOn, LedOff, LedIndices.ToArray(), Debounce, Type,
            ChildOfCombined);
    }
}