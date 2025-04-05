using Avalonia.Input;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedUsbHostInput : SerializedInput
{
    public SerializedUsbHostInput()
    {
        
    }
    public SerializedUsbHostInput(UsbHostInputType type, Key key, MouseButtonType mouseButtonType,
        MouseAxisType mouseAxisType, bool combined)
    {
        Type = type;
        Combined = combined;
        Key = key;
        MouseButtonType = mouseButtonType;
        MouseAxisType = mouseAxisType;
    }

    [ProtoMember(3)] private UsbHostInputType Type { get; }
    [ProtoMember(4)] public bool Combined { get; }
    [ProtoMember(5)] private Key Key { get; }
    [ProtoMember(6)] private MouseButtonType MouseButtonType { get; }
    [ProtoMember(7)] private MouseAxisType MouseAxisType { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return Type switch
        {
            UsbHostInputType.KeyboardInput => new UsbHostInput(Key, model, Combined),
            UsbHostInputType.MouseAxis => new UsbHostInput(MouseAxisType, model, Combined),
            UsbHostInputType.MouseButton => new UsbHostInput(MouseButtonType, model, Combined),
            _ => new UsbHostInput(Type, model, Combined)
        };
    }
}