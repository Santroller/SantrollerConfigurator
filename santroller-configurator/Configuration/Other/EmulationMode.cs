using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Other;

public partial class EmulationMode : Output
{
    private readonly SourceList<EmulationModeType> _emulationModes = new();
    private EmulationModeType _emulationModeType;

    public EmulationMode(ConfigViewModel model, bool enabled, Input input, EmulationModeType type) : base(
        model, enabled, input, Colors.Black, Colors.Black, [], [], [], false,
        false, false, -1, false)
    {
        Type = type;
        _emulationModes.AddRange(Enum.GetValues<EmulationModeType>());
        _emulationModes.Connect()
            .Filter(this.WhenAnyValue(x => x.Model.DeviceControllerType, x => x.Model.IsPico).Select(CreateFilter))
            .Bind(out var modes)
            .Subscribe();
        EmulationModes = modes;
        UpdateExplanation();
    }

    private void UpdateExplanation()
    {
        Explanation = Type switch
        {
            EmulationModeType.Fnf => Resources.ModeBindingProMode,
            EmulationModeType.FnfHid => Resources.ModeBindingProModeWindows,
            EmulationModeType.FnfLayer => Resources.ModeBindingProModeToggle,
            EmulationModeType.FnfIos => Resources.ModeBindingProModeiOS,
            _ => ""
        };
    }

    [Reactive] private string _explanation = "";

    public ReadOnlyObservableCollection<EmulationModeType> EmulationModes { get; }

    public EmulationModeType Type
    {
        get => _emulationModeType;
        set
        {
            this.RaiseAndSetIfChanged(ref _emulationModeType, value);
            UpdateDetails();
            UpdateExplanation();
            switch (Type)
            {
                // 
                case EmulationModeType.FnfLayer when Input is not MacroInput:
                {
                    SetInput(InputType.MacroInput, null, null, null, null, null, null, null, null);
                    break;
                }
                // Set 6KRO for compatibility
                case EmulationModeType.Fnf:
                    Model.RolloverMode = RolloverMode.SixKro;
                    break;
            }

            var idx = Model.Bindings.Items.IndexOf(this);
            if (idx == -1) return;
            Model.Bindings.Remove(this);
            Model.Bindings.Insert(idx, this);
        }
    }

    public override bool IsCombined => false;
    public override bool IsStrum => false;

    public override bool IsKeyboard => false;
    public virtual bool IsController => false;
    public override bool SupportsLeds => false;
    public override string LedOnLabel => "";
    public override string LedOffLabel => "";

    private static Func<EmulationModeType, bool> CreateFilter(
        (DeviceControllerType deviceControllerType, bool isPico) data)
    {
        return mode => (mode != EmulationModeType.Wii || data.deviceControllerType is DeviceControllerType.RockBandDrums
                           or DeviceControllerType.RockBandGuitar) &&
                       (mode != EmulationModeType.Ps2OnPs3 || data.deviceControllerType is DeviceControllerType.GuitarHeroGuitar) &&
                       (mode != EmulationModeType.Arcade || data.deviceControllerType is DeviceControllerType.GuitarHeroGuitar) &&
                       (mode != EmulationModeType.XboxOne || data.isPico) &&
                       (mode is not (EmulationModeType.Fnf or EmulationModeType.FnfHid or EmulationModeType.FnfIos
                            or EmulationModeType.FnfLayer) || data.deviceControllerType.Is5FretGuitar() ||
                        data.deviceControllerType.IsDrum());
    }

    private string GetDefinition()
    {
        return GetDefinitionFor(Type);
    }

    public static string GetDefinitionFor(EmulationModeType type)
    {
        return type switch
        {
            EmulationModeType.Xbox360 => "XBOX360",
            EmulationModeType.XboxOne => "XBOXONE",
            EmulationModeType.Xbox => "OG_XBOX",
            EmulationModeType.Wii => "WII_RB",
            EmulationModeType.Ps3 => "PS3",
            EmulationModeType.Ps4Or5 => "PS4",
            EmulationModeType.Switch => "SWITCH",
            EmulationModeType.Fnf => "KEYBOARD_MOUSE",
            EmulationModeType.FnfHid => "FNF",
            EmulationModeType.Arcade => "ARCADE",
            EmulationModeType.FnfLayer => "",
            EmulationModeType.FnfIos => "IOS_FESTIVAL",
            EmulationModeType.Ps2OnPs3 => "PS2_ON_PS3",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedEmulationMode(Type, Input.Serialise(), Enabled);
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        var title = Resources.ConsoleModeBindingTitle;
        if (Type is EmulationModeType.Fnf or EmulationModeType.FnfHid or EmulationModeType.FnfIos
            or EmulationModeType.FnfLayer)
        {
            title = Resources.ModeBindingTitle;
        }

        return string.Format(title, EnumToStringConverter.Convert(Type));
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
        if (Type is EmulationModeType.FnfLayer)
        {
            return mode == ConfigField.DetectionFestival
                ? $$"""
                    if ((millis() - last_festival_toggle) > 1000 && {{Input.Generate()}}) {
                        last_festival_toggle = millis();
                        festival_gameplay_mode = !festival_gameplay_mode;
                    }
                    """
                : "";
        }

        return mode == ConfigField.Detection
            ? $$"""
                if ({{Input.Generate()}} && output_console_type != {{GetDefinition()}}) {
                    set_console_type({{GetDefinition()}});
                }
                """
            : "";
    }

    public override void UpdateBindings()
    {
    }

    public override string GenerateOutput(ConfigField mode)
    {
        return "";
    }
}