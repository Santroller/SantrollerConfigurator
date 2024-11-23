using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedDjInput : SerializedInput
{
    public SerializedDjInput()
    {
        
    }
    public SerializedDjInput(bool peripheral, int sda, int scl, UsbHostInputType type)
    {
        Sda = sda;
        Scl = scl;
        Type = type;
        Peripheral = peripheral;
    }

    [ProtoMember(1)] private int Sda { get; }
    [ProtoMember(2)] private int Scl { get; }
    [ProtoMember(3)] private UsbHostInputType Type { get; }
    [ProtoMember(5)] private bool Peripheral { get; }


    public override Input Generate(ConfigViewModel model)
    {
        return new DjInput(Type, model, Peripheral, Sda, Scl);
    }
}