using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedMpr121Input : SerializedInput
{
    public SerializedMpr121Input(bool peripheral, int input)
    {
        Input = input;
        Peripheral = peripheral;
    }

    [ProtoMember(3)] private int Input { get; }
    [ProtoMember(4)] private bool Peripheral { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new Mpr121Input(Input, model, Peripheral);
    }
}