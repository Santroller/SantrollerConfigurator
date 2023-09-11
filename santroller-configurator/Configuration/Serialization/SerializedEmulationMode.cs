using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedEmulationMode : SerializedOutput
{
    public SerializedEmulationMode(EmulationModeType type, SerializedInput input)
    {
        Input = input;
        Type = type;
    }

    [ProtoMember(1)] public SerializedInput Input { get; }
    [ProtoMember(2)] public EmulationModeType Type { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var output = new EmulationMode(model, Input.Generate(model), Type);
        model.Bindings.Add(output);
        return output;
    }
}