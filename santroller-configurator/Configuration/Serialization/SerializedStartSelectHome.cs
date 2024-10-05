using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedStartSelectHome : SerializedOutput
{
    public SerializedStartSelectHome()
    {
        
    }
    public SerializedStartSelectHome(bool wii, bool peripheral, bool enabled)
    {
        Wii = wii;
        Peripheral = peripheral;
        Enabled = enabled;
    }

    [ProtoMember(2)] public bool Wii { get; }
    [ProtoMember(8)] private bool Peripheral { get; }
    [ProtoMember(9)] private bool Enabled { get; } = true;

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new StartSelectHome(model, Enabled, Peripheral, Wii);
        model.Bindings.Add(combined);
        return combined;
    }
}