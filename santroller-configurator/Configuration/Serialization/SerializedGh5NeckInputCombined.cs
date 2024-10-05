using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedGh5NeckInputCombined : SerializedInput
{
    public SerializedGh5NeckInputCombined()
    {
        
    }
    public SerializedGh5NeckInputCombined(Gh5NeckInputType type, bool peripheral)
    {
        Type = type;
        Peripheral = peripheral;
    }

    [ProtoMember(3)] private Gh5NeckInputType Type { get; }
    [ProtoMember(8)] private bool Peripheral { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new Gh5NeckInput(Type, model, Peripheral, combined: true);
    }
}