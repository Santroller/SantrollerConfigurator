using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedLed : SerializedOutput
{
    public SerializedLed(Color ledOn, Color ledOff, byte[] ledIndex, byte[] ledIndexPeripheral, LedCommandType type, int param1, int param2,
        bool outputEnabled, bool peripheral, bool inverted, int pin)
    {
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        LedIndex = ledIndex;
        LedIndexPeripheral = ledIndexPeripheral;
        Type = type;
        OutputEnabled = outputEnabled;
        Pin = pin;
        Param1 = param1;
        Param2 = param2;
        Inverted = inverted;
        Peripheral = peripheral;
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

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new Led(model, OutputEnabled, Inverted, Pin, Peripheral, Color.FromUInt32(LedOn), Color.FromUInt32(LedOff),
            LedIndex, LedIndexPeripheral, Type, Param1, Param2);
        model.Bindings.Add(combined);
        return combined;
    }
}