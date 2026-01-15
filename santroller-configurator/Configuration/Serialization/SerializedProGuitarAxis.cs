using System;
using System.ComponentModel;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedProGuitarAxis : SerializedOutput
{
    public SerializedProGuitarAxis()
    {
        LedIndex = [];
        LedIndexMpr121 = [];
        LedIndexPeripheral = [];
    }
    public SerializedProGuitarAxis(SerializedInput input, Color ledOn, Color ledOff, byte[] ledIndex, byte[] ledIndexPeripheral, int min, int max, int deadzone,
        ProGuitarType type, bool outputEnabled, int outputPin, bool outputInverted, bool outputPeripheral, bool childOfCombined, byte[] ledIndexMpr121)
    {
        Input = input;
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        LedIndex = ledIndex;
        LedIndexPeripheral = ledIndexPeripheral;
        Type = type;
        ChildOfCombined = childOfCombined;
        LedIndexMpr121 = ledIndexMpr121;
        OutputEnabled = outputEnabled;
        OutputPin = outputPin;
        OutputInverted = outputInverted;
        OutputPeripheral = outputPeripheral;
        Min = min;
        Max = max;
        Deadzone = deadzone;
    }

    [ProtoMember(1)] public SerializedInput? Input { get; }
    [ProtoMember(2)] public uint LedOn { get; }
    [ProtoMember(3)] public uint LedOff { get; }
    [ProtoMember(5)] public ProGuitarType Type { get; }
    [ProtoMember(6)] public byte[] LedIndex { get; }
    [ProtoMember(7)] public bool ChildOfCombined { get; }
    [ProtoMember(8)] public byte[] LedIndexPeripheral { get; }
    [ProtoMember(10)] public bool OutputEnabled { get; }
    [ProtoMember(11)] public int OutputPin { get; }
    [ProtoMember(12)] public bool OutputInverted { get; }
    [ProtoMember(13)] public bool OutputPeripheral { get; }
    
    [ProtoMember(14)] public byte[] LedIndexMpr121 { get; }
    [ProtoMember(15)] [DefaultValue(true)] private bool Enabled { get; } = true;
    [ProtoMember(16)] public int Min { get; }
    [ProtoMember(17)] public int Max { get; }
    [ProtoMember(18)] public int Deadzone { get; }

    public override Output Generate(ConfigViewModel model)
    {
        if (Input == null)
        {
            throw new NotImplementedException("Null child unexpected!");
        }
        var combined = new ProGuitarAxis(model, Enabled,Input.Generate(model), Color.FromUInt32(LedOn),
            Color.FromUInt32(LedOff), LedIndex, LedIndexPeripheral, LedIndexMpr121,  Min, Max, Deadzone, Type, OutputEnabled, OutputPeripheral, OutputInverted, OutputPin, ChildOfCombined);
        model.Bindings.Add(combined);
        return combined;
    }
}