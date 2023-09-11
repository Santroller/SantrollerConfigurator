using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedDrumAxis : SerializedOutput
{
    public SerializedDrumAxis(SerializedInput input, DrumAxisType type, Color ledOn, Color ledOff, byte[] ledIndex,
        int min, int max, int deadzone, int debounce, bool childOfCombined)
    {
        Input = input;
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        Min = min;
        Max = max;
        Deadzone = deadzone;
        Type = type;
        LedIndex = ledIndex;
        Debounce = debounce;
        ChildOfCombined = childOfCombined;
    }

    [ProtoMember(1)] public SerializedInput Input { get; }
    [ProtoMember(2)] public uint LedOn { get; }
    [ProtoMember(3)] public uint LedOff { get; }
    [ProtoMember(4)] public byte[] LedIndex { get; }
    [ProtoMember(5)] public int Min { get; }
    [ProtoMember(6)] public int Max { get; }
    [ProtoMember(7)] public int Deadzone { get; }
    [ProtoMember(9)] public int Debounce { get; }
    [ProtoMember(10)] public DrumAxisType Type { get; }
    [ProtoMember(11)] public bool ChildOfCombined { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new DrumAxis(model, Input.Generate(model), Color.FromUInt32(LedOn),
            Color.FromUInt32(LedOff), LedIndex, Min, Max, Deadzone,
            Debounce, Type, ChildOfCombined);
        model.Bindings.Add(combined);
        return combined;
    }
}