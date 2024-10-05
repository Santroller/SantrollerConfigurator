using System;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedDjAxis : SerializedOutput
{
    public SerializedDjAxis()
    {
        LedIndex = [];
        LedIndexPeripheral = [];
        LedIndexMpr121 = [];
    }
    public SerializedDjAxis(SerializedInput input, bool enabled, DjAxisType type, Color ledOn, Color ledOff, byte[] ledIndex, byte[] ledIndexPeripheral,
        int min, int max, int deadzone, bool outputEnabled, int outputPin, bool outputInverted, bool outputPeripheral, bool childOfCombined, byte[] ledIndexMpr121)
    {
        Input = input;
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        Min = min;
        Max = max;
        DeadzoneOrMultiplier = deadzone;
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
    
    public SerializedDjAxis(SerializedInput input, bool enabled, DjAxisType type, Color ledOn, Color ledOff, byte[] ledIndex, byte[] ledIndexPeripheral, int multiplier, int ledMultiplier, bool outputEnabled, int outputPin, bool outputInverted, bool outputPeripheral, bool childOfCombined, byte[] ledIndexMpr121)
    {
        Input = input;
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        Min = 0;
        Max = 0;
        DeadzoneOrMultiplier = multiplier;
        LedMultiplier = ledMultiplier;
        Type = type;
        LedIndex = ledIndex;
        LedIndexPeripheral = ledIndexPeripheral;
        ChildOfCombined = childOfCombined;
        LedIndexMpr121 = ledIndexMpr121;
        Enabled = enabled;
    }

    [ProtoMember(1)] public SerializedInput? Input { get; }
    [ProtoMember(2)] public uint LedOn { get; }
    [ProtoMember(3)] public uint LedOff { get; }
    [ProtoMember(4)] public byte[] LedIndex { get; }
    [ProtoMember(5)] public int Min { get; }
    [ProtoMember(6)] public int Max { get; }
    [ProtoMember(7)] public int DeadzoneOrMultiplier { get; }
    [ProtoMember(8)] public bool ChildOfCombined { get; }
    [ProtoMember(10)] public DjAxisType Type { get; }
    [ProtoMember(11)] public byte[] LedIndexPeripheral { get; }
    [ProtoMember(12)] public int LedMultiplier { get; }
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
        DjAxis combined;
        if (Type is DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity or DjAxisType.EffectsKnob)
        {
            combined = new DjAxis(model, Enabled, Input.Generate(model), Color.FromUInt32(LedOn),
                Color.FromUInt32(LedOff), LedIndex, LedIndexPeripheral, LedIndexMpr121, DeadzoneOrMultiplier, LedMultiplier,
                Type, OutputEnabled, OutputPeripheral, OutputInverted, OutputPin, ChildOfCombined);
        }
        else
        {
            combined = new DjAxis(model, Enabled, Input.Generate(model), Color.FromUInt32(LedOn),
                Color.FromUInt32(LedOff), LedIndex, LedIndexPeripheral, LedIndexMpr121, Min, Max, DeadzoneOrMultiplier,
                Type, OutputEnabled, OutputPeripheral, OutputInverted, OutputPin, ChildOfCombined);
        }
        
        model.Bindings.Add(combined);
        return combined;
    }
}