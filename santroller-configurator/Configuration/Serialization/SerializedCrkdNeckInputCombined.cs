using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedCrkdNeckInputCombined : SerializedInput
{
    public SerializedCrkdNeckInputCombined()
    {
        
    }
    public SerializedCrkdNeckInputCombined(CrkdNeckInputType type, bool peripheral)
    {
        Type = type;
        Peripheral = peripheral;
    }

    [ProtoMember(3)] private CrkdNeckInputType Type { get; }
    [ProtoMember(8)] private bool Peripheral { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new CrkdNeckInput(Type, model, Peripheral, combined: true);
    }
}