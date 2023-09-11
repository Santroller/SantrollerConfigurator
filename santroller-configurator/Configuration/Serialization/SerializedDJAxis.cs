using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedDjAxis : SerializedOutput
{
    public SerializedDjAxis(SerializedInput input, DjAxisType type, Color ledOn, Color ledOff, byte[] ledIndex,
        int min, int max, int deadzone, bool childOfCombined)
    {
        Input = input;
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        Min = min;
        Max = max;
        DeadzoneOrMultiplier = deadzone;
        Type = type;
        LedIndex = ledIndex;
        ChildOfCombined = childOfCombined;
    }
    
    public SerializedDjAxis(SerializedInput input, DjAxisType type, Color ledOn, Color ledOff, byte[] ledIndex, int multiplier, bool childOfCombined)
    {
        Input = input;
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        Min = 0;
        Max = 0;
        DeadzoneOrMultiplier = multiplier;
        Type = type;
        LedIndex = ledIndex;
        ChildOfCombined = childOfCombined;
    }

    [ProtoMember(1)] public SerializedInput Input { get; }
    [ProtoMember(2)] public uint LedOn { get; }
    [ProtoMember(3)] public uint LedOff { get; }
    [ProtoMember(4)] public byte[] LedIndex { get; }
    [ProtoMember(5)] public int Min { get; }
    [ProtoMember(6)] public int Max { get; }
    [ProtoMember(7)] public int DeadzoneOrMultiplier { get; }
    [ProtoMember(8)] public bool ChildOfCombined { get; }
    [ProtoMember(10)] public DjAxisType Type { get; }

    public override Output Generate(ConfigViewModel model)
    {
        DjAxis combined;
        if (Type is DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity or DjAxisType.EffectsKnob)
        {
            combined = new DjAxis(model, Input.Generate(model), Color.FromUInt32(LedOn),
                Color.FromUInt32(LedOff), LedIndex, DeadzoneOrMultiplier,
                Type, ChildOfCombined);
        }
        else
        {
            combined = new DjAxis(model, Input.Generate(model), Color.FromUInt32(LedOn),
                Color.FromUInt32(LedOff), LedIndex, Min, Max, DeadzoneOrMultiplier,
                Type, ChildOfCombined);
        }
        
        model.Bindings.Add(combined);
        return combined;
    }
}