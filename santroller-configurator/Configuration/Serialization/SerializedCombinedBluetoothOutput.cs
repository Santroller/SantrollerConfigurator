using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;
using BluetoothCombinedOutput = GuitarConfigurator.NetCore.Configuration.Outputs.Combined.BluetoothCombinedOutput;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedCombinedBluetoothOutput : SerializedOutput
{
    [ProtoMember(1)] public List<SerializedOutput> Outputs { get; }

    [ProtoMember(2)] public byte[] Enabled { get; }
    public SerializedCombinedBluetoothOutput(List<Output> outputs)
    {
        Outputs = outputs.Select(s => s.Serialize()).ToList();
        Enabled = GetBytes(new BitArray(outputs.Select(s => s.Enabled).ToArray()));
    }
    public override Output Generate(ConfigViewModel model)
    {
        var combined = new BluetoothCombinedOutput(model);
        model.Bindings.Add(combined);
        var array = new BitArray(Enabled);
        var outputs = Outputs.Select(s => s.Generate(model)).ToList();
        for (var i = 0; i < outputs.Count; i++) outputs[i].Enabled = array[i];
        combined.SetOutputsOrDefaults(outputs);
        return combined;
    }
}