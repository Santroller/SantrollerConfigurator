using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using AsmResolver;
using AsmResolver.IO;
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
using AsmResolver.PE;
using AsmResolver.PE.DotNet.Builder;
using AsmResolver.PE.File;
using AsmResolver.PE.File.Headers;
using AsmResolver.PE.Win32Resources;
using AsmResolver.PE.Win32Resources.Builder;
using AsmResolver.PE.Win32Resources.Icon;
using Avalonia;

namespace SantrollerConfiguratorBuilder.NetCore.ViewModels;

public partial class BuilderMainWindowViewModel : MainWindowViewModel
{
    public BuilderConfig Config { get; }

    [Reactive] public BrandedConfigurationStore? SelectedTool { get; set; }
    [Reactive] public BrandedConfiguration? Selected { get; set; }

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

        // Extract linux executable and append branded config into executable. Also append length so we can easily find the where the config is in the executable.
        var assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
        var uri = new Uri($"avares://{assemblyName}/Assets/SantrollerConfiguratorBranded-linux-64");
        await using var linuxOutput =
            File.Open(SelectedTool.ToolName + "-linux-64", FileMode.Create, FileAccess.ReadWrite);
        await using var linuxInput = AssetLoader.Open(uri);
        var len = linuxInput.Length;
        await linuxInput.CopyToAsync(linuxOutput).ConfigureAwait(false);
        await using var linuxWriter = new BinaryWriter(linuxOutput);
        Serializer.SerializeWithLengthPrefix(linuxOutput, new SerialisedBrandedConfigurationStore(SelectedTool),
            PrefixStyle.Base128);
        linuxWriter.Write((int) len);

        // Extract windows executable and append branded config into executable. Also append length so we can easily find the where the config is in the executable.
        uri = new Uri($"avares://{assemblyName}/Assets/SantrollerConfiguratorBranded-win-64.exe");
        await using var windowsOutput =
            File.Open(SelectedTool.ToolName + "-win-64.exe", FileMode.Create, FileAccess.ReadWrite);
        await using var windowsInput = AssetLoader.Open(uri);
        len = windowsInput.Length;
        var bytes = new byte[len];
        _ = await windowsInput.ReadAsync(bytes);
        // Open the executable, and update the icon
        var peFile = PEFile.FromBytes(bytes);
        var image = new SerializedPEImage(peFile, new PEReaderParameters());
        if (image.Resources != null)
        {
            var icons = IconResource.FromDirectory(image.Resources);
            if (icons != null)
            {
                var group = icons.GetIconGroups().First();
                var iconEntry = group.GetIconEntries().First();
                ConvertToIco(SelectedTool.Logo, iconEntry);
                icons.WriteToDirectory(image.Resources);
                var resources = new ResourceDirectoryBuffer();
                resources.AddDirectory(image.Resources);
                var section = peFile.Sections.First(s => s.Name == ".rsrc");
                section.Contents = resources;
                peFile.AlignSections();
            }
        }

        var writer = new BinaryStreamWriter(windowsOutput);
        peFile.Write(writer);
        len = writer.Length;
        await using var windowsWriter = new BinaryWriter(windowsOutput);
        Serializer.SerializeWithLengthPrefix(windowsOutput, new SerialisedBrandedConfigurationStore(SelectedTool),
            PrefixStyle.Base128);
        windowsWriter.Write((int) len);

        // Extract macos app zip.
        uri = new Uri($"avares://{assemblyName}/Assets/SantrollerConfiguratorBranded-macOS.zip");
        await using var macosOutput =
            File.Open(SelectedTool.ToolName + "-macOS.zip", FileMode.Create, FileAccess.ReadWrite);
        await using var macosInput = AssetLoader.Open(uri);
        await macosInput.CopyToAsync(macosOutput).ConfigureAwait(false);
        macosOutput.Seek(0, SeekOrigin.Begin);

        // Edit zip file, copying in the branding.bin, as macos isn't single file so we don't need to append.
        using var archive = new ZipArchive(macosOutput, ZipArchiveMode.Update);
        var entry = archive.CreateEntry("SantrollerConfiguratorBranded.app/Contents/MacOS/branding.bin");
        await using var branding = entry.Open();
        Serializer.SerializeWithLengthPrefix(branding, new SerialisedBrandedConfigurationStore(SelectedTool),
            PrefixStyle.Base128);
        branding.Close();

        // Modify info.plist with the tools name
        entry = archive.GetEntry("SantrollerConfiguratorBranded.app/Contents/Info.plist")!;
        await using var info = entry.Open();
        await using var stream = new MemoryStream();
        await using var infoWriter = new StreamWriter(stream);
        var infoReader = new StreamReader(info);
        while (!infoReader.EndOfStream)
        {
            var line = await infoReader.ReadLineAsync();
            await infoWriter.WriteLineAsync(line);
            if (line!.Contains("CFBundleName"))
            {
                await infoReader.ReadLineAsync();
                await infoWriter.WriteLineAsync($"<string>{SelectedTool.ToolName}</string>");
            }
            else if (line.Contains("CFBundleDisplayName"))
            {
                await infoReader.ReadLineAsync();
                await infoWriter.WriteLineAsync($"<string>{SelectedTool.ToolName}</string>");
            }
        }

        info.SetLength(0);
        info.Seek(0, SeekOrigin.Begin);
        await infoWriter.FlushAsync();
        stream.Seek(0, SeekOrigin.Begin);
        await stream.CopyToAsync(info);
        info.Close();
        entry = archive.GetEntry("SantrollerConfiguratorBranded.app/Contents/MacOS/Resources/icon.icns")!;
        
        await using var info2 = entry.Open();
        info2.SetLength(0);
        info2.Seek(0, SeekOrigin.Begin);
        ConvertToIcns(SelectedTool.Logo, info2);
        info2.Close();
        entry = archive.GetEntry("SantrollerConfiguratorBranded.app/Contents/Resources/icon.icns")!;
        await using var info3 = entry.Open();
        info3.SetLength(0);
        info3.Seek(0, SeekOrigin.Begin);
        ConvertToIcns(SelectedTool.Logo, info3);
        info3.Close();
        foreach (var oldEntry in archive.Entries.ToList())
        {
            var newEntry = archive.CreateEntry(oldEntry.FullName.Replace("SantrollerConfiguratorBranded.app",
                SelectedTool.ToolName + ".app"));
            await using var oldStream = oldEntry.Open();
            await using var newStream = newEntry.Open();
            await oldStream.CopyToAsync(newStream);
            oldStream.Close();
            newEntry.ExternalAttributes = oldEntry.ExternalAttributes;
            oldEntry.Delete();
        }

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

    public static void ConvertToIco(Bitmap img, (IconGroupDirectoryEntry, IconEntry) valueTuple)
    {
        img = img.CreateScaledBitmap(new PixelSize(64, 64));
        using var msImg = new MemoryStream();
        img.Save(msImg);
        Array.Copy(msImg.ToArray(), valueTuple.Item2.RawIcon, msImg.Length);
        valueTuple.Item1.Height = 64;
        valueTuple.Item1.Width = 64;
        valueTuple.Item1.ColorCount = 0;
        valueTuple.Item1.Reserved = 0;
        valueTuple.Item1.BytesInRes = (uint) msImg.Length;
        valueTuple.Item1.ColorPlanes = 0;
        valueTuple.Item1.PixelBitCount = 32;
        valueTuple.Item2.UpdateOffsets(new RelocationParameters());
        valueTuple.Item1.UpdateOffsets(new RelocationParameters());
    }

    static ReadOnlySpan<byte> GetIcnsIconType(int width, bool isScale2x)
    {
        var iconType = width switch
        {
            16 => isScale2x ? null : "icp4"u8,
            32 => isScale2x ? "ic11"u8 : "icp5"u8,
            64 => isScale2x ? "ic12"u8 : "icp6"u8,
            128 => isScale2x ? null : "ic07"u8,
            256 => isScale2x ? "ic13"u8 : "ic08"u8,
            512 => isScale2x ? "ic14"u8 : "ic09"u8,
            _ => "ic10"u8
        };

        return iconType;
    }

    private static byte[] GetBigEndianBytes(int value)
    {
        var bytes = BitConverter.GetBytes(value);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);

        return bytes;
    }

    private static void ConvertToIcns(Bitmap img, Stream outputFile)
    {
        var icnsData = new List<byte>();
        var sizeAll = 0;

        img = img.CreateScaledBitmap(new PixelSize(512, 512));
        using var msImg = new MemoryStream();
        img.Save(msImg);
        msImg.Seek(0, SeekOrigin.Begin);
        var iconType = GetIcnsIconType(512, false);
        var sizeIcon = 4 + 4 + Convert.ToInt32(msImg.Length);
        icnsData.AddRange(GetBigEndianBytes(sizeIcon));
        msImg.CopyTo(outputFile);
        sizeAll += 4 + 4 + sizeIcon;

        outputFile.Write("icns"u8);
        sizeAll = 4 + 4 + sizeAll;
        var sizeAllArray = GetBigEndianBytes(sizeAll);
        outputFile.Write(sizeAllArray);
        outputFile.Write(iconType);
        outputFile.Write(icnsData.ToArray());
    }
}