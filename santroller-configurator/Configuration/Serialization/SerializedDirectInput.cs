using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedDirectInput : SerializedInput
{
    public SerializedDirectInput(int pin, bool peripheral, bool inverted, DevicePinMode pinMode)
    {
        Pin = pin;
        PinMode = pinMode;
        Inverted = inverted;
        Peripheral = peripheral;
    }

    [ProtoMember(1)] private int Pin { get; }
    [ProtoMember(2)] private DevicePinMode PinMode { get; }
    [ProtoMember(3)] private bool Inverted { get; }
    [ProtoMember(4)] private bool Peripheral { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new DirectInput(Pin, Inverted, Peripheral, PinMode, model);
    }
}