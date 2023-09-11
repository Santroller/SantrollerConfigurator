using System;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedMacroInput : SerializedInput
{
    public SerializedMacroInput(SerializedInput child1, SerializedInput child2)
    {
        Child1 = child1;
        Child2 = child2;
    }

    [ProtoMember(1)] public SerializedInput Child1 { get; }
    [ProtoMember(2)] public SerializedInput Child2 { get; }

    public override Input Generate(ConfigViewModel model)
    {
        // Need to make sure a binding exists so that the inputs generated below have an output to grab their pins from (combined only)
        model.Bindings.Add(new ControllerButton(model, Child1.Generate(model), Colors.Transparent, Colors.Transparent, Array.Empty<byte>(), 10, StandardButtonType.A, false));
        return new MacroInput(Child1.Generate(model), Child2.Generate(model), model);
    }
}