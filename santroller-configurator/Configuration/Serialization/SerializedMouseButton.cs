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
public class SerializedMouseButton : SerializedOutput
{
    public SerializedMouseButton()
    {
        LedIndex = [];
        LedIndexMpr121 = [];
        LedIndexPeripheral = [];
    }
    public SerializedMouseButton(SerializedInput input, Color ledOn, Color ledOff, byte[] ledIndex, byte[] ledIndexPeripheral, int debounce,
        MouseButtonType type, bool outputEnabled, int outputPin, bool outputInverted, bool outputPeripheral, byte[] ledIndexMpr121)
    {
        Input = input;
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        LedIndex = ledIndex;
        LedIndexPeripheral = ledIndexPeripheral;
        Debounce = debounce;
        Type = type;
        OutputEnabled = outputEnabled;
        OutputPin = outputPin;
        OutputInverted = outputInverted;
        OutputPeripheral = outputPeripheral;
        LedIndexMpr121 = ledIndexMpr121;
    }

    [ProtoMember(1)] public SerializedInput? Input { get; }
    [ProtoMember(2)] public uint LedOn { get; }
    [ProtoMember(3)] public uint LedOff { get; }
    [ProtoMember(5)] public MouseButtonType Type { get; }
    [ProtoMember(6)] public byte[] LedIndex { get; }
    [ProtoMember(7)] public byte[] LedIndexPeripheral { get; }
    [ProtoMember(8)] public int Debounce { get; }
    [ProtoMember(9)] public bool OutputEnabled { get; }
    [ProtoMember(10)] public int OutputPin { get; }
    [ProtoMember(11)] public bool OutputInverted { get; }
    [ProtoMember(12)] public bool OutputPeripheral { get; }
    
    [ProtoMember(13)] public byte[] LedIndexMpr121 { get; }
    [ProtoMember(14)] [DefaultValue(true)] private bool Enabled { get; } = true;

    public override Output Generate(ConfigViewModel model)
    {
        if (Input == null)
        {
            throw new NotImplementedException("Null child unexpected!");
        }
        var combined = new MouseButton(model, Enabled, Input.Generate(model), Color.FromUInt32(LedOn),
            Color.FromUInt32(LedOff), LedIndex, LedIndexPeripheral, LedIndexMpr121, Debounce, Type, OutputEnabled, OutputPeripheral, OutputInverted, OutputPin);
        model.Bindings.Add(combined);
        return combined;
    }
}