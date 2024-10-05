using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedGh5CombinedOutput : SerializedOutput
{
    public SerializedGh5CombinedOutput()
    {
        
    }
    public SerializedGh5CombinedOutput(bool peripheral, int sda, int scl, IReadOnlyCollection<Output> outputs)
    {
        Sda = sda;
        Scl = scl;
        Outputs = outputs.Select(s => s.Serialize()).ToList();
        Peripheral = peripheral;
    }

    [ProtoMember(4)] public int Sda { get; }
    [ProtoMember(5)] public int Scl { get; }

    [ProtoMember(6)] public List<SerializedOutput> Outputs { get; } = [];
    [ProtoMember(8)] private bool Peripheral { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new Gh5CombinedOutput(model, Peripheral, Sda, Scl);
        model.Bindings.Add(combined);
        combined.SetOutputsOrDefaults(Outputs.Select(s => s.Generate(model)));
        return combined;
    }
}