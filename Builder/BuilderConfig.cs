using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using GuitarConfigurator.NetCore;
using GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace SantrollerConfiguratorBuilder.NetCore;

public class BuilderConfig
{
    public ObservableCollection<BrandedConfigurationStore> Configurations { get; } = new();

    public BuilderConfig(MainWindowViewModel screen)
    {
        var path = Path.Combine(AssetUtils.GetAppDataFolder(), "Builder", "config.bin");
        if (!Path.Exists(path)) return;
        using var stream = File.OpenRead(path);
        Configurations.AddRange(Serializer.Deserialize<SerialisedBuilderConfig>(stream).Configurations.Select(s => new BrandedConfigurationStore(s, false, screen)));
    }


    public void Save()
    {
        var path = Path.Combine(AssetUtils.GetAppDataFolder(), "Builder", "config.bin");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.Delete(path);
        using var stream = File.OpenWrite(path);
        Serializer.Serialize(stream, new SerialisedBuilderConfig(this));
    }
}