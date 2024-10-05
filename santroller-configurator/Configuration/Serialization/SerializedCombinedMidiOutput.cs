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
public class SerializedCombinedMidiOutput : SerializedOutput
{
    public SerializedCombinedMidiOutput()
    {
        Outputs = [];
    }
    [ProtoMember(1)] public List<SerializedOutput> Outputs { get; }
    [ProtoMember(3)] public byte FirstNote { get; }
    
    public SerializedCombinedMidiOutput(List<Output> outputs, byte firstNote)
    {
        Outputs = outputs.Select(s => s.Serialize()).ToList();
        FirstNote = firstNote;
    }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new MidiCombinedOutput(model, FirstNote);
        model.Bindings.Add(combined);
        combined.SetOutputsOrDefaults(Outputs.Select(s => s.Generate(model)));
        return combined;
    }
}