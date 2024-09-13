using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;

public partial class BrandedConfigurationSection : ReactiveObject
{
    public ObservableCollection<BrandedConfiguration> Configurations { get; } = new();
    [Reactive] private string _name;

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