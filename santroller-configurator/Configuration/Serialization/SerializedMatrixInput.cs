using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedMatrixInput : SerializedInput
{
    public SerializedMatrixInput()
    {
        
    }
    public SerializedMatrixInput(int pin, int outPin, bool inverted)
    {
        Pin = pin;
        Inverted = inverted;
        OutPin = outPin;
    }

    [ProtoMember(1)] private int Pin { get; }
    [ProtoMember(3)] private bool Inverted { get; }
    [ProtoMember(4)] private int OutPin { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new MatrixInput(Pin, OutPin, Inverted, model);
    }
}