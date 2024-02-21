using System;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using GuitarConfigurator.NetCore;
using GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Path = System.IO.Path;

namespace SantrollerConfiguratorBranded.NetCore.ViewModels;

public partial class BrandedMainWindowViewModel : MainWindowViewModel
{
    public BrandedConfigurationStore Config { get; }

    [Reactive] public BrandedConfigurationSection SelectedSection { get; set; }
    [Reactive] public BrandedConfiguration SelectedConfig { get; set; }

    public ConfigViewModel? Model { get; set; }

    private TaskCompletionSource<string>? _bootloaderPath;

    [ObservableAsProperty] public bool ReadyToConfigureBranded { get; }

    private bool _writing;

    public bool HasMultipleConfigs => Config.Configurations.Count > 1;

    [RelayCommand]
    public async Task LoadSelectedConfig()
    {
        if (SelectedDevice is not Santroller || Model == null) return;
        new SerializedConfiguration(SelectedConfig.Model).LoadConfiguration(Model);
        await Overwrite();
    }

    private string ColorToHex(Avalonia.Media.Color color)
    {
        return ColorTranslator.ToHtml(Color.FromArgb(color.A, color.R, color.G, color.B));
    }

    public BrandedMainWindowViewModel() : base(true)
    {
        Config = BrandedConfigurationStore.LoadBranding(this);
        Logo = Config.Logo;
        ProgressBarPrimary = ColorToHex(Config.PrimaryColor);
        ProgressBarWarning = ColorToHex(Config.WarningColor);
        ProgressBarError = ColorToHex(Config.ErrorColor);
        ProgressbarColor = ProgressBarPrimary;
        SelectedSection = Config.Configurations.First();
        SelectedConfig = SelectedSection.Configurations.First();
        Router.NavigateAndReset.Execute(new BrandedMainViewModel(this));
        AvailableDevices.Connect().ObserveOn(RxApp.MainThreadScheduler).Subscribe(s =>
        {
            foreach (var change in s)
                switch (change.Reason)
                {
                    case ListChangeReason.Add:
                        DeviceAdded(change.Item.Current);
                        break;
                    case ListChangeReason.Remove:
                        DeviceRemoved(change.Item.Current);
                        break;
                }
        });
        this.WhenAnyValue(x => x.SelectedSection).Subscribe(s => { SelectedConfig = s.Configurations.First(); });
        this.WhenAnyValue(x => x.SelectedDevice, x => x.Installed, x => x.IsPeripheral, x => x.PeripheralErrorText)
            .Select(s =>
                s is {Item1: not null, Item2: true} &&
                (!s.Item3 || s.Item4 == null))
            .ToPropertyEx(this, s => s.ReadyToConfigureBranded);
    }

    public void DeviceAdded(IConfigurableDevice device)
    {
        if (!_writing) return;
        switch (device)
        {
            case PicoDevice pico when _bootloaderPath != null:
                _bootloaderPath.SetResult(pico.GetPath());
                break;
            case Santroller santroller when santroller.Manufacturer ==
                SelectedConfig.VendorName && santroller.Product == SelectedConfig.ProductName:
                SelectedDevice = device;
                _writing = false;
                Complete(100);
                if (Model == null)
                {
                    SelectedConfig.Model.Device = SelectedDevice;
                    Router.Navigate.Execute(SelectedConfig.Model);
                }
                else
                {
                    Model.Device = SelectedDevice;
                    Model.UpdateBluetoothAddress();
                    Model.SetUpDiff();
                }

                break;
        }
    }

    public void DeviceRemoved(IConfigurableDevice device)
    {
    }

    public override IObservable<PlatformIo.PlatformIoState> Write(ConfigViewModel config, bool write, string extra = "",
        int startingPercentage = 0, int endingPercentage = 100)
    {
        return Observable.FromAsync(_ => Overwrite());
    }

    [RelayCommand]
    public async Task<PlatformIo.PlatformIoState> OverwriteNew()
    {
        if (SelectedDevice == null) return new PlatformIo.PlatformIoState(100, "No device selected", null);
        Model = null;
        return await Overwrite();
    }

    [RelayCommand]
    public async Task<PlatformIo.PlatformIoState> Overwrite()
    {
        if (SelectedDevice == null) return new PlatformIo.PlatformIoState(100, "No device selected", null);
        if (Model == null)
        {
            Model = new ConfigViewModel(this, SelectedDevice, true);
            new SerializedConfiguration(SelectedConfig.Model).LoadConfiguration(Model);
        }

        _writing = true;
        _bootloaderPath = new TaskCompletionSource<string>();
        StartWorking();
        Progress = 0;
        Message = "Looking for pico";
        string path;
        if (SelectedDevice is not PicoDevice pico)
        {
            SelectedDevice.Bootloader();
            path = await _bootloaderPath.Task;
            _bootloaderPath = null;
        }
        else
        {
            path = pico.GetPath();
        }

        Progress = 50;
        Message = "Writing";
        await SelectedConfig.BuildUf2(Model, Path.Combine(path, "firmware.uf2"));
        return new PlatformIo.PlatformIoState(90,
            Windows ? "Waiting for device (Stuck here? Try clicking refresh devices)" : "Waiting for device", null);
    }

    public void test1()
    {
        if (SelectedDevice is not Santroller santroller) return;
        SelectedConfig = Config.Configurations.SelectMany(s => s.Configurations).First(s =>
            s.VendorName == santroller.Manufacturer && s.ProductName == santroller.Product);
    }

    public void test2()
    {
        if (SelectedDevice is not Santroller santroller) return;
        Model = new ConfigViewModel(this, SelectedDevice, true);
    }

    public void test3()
    {
        if (Model == null) return;
        new SerializedConfiguration(SelectedConfig.Model).LoadConfiguration(Model);
    }

    public void test4()
    {
        if (Model == null) return;
        Model.UpdateBluetoothAddress();
    }

    public void test5()
    {
        if (SelectedDevice is not Santroller santroller) return;
        if (Model == null) return;
        santroller.LoadConfiguration(Model, true);
    }

    public void test6()
    {
        if (Model == null) return;
        Router.Navigate.Execute(Model);
    }

    public void test7()
    {
        if (Model == null) return;
        Model.SetUpDiff();
    }
    [RelayCommand]
    public void ConfigureBranded()
    {
        if (SelectedDevice is not Santroller) return;
        test1();
        test2();
        test3();
        test4();
        test5();
        test6();
        test7();
    }

    [RelayCommand]
    public void ResetBranded()
    {
        if (SelectedDevice is not Santroller) return;
        SelectedDevice.Bootloader();
    }
}