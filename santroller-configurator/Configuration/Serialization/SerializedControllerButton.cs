using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedControllerButton : SerializedOutput
{
    public SerializedControllerButton(SerializedInput input, Color ledOn, Color ledOff, byte[] ledIndex, byte debounce,
        StandardButtonType type, bool childOfCombined)
    {
        Input = input;
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        LedIndex = ledIndex;
        Debounce = debounce;
        Type = type;
        ChildOfCombined = childOfCombined;
    }

    [ProtoMember(1)] public SerializedInput Input { get; }
    [ProtoMember(2)] public uint LedOn { get; }
    [ProtoMember(3)] public uint LedOff { get; }
    [ProtoMember(4)] public byte Debounce { get; }
    [ProtoMember(5)] public StandardButtonType Type { get; }
    [ProtoMember(6)] public byte[] LedIndex { get; }
    [ProtoMember(7)] public bool ChildOfCombined { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var output = new ControllerButton(model, Input.Generate(model), Color.FromUInt32(LedOn),
            Color.FromUInt32(LedOff), LedIndex, Debounce, Type, ChildOfCombined);
        model.Bindings.Add(output);
        return output;
    }
}