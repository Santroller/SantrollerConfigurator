using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedUsbHostInput : SerializedInput
{
    public SerializedUsbHostInput(UsbHostInputType type, bool combined)
    {
        Type = type;
        Combined = combined;
    }

    [ProtoMember(3)] private UsbHostInputType Type { get; }
    [ProtoMember(4)] public bool Combined { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new UsbHostInput(Type, model, Combined);
    }
}