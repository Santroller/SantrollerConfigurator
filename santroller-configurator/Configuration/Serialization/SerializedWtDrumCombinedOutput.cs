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
public class SerializedWtDrumCombinedOutput : SerializedOutput
{
    // ReSharper disable once UnusedMember.Global
    public SerializedWtDrumCombinedOutput()
    {
    }

    public SerializedWtDrumCombinedOutput(int miso, int mosi, int sck, int cs, IReadOnlyCollection<Output> outputs)
    {
        Miso = miso;
        Mosi = mosi;
        Sck = sck;
        Cs = cs;
        Outputs = outputs.Select(s => s.Serialize()).ToList();
        Enabled = GetBytes(new BitArray(outputs.Select(s => s.Enabled).ToArray()));
    }

    [ProtoMember(1)] public int Miso { get; }
    [ProtoMember(2)] public int Mosi { get; }
    [ProtoMember(3)] public int Sck { get; }
    [ProtoMember(4)] public int Cs { get; }
    [ProtoMember(5)] public List<SerializedOutput> Outputs { get; } = [];
    [ProtoMember(6)] public byte[] Enabled { get; } = Array.Empty<byte>();

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new WtDrumCombinedOutput(model, Miso, Mosi, Sck, Cs);
        model.Bindings.Add(combined);
        var array = new BitArray(Enabled);
        var outputs = Outputs.Select(s => s.Generate(model)).ToList();
        for (var i = 0; i < outputs.Count; i++) outputs[i].Enabled = array[i];
        combined.SetOutputsOrDefaults(outputs);
        return combined;
    }
}
