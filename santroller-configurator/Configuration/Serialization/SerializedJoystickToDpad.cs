using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedJoystickToDpad : SerializedOutput
{
    public SerializedJoystickToDpad(int threshold, bool wii, bool peripheral)
    {
        Threshold = threshold;
        Wii = wii;
        Peripheral = peripheral;
    }

    [ProtoMember(1)] public int Threshold { get; }
    [ProtoMember(2)] public bool Wii { get; }
    [ProtoMember(8)] private bool Peripheral { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new JoystickToDpad(model, Peripheral, Threshold, Wii);
        model.Bindings.Add(combined);
        return combined;
    }
}