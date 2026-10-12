using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedWtDrumInput : SerializedInput
{
    public SerializedWtDrumInput(int miso, int mosi, int sck, int cs, DrumBrainInputType type)
    {
        Miso = miso;
        Mosi = mosi;
        Sck = sck;
        Cs = cs;
        Type = type;
    }

    [ProtoMember(1)] private int Miso { get; }
    [ProtoMember(2)] private int Mosi { get; }
    [ProtoMember(3)] private int Sck { get; }
    [ProtoMember(4)] private int Cs { get; }
    [ProtoMember(5)] private DrumBrainInputType Type { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new WtDrumInput(Type, model, Miso, Mosi, Sck, Cs);
    }
}
