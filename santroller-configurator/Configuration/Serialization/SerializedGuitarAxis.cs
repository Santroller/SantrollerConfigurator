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
    public SerializedGuitarAxis(SerializedInput input, GuitarAxisType type, Color ledOn, Color ledOff, byte[] ledIndex, byte[] ledIndexPeripheral,
        bool invert, int min, int max, int deadzone, bool outputEnabled, int outputPin, bool outputInverted, bool outputPeripheral, bool childOfCombined)
    {
        Input = input;
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        Invert = invert;
        Min = min;
        Max = max;
        Deadzone = deadzone;
        Type = type;
        LedIndex = ledIndex;
        LedIndexPeripheral = ledIndexPeripheral;
        ChildOfCombined = childOfCombined;
        OutputEnabled = outputEnabled;
        OutputPin = outputPin;
        OutputInverted = outputInverted;
        OutputPeripheral = outputPeripheral;
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
    [ProtoMember(11)] public byte[] LedIndexPeripheral { get; }
    [ProtoMember(12)] public bool Invert { get; }
    [ProtoMember(13)] public bool OutputEnabled { get; }
    [ProtoMember(14)] public int OutputPin { get; }
    [ProtoMember(15)] public bool OutputInverted { get; }
    [ProtoMember(16)] public bool OutputPeripheral { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new GuitarAxis(model, Input.Generate(model), Color.FromUInt32(LedOn),
            Color.FromUInt32(LedOff), LedIndex, LedIndexPeripheral, Min, Max, Deadzone,
            Invert, Type, OutputEnabled, OutputPeripheral, OutputInverted, OutputPin, ChildOfCombined);
        model.Bindings.Add(combined);
        return combined;
    }
}