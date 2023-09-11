using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedWiiInputCombined : SerializedInput
{
    public SerializedWiiInputCombined(WiiInputType type, bool peripheral)
    {
        Type = type;
        Peripheral = peripheral;
    }

    [ProtoMember(3)] private WiiInputType Type { get; }
    [ProtoMember(4)] private bool Peripheral { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new WiiInput(Type, model, Peripheral, combined: true);
    }
}