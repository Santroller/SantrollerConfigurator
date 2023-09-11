using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedDigitalToAnalog : SerializedInput
{
    public SerializedDigitalToAnalog(SerializedInput child, int on, bool trigger, bool tilt)
    {
        Child = child;
        On = on;
        Trigger = trigger;
        Tilt = tilt;
    }

    [ProtoMember(1)] private SerializedInput Child { get; }
    [ProtoMember(2)] private int On { get; }
    [ProtoMember(3)] private bool Trigger { get; }
    [ProtoMember(4)] private bool Tilt { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return Tilt
            ? new DigitalToAnalog(Child.Generate(model), model)
            : new DigitalToAnalog(Child.Generate(model), On, Trigger, model);
    }
}