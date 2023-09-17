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
    // ReSharper disable once UnusedMember.Global
    public SerializedGh5CombinedOutput()
    {
        
    }
    public SerializedGh5CombinedOutput(bool peripheral, int sda, int scl, IReadOnlyCollection<Output> outputs)
    {
        Sda = sda;
        Scl = scl;
        Outputs = outputs.Select(s => s.Serialize()).ToList();
        Enabled = GetBytes(new BitArray(outputs.Select(s => s.Enabled).ToArray()));
        Peripheral = peripheral;
    }

    [ProtoMember(4)] public int Sda { get; }
    [ProtoMember(5)] public int Scl { get; }

    [ProtoMember(6)] public List<SerializedOutput> Outputs { get; } = new();

    [ProtoMember(7)] public byte[] Enabled { get; } = Array.Empty<byte>();
    [ProtoMember(8)] private bool Peripheral { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new Gh5CombinedOutput(model, Peripheral, Sda, Scl);
        model.Bindings.Add(combined);
        var array = new BitArray(Enabled);
        var outputs = Outputs.Select(s => s.Generate(model)).ToList();
        for (var i = 0; i < outputs.Count; i++) outputs[i].Enabled = array[i];
        combined.SetOutputsOrDefaults(outputs);
        return combined;
    }
}