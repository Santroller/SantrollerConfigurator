using System;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedDrumAxis : SerializedOutput
{
    public SerializedDrumAxis()
    {
        LedIndex = [];
        LedIndexPeripheral = [];
        LedIndexMpr121 = [];
    }
    public SerializedDrumAxis(SerializedInput input, bool enabled, DrumAxisType type, Color ledOn, Color ledOff, byte[] ledIndex, byte[] ledIndexPeripheral,
        int min, int max, int deadzone, int debounce, bool outputEnabled, int outputPin, bool outputInverted, bool outputPeripheral, bool childOfCombined, byte[] ledIndexMpr121)
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
        Debounce = debounce;
        ChildOfCombined = childOfCombined;
        LedIndexMpr121 = ledIndexMpr121;
        OutputEnabled = outputEnabled;
        OutputPin = outputPin;
        OutputInverted = outputInverted;
        OutputPeripheral = outputPeripheral;
        Enabled = enabled;
    }

    [ProtoMember(1)] public SerializedInput? Input { get; }
    [ProtoMember(2)] public uint LedOn { get; }
    [ProtoMember(3)] public uint LedOff { get; }
    [ProtoMember(4)] public byte[] LedIndex { get; }
    [ProtoMember(5)] public int Min { get; }
    [ProtoMember(6)] public int Max { get; }
    [ProtoMember(7)] public int Deadzone { get; }
    [ProtoMember(9)] public int Debounce { get; }
    [ProtoMember(10)] public DrumAxisType Type { get; }
    [ProtoMember(11)] public bool ChildOfCombined { get; }
    [ProtoMember(12)] public byte[] LedIndexPeripheral { get; }
    [ProtoMember(13)] public bool OutputEnabled { get; }
    [ProtoMember(14)] public int OutputPin { get; }
    [ProtoMember(15)] public bool OutputInverted { get; }
    [ProtoMember(16)] public bool OutputPeripheral { get; }
    [ProtoMember(17)] public byte[] LedIndexMpr121 { get; }
    [ProtoMember(18)] private bool Enabled { get; } = true;

    public override Output Generate(ConfigViewModel model)
    {
        if (Input == null)
        {
            throw new NotImplementedException("Null child unexpected!");
        }
        var combined = new DrumAxis(model, Enabled, Input.Generate(model), Color.FromUInt32(LedOn),
            Color.FromUInt32(LedOff), LedIndex, LedIndexPeripheral, LedIndexMpr121, Min, Max, Deadzone,
            Debounce, Type, OutputEnabled, OutputPeripheral, OutputInverted, OutputPin, ChildOfCombined);
        model.Bindings.Add(combined);
        return combined;
    }
}