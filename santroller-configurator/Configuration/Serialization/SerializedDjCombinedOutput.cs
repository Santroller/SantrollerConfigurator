using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedDjCombinedOutput : SerializedOutput
{
    public SerializedDjCombinedOutput()
    {
        Outputs = [];
    }
    public SerializedDjCombinedOutput(bool peripheral, int sda, int scl, List<Output> outputs)
    {
        Sda = sda;
        Scl = scl;
        Outputs = outputs.Select(s => s.Serialize()).ToList();
        Peripheral = peripheral;
    }

    [ProtoMember(4)] public int Sda { get; }
    [ProtoMember(5)] public int Scl { get; }
    [ProtoMember(6)] public List<SerializedOutput> Outputs { get; }
    [ProtoMember(8)] private bool Peripheral { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new DjCombinedOutput(model, Peripheral, Sda, Scl);
        model.Bindings.Add(combined);
        // Since we filter out sda and scl from inputs for size, we need to make sure its assigned before we construct the inputs.
        model.Microcontroller.AssignTwiPins(model, DjInput.DjTwiType, Peripheral, Sda, Scl, DjInput.DjTwiFreq, false);
        combined.SetOutputsOrDefaults(Outputs.Select(s => s.Generate(model)));
        return combined;
    }
}