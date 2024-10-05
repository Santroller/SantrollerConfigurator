using System;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedReset : SerializedOutput
{
    public SerializedReset()
    {
        
    }
    public SerializedReset(SerializedInput input)
    {
        Input = input;
    }

    [ProtoMember(1)] public SerializedInput? Input { get; }
    [ProtoMember(2)] private bool Enabled { get; } = true;

    public override Output Generate(ConfigViewModel model)
    {
        if (Input == null) throw new InvalidOperationException("Input was null!");
        var output = new Reset(model, Enabled,Input.Generate(model));
        model.Bindings.Add(output);
        return output;
    }
}