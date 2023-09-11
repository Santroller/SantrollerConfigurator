using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using GuitarConfigurator.NetCore.Devices;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.ViewModels;

public class RestoreViewModel : ReactiveObject, IRoutableViewModel
{
    private readonly Santroller _santroller;
    private bool _done;

    public RestoreViewModel(MainWindowViewModel screen, Santroller device)
    {
        Main = screen;
        Main.Message = "Starting restore";
        Main.Progress = 0;
        HostScreen = screen;
        _santroller = device;
        GoBack = ReactiveCommand.CreateFromObservable(() => Main.GoBack.Execute(),
            this.WhenAnyValue(x => x.Main.Working).Select(x => !x).ObserveOn(RxApp.MainThreadScheduler));
    }

    public MainWindowViewModel Main { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> GoBack { get; }

    public string? UrlPathSegment => Guid.NewGuid().ToString()[..5];
    public IScreen HostScreen { get; }

    public IDisposable RegisterConnections()
    {
        var ret =
            Main.AvailableDevices.Connect().Subscribe(s =>
            {
                foreach (var change in s)
                    switch (change.Reason)
                    {
                        case ListChangeReason.Add:
                            AddDevice(change.Item.Current);
                            break;
                        case ListChangeReason.Remove:
                            RemoveDevice(change.Item.Current);
                            break;
                    }
            });
        _santroller.Bootloader();
        Main.Working = true;
        Main.Message = "Entering programming mode";
        Main.Progress = 10;
        return ret;
    }

    private void RemoveDevice(IConfigurableDevice device)
    {
    }

    private void AddDevice(IConfigurableDevice device)
    {
        var configFile = Path.Combine(AssetUtils.GetAppDataFolder(), "platformio", "packages", "tool-avrdude",
            "avrdude.conf");
        if (_santroller.IsPico() && device is PicoDevice)
        {
            if (_done) return;
            Main.Message = "Programming";
            Main.Progress = 50;
            // Copy blank firmware back to device
            var firmware = Path.Combine(AssetUtils.GetAppDataFolder(), "default_firmwares", "pico.uf2");
            File.Copy(firmware, Path.Combine(device.GetUploadPortAsync().Result!, "firmware.uf2"));
            Main.Complete(100);
            _done = true;
        }
        else if (_santroller.HasDfuMode() && device is Dfu dfu)
        {
            Main.Message = "Programming";
            Main.Progress = 50;
            // Write back a default firmware
            _ = Main.Pio.RunAvrdudeErase(dfu, "", 0, 100, _santroller.Board).Subscribe(s => { }, s => { }, () =>
            {
                Main.Message = "Exiting Programming mode";
                Main.Progress = 90;
                dfu.Launch();
            });
        }
        else if (!_santroller.IsPico() && !_santroller.HasDfuMode() && device is Arduino)
        {
            Main.Message = "Programming";
            Main.Progress = 50;
            // Erase the device so it stays in bootloader mode, the ide can just program that
            _ = Main.Pio.RunAvrdudeErase(device, "", 0, 100).Subscribe(s => { }, s => { }, () => { Main.Complete(100); });
        }
        else if (!_santroller.IsPico() && _santroller.HasDfuMode() && device is Arduino)
        {
            Main.Complete(100);
        }
    }
}