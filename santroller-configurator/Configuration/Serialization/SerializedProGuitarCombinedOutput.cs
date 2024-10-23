using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedProGuitarCombinedOutput : SerializedOutput
{
    public SerializedProGuitarCombinedOutput()
    {
        
    }
    public override Output Generate(ConfigViewModel model)
    {
        var combined = new ProGuitarCombinedOutput(model);
        model.Bindings.Add(combined);
        return combined;
    }
}