using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedEncoderInput : SerializedInput
{
    public SerializedEncoderInput()
    {
        
    }
    public SerializedEncoderInput(bool peripheral, int pin)
    {
        Pin = pin;
        Peripheral = peripheral;
    }

    [ProtoMember(1)] private int Pin { get; }
    [ProtoMember(2)] private bool Peripheral { get; }


    public override Input Generate(ConfigViewModel model)
    {
        return new EncoderInput(Pin, Peripheral, model);
    }
}