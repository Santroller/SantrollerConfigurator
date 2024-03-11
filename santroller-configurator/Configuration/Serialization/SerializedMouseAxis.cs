using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedMouseAxis : SerializedOutput
{
    public SerializedMouseAxis(SerializedInput input, MouseAxisType type, Color ledOn, Color ledOff, byte[] ledIndex, byte[] ledIndexPeripheral,
        int min, int max,
        int deadzone, bool outputEnabled, int outputPin, bool outputInverted, bool outputPeripheral)
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
    [ProtoMember(7)] public byte[] LedIndex { get; }
    [ProtoMember(8)] public byte[] LedIndexPeripheral { get; }
    [ProtoMember(9)] public bool OutputEnabled { get; }
    [ProtoMember(10)] public int OutputPin { get; }
    [ProtoMember(11)] public bool OutputInverted { get; }
    [ProtoMember(12)] public bool OutputPeripheral { get; }

    public MouseAxisType Type { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new MouseAxis(model, Input.Generate(model), Color.FromUInt32(LedOn),
            Color.FromUInt32(LedOff), LedIndex, LedIndexPeripheral, Min, Max, Deadzone,
            Type, OutputEnabled, OutputPeripheral, OutputInverted, OutputPin);
        model.Bindings.Add(combined);
        return combined;
    }
}