using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedCrkdNeckInput : SerializedInput
{
    public SerializedCrkdNeckInput()
    {
        
    }
    public SerializedCrkdNeckInput(bool peripheral, int tx, int rx, CrkdNeckInputType type)
    {
        Tx = tx;
        Rx = rx;
        Type = type;
        Peripheral = peripheral;
    }

    [ProtoMember(1)] private int Tx { get; }
    [ProtoMember(2)] private int Rx { get; }
    [ProtoMember(3)] private CrkdNeckInputType Type { get; }
    [ProtoMember(4)] private bool Peripheral { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new CrkdNeckInput(Type, model, Peripheral, Tx, Rx);
    }
}