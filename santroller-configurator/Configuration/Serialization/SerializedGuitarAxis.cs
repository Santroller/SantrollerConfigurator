using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedGuitarAxis : SerializedOutput
{
    public SerializedGuitarAxis(SerializedInput input, GuitarAxisType type, Color ledOn, Color ledOff, byte[] ledIndex,
        int min, int max, int deadzone, bool childOfCombined)
    {
        Input = input;
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        Min = min;
        Max = max;
        Deadzone = deadzone;
        Type = type;
        LedIndex = ledIndex;
        ChildOfCombined = childOfCombined;
    }

    [ProtoMember(1)] public virtual SerializedInput Input { get; }
    [ProtoMember(2)] public virtual uint LedOn { get; }
    [ProtoMember(3)] public virtual uint LedOff { get; }
    [ProtoMember(4)] public virtual byte[] LedIndex { get; }
    [ProtoMember(5)] public int Min { get; }
    [ProtoMember(6)] public int Max { get; }
    [ProtoMember(7)] public int Deadzone { get; }
    [ProtoMember(8)] public bool ChildOfCombined { get; }
    [ProtoMember(10)] public GuitarAxisType Type { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new GuitarAxis(model, Input.Generate(model), Color.FromUInt32(LedOn),
            Color.FromUInt32(LedOff), LedIndex, Min, Max, Deadzone,
            Type, ChildOfCombined);
        model.Bindings.Add(combined);
        return combined;
    }
}