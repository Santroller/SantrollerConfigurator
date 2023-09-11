using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedWiiCombinedOutput : SerializedOutput
{
    public SerializedWiiCombinedOutput(bool peripheral, int sda, int scl, List<Output> outputs)
    {
        Peripheral = peripheral;
        Sda = sda;
        Scl = scl;
        Outputs = outputs.Select(s => s.Serialize()).ToList();
        Enabled = GetBytes(new BitArray(outputs.Select(s => s.Enabled).ToArray()));
    }

    [ProtoMember(4)] public int Sda { get; }
    [ProtoMember(5)] public int Scl { get; }
    [ProtoMember(6)] public List<SerializedOutput> Outputs { get; }
    [ProtoMember(7)] public byte[] Enabled { get; }
    [ProtoMember(8)] private bool Peripheral { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new WiiCombinedOutput(model, Peripheral, Sda, Scl);
        model.Bindings.Add(combined);
        var outputs = Outputs.Select(s => s.Generate(model)).ToList();
        var array = new BitArray(Enabled);
        for (var i = 0; i < outputs.Count; i++) outputs[i].Enabled = array[i];
        combined.SetOutputsOrDefaults(outputs);
        return combined;
    }
}