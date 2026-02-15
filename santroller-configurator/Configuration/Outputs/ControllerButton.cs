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

public class ControllerButton : OutputButton
{
    public ControllerButton(ConfigViewModel model, bool enabled, Input input, Color ledOn, Color ledOff,
        byte[] ledIndices,
        byte[] ledIndicesPeripheral, byte[] ledIndicesMpr121, int debounce, StandardButtonType type, bool outputEnabled,
        bool outputPeripheral, bool outputInverted, int outputPin, bool childOfCombined) : base(model, enabled, input,
        ledOn,
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
        { StandardButtonType.Back, Key.Space },
        { StandardButtonType.DpadUp, Key.Up },
        { StandardButtonType.DpadDown, Key.Down },
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

    public override string Generate(ConfigField mode, int debounceIndex, int ledIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes, bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros,
        BinaryWriter? writer)
    {
        // RB / Festival can use dpad left for star power activation, which works better in some cases
        if (mode is ConfigField.XboxOne or ConfigField.Ps4 && Type is StandardButtonType.Back &&
            Model.DeviceControllerType.Is5FretGuitar())
        {
            return $$"""
                          if (MAP_XBOX_ONE_SELECT_DPAD_LEFT) {
                              {{base.Generate(mode, debounceIndex, ledIndex, extra, combinedExtra, strumIndexes, combinedDebounce, macros, writer)}}    
                          } else {
                              {{base.Generate(ConfigField.Xbox360, debounceIndex, ledIndex, extra, combinedExtra, strumIndexes, combinedDebounce, macros, writer)}}
                          }
                     """;
        }

        return base.Generate(mode, debounceIndex, ledIndex, extra, combinedExtra, strumIndexes, combinedDebounce,
            macros, writer);
    }

    public override string GenerateOutput(ConfigField mode)
    {
        // No guide button on og xbox or PS2
        if (mode is ConfigField.Xbox or ConfigField.Ps2 && Type is StandardButtonType.Guide)
        {
            return "";
        }

        if (mode is ConfigField.XboxOne or ConfigField.Ps4 && Type is StandardButtonType.Back &&
            Model.DeviceControllerType.Is5FretGuitar())
        {
            return GetReportField(StandardButtonType.DpadLeft);
        }

        // No dpad left or right on ps2 guitars
        if (mode is ConfigField.Ps2 && Type is StandardButtonType.DpadLeft or StandardButtonType.DpadRight &&
            Model.DeviceControllerType.IsGuitar())
        {
            return "";
        }

        // No thumb click on wii
        if (mode is ConfigField.Wii && Type is StandardButtonType.LeftThumbClick or StandardButtonType.RightThumbClick)
        {
            return "";
        }

        // capture button only exists on switch (which uses ps3 mappings) and ps4/5
        if (mode is not (ConfigField.Ps3 or ConfigField.Ps4) && Type is StandardButtonType.Capture)
        {
            return "";
        }

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

        if (mode is ConfigField.Wii && Model.DeviceControllerType is DeviceControllerType.Turntable)
        {
            // wii doesn't have nav buttons, so map nav to left platter (y is euphoria so that is on the turntable.)
            switch (Type)
            {
                case StandardButtonType.A:
                    return GetReportField(DjInputType.LeftGreen);
                case StandardButtonType.B:
                    return GetReportField(DjInputType.LeftRed);
                case StandardButtonType.X:
                    return GetReportField(DjInputType.LeftBlue);
            }
        }

        return mode is ConfigField.Ps3 or ConfigField.Xbox or ConfigField.Ps3WithoutCapture or ConfigField.Ps4
            or ConfigField.Shared or ConfigField.XboxOne
            or ConfigField.Xbox360 or ConfigField.Universal or ConfigField.Wii or ConfigField.Ps2
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