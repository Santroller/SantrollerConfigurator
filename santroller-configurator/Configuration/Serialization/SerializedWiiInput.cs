using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedWiiInput : SerializedInput
{
    public SerializedWiiInput(int sda, int scl, WiiInputType type, bool peripheral)
    {
        Sda = sda;
        Scl = scl;
        Type = type;
        Peripheral = peripheral;
    }

    [ProtoMember(1)] private int Sda { get; }
    [ProtoMember(2)] private int Scl { get; }
    [ProtoMember(3)] private WiiInputType Type { get; }
    [ProtoMember(4)] private bool Peripheral { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new WiiInput(Type, model, Peripheral, Sda, Scl);
    }
}