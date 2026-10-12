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
public class SerializedBhDrumCombinedOutput : SerializedOutput
{
    // ReSharper disable once UnusedMember.Global
    public SerializedBhDrumCombinedOutput()
    {
    }

    public SerializedBhDrumCombinedOutput(int sda, int scl, IReadOnlyCollection<Output> outputs)
    {
        Sda = sda;
        Scl = scl;
        Outputs = outputs.Select(s => s.Serialize()).ToList();
        Enabled = GetBytes(new BitArray(outputs.Select(s => s.Enabled).ToArray()));
    }

    [ProtoMember(1)] public int Sda { get; }
    [ProtoMember(2)] public int Scl { get; }
    [ProtoMember(3)] public List<SerializedOutput> Outputs { get; } = [];
    [ProtoMember(4)] public byte[] Enabled { get; } = Array.Empty<byte>();

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new BhDrumCombinedOutput(model, Sda, Scl);
        model.Bindings.Add(combined);
        var array = new BitArray(Enabled);
        var outputs = Outputs.Select(s => s.Generate(model)).ToList();
        for (var i = 0; i < outputs.Count; i++) outputs[i].Enabled = array[i];
        combined.SetOutputsOrDefaults(outputs);
        return combined;
    }
}
