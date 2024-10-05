using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedGhWtInputCombined : SerializedInput
{
    public SerializedGhWtInputCombined()
    {
        
    }
    public SerializedGhWtInputCombined(GhWtInputType type, bool peripheral)
    {
        Type = type;
        Peripheral = peripheral;
    }

    [ProtoMember(2)] private GhWtInputType Type { get; }
    [ProtoMember(3)] private bool Peripheral { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new GhWtTapInput(Type, model, Peripheral, 0, 0, 0, 0, true);
    }
}