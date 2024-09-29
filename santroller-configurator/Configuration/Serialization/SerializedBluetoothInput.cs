using Avalonia.Input;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedBluetoothInput : SerializedInput
{
    public SerializedBluetoothInput(UsbHostInputType type, Key key, MouseButtonType mouseButtonType,
        MouseAxisType mouseAxisType, ProKeyType proKeyType, bool combined)
    {
        Type = type;
        Combined = combined;
        Key = key;
        MouseButtonType = mouseButtonType;
        MouseAxisType = mouseAxisType;
        ProKeyType = proKeyType;
    }

    [ProtoMember(3)] private UsbHostInputType Type { get; }
    [ProtoMember(4)] public bool Combined { get; }
    [ProtoMember(5)] private Key Key { get; }
    [ProtoMember(6)] private MouseButtonType MouseButtonType { get; }
    [ProtoMember(7)] private MouseAxisType MouseAxisType { get; }
    [ProtoMember(8)] private ProKeyType ProKeyType { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return Type switch
        {
            UsbHostInputType.KeyboardInput => new BluetoothInput(Key, model, Combined),
            UsbHostInputType.MouseAxis => new BluetoothInput(MouseAxisType, model, Combined),
            UsbHostInputType.MouseButton => new BluetoothInput(MouseButtonType, model, Combined),
            UsbHostInputType.ProKey => new BluetoothInput(ProKeyType, model, Combined),
            _ => new BluetoothInput(Type, model, Combined)
        };
    }
}