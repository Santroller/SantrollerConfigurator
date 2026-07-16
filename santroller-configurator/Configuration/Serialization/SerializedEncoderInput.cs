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
    public SerializedEncoderInput(bool peripheral, int pin, int pollrate)
    {
        Pin = pin;
        Peripheral = peripheral;
        Pollrate = pollrate;
    }

    [ProtoMember(1)] private int Pin { get; }
    [ProtoMember(2)] private bool Peripheral { get; }
    [ProtoMember(3)] private int Pollrate { get; }


    public override Input Generate(ConfigViewModel model)
    {
        return new EncoderInput(Pin,Pollrate, Peripheral, model);
    }
}