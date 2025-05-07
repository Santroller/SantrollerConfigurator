using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Other;

public class Wakeup360 : Output
{
    public Wakeup360(ConfigViewModel model, bool enabled, Input input) : base(
        model, enabled, input, Colors.Black, Colors.Black, [], [], [], false, false, false, -1,
        false)
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
        return new SerializedWakeup360(Input.Serialise());
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return Resources.SimpleTypeWakeup360;
    }

    public override Enum GetOutputType()
    {
        return SimpleType.Wakeup360;
    }

    public override string Generate(ConfigField mode, int debounceIndex, int ledIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        return mode == ConfigField.Shared
            ? $$"""
                if ({{Input.Generate(writer)}}) {
                    wakeup_360();
                }
                """
            : "";
    }

    public override string GenerateOutput(ConfigField mode)
    {
        return "";
    }

    public override void UpdateBindings()
    {
    }
}