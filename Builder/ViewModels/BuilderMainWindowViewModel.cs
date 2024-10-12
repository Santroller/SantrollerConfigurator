using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
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
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.ViewModels;
using Mono.Unix;
using ProtoBuf;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace SantrollerConfiguratorBuilder.NetCore.ViewModels;

public partial class BuilderMainWindowViewModel : MainWindowViewModel
{
    public BuilderConfig Config { get; }

    [Reactive] private BrandedConfigurationStore? _selectedTool;
    [Reactive] private BrandedConfigurationSection? _selectedSection;
    [Reactive] private BrandedConfigurationSection? _selectedCopySection;
    [Reactive] private BrandedConfiguration? _selected;
    [Reactive] private bool _imageError;

    public Interaction<BuilderMainWindowViewModel, IStorageFile?>
        LoadConfig { get; } =
        new();

    public Interaction<BuilderMainWindowViewModel, IStorageFile?>
        SaveUf2Handler { get; } =
        new();

    public Interaction<BuilderMainWindowViewModel, IStorageFolder?>
        SaveBinaryHandler { get; } =
        new();


    public BuilderMainWindowViewModel() : base(true, false, true)
    {
        Config = new BuilderConfig(this);
        if (Config.Configurations.Count != 0)
        {
            SelectedTool = Config.Configurations.First();
        }

        if (SelectedTool != null && SelectedTool.Configurations.Any())
        {
            SelectedSection = SelectedTool.Configurations.First();
            SelectedCopySection = SelectedSection;
        }

        Router.NavigateAndReset.Execute(new BuilderAuthViewModel(this));
        this.WhenAnyValue(s => s.SelectedDevice).Subscribe(s =>
        {
            if (Selected != null && SelectedDevice != null && !Working && HasSidebar)
            {
                Selected.Model.Device = SelectedDevice;
            }
        });

        this.WhenAnyValue(s => s.SelectedTool).Subscribe(s =>
        {
            if (SelectedTool == null || !SelectedTool.Configurations.Any()) return;
            SelectedSection = SelectedTool.Configurations.First();
            SelectedCopySection = SelectedSection;
        });

        this.WhenAnyValue(s => s.SelectedSection).Subscribe(s =>
        {
            if (SelectedSection != null && SelectedSection.Configurations.Any())
            {
                Selected = SelectedSection.Configurations.First();
            }
        });
    }

    [RelayCommand]
    public void Move()
    {
        if (Selected == null || SelectedSection == null || SelectedCopySection == null) return;
        var config = Selected;
        SelectedSection.Configurations.Remove(config);
        SelectedCopySection.Configurations.Add(config);
        Selected = SelectedSection.Configurations.First();
    }

    [RelayCommand]
    public void Copy()
    {
        if (Selected == null || SelectedSection == null) return;
        var c = new BrandedConfiguration(new SerialisedBrandedConfiguration(Selected), false,
            this);
        c.ProductName += " (Copy)";
        SelectedSection.Configurations.Add(c);
        Selected = c;
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
    public void AddSection()
    {
        if (SelectedTool == null) return;
        var item = new BrandedConfigurationSection("Type Name", new List<BrandedConfiguration>());
        SelectedTool.Configurations.Add(item);
        SelectedSection = item;
        SelectedCopySection = SelectedSection;
    }

    [RelayCommand]
    public void RemoveSection()
    {
        if (SelectedTool == null) return;
        if (SelectedSection == null) return;
        SelectedTool.Configurations.Remove(SelectedSection);
        SelectedSection = SelectedTool.Configurations.Any() ? SelectedTool.Configurations.First() : null;
        SelectedCopySection = SelectedSection;
    }

    [RelayCommand]
    public void AddDevice()
    {
        if (SelectedSection == null) return;
        var item = new BrandedConfiguration("Vendor Name", "Product Name", this);
        SelectedSection.Configurations.Add(item);
        Selected = item;
    }

    [RelayCommand]
    public void RemoveDevice()
    {
        if (SelectedSection == null) return;
        if (Selected == null) return;
        SelectedSection.Configurations.Remove(Selected);
        Selected = SelectedSection.Configurations.Any() ? SelectedSection.Configurations.First() : null;
    }

    [RelayCommand]
    public void StartConfiguring()
    {
        if (SelectedTool == null || Selected == null) return;
        if (SelectedDevice != null)
        {
            Selected.Model.Device = SelectedDevice;
        }

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

    private async void SaveUf2File(ConfigViewModel model)
    {
        if (Selected == null) return;
        Selected.LoadUf2();
        Complete(100);
        var output = await SaveUf2Handler.Handle(this);
        if (output == null) return;
        await Selected.BuildUf2(model, WebUtility.UrlDecode(output.Path.AbsolutePath));
    }

    private async void SaveToDevice(ConfigViewModel model)
    {
        if (Selected == null || SelectedDevice == null) return;
        Working = true;
        Selected.LoadUf2();
        var path = await SelectedDevice.GetUploadPortAsync();
        if (path == null)
        {
            return;
        }

        await Selected.BuildUf2(model, Path.Join(path, "firmware.uf2"));
        Complete(100);
    }

    public override IObservable<PlatformIo.PlatformIoState> Write(ConfigViewModel config, bool write, string extra = "",
        int startingPercentage = 0, int endingPercentage = 100)
    {
        if (Selected != null && string.IsNullOrEmpty(extra))
        {
            extra = Selected.ExtraConfig();
        }

        var state = base.Write(config, false, extra, startingPercentage, endingPercentage);
        state.ObserveOn(RxApp.MainThreadScheduler).Subscribe(UpdateProgress, _ => { }, () =>
        {
            if (extra.Length == 0 || !write) return;
            Progress = 50;
            Message = GuitarConfigurator.NetCore.Resources.WritingMessage;
            SaveToDevice(config);
        });


        return state;
    }

    public override IObservable<PlatformIo.PlatformIoState> SaveUf2(ConfigViewModel model)
    {
        if (Selected == null)
            return Observable.Return(new PlatformIo.PlatformIoState(100,
                GuitarConfigurator.NetCore.Resources.DoneMessage, false, null));
        var state = Write(model, false);

        state.ObserveOn(RxApp.MainThreadScheduler).Subscribe(UpdateProgress, _ => { }, () => SaveUf2File(model));

        return state;
    }

    [RelayCommand]
    public void MoveSectionDown()
    {
        if (SelectedTool == null || SelectedSection == null) return;
        var from = SelectedTool.Configurations.IndexOf(SelectedSection);
        if (from == SelectedTool.Configurations.Count - 1)
        {
            return;
        }

        var old = SelectedSection;
        SelectedTool.Configurations.Move(from, from + 1);
        SelectedSection = null;
        SelectedSection = old;
        SelectedCopySection = SelectedSection;
    }

    [RelayCommand]
    public void MoveSectionUp()
    {
        if (SelectedTool == null || SelectedSection == null) return;
        var from = SelectedTool.Configurations.IndexOf(SelectedSection);
        if (from == 0)
        {
            return;
        }

        var old = SelectedSection;
        SelectedTool.Configurations.Move(from, from - 1);
        SelectedSection = null;
        SelectedSection = old;
        SelectedCopySection = SelectedSection;
    }

    [RelayCommand]
    public void MoveDown()
    {
        if (SelectedSection == null || Selected == null) return;
        var from = SelectedSection.Configurations.IndexOf(Selected);
        if (from == SelectedSection.Configurations.Count - 1)
        {
            return;
        }

        var old = Selected;
        SelectedSection.Configurations.Move(from, from + 1);
        Selected = null;
        Selected = old;
    }

    [RelayCommand]
    public void MoveUp()
    {
        if (SelectedSection == null || Selected == null) return;
        var from = SelectedSection.Configurations.IndexOf(Selected);
        if (from == 0)
        {
            return;
        }

        var old = Selected;
        SelectedSection.Configurations.Move(from, from - 1);
        Selected = null;
        Selected = old;
    }

    [RelayCommand]
    public async Task Package()
    {
        Save();
        if (SelectedTool == null) return;
        var names = SelectedTool.Configurations.SelectMany(s => s.Configurations.Select(s2 => s2.ProductName)).ToList();
        if (names.ToHashSet().Count != names.Count)
        {
            ProgressbarColor = ProgressBarError;
            Complete(100);
            Message = Resources.UniqueName;
            return;
        }

        // Compile all configs and save the resulting UF2 into the brandedconfiguration
        var start = 0;
        var steps = 100 / (SelectedTool.Configurations.Sum(s => s.Configurations.Count) + 4);
        foreach (var section in SelectedTool.Configurations)
        {
            foreach (var config in section.Configurations)
            {
                if (config.Model.HasError)
                {
                    ProgressbarColor = ProgressBarError;
                    Complete(100);
                    Message = string.Format(Resources.ConfigIncomplete, config.ProductName);
                    return;
                }

                config.Model.Variant = config.ProductName;
                await Write(config.Model, false, config.ExtraConfig(), start, steps);
                if (!config.LoadUf2())
                {
                    ProgressbarColor = ProgressBarError;
                    Message = Resources.UnableToFindFirmware;
                    return;
                }

                start += steps;
                Progress = start;
            }
        }

        Message = "Building Linux executable";
        start += steps;
        Progress = start;
        var folder = await SaveBinaryHandler.Handle(this);
        if (folder == null)
        {
            Complete(100);
            return;
        }

        var workingDir = WebUtility.UrlDecode(folder.Path.AbsolutePath);
        // Extract linux executable and append branded config into executable.
        var assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
        var uri = new Uri($"avares://{assemblyName}/Assets/SantrollerConfiguratorBranded-linux-64");
        await using var linuxOutput =
            File.Open(Path.Join(workingDir, $"{SelectedTool.ToolName} - v{GitVersionInformation.SemVer}-linux-64"),
                FileMode.Create,
                FileAccess.ReadWrite);
        await using var linuxInput = AssetLoader.Open(uri);
        await linuxInput.CopyToAsync(linuxOutput).ConfigureAwait(false);
        await ExecutableUtils.AppendConfig(linuxOutput, SelectedTool);

        Message = "Building windows executable";
        start += steps;
        Progress = start;
        // Extract windows executable and append branded config into executable.
        uri = new Uri($"avares://{assemblyName}/Assets/SantrollerConfiguratorBranded-win-64.exe");
        await using var windowsInput = AssetLoader.Open(uri);
        await using var windowsOutput =
            File.Open(Path.Join(workingDir, $"{SelectedTool.ToolName} - v{GitVersionInformation.SemVer}-win-64.exe"),
                FileMode.Create,
                FileAccess.ReadWrite);
        await ExecutableUtils.UpdatePeFileIcon(SelectedTool.Icon, windowsInput, windowsOutput);
        await ExecutableUtils.AppendConfig(windowsOutput, SelectedTool);

        Message = "Building macOS package";
        start += steps;
        Progress = start;
        // Extract macos app zip, insert config and update icons and application name
        uri = new Uri($"avares://{assemblyName}/Assets/SantrollerConfiguratorBranded-macOS.zip");
        var tempDir = Path.Join(workingDir, "santroller_temp");
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
        }

        Directory.CreateDirectory(tempDir);
        await using var macosOutputTemp =
            File.Open(
                Path.Join(tempDir, $"{SelectedTool.ToolName} - v{GitVersionInformation.SemVer}-macOS.zip"),
                FileMode.Create,
                FileAccess.ReadWrite);

        await using var macosInput = AssetLoader.Open(uri);
        await macosInput.CopyToAsync(macosOutputTemp).ConfigureAwait(false);
        macosOutputTemp.Seek(0, SeekOrigin.Begin);
        var zipRoot = Path.Join(workingDir, "santroller_temp", "zip");
        var contentsRoot = Path.Join(zipRoot, "SantrollerConfiguratorBranded.app", "Contents");
        int attrs;
        // Since macOS executables are directories, we put the branding in a file instead of appending
        using (var archive = new ZipArchive(macosOutputTemp, ZipArchiveMode.Update))
        {
            archive.ExtractToDirectory(zipRoot);
            attrs =
                archive.GetEntry("SantrollerConfiguratorBranded.app/Contents/MacOS/SantrollerConfiguratorBranded")!
                    .ExternalAttributes;
        }

        await using (var branding = File.OpenWrite(Path.Join(contentsRoot, "Resources", "branding.bin")))
        {
            Serializer.SerializeWithLengthPrefix(branding, new SerialisedBrandedConfigurationStore(SelectedTool),
                PrefixStyle.Base128);
        }

        // Update icons and info.plist so that the executable has the correct name and icons
        await ExecutableUtils.UpdatePlist(SelectedTool.ToolNameVersioned, Path.Join(contentsRoot, "Info.plist"));

        await ExecutableUtils.OverwriteIcns(SelectedTool.Icon, Path.Join(contentsRoot, "Resources", "icon.icns"));

        foreach (var f in Directory.EnumerateFiles(contentsRoot, "._*", SearchOption.AllDirectories))
        {
            File.Delete(f);
        }

        Directory.Delete(Path.Join(contentsRoot, "MacOS", "Resources"), true);
        Directory.Move(Path.Join(zipRoot, "SantrollerConfiguratorBranded.app"),
            Path.Join(zipRoot, SelectedTool.ToolNameVersioned + ".app"));

#if Windows
        var rcodeSign = new Uri($"avares://{assemblyName}/Assets/rcodesign/win/rcodesign.exe");
        var rcodeSizeExecutable = "rcodesign.exe";
#elif OSX
        var rcodeSign = new Uri($"avares://{assemblyName}/Assets/rcodesign/macos/rcodesign");
        var rcodeSizeExecutable = "rcodesign";
#else
        var rcodeSign = new Uri($"avares://{assemblyName}/Assets/rcodesign/linux/rcodesign");
        var rcodeSizeExecutable = "rcodesign";
#endif
        var rcodeSizeProcess = Path.Join(workingDir, "santroller_temp", rcodeSizeExecutable);
        await using (var rcodeSignTemp = File.OpenWrite(rcodeSizeProcess))
        {
            await using var rcodeSignInput = AssetLoader.Open(rcodeSign);
            await rcodeSignInput.CopyToAsync(rcodeSignTemp).ConfigureAwait(false);
        }
#if !Windows
        var test = new UnixFileInfo(rcodeSizeProcess);
        test.FileAccessPermissions |= FileAccessPermissions.UserExecute | FileAccessPermissions.GroupExecute |
                                      FileAccessPermissions.OtherExecute;
#endif
        Message = "Signing macOS package";
        start += steps;
        Progress = start;
        var process = Process.Start(rcodeSizeProcess,
            ["sign", Path.Join(zipRoot, SelectedTool.ToolNameVersioned + ".app")]);
        await process.WaitForExitAsync();
        var outputZip = Path.Join(workingDir, $"{SelectedTool.ToolName} - v{GitVersionInformation.SemVer}-macOS.zip");
        if (File.Exists(outputZip))
        {
            File.Delete(outputZip);
        }

        ZipFile.CreateFromDirectory(zipRoot, outputZip);
        await using var macosOutput = File.Open(outputZip, FileMode.Open);
        using var archive2 = new ZipArchive(macosOutput, ZipArchiveMode.Update);
        archive2.GetEntry($"{SelectedTool.ToolNameVersioned}.app/Contents/MacOS/SantrollerConfiguratorBranded")!
            .ExternalAttributes = attrs;
        Directory.Delete(tempDir, true);
        Complete(100);
    }

    public override void SetDifference(bool difference)
    {
        HasChanges = difference;

        if (Working) return;
        if (DeviceNotProgrammed)
        {
            Progress = 100;
            Message = "Device is not programmed, hit write configuration to set device up";
            ProgressbarColor = ProgressBarError;
            return;
        }

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

    public override void SaveConfiguration()
    {
        Save();
    }
}