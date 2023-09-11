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

public class MouseButton : OutputButton
{
    public MouseButton(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices, byte debounce,
        MouseButtonType type) : base(model, input, ledOn, ledOff, ledIndices, debounce, false)
    {
        Type = type;
        UpdateDetails();
    }

    public override bool IsKeyboard => true;
    public virtual bool IsController => false;

    public MouseButtonType Type { get; }
    public override bool IsStrum => false;

    public override bool IsCombined => false;

    public override void UpdateBindings()
    {
    }

    public override string GenerateOutput(ConfigField mode)
    {
        if (mode != ConfigField.Mouse) return "";
        return GetReportField(Type);
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
        return mode is not (ConfigField.Mouse or ConfigField.Shared)
            ? ""
            : base.Generate(mode, debounceIndex, extra, combinedExtra, combinedDebounce, macros, writer);
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedMouseButton(Input.Serialise(), LedOn, LedOff, LedIndices.ToArray(), Debounce, Type);
    }
}