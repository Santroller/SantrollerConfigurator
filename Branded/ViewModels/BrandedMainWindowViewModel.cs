using System;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using GuitarConfigurator.NetCore;
using GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using Path = System.IO.Path;

namespace SantrollerConfiguratorBranded.NetCore.ViewModels;

public partial class BrandedMainWindowViewModel : MainWindowViewModel
{
    public BrandedConfigurationStore Config { get; }

    public BrandedConfiguration SelectedConfig { get; set; }

    private bool _writing;

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
            case PicoDevice pico:
                Progress = 50;
                Message = "Writing";
                SelectedConfig.BuildUf2(Path.Combine(pico.GetPath(), "firmware.uf2"));
                SelectedDevice = device;
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

    public override IObservable<PlatformIo.PlatformIoState> Write(ConfigViewModel config, string extra = "", int startingPercentage = 0, int endingPercentage = 100)
    {
        Overwrite();
        return Observable.Return(new PlatformIo.PlatformIoState(0, "", ""));
    }

    [RelayCommand]
    public void Overwrite()
    {
        _writing = true;
        StartWorking();
        Progress = 0;
        Message = "Looking for pico";
        if (SelectedDevice is not PicoDevice)
        {
            SelectedDevice!.Bootloader();
        }
        else
        {
            DeviceAdded(SelectedDevice);
        }
    }

    [RelayCommand]
    public void ConfigureBranded()
    {
        if (SelectedDevice is not Santroller santroller) return;
        SelectedConfig = Config.Configurations.First(s =>
            s.VendorName == santroller.Manufacturer && s.ProductName == santroller.Product);
        SelectedConfig.Model.Device = SelectedDevice;
        SelectedConfig.Model.UpdateBluetoothAddress();
        santroller.LoadConfiguration(SelectedConfig.Model, true);
        Router.Navigate.Execute(SelectedConfig.Model);
        SelectedConfig.Model.SetUpDiff();
    }
    [RelayCommand]
    public void ResetBranded()
    {
        if (SelectedDevice is not Santroller) return;
        SelectedDevice.Bootloader();
    }
}