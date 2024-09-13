using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;

public partial class BrandedConfigurationStore : ReactiveObject
{
    public BrandedConfigurationStore(string toolName, Color primaryColor, Color warningColor, Color errorColor)
    {
        ToolName = toolName;
        PrimaryColor = primaryColor;
        WarningColor = warningColor;
        ErrorColor = errorColor;
        Logo = new Bitmap(AssetLoader.Open(new Uri("avares://SantrollerConfigurator/Assets/Icons/logo.png")));
        Icon = new Bitmap(AssetLoader.Open(new Uri("avares://SantrollerConfigurator/Assets/icon.png")));
        _toolNameVersionedHelper = this.WhenAnyValue(x => x.ToolName).Select(s => s + " - v" + GitVersionInformation.SemVer)
            .ToProperty(this, x => x.ToolNameVersioned);
    }

    public BrandedConfigurationStore(SerialisedBrandedConfigurationStore store, bool branded,
        MainWindowViewModel screen)
    {
        ToolName = store.ToolName;
        PrimaryColor = store.PrimaryColor;
        WarningColor = store.WarningColor;
        ErrorColor = store.ErrorColor;
        if (store.Logo.Length != 0)
        {
            var stream = new MemoryStream(store.Logo);
            Logo = new Bitmap(stream);
            stream = new MemoryStream(store.Icon);
            Icon = new Bitmap(stream);
        }
        else
        {
            Logo = new Bitmap(AssetLoader.Open(new Uri("avares://SantrollerConfigurator/Assets/Icons/logo.png")));
            Icon = new Bitmap(AssetLoader.Open(new Uri("avares://SantrollerConfigurator/Assets/icon.png")));
        }

        if (store.OldConfigurations.Count != 0)
        {
            Configurations.Add(new BrandedConfigurationSection("Type Name", store.OldConfigurations.Select(s => new BrandedConfiguration(s, branded, screen))));   
        }
        else
        {
            Configurations.AddRange(
                store.Configurations.Select(s => new BrandedConfigurationSection(s, branded, screen)));
        }

        _toolNameVersionedHelper = this.WhenAnyValue(x => x.ToolName).Select(s => s + " - v" + GitVersionInformation.SemVer)
            .ToProperty(this, x => x.ToolNameVersioned);
    }

    [ObservableAsProperty] private string _toolNameVersioned = null!;
    [Reactive] private string _toolName;

    [Reactive] private Color _warningColor;

    [Reactive] private Color _primaryColor;

    [Reactive] private Color _errorColor;

    [Reactive] private Bitmap _logo;

    [Reactive] private Bitmap _icon;

    public WindowIcon WindowIcon => new(Icon);
    public ObservableCollection<BrandedConfigurationSection> Configurations { get; } = new();

    public static BrandedConfigurationStore LoadBranding(MainWindowViewModel model)
    {
#if !OSX && SINGLE_FILE
        var stream = File.OpenRead(Environment.ProcessPath!);
        var reader = new BinaryReader(stream);
        stream.Seek(-sizeof(int), SeekOrigin.End);
        var offset = reader.ReadInt32();
        stream.Seek(offset, SeekOrigin.Begin);
#else
        var path = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "branding.bin");
        if (!File.Exists(path))
        {
            path = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Environment.ProcessPath)!)!, "Resources", "branding.bin");
        }
        var stream = File.OpenRead(path);
#endif
        return new BrandedConfigurationStore(
            Serializer.DeserializeWithLengthPrefix<SerialisedBrandedConfigurationStore>(stream, PrefixStyle.Base128),
            true,
            model);
    }
}