using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;
using BluetoothCombinedOutput = GuitarConfigurator.NetCore.Configuration.Outputs.Combined.BluetoothCombinedOutput;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedCombinedBluetoothOutput : SerializedOutput
{
    public SerializedCombinedBluetoothOutput()
    {
        Outputs = [];
    }
    [ProtoMember(1)] public List<SerializedOutput> Outputs { get; }
    public SerializedCombinedBluetoothOutput(List<Output> outputs)
    {
        Outputs = outputs.Select(s => s.Serialize()).ToList();
    }
    public override Output Generate(ConfigViewModel model)
    {
        var combined = new BluetoothCombinedOutput(model);
        model.Bindings.Add(combined);
        combined.SetOutputsOrDefaults(Outputs.Select(s => s.Generate(model)));
        return combined;
    }
}