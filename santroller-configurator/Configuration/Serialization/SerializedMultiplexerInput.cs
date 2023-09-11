using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedMultiplexerInput : SerializedInput
{
    public SerializedMultiplexerInput(int pin, bool peripheral, int pins0, int pins1, int pins2, int pins3, MultiplexerType type,
        int channel)
    {
        Pin = pin;
        PinS0 = pins0;
        PinS1 = pins1;
        PinS2 = pins2;
        PinS3 = pins3;
        Type = type;
        Channel = channel;
        Peripheral = peripheral;
    }

    [ProtoMember(1)] private int Pin { get; }
    [ProtoMember(2)] private int PinS0 { get; }
    [ProtoMember(3)] private int PinS1 { get; }
    [ProtoMember(4)] private int PinS2 { get; }
    [ProtoMember(5)] private int PinS3 { get; }
    [ProtoMember(6)] public MultiplexerType Type { get; }
    [ProtoMember(7)] public int Channel { get; }
    
    [ProtoMember(8)] private bool Peripheral { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new MultiplexerInput(Pin, Peripheral, Channel, PinS0, PinS1, PinS2, PinS3, Type, model);
    }
}