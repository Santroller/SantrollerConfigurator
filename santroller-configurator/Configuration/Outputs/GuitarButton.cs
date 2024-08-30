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

    public GuitarButton(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte[] ledIndicesPeripheral, byte[] ledIndicesMpr121, int debounce,
        InstrumentButtonType type, bool outputEnabled, bool outputPeripheral, bool outputInverted, int outputPin,
        bool childOfCombined) : base(model, input, ledOn, ledOff, ledIndices, ledIndicesPeripheral, ledIndicesMpr121, debounce,
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

    public override string GenerateOutput(ConfigField mode)
    {
        if (mode is ConfigField.Keyboard)
        {
            if (Model.IsFortniteFestivalPro)
            {
                return _fortniteProKeys.TryGetValue(Type, out var forniteKeyPro) ? GetReportField(forniteKeyPro) : "";
            }
            return _fortniteKeys.TryGetValue(Type, out var forniteKey) ? GetReportField(forniteKey) : "";
        }
        // no mapping for white3 on ps2
        if (mode is ConfigField.Ps2 && Type is InstrumentButtonType.White3)
        {
            return "";
        }
        // PS3 and 360 just set the standard buttons, and rely on the solo flag
        // XB1 however has things broken out
        // For the universal report, we only put standard frets on nav, not solo
        var usesFaceButtons = mode is not (ConfigField.XboxOne or ConfigField.Universal or ConfigField.Ps4 or ConfigField.Shared);
        return Type switch
        {
            InstrumentButtonType.StrumUp => GetReportField(StandardButtonType.DpadUp),
            InstrumentButtonType.StrumDown => GetReportField(StandardButtonType.DpadDown),

            InstrumentButtonType.Green when mode is ConfigField.Universal => GetReportField(StandardButtonType.A),
            InstrumentButtonType.Red when mode is ConfigField.Universal => GetReportField(StandardButtonType.B),
            InstrumentButtonType.Yellow when mode is ConfigField.Universal => GetReportField(StandardButtonType.Y),
            InstrumentButtonType.Blue when mode is ConfigField.Universal => GetReportField(StandardButtonType.X),
            InstrumentButtonType.Orange when mode is ConfigField.Universal => GetReportField(StandardButtonType
                .LeftShoulder),

            InstrumentButtonType.SoloGreen or InstrumentButtonType.Green when usesFaceButtons =>
                GetReportField(StandardButtonType.A),
            InstrumentButtonType.SoloRed or InstrumentButtonType.Red when usesFaceButtons =>
                GetReportField(StandardButtonType.B),
            InstrumentButtonType.SoloYellow or InstrumentButtonType.Yellow when usesFaceButtons =>
                GetReportField(StandardButtonType.Y),
            InstrumentButtonType.SoloBlue or InstrumentButtonType.Blue when usesFaceButtons =>
                GetReportField(StandardButtonType.X),
            InstrumentButtonType.SoloOrange or InstrumentButtonType.Orange when usesFaceButtons =>
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
        if (deviceControllerType.IsFortnite())
        {
            return Resources.ResourceManager.GetString("Fortnite" + Type, Resources.Culture) ?? "";
        }

        return EnumToStringConverter.Convert(Type);
    }

    public override Enum GetOutputType()
    {
        return Type;
    }

    public override string Generate(ConfigField mode, int debounceIndex, int ledIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        if (mode is not (ConfigField.Shared or ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4
            or ConfigField.Xbox360
            or ConfigField.Universal or ConfigField.Keyboard
            or ConfigField.XboxOne or ConfigField.Reset or ConfigField.Xbox or ConfigField.Wii or ConfigField.Ps2 or ConfigField.Festival)) return "";
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

        if (mode is ConfigField.Shared && Model.DeviceControllerType is DeviceControllerType.FortniteGuitarStrum)
        {
            if (Type is InstrumentButtonType.Green or InstrumentButtonType.Red or InstrumentButtonType.Yellow
                or InstrumentButtonType.Blue or InstrumentButtonType.Orange)
            {
                combinedExtra = string.Join(" || ", strumIndexes.Distinct().Select(x => $"debounce[{x}]"));
            }
        }

        // GHL Guitars map strum up and strum down to dpad up and down, and also the stick
        if (Model.DeviceControllerType is DeviceControllerType.LiveGuitar &&
            Type is InstrumentButtonType.StrumDown or InstrumentButtonType.StrumUp &&
            mode is ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4 or ConfigField.Xbox360 or ConfigField.Xbox)
            return base.Generate(mode, debounceIndex, ledIndex,
                $"report->strumBar={(Type is InstrumentButtonType.StrumDown ? "0xFF" : "0x00")};", combinedExtra,
                strumIndexes, combinedDebounce, macros, writer);

        // XB1 also needs to set the normal face buttons, which can conveniently be done using the PS3 format
        if (mode is ConfigField.XboxOne or ConfigField.Ps4 && Type is not (InstrumentButtonType.StrumUp or InstrumentButtonType.StrumDown))
            extra = $"{GenerateOutput(ConfigField.Ps3)}=true;";
        
        if (Model is not {DeviceControllerType: DeviceControllerType.RockBandGuitar} || mode is ConfigField.Wii or ConfigField.Ps2)
            return base.Generate(mode, debounceIndex, ledIndex, extra, combinedExtra, strumIndexes, combinedDebounce, macros,
                writer);

        //This stuff is only relevant for rock band guitars
        // Set solo flag (not relevant for universal)
        if (Type is InstrumentButtonType.SoloBlue or InstrumentButtonType.SoloGreen
                or InstrumentButtonType.SoloOrange or InstrumentButtonType.SoloRed
                or InstrumentButtonType.SoloYellow && mode is not (ConfigField.Shared or ConfigField.Universal or ConfigField.Ps2))
            extra += "report->solo=true;";
        
        return base.Generate(mode, debounceIndex, ledIndex, extra, combinedExtra, strumIndexes, combinedDebounce, macros, writer);
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedRbButton(Input!.Serialise(), LedOn, LedOff, LedIndices.ToArray(),
            LedIndicesPeripheral.ToArray(), Debounce, Type, OutputEnabled, OutputPin, OutputInverted, PeripheralOutput,
            ChildOfCombined, LedIndicesMpr121.ToArray());
    }
}
