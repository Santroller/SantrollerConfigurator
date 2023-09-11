using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedBluetoothOutput : SerializedOutput
{
    public override Output Generate(ConfigViewModel model)
    {
        var output = new BluetoothOutput(model);
        model.Bindings.Add(output);
        return output;
    }
}