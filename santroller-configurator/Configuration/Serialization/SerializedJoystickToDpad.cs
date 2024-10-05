using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedJoystickToDpad : SerializedOutput
{
    public SerializedJoystickToDpad()
    {
        
    }
    public SerializedJoystickToDpad(int threshold, bool wii, bool peripheral, bool enabled)
    {
        Threshold = threshold;
        Wii = wii;
        Peripheral = peripheral;
        Enabled = enabled;
    }

    [ProtoMember(1)] public int Threshold { get; }
    [ProtoMember(2)] public bool Wii { get; }
    [ProtoMember(8)] private bool Peripheral { get; }
    [ProtoMember(9)] private bool Enabled { get; } = true;

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new JoystickToDpad(model, Enabled, Peripheral, Threshold, Wii);
        model.Bindings.Add(combined);
        return combined;
    }
}