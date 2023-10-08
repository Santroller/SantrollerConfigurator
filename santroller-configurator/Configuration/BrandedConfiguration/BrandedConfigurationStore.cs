using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;

public class BrandedConfigurationStore : ReactiveObject
{
    public BrandedConfigurationStore(string toolName, Color primaryColor, Color warningColor, Color errorColor)
    {
        ToolName = toolName;
        PrimaryColor = primaryColor;
        WarningColor = warningColor;
        ErrorColor = errorColor;
        Logo = new Bitmap(AssetLoader.Open(new Uri($"avares://SantrollerConfigurator/Assets/Icons/logo.png")));
    }

    public BrandedConfigurationStore(SerialisedBrandedConfigurationStore store, bool branded,
        MainWindowViewModel screen)
    {
        ToolName = store.ToolName;
        PrimaryColor = store.PrimaryColor;
        WarningColor = store.WarningColor;
        ErrorColor = store.ErrorColor;
        if (store.Logo.Any())
        {
            var stream = new MemoryStream(store.Logo);
            Logo = new Bitmap(stream);
        }
        else
        {
            Logo = new Bitmap(AssetLoader.Open(new Uri($"avares://SantrollerConfigurator/Assets/Icons/logo.png")));
        }

        Configurations.AddRange(store.Configurations.Select(s => new BrandedConfiguration(s, branded, screen)));
    }

    [Reactive]
    public string ToolName { get; set; }
    
    [Reactive]
    public Color WarningColor { get; set; }
    
    [Reactive]
    public Color PrimaryColor { get; set; }
    
    [Reactive]
    public Color ErrorColor { get; set; }
    
    [Reactive]
    public Bitmap Logo { get; set; }

    public WindowIcon Icon => new(Logo);
    public ObservableCollection<BrandedConfiguration> Configurations { get; } = new();

    public static BrandedConfigurationStore LoadBranding(MainWindowViewModel model)
    {
#if SINGLE_FILE
        var stream = File.OpenRead(Environment.ProcessPath!);
        var reader = new BinaryReader(stream);
        stream.Seek(-sizeof(int), SeekOrigin.End);
        var offset = reader.ReadInt32();
        stream.Seek(offset, SeekOrigin.Begin);
#else
        var path = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "branding.bin");
        var stream = File.OpenRead(path);
#endif
        return new BrandedConfigurationStore(Serializer.DeserializeWithLengthPrefix<SerialisedBrandedConfigurationStore>(stream, PrefixStyle.Base128), true,
            model);
    }
}