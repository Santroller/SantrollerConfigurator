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
public class SerializedGhwtCombinedOutput : SerializedOutput
{
    public SerializedGhwtCombinedOutput(bool peripheral, int pin, int pinS0, int pinS1, int pinS2, List<Output> outputs)
    {
        Pin = pin;
        PinS0 = pinS0;
        PinS1 = pinS1;
        PinS2 = pinS2;
        Outputs = outputs.Select(s => s.Serialize()).ToList();
        Enabled = GetBytes(new BitArray(outputs.Select(s => s.Enabled).ToArray()));
        Peripheral = peripheral;
    }

    [ProtoMember(2)] public int Pin { get; }
    [ProtoMember(3)] public int PinS0 { get; }

    [ProtoMember(4)] public int PinS1 { get; }

    [ProtoMember(5)] public int PinS2 { get; }


    [ProtoMember(6)] public List<SerializedOutput> Outputs { get; }
    [ProtoMember(7)] public byte[] Enabled { get; }
    [ProtoMember(8)] private bool Peripheral { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new GhwtCombinedOutput(model, Peripheral, Pin, PinS0, PinS1, PinS2);
        model.Bindings.Add(combined);
        var array = new BitArray(Enabled);
        var outputs = Outputs.Select(s => s.Generate(model)).ToList();
        for (var i = 0; i < outputs.Count; i++) outputs[i].Enabled = array[i];
        combined.SetOutputsOrDefaults(outputs);
        return combined;
    }
}