using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedRbButton : SerializedOutput
{
    public SerializedRbButton(SerializedInput input, Color ledOn, Color ledOff, byte[] ledIndex, byte[] ledIndexPeripheral, int debounce,
        InstrumentButtonType type, bool childOfCombined)
    {
        Input = input;
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        LedIndex = ledIndex;
        LedIndexPeripheral = ledIndexPeripheral;
        Debounce = debounce;
        Type = type;
        ChildOfCombined = childOfCombined;
    }

    [ProtoMember(1)] public SerializedInput Input { get; }
    [ProtoMember(2)] public uint LedOn { get; }
    [ProtoMember(3)] public uint LedOff { get; }
    [ProtoMember(5)] public InstrumentButtonType Type { get; }
    [ProtoMember(6)] public byte[] LedIndex { get; }
    [ProtoMember(8)] public byte[] LedIndexPeripheral { get; }
    [ProtoMember(7)] public bool ChildOfCombined { get; }
    [ProtoMember(8)] public int Debounce { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new GuitarButton(model, Input.Generate(model), Color.FromUInt32(LedOn),
            Color.FromUInt32(LedOff), LedIndex, LedIndexPeripheral, Debounce, Type, ChildOfCombined);
        model.Bindings.Add(combined);
        return combined;
    }
}