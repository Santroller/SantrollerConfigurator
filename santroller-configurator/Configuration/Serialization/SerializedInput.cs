using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
[ProtoInclude(101, typeof(SerializedAnalogToDigital))]
[ProtoInclude(102, typeof(SerializedDigitalToAnalog))]
[ProtoInclude(103, typeof(SerializedDirectInput))]
[ProtoInclude(104, typeof(SerializedDjInput))]
[ProtoInclude(105, typeof(SerializedGh5NeckInput))]
[ProtoInclude(106, typeof(SerializedGhWtInput))]
[ProtoInclude(107, typeof(SerializedPs2Input))]
[ProtoInclude(108, typeof(SerializedWiiInput))]
[ProtoInclude(109, typeof(SerializedWiiInputCombined))]
[ProtoInclude(110, typeof(SerializedPs2InputCombined))]
[ProtoInclude(111, typeof(SerializedGhWtInputCombined))]
[ProtoInclude(112, typeof(SerializedGh5NeckInputCombined))]
[ProtoInclude(113, typeof(SerializedDjInputCombined))]
[ProtoInclude(114, typeof(SerializedMacroInput))]
[ProtoInclude(115, typeof(SerializedMultiplexerInput))]
[ProtoInclude(116, typeof(SerializedUsbHostInput))]
[ProtoInclude(117, typeof(SerializedConstantInput))]
public abstract class SerializedInput
{
    public abstract Input Generate(ConfigViewModel model);
}