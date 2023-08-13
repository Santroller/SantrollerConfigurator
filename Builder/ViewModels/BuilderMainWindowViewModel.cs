using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;
using GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;
using ProtoBuf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SantrollerConfiguratorBuilder.NetCore.ViewModels;

public partial class BuilderMainWindowViewModel : GuitarConfigurator.NetCore.ViewModels.MainWindowViewModel
{
    public BuilderConfig Config { get; }

    [Reactive] public BrandedConfigurationStore? SelectedTool { get; set; }
    [Reactive] public BrandedConfiguration? Selected { get; set; }

    public BuilderMainWindowViewModel() : base(true)
    {
        Config = new BuilderConfig(this);
        if (Config.Configurations.Any())
        {
            SelectedTool = Config.Configurations.First();
        }

        Router.NavigateAndReset.Execute(new BuilderMainViewModel(this));
        this.WhenAnyValue(s => s.SelectedDevice).Subscribe(s =>
        {
            if (Selected != null && SelectedDevice != null)
            {
                Selected.Model.Device = SelectedDevice;
            }
        });
        this.WhenAnyValue(s => s.SelectedTool).Subscribe(s =>
        {
            if (SelectedTool != null && SelectedTool.Configurations.Any())
            {
                Selected = SelectedTool.Configurations.First();
            }
        });
    }

    [RelayCommand]
    public void AddConfig()
    {
        var item = new BrandedConfigurationStore("Tool Name", "Get support by visiting example.com");
        Config.Configurations.Add(item);
        SelectedTool = item;
    }

    [RelayCommand]
    public void RemoveConfig()
    {
        if (SelectedTool == null) return;
        Config.Configurations.Remove(SelectedTool);
        SelectedTool = Config.Configurations.Any() ? Config.Configurations.First() : null;
    }

    [RelayCommand]
    public void AddDevice()
    {
        if (SelectedTool == null) return;
        var item = new BrandedConfiguration("Vendor Name", "Product Name", this);
        SelectedTool.Configurations.Add(item);
        Selected = item;
    }

    [RelayCommand]
    public void RemoveDevice()
    {
        if (SelectedTool == null) return;
        if (Selected == null) return;
        SelectedTool.Configurations.Remove(Selected);
        Selected = SelectedTool.Configurations.Any() ? SelectedTool.Configurations.First() : null;
    }

    [RelayCommand]
    public void StartConfiguring()
    {
        if (SelectedTool == null || Selected == null) return;
        Router.Navigate.Execute(Selected.Model);
    }

    [RelayCommand]
    public void Save()
    {
        Config.Save();
    }

    [RelayCommand]
    public async Task Package()
    {
        if (SelectedTool == null) return;
        var start = 0;
        var steps = 100 / SelectedTool.Configurations.Count;
        foreach (var config in SelectedTool.Configurations)
        {
            config.Model.Variant = config.ProductName;
            await Write(config.Model, config.ExtraConfig(), start, steps);
            config.LoadUf2();
            start += steps;
            Progress = start;
        }
        
        var assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
        var uri = new Uri($"avares://{assemblyName}/Assets/SantrollerConfiguratorBranded-linux-64");
        await using var linuxOutput = File.Open(SelectedTool.ToolName+"-linux-64", FileMode.OpenOrCreate, FileAccess.ReadWrite);
        await using var linuxInput = AssetLoader.Open(uri);
        await linuxInput.CopyToAsync(linuxOutput).ConfigureAwait(false);
        await using var linuxWriter = new BinaryWriter(linuxOutput);
        Serializer.Serialize(linuxOutput, this);
        linuxWriter.Write((int)linuxInput.Length);
        uri = new Uri($"avares://{assemblyName}/Assets/SantrollerConfiguratorBranded-win-64.exe");
        await using var windowsOutput = File.Open(SelectedTool.ToolName+"-win-64", FileMode.OpenOrCreate, FileAccess.ReadWrite);
        await using var windowsInput = AssetLoader.Open(uri);
        await windowsInput.CopyToAsync(windowsOutput).ConfigureAwait(false);
        await using var windowsWriter = new BinaryWriter(windowsOutput);
        Serializer.Serialize(windowsOutput, this);
        windowsWriter.Write((int)windowsInput.Length);
        // TODO: build a macos zip file, and then throw a branding.bin file into that
        SelectedTool!.WriteBranding("test", "out.bin");
        Complete(100);
    }
}