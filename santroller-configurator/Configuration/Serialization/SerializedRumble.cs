using System.ComponentModel;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedRumble : SerializedOutput
{
    public SerializedRumble()
    {
        
    }
    public SerializedRumble(RumbleMotorType type, int pin, bool peripheral)
    {
        Type = type;
        Pin = pin;
        Peripheral = peripheral;
    }

    [ProtoMember(1)] public RumbleMotorType Type { get; }
    [ProtoMember(2)] public int Pin { get; }
    [ProtoMember(3)] public bool Peripheral { get; }
    [ProtoMember(4)] [DefaultValue(true)] private bool Enabled { get; } = true;

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new Rumble(model, Enabled, Pin, Peripheral, Type);
        model.Bindings.Add(combined);
        return combined;
    }
}