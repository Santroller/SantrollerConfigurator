using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedStartSelectHome : SerializedOutput
{
    public SerializedStartSelectHome(bool wii, bool peripheral)
    {
        Wii = wii;
        Peripheral = peripheral;
    }

    [ProtoMember(2)] public bool Wii { get; }
    [ProtoMember(8)] private bool Peripheral { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new StartSelectHome(model, Peripheral, Wii);
        model.Bindings.Add(combined);
        return combined;
    }
}