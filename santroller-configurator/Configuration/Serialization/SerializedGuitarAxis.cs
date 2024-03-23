using System;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedGuitarAxis : SerializedOutput
{
    public SerializedGuitarAxis(SerializedInput input, GuitarAxisType type, int pickupSelectorNotch2, int pickupSelectorNotch3, int pickupSelectorNotch4, int pickupSelectorNotch5,
        Color ledOn, Color ledOff, byte[] ledIndex, byte[] ledIndexPeripheral, bool invert, int min, int max,
        int deadzone, bool outputEnabled, int outputPin, bool outputInverted, bool outputPeripheral,
        bool childOfCombined, byte[] ledIndexMpr121)
    {
        Input = input;
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        Invert = invert;
        Min = min;
        Max = max;
        Deadzone = deadzone;
        Type = type;
        LedIndex = ledIndex;
        LedIndexPeripheral = ledIndexPeripheral;
        ChildOfCombined = childOfCombined;
        LedIndexMpr121 = ledIndexMpr121;
        PickupSelectorNotch2 = pickupSelectorNotch2;
        PickupSelectorNotch3 = pickupSelectorNotch3;
        PickupSelectorNotch4 = pickupSelectorNotch4;
        PickupSelectorNotch5 = pickupSelectorNotch5;
        OutputEnabled = outputEnabled;
        OutputPin = outputPin;
        OutputInverted = outputInverted;
        OutputPeripheral = outputPeripheral;
    }

    [ProtoMember(1)] public virtual SerializedInput Input { get; }
    [ProtoMember(2)] public virtual uint LedOn { get; }
    [ProtoMember(3)] public virtual uint LedOff { get; }
    [ProtoMember(4)] public virtual byte[] LedIndex { get; }
    [ProtoMember(5)] public int Min { get; }
    [ProtoMember(6)] public int Max { get; }
    [ProtoMember(7)] public int Deadzone { get; }
    [ProtoMember(8)] public bool ChildOfCombined { get; }
    [ProtoMember(10)] public GuitarAxisType Type { get; }
    [ProtoMember(11)] public byte[] LedIndexPeripheral { get; }
    [ProtoMember(12)] public bool Invert { get; }
    [ProtoMember(13)] public bool OutputEnabled { get; }
    [ProtoMember(14)] public int OutputPin { get; }
    [ProtoMember(15)] public bool OutputInverted { get; }
    [ProtoMember(16)] public bool OutputPeripheral { get; }

    [ProtoMember(17)] public byte[] LedIndexMpr121 { get; }
    [ProtoMember(18)] public int PickupSelectorNotch1 { get; }
    [ProtoMember(19)] public int PickupSelectorNotch2 { get; }
    [ProtoMember(20)] public int PickupSelectorNotch3 { get; }
    [ProtoMember(21)] public int PickupSelectorNotch4 { get; }
    [ProtoMember(22)] public int PickupSelectorNotch5 { get; }

    public override Output Generate(ConfigViewModel model)
    {
        if (Type == GuitarAxisType.Pickup)
        {
            var combined = new GuitarAxis(model, Input.Generate(model), PickupSelectorNotch2, PickupSelectorNotch3, PickupSelectorNotch4, PickupSelectorNotch5, Color.FromUInt32(LedOn),
                Color.FromUInt32(LedOff), LedIndex, LedIndexPeripheral, LedIndexMpr121 ?? Array.Empty<byte>(), Min, Max,
                Deadzone,
                Invert, Type, OutputEnabled, OutputPeripheral, OutputInverted, OutputPin, ChildOfCombined);
            model.Bindings.Add(combined);
            return combined;
        }
        var combined2 = new GuitarAxis(model, Input.Generate(model), Color.FromUInt32(LedOn),
            Color.FromUInt32(LedOff), LedIndex, LedIndexPeripheral, LedIndexMpr121 ?? Array.Empty<byte>(), Min, Max,
            Deadzone,
            Invert, Type, OutputEnabled, OutputPeripheral, OutputInverted, OutputPin, ChildOfCombined);
        model.Bindings.Add(combined2);
        return combined2;
    }
}