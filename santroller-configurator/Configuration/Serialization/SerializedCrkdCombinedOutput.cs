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
public class SerializedCrkdCombinedOutput : SerializedOutput
{
    public SerializedCrkdCombinedOutput()
    {
        
    }
    public SerializedCrkdCombinedOutput(bool peripheral, int tx, int rx, IReadOnlyCollection<Output> outputs)
    {
        Tx = tx;
        Rx = rx;
        Outputs = outputs.Select(s => s.Serialize()).ToList();
        Peripheral = peripheral;
    }

    [ProtoMember(4)] public int Tx { get; }
    [ProtoMember(5)] public int Rx { get; }

    [ProtoMember(6)] public List<SerializedOutput> Outputs { get; } = [];
    [ProtoMember(8)] private bool Peripheral { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new CrkdCombinedOutput(model, Peripheral, Tx, Rx);
        model.Bindings.Add(combined);
        combined.SetOutputsOrDefaults(Outputs.Select(s => s.Generate(model)));
        return combined;
    }
}