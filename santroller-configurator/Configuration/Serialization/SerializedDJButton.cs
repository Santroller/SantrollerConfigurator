using System;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedDjButton : SerializedOutput
{
    public SerializedDjButton(SerializedInput input, Color ledOn, Color ledOff, byte[] ledIndex, byte[] ledIndexPeripheral, int debounce,
        DjInputType type, bool outputEnabled, int outputPin, bool outputInverted, bool outputPeripheral, bool childOfCombined, byte[] ledIndexMpr121)
    {
        Input = input;
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        LedIndex = ledIndex;
        LedIndexPeripheral = ledIndexPeripheral;
        Debounce = debounce;
        Type = type;
        ChildOfCombined = childOfCombined;
        LedIndexMpr121 = ledIndexMpr121;
        OutputEnabled = outputEnabled;
        OutputPin = outputPin;
        OutputInverted = outputInverted;
        OutputPeripheral = outputPeripheral;
    }

    [ProtoMember(1)] public SerializedInput Input { get; }
    [ProtoMember(2)] public uint LedOn { get; }
    [ProtoMember(3)] public uint LedOff { get; }
    [ProtoMember(5)] public DjInputType Type { get; }
    [ProtoMember(6)] public byte[] LedIndex { get; }
    [ProtoMember(7)] public bool ChildOfCombined { get; }
    [ProtoMember(8)] public byte[] LedIndexPeripheral { get; }
    [ProtoMember(9)] public int Debounce { get; }
    [ProtoMember(10)] public bool OutputEnabled { get; }
    [ProtoMember(11)] public int OutputPin { get; }
    [ProtoMember(12)] public bool OutputInverted { get; }
    [ProtoMember(13)] public bool OutputPeripheral { get; }
    
    [ProtoMember(14)] public byte[] LedIndexMpr121 { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new DjButton(model, Input.Generate(model), Color.FromUInt32(LedOn),
            Color.FromUInt32(LedOff), LedIndex, LedIndexPeripheral, LedIndexMpr121 ?? Array.Empty<byte>(), Debounce, Type, OutputEnabled, OutputPeripheral, OutputInverted, OutputPin, ChildOfCombined);
        model.Bindings.Add(combined);
        return combined;
    }
}