using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedReset : SerializedOutput
{
    public SerializedReset(SerializedInput input)
    {
        Input = input;
    }

    [ProtoMember(1)] public SerializedInput Input { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var output = new Reset(model, Input.Generate(model));
        model.Bindings.Add(output);
        return output;
    }
}