using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedMidiInput : SerializedInput
{
    public SerializedMidiInput(MidiType type, int key)
    {
        Key = key;
        Type = type;
    }

    [ProtoMember(1)] private int Key { get; }
    [ProtoMember(2)] private MidiType Type { get; }


    public override Input Generate(ConfigViewModel model)
    {
        return new MidiInput(Type, Key, model);
    }
}