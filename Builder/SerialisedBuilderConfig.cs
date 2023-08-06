using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;
using ProtoBuf;

namespace SantrollerConfiguratorBuilder.NetCore;

[ProtoContract(SkipConstructor = true)]
public class SerialisedBuilderConfig
{
    [ProtoMember(1)] public List<SerialisedBrandedConfigurationStore> Configurations { get; }

    public SerialisedBuilderConfig(BuilderConfig config)
    {
        Configurations = config.Configurations.Select(s => new SerialisedBrandedConfigurationStore(s)).ToList();
    }
}