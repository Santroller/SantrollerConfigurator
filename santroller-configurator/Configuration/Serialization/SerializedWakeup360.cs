using System;
using System.ComponentModel;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedWakeup360 : SerializedOutput
{
    public SerializedWakeup360()
    {
        
    }
    public SerializedWakeup360(SerializedInput input)
    {
        Input = input;
    }

    [ProtoMember(1)] public SerializedInput? Input { get; }
    [ProtoMember(2)] [DefaultValue(true)] private bool Enabled { get; } = true;

    public override Output Generate(ConfigViewModel model)
    {
        if (Input == null) throw new InvalidOperationException("Input was null!");
        var output = new Wakeup360(model, Enabled,Input.Generate(model));
        model.Bindings.Add(output);
        return output;
    }
}