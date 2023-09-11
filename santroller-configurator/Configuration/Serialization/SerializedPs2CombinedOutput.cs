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
public class SerializedPs2CombinedOutput : SerializedOutput
{
    public SerializedPs2CombinedOutput(bool peripheral, int miso, int mosi, int sck, int att, int ack, List<Output> outputs)
    {
        Miso = miso;
        Mosi = mosi;
        Sck = sck;
        Att = att;
        Ack = ack;
        Outputs = outputs.Select(s => s.Serialize()).ToList();
        Enabled = GetBytes(new BitArray(outputs.Select(s => s.Enabled).ToArray()));
        Peripheral = peripheral;
    }

    [ProtoMember(4)] public int Miso { get; }
    [ProtoMember(5)] public int Mosi { get; }
    [ProtoMember(6)] public int Sck { get; }
    [ProtoMember(7)] public int Att { get; }
    [ProtoMember(8)] public int Ack { get; }

    [ProtoMember(9)] public List<SerializedOutput> Outputs { get; }

    [ProtoMember(10)] public byte[] Enabled { get; }
    [ProtoMember(11)] private bool Peripheral { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new Ps2CombinedOutput(model, Peripheral, Miso, Mosi, Sck, Att, Ack);
        model.Bindings.Add(combined);
        var outputs = Outputs.Select(s => s.Generate(model)).ToList();
        var array = new BitArray(Enabled);
        for (var i = 0; i < outputs.Count; i++) outputs[i].Enabled = array[i];
        combined.SetOutputsOrDefaults(outputs);
        return combined;
    }
}