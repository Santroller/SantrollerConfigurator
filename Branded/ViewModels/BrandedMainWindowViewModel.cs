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
using Path = System.IO.Path;

namespace SantrollerConfiguratorBranded.NetCore.ViewModels;

public partial class BrandedMainWindowViewModel : MainWindowViewModel
{
    public BrandedConfigurationStore Config { get; }

    public ConfigViewModel? Model { get; set; }
    public BrandedConfiguration SelectedConfig { get; set; }

    private TaskCompletionSource<string>? _bootloaderPath;

    private bool _writing;

    public bool HasMultipleConfigs => Config.Configurations.Count > 1;

    [RelayCommand]
    public void LoadSelectedConfig()
    {
        if (SelectedDevice is not Santroller || Model == null) return;
        new SerializedConfiguration(SelectedConfig.Model).LoadConfiguration(Model);
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
        SelectedConfig = Config.Configurations.First();
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
                SelectedConfig.Model.Device = SelectedDevice;
                Router.Navigate.Execute(SelectedConfig.Model);
                SelectedConfig.Model.UpdateBluetoothAddress();
                SelectedConfig.Model.SetUpDiff();
                break;
        }
    }

    public void DeviceRemoved(IConfigurableDevice device)
    {
    }

    public override IObservable<PlatformIo.PlatformIoState> Write(ConfigViewModel config, string extra = "",
        int startingPercentage = 0, int endingPercentage = 100)
    {
        return Observable.FromAsync(_ => Overwrite());
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
        StartWorking();
        Progress = 0;
        Message = "Looking for pico";
        _bootloaderPath = new TaskCompletionSource<string>();
        SelectedDevice.Bootloader();
        var path = await _bootloaderPath.Task;
        _bootloaderPath = null;
        Progress = 50;
        Message = "Writing";
        await SelectedConfig.BuildUf2(Model, Path.Combine(path!, "firmware.uf2"));
        return new PlatformIo.PlatformIoState(90, "Waiting for device", null);
    }

    [RelayCommand]
    public void ConfigureBranded()
    {
        if (SelectedDevice is not Santroller santroller) return;
        SelectedConfig = Config.Configurations.First(s =>
            s.VendorName == santroller.Manufacturer && s.ProductName == santroller.Product);
        Model = new ConfigViewModel(this, SelectedDevice, true);
        new SerializedConfiguration(SelectedConfig.Model).LoadConfiguration(Model);
        Model.UpdateBluetoothAddress();
        santroller.LoadConfiguration(Model, true);
        Router.Navigate.Execute(Model);
        Model.SetUpDiff();
    }

    [RelayCommand]
    public void ResetBranded()
    {
        if (SelectedDevice is not Santroller) return;
        SelectedDevice.Bootloader();
    }
}