using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedAccelInput : SerializedInput
{
    public SerializedAccelInput(AccelInputType type)
    {
        Type = type;
    }
    [ProtoMember(3)] private AccelInputType Type { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new AccelInput(Type, model);
    }
}