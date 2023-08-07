using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using ProtoBuf;

namespace SantrollerConfiguratorBuilder.NetCore;

[ProtoContract]
public class SerialisedBuilderConfig
{
    [ProtoMember(1)] public List<SerialisedBrandedConfigurationStore> Configurations { get; } = new();

    public SerialisedBuilderConfig()
    {
    }

    public SerialisedBuilderConfig(BuilderConfig config)
    {
        Configurations.AddRange(config.Configurations.Select(s => new SerialisedBrandedConfigurationStore(s)).ToList());
    }
}