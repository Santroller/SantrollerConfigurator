using Avalonia.Input;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedKeyboardButton : SerializedOutput
{
    public SerializedKeyboardButton(SerializedInput input, Color ledOn, Color ledOff, byte[] ledIndex, byte debounce,
        Key type)
    {
        Input = input;
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        Debounce = debounce;
        Type = type;
        LedIndex = ledIndex;
    }

    [ProtoMember(1)] public SerializedInput Input { get; }
    [ProtoMember(2)] public uint LedOn { get; }
    [ProtoMember(3)] public uint LedOff { get; }

    [ProtoMember(6)] public byte[] LedIndex { get; }
    [ProtoMember(4)] public byte Debounce { get; }
    [ProtoMember(5)] public Key Type { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new KeyboardButton(model, Input.Generate(model), Color.FromUInt32(LedOn),
            Color.FromUInt32(LedOff), LedIndex, Debounce, Type);
        model.Bindings.Add(combined);
        return combined;
    }
}