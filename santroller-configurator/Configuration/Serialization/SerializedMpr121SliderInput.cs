using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedMpr121SliderInput : SerializedInput
{
    public SerializedMpr121SliderInput()
    {
        
    }
    public SerializedMpr121SliderInput(bool peripheral, int inputGreen, int inputRed, int inputYellow, int inputBlue, int inputOrange)
    {
        Peripheral = peripheral;
        InputGreen = inputGreen;
        InputRed = inputRed;
        InputYellow = inputYellow;
        InputBlue = inputBlue;
        InputOrange = inputOrange;
    }

    [ProtoMember(3)] private int InputGreen { get; }
    [ProtoMember(4)] private int InputRed { get; }
    [ProtoMember(5)] private int InputYellow { get; }
    [ProtoMember(6)] private int InputBlue { get; }
    [ProtoMember(7)] private int InputOrange { get; }
    [ProtoMember(8)] private bool Peripheral { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new Mpr121SliderInput(model, Peripheral, InputGreen, InputRed, InputYellow, InputBlue, InputOrange);
    }
}