using System;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedEmulationMode : SerializedOutput
{
    public SerializedEmulationMode()
    {
    }
    public SerializedEmulationMode(EmulationModeType type, SerializedInput input, bool enabled)
    {
        Input = input;
        Type = type;
        Enabled = enabled;
    }

    [ProtoMember(1)] public SerializedInput? Input { get; }
    [ProtoMember(2)] public EmulationModeType Type { get; }
    [ProtoMember(3)] private bool Enabled { get; } = true;

    public override Output Generate(ConfigViewModel model)
    {
        if (Input == null)
        {
            throw new NotImplementedException("Null child unexpected!");
        }
        var output = new EmulationMode(model, Enabled, Input.Generate(model), Type);
        model.Bindings.Add(output);
        return output;
    }
}