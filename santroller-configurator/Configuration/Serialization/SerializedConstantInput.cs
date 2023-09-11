using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedConstantInput : SerializedInput
{
    public SerializedConstantInput(int value, bool analog, int min, int max, bool tapBar, bool rbPickup)
    {
        Value = value;
        TapBar = tapBar;
        RbPickup = rbPickup;
        Analog = analog;
        Min = min;
        Max = max;
    }

    [ProtoMember(1)] private int Value { get; }
    [ProtoMember(2)] private bool Analog { get; }
    [ProtoMember(3)] private bool TapBar { get; }
    [ProtoMember(4)] private bool RbPickup { get; }
    [ProtoMember(5)] private int Min { get; }
    [ProtoMember(6)] private int Max { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new ConstantInput(model, Value, Analog, Min, Max, TapBar, RbPickup);
    }
}