using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerialisedBrandedConfigurationSection
{
    public SerialisedBrandedConfigurationSection()
    {
        Name = "";
        Configurations = [];
    }
    [ProtoMember(1)] public string Name;
    [ProtoMember(2)] public List<SerialisedBrandedConfiguration> Configurations;

    public SerialisedBrandedConfigurationSection(BrandedConfigurationSection section)
    {
        Name = section.Name;
        Configurations = section.Configurations.Select(s => new SerialisedBrandedConfiguration(s)).ToList();
    }
}