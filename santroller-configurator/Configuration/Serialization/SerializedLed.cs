using System;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedLed : SerializedOutput
{
    public SerializedLed()
    {
        LedIndex = [];
        LedIndexPeripheral = [];
        LedIndexMpr121 = [];
    }
    public SerializedLed(bool enabled, Color ledOn, Color ledOff, byte[] ledIndex, byte[] ledIndexPeripheral, LedCommandType type, int param1, int param2,
        bool outputEnabled, bool peripheral, bool inverted, int pin, byte[] ledIndexMpr121)
    {
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        LedIndex = ledIndex;
        LedIndexPeripheral = ledIndexPeripheral;
        Type = type;
        OutputEnabled = outputEnabled;
        Pin = pin;
        LedIndexMpr121 = ledIndexMpr121;
        Param1 = param1;
        Param2 = param2;
        Inverted = inverted;
        Peripheral = peripheral;
        Enabled = enabled;
    }

    [ProtoMember(1)] public uint LedOn { get; }
    [ProtoMember(2)] public uint LedOff { get; }
    [ProtoMember(3)] public byte[] LedIndex { get; }
    [ProtoMember(4)] public LedCommandType Type { get; }
    [ProtoMember(5)] public bool OutputEnabled { get; }
    [ProtoMember(6)] public int Pin { get; }
    [ProtoMember(7)] public int Param1 { get; }
    [ProtoMember(8)] public int Param2 { get; }
    [ProtoMember(9)] public bool Inverted { get; }
    [ProtoMember(10)] public bool Peripheral { get; }
    [ProtoMember(11)] public byte[] LedIndexPeripheral { get; }
    
    [ProtoMember(12)] public byte[] LedIndexMpr121 { get; }
    [ProtoMember(13)] private bool Enabled { get; } = true;

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new Led(model, Enabled, OutputEnabled, Inverted, Pin, Peripheral, Color.FromUInt32(LedOn), Color.FromUInt32(LedOff),
            LedIndex, LedIndexPeripheral, LedIndexMpr121, Type, Param1, Param2);
        model.Bindings.Add(combined);
        return combined;
    }
}