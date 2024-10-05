using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedCloneNeckInputCombined : SerializedInput
{
    public SerializedCloneNeckInputCombined()
    {
        
    }
    public SerializedCloneNeckInputCombined(Gh5NeckInputType type, bool peripheral)
    {
        Type = type;
        Peripheral = peripheral;
    }

    [ProtoMember(3)] private Gh5NeckInputType Type { get; }
    [ProtoMember(8)] private bool Peripheral { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new CloneNeckInput(Type, model, Peripheral, combined: true);
    }
}