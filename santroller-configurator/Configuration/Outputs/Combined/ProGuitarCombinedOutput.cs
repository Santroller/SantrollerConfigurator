using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public class ProGuitarCombinedOutput : CombinedOutput
{
    public ProGuitarCombinedOutput(ConfigViewModel model) : base(
        model)
    {
        UpdateDetails();
    }
    public override SerializedOutput Serialize()
    {
        return new SerializedProGuitarCombinedOutput();
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return Resources.ProGuitarInputsTitle;
    }

    public override Enum GetOutputType()
    {
        return SimpleType.ProGuitar;
    }

    public override void SetOutputsOrDefaults(IEnumerable<Output> outputs)
    {
        Outputs.Clear();
    }

    public void UpdateDefaults()
    {
    }

    public void CreateDefaults()
    {
        Outputs.Clear();
    }

    public override void UpdateBindings()
    {
        CreateDefaults();
    }
}