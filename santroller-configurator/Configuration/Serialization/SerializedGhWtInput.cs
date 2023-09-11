using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedGhWtInput : SerializedInput
{
    public SerializedGhWtInput(bool peripheral, int pin, int pins0, int pins1, int pins2, GhWtInputType type)
    {
        Pin = pin;
        PinS0 = pins0;
        PinS1 = pins1;
        PinS2 = pins2;
        Type = type;
        Peripheral = peripheral;
    }

    [ProtoMember(1)] private int Pin { get; }
    [ProtoMember(2)] private int PinS0 { get; }
    [ProtoMember(3)] private int PinS1 { get; }
    [ProtoMember(4)] private int PinS2 { get; }

    [ProtoMember(5)] private GhWtInputType Type { get; }
    [ProtoMember(8)] private bool Peripheral { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new GhWtTapInput(Type, model, Peripheral, Pin, PinS0, PinS1, PinS2);
    }
}