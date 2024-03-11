using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedControllerAxis : SerializedOutput
{
    public SerializedControllerAxis(SerializedInput input, StandardAxisType type, Color ledOn, Color ledOff,
        byte[] ledIndex, byte[] ledIndexPeripheral, int min, int max, int deadzone, int threshold, bool outputEnabled, int outputPin, bool outputInverted, bool outputPeripheral, bool childOfCombined)
    {
        Input = input;
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        Min = min;
        Max = max;
        Deadzone = deadzone;
        Type = type;
        LedIndex = ledIndex;
        LedIndexPeripheral = ledIndexPeripheral;
        ChildOfCombined = childOfCombined;
        Threshold = threshold;
        OutputEnabled = outputEnabled;
        OutputPin = outputPin;
        OutputInverted = outputInverted;
        OutputPeripheral = outputPeripheral;
    }

    [ProtoMember(1)] public SerializedInput Input { get; }
    [ProtoMember(2)] public uint LedOn { get; }
    [ProtoMember(3)] public uint LedOff { get; }
    [ProtoMember(4)] public int Min { get; }
    [ProtoMember(5)] public int Max { get; }
    [ProtoMember(6)] public int Deadzone { get; }
    [ProtoMember(7)] public StandardAxisType Type { get; }
    [ProtoMember(8)] public byte[] LedIndex { get; }
    [ProtoMember(9)] public bool ChildOfCombined { get; }
    [ProtoMember(10)] public int Threshold { get; }
    [ProtoMember(11)] public byte[] LedIndexPeripheral { get; }
    [ProtoMember(12)] public bool OutputEnabled { get; }
    [ProtoMember(13)] public int OutputPin { get; }
    [ProtoMember(14)] public bool OutputInverted { get; }
    [ProtoMember(15)] public bool OutputPeripheral { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var input = Input.Generate(model);
        var output = new ControllerAxis(model, input, Color.FromUInt32(LedOn), Color.FromUInt32(LedOff), LedIndex, LedIndexPeripheral, Min,
            Max,
            Deadzone,
            Threshold,
            Type, OutputEnabled, OutputPeripheral, OutputInverted, OutputPin, ChildOfCombined);
        model.Bindings.Add(output);
        return output;
    }
}