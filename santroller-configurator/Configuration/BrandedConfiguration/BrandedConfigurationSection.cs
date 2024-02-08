using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;

public class BrandedConfigurationSection : ReactiveObject
{
    public ObservableCollection<BrandedConfiguration> Configurations { get; } = new();
    [Reactive] public string Name { get; set; }

    public BrandedConfigurationSection(SerialisedBrandedConfigurationSection section, bool branded, MainWindowViewModel screen)
    {
        Configurations.AddRange(section.Configurations.Select(s => new BrandedConfiguration(s, branded, screen)));
        Name = section.Name;
    }

    public BrandedConfigurationSection(string name, IEnumerable<BrandedConfiguration> configs)
    {
        Name = name;
        Configurations.AddRange(configs);
    }
}