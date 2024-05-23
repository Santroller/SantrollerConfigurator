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
public class SerializedCombinedMidiOutput : SerializedOutput
{
    [ProtoMember(1)] public List<SerializedOutput> Outputs { get; }

    [ProtoMember(2)] public byte[] Enabled { get; }
    [ProtoMember(3)] public byte FirstNote { get; }
    
    public SerializedCombinedMidiOutput(List<Output> outputs, byte firstNote)
    {
        Outputs = outputs.Select(s => s.Serialize()).ToList();
        Enabled = GetBytes(new BitArray(outputs.Select(s => s.Enabled).ToArray()));
        FirstNote = firstNote;
    }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new MidiCombinedOutput(model, FirstNote);
        model.Bindings.Add(combined);
        var array = new BitArray(Enabled);
        var outputs = Outputs.Select(s => s.Generate(model)).ToList();
        for (var i = 0; i < outputs.Count; i++) outputs[i].Enabled = array[i];
        combined.SetOutputsOrDefaults(outputs);
        return combined;
    }
}