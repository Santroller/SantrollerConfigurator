using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedCloneNeckInput : SerializedInput
{
    public SerializedCloneNeckInput()
    {
        
    }
    public SerializedCloneNeckInput(bool peripheral, int sda, int scl, Gh5NeckInputType type)
    {
        Sda = sda;
        Scl = scl;
        Type = type;
        Peripheral = peripheral;
    }

    [ProtoMember(1)] private int Sda { get; }
    [ProtoMember(2)] private int Scl { get; }
    [ProtoMember(3)] private Gh5NeckInputType Type { get; }
    [ProtoMember(4)] private bool Peripheral { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new CloneNeckInput(Type, model, Peripheral, Sda, Scl);
    }
}