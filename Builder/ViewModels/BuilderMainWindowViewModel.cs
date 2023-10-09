using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using GuitarConfigurator.NetCore;
using GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SantrollerConfiguratorBuilder.NetCore.ViewModels;

public partial class BuilderMainWindowViewModel : MainWindowViewModel
{
    public BuilderConfig Config { get; }

    [Reactive] public BrandedConfigurationStore? SelectedTool { get; set; }
    [Reactive] public BrandedConfiguration? Selected { get; set; }
    [Reactive] public bool ImageError { get; set; }

    public Interaction<BuilderMainWindowViewModel, IStorageFile?>
        LoadConfig { get; } =
        new();

    public Interaction<BuilderMainWindowViewModel, IStorageFile?>
        SaveUf2Handler { get; } =
        new();


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
            if (Selected != null && SelectedDevice != null && !Working)
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
        var item = new BrandedConfigurationStore("Tool Name",
            Color.Parse("#FF0078D7"), Color.Parse("#FFd7cb00"), Color.Parse("red"));
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
        Selected.Model.SetUpDiff();
    }

    [RelayCommand]
    public void Save()
    {
        Config.Save();
    }

    [RelayCommand]
    public async Task SelectLogo()
    {
        if (SelectedTool == null) return;
        var output = await LoadConfig.Handle(this);
        if (output != null)
        {
            SelectedTool.Logo = new Bitmap(await output.OpenReadAsync());
        }
    }

    [RelayCommand]
    public async Task SelectIcon()
    {
        if (SelectedTool == null) return;
        var output = await LoadConfig.Handle(this);
        if (output != null)
        {
            var bitmap = new Bitmap(await output.OpenReadAsync());
            if (Math.Abs(bitmap.Size.Width - bitmap.Size.Height) > 1)
            {
                ImageError = true;
                return;
            }

            ImageError = false;
            SelectedTool.Icon = bitmap;
        }
    }
    private async void SaveUf2File()
    {
        Complete(100);
        var output = await SaveUf2Handler.Handle(this);
        if (output == null) return;
        var uf2File = Path.Combine(AssetUtils.GetAppDataFolder(), "Santroller", ".pio", "build", "pico",
            "firmware.uf2");
        var fileStream = File.OpenRead(uf2File);
        await fileStream.CopyToAsync(await output.OpenWriteAsync());
    }

    public override IObservable<PlatformIo.PlatformIoState> SaveUf2(ConfigViewModel model)
    {
        if (Selected == null) return Observable.Return(new PlatformIo.PlatformIoState(100, "Done", null));
        var state = Write(model, Selected.ExtraConfig());

        state.ObserveOn(RxApp.MainThreadScheduler).Subscribe(UpdateProgress, _ => { }, SaveUf2File);

        return state;
    }

    [RelayCommand]
    public async Task Package()
    {
        if (SelectedTool == null) return;
        // Check for duplicate variant names
        var names = SelectedTool.Configurations.Select(s => s.ProductName).ToList();
        if (names.ToHashSet().Count != names.Count)
        {
            ProgressbarColor = ProgressBarError;
            Complete(100);
            Message = Resources.UniqueName;
            return;
        }

        // Compile all configs and save the resulting UF2 into the brandedconfiguration
        var start = 0;
        var steps = 100 / SelectedTool.Configurations.Count;
        foreach (var config in SelectedTool.Configurations)
        {
            if (config.Model.HasError)
            {
                ProgressbarColor = ProgressBarError;
                Complete(100);
                Message = string.Format(Resources.ConfigIncomplete, config.ProductName);
                return;
            }

            config.Model.Variant = config.ProductName;
            await Write(config.Model, config.ExtraConfig(), start, steps);
            config.LoadUf2();
            start += steps;
            Progress = start;
        }

        // Extract linux executable and append branded config into executable.
        var assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
        var uri = new Uri($"avares://{assemblyName}/Assets/SantrollerConfiguratorBranded-linux-64");
        await using var linuxOutput =
            File.Open(SelectedTool.ToolName + "-linux-64", FileMode.Create, FileAccess.ReadWrite);
        await using var linuxInput = AssetLoader.Open(uri);
        await linuxInput.CopyToAsync(linuxOutput).ConfigureAwait(false);
        await ExecutableUtils.AppendConfig(linuxOutput, SelectedTool);

        // Extract windows executable and append branded config into executable.
        uri = new Uri($"avares://{assemblyName}/Assets/SantrollerConfiguratorBranded-win-64.exe");
        await using var windowsInput = AssetLoader.Open(uri);
        await using var windowsOutput =
            File.Open(SelectedTool.ToolName + "-win-64.exe", FileMode.Create, FileAccess.ReadWrite);
        await ExecutableUtils.UpdatePeFileIcon(SelectedTool.Logo, windowsInput, windowsOutput);
        await ExecutableUtils.AppendConfig(windowsOutput, SelectedTool);

        // Extract macos app zip, insert config and update icons and application name
        uri = new Uri($"avares://{assemblyName}/Assets/SantrollerConfiguratorBranded-macOS.zip");
        await using var macosOutput =
            File.Open(SelectedTool.ToolName + "-macOS.zip", FileMode.Create, FileAccess.ReadWrite);
        await using var macosInput = AssetLoader.Open(uri);
        await macosInput.CopyToAsync(macosOutput).ConfigureAwait(false);
        macosOutput.Seek(0, SeekOrigin.Begin);
        
        // Since macOS executables are directories, we put the branding in a file instead of appending
        using var archive = new ZipArchive(macosOutput, ZipArchiveMode.Update);
        var entry = archive.CreateEntry("SantrollerConfiguratorBranded.app/Contents/MacOS/branding.bin");
        await using var branding = entry.Open();
        Serializer.SerializeWithLengthPrefix(branding, new SerialisedBrandedConfigurationStore(SelectedTool),
            PrefixStyle.Base128);
        branding.Close();
        
        // Update icons and info.plist so that the executable has the correct name and icons
        await ExecutableUtils.UpdatePlist(SelectedTool.ToolName, archive.GetEntry("SantrollerConfiguratorBranded.app/Contents/Info.plist")!);
        await ExecutableUtils.OverwriteIcns(SelectedTool.Logo, archive.GetEntry("SantrollerConfiguratorBranded.app/Contents/MacOS/Resources/icon.icns")!);
        await ExecutableUtils.OverwriteIcns(SelectedTool.Logo, archive.GetEntry("SantrollerConfiguratorBranded.app/Contents/Resources/icon.icns")!);
        await ExecutableUtils.RenameDirectoryInZip("SantrollerConfiguratorBranded.app", SelectedTool.ToolName + ".app",
            archive);

        Complete(100);
    }

    public override void SetDifference(bool difference)
    {
        HasChanges = difference;
        if (Working) return;
        if (!difference)
        {
            ProgressbarColor = ProgressBarPrimary;
            Complete(100);
        }
        else
        {
            ProgressbarColor = ProgressBarWarning;
            Message = "You have unsaved changes, click `Save Changes and return` to save them";
        }
    }

    
}