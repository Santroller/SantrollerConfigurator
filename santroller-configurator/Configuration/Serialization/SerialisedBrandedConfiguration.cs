using GuitarConfigurator.NetCore.Utils;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerialisedBrandedConfiguration
{
    [ProtoMember(1)] public string VendorName;
    [ProtoMember(2)] public string ProductName;
    [ProtoMember(3)] public SerializedConfiguration Configuration;
    [ProtoMember(4)] public Uf2Block[] Uf2 { get; private set; }
    public SerialisedBrandedConfiguration(BrandedConfiguration.BrandedConfiguration configuration)
    {
        Configuration = new SerializedConfiguration(configuration.Model);
        Uf2 = configuration.Uf2;
        VendorName = configuration.VendorName;
        ProductName = configuration.ProductName;
    }
}