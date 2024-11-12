using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedWiiInput : SerializedInput
{
    public SerializedWiiInput()
    {
        
    }
    public SerializedWiiInput(int sda, int scl, UsbHostInputType type, bool peripheral)
    {
        Sda = sda;
        Scl = scl;
        Type = type;
        Peripheral = peripheral;
    }

    [ProtoMember(1)] private int Sda { get; }
    [ProtoMember(2)] private int Scl { get; }
    [ProtoMember(3)] private UsbHostInputType Type { get; }
    [ProtoMember(4)] private bool Peripheral { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new WiiInput(Type, model, Peripheral, Sda, Scl);
    }
}