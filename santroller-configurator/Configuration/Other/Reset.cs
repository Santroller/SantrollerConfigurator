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

namespace GuitarConfigurator.NetCore.Configuration.Other;

public class Reset : Output
{

    public Reset(ConfigViewModel model, Input input) : base(
        model, input, Colors.Black, Colors.Black, Array.Empty<byte>(), false)
    {
    }

    public override bool IsCombined => false;
    public override bool IsStrum => false;

    public override bool IsKeyboard => false;
    public virtual bool IsController => false;
    public override string LedOnLabel => "";
    public override string LedOffLabel => "";


    public override SerializedOutput Serialize()
    {
        return new SerializedReset(Input.Serialise());
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return "Reset";
    }

    public override Enum GetOutputType()
    {
        return SimpleType.Reset;
    }

    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        return mode != ConfigField.Detection
            ? ""
            : $$"""
                if ({{Input.Generate()}}) {
                    set_console_type(UNIVERSAL);
                }
                """;
    }

    public override void UpdateBindings()
    {
    }
}