using System;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedPs3Axis : SerializedOutput
{
    public SerializedPs3Axis()
    {
        LedIndex = [];
        LedIndexMpr121 = [];
        LedIndexPeripheral = [];
    }
    public SerializedPs3Axis(SerializedInput input, Ps3AxisType type, Color ledOn, Color ledOff, byte[] ledIndex, byte[] ledIndexPeripheral,
        int min, int max, int deadzone, bool outputEnabled, int outputPin, bool outputInverted, bool outputPeripheral, bool childOfCombined, byte[] ledIndexMpr121)
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
        LedIndexMpr121 = ledIndexMpr121;
        OutputEnabled = outputEnabled;
        OutputPin = outputPin;
        OutputInverted = outputInverted;
        OutputPeripheral = outputPeripheral;
    }

    [ProtoMember(1)] public SerializedInput? Input { get; }
    [ProtoMember(2)] public uint LedOn { get; }
    [ProtoMember(3)] public uint LedOff { get; }
    [ProtoMember(8)] public byte[] LedIndex { get; }
    [ProtoMember(4)] public int Min { get; }
    [ProtoMember(5)] public int Max { get; }
    [ProtoMember(6)] public int Deadzone { get; }

    [ProtoMember(7)] public Ps3AxisType Type { get; }

    [ProtoMember(9)] public bool ChildOfCombined { get; }
    [ProtoMember(10)] public byte[] LedIndexPeripheral { get; }
    [ProtoMember(11)] public bool OutputEnabled { get; }
    [ProtoMember(12)] public int OutputPin { get; }
    [ProtoMember(13)] public bool OutputInverted { get; }
    [ProtoMember(14)] public bool OutputPeripheral { get; }
    
    [ProtoMember(15)] public byte[] LedIndexMpr121 { get; }
    [ProtoMember(16)] private bool Enabled { get; } = true;

    public override Output Generate(ConfigViewModel model)
    {
        if (Input == null)
        {
            throw new NotImplementedException("Null child unexpected!");
        }
        var combined = new Ps3Axis(model, Enabled, Input.Generate(model), Color.FromUInt32(LedOn),
            Color.FromUInt32(LedOff), LedIndex, LedIndexPeripheral, LedIndexMpr121, Min, Max, Deadzone,
            Type, OutputEnabled, OutputPeripheral, OutputInverted, OutputPin, ChildOfCombined);
        model.Bindings.Add(combined);
        return combined;
    }
}