using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedDjInput : SerializedInput
{
    public SerializedDjInput(bool peripheral, int sda, int scl, DjInputType type, bool smoothing)
    {
        Sda = sda;
        Scl = scl;
        Type = type;
        Smoothing = smoothing;
        Peripheral = peripheral;
    }

    [ProtoMember(1)] private int Sda { get; }
    [ProtoMember(2)] private int Scl { get; }
    [ProtoMember(3)] private DjInputType Type { get; }
    [ProtoMember(4)] private bool Smoothing { get; }
    [ProtoMember(5)] private bool Peripheral { get; }


    public override Input Generate(ConfigViewModel model)
    {
        return new DjInput(Type, model, Peripheral, Smoothing, Sda, Scl);
    }
}