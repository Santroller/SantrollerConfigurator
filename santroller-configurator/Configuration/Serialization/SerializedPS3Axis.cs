using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedPs3Axis : SerializedOutput
{
    public SerializedPs3Axis(SerializedInput input, Ps3AxisType type, Color ledOn, Color ledOff, byte[] ledIndex,
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

    [ProtoMember(1)] public SerializedInput Input { get; }
    [ProtoMember(2)] public uint LedOn { get; }
    [ProtoMember(3)] public uint LedOff { get; }
    [ProtoMember(8)] public byte[] LedIndex { get; }
    [ProtoMember(4)] public int Min { get; }
    [ProtoMember(5)] public int Max { get; }
    [ProtoMember(6)] public int Deadzone { get; }

    [ProtoMember(7)] public Ps3AxisType Type { get; }

    [ProtoMember(9)] public bool ChildOfCombined { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new Ps3Axis(model, Input.Generate(model), Color.FromUInt32(LedOn),
            Color.FromUInt32(LedOff), LedIndex, Min, Max, Deadzone,
            Type, ChildOfCombined);
        model.Bindings.Add(combined);
        return combined;
    }
}