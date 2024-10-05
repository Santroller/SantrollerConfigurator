using System;
using Avalonia.Input;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedKeyboardButton : SerializedOutput
{
    public SerializedKeyboardButton(SerializedInput input, byte[] ledIndex, byte[] ledIndexPeripheral, byte[] ledIndexMpr121)
    {
        Input = input;
        LedIndex = ledIndex;
        LedIndexPeripheral = ledIndexPeripheral;
        LedIndexMpr121 = ledIndexMpr121;
    }
    public SerializedKeyboardButton(SerializedInput input, bool enabled, Color ledOn, Color ledOff, byte[] ledIndex,
        byte[] ledIndexPeripheral, int debounce,
        Key type, bool outputEnabled, int outputPin, bool outputInverted, bool outputPeripheral, byte[] ledIndexMpr121)
    {
        Input = input;
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        Debounce = debounce;
        Type = type;
        LedIndex = ledIndex;
        LedIndexPeripheral = ledIndexPeripheral;
        OutputEnabled = outputEnabled;
        OutputPin = outputPin;
        OutputInverted = outputInverted;
        OutputPeripheral = outputPeripheral;
        LedIndexMpr121 = ledIndexMpr121;
        Enabled = enabled;
    }

    [ProtoMember(1)] public SerializedInput Input { get; }
    [ProtoMember(2)] public uint LedOn { get; }
    [ProtoMember(3)] public uint LedOff { get; }

    [ProtoMember(6)] public byte[] LedIndex { get; }
    [ProtoMember(5)] public Key Type { get; }
    [ProtoMember(7)] public byte[] LedIndexPeripheral { get; }
    [ProtoMember(8)] public int Debounce { get; }
    [ProtoMember(9)] public bool OutputEnabled { get; }
    [ProtoMember(10)] public int OutputPin { get; }
    [ProtoMember(11)] public bool OutputInverted { get; }
    [ProtoMember(12)] public bool OutputPeripheral { get; }

    [ProtoMember(13)] public byte[] LedIndexMpr121 { get; }
    [ProtoMember(14)] private bool Enabled { get; } = true;

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new KeyboardButton(model, Enabled, Input.Generate(model), Color.FromUInt32(LedOn),
            Color.FromUInt32(LedOff), LedIndex, LedIndexPeripheral, LedIndexMpr121, Debounce,
            Type, OutputEnabled, OutputPeripheral, OutputInverted, OutputPin);
        model.Bindings.Add(combined);
        return combined;
    }
}