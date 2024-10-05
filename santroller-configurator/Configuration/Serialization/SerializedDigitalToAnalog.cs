using System;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedDigitalToAnalog : SerializedInput
{
    public SerializedDigitalToAnalog()
    {
    }
    public SerializedDigitalToAnalog(SerializedInput child, int on, bool trigger, DigitalToAnalogType type)
    {
        Child = child;
        On = on;
        Trigger = trigger;
        Type = type;
    }

    [ProtoMember(1)] private SerializedInput? Child { get; }
    [ProtoMember(2)] private int On { get; }
    [ProtoMember(3)] private bool Trigger { get; }
    [ProtoMember(4)] private DigitalToAnalogType Type { get; }

    public override Input Generate(ConfigViewModel model)
    { 
        if (Child == null)
        {
            throw new NotImplementedException("Null child unexpected!");
        }
        return new DigitalToAnalog(Child.Generate(model), On, model, Type);
    }
}