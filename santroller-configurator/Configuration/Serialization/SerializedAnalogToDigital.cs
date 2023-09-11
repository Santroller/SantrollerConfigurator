using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedAnalogToDigital : SerializedInput
{
    public SerializedAnalogToDigital(SerializedInput child, AnalogToDigitalType type, int threshold)
    {
        Child = child;
        Type = type;
        Threshold = threshold;
    }

    [ProtoMember(1)] public SerializedInput Child { get; }
    [ProtoMember(2)] public AnalogToDigitalType Type { get; }
    [ProtoMember(3)] public int Threshold { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new AnalogToDigital(Child.Generate(model), Type, Threshold, model);
    }
}