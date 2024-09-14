using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using GuitarConfigurator.NetCore.ViewModels;
using Nefarius.Drivers.WinUSB;
using Nefarius.Utilities.DeviceManagement.PnP;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Devices;

public partial class ConfigurableUsbDeviceManager
{
    private enum EventType
    {
        DeviceArrival,
        DeviceRemoveComplete
    }
    private readonly DeviceNotificationListener _deviceNotificationListener = new();
    private MainWindowViewModel _model;

    public ConfigurableUsbDeviceManager(MainWindowViewModel model)
    {
        _model = model;
    }

    public void Register()
    {
        _deviceNotificationListener.DeviceArrived += DeviceArrived;
        _deviceNotificationListener.DeviceRemoved += DeviceRemoved;
        var guids = new[] {DeviceInterfaceIds.UsbDevice, Ardwiino.DeviceGuid, Santroller.DeviceGuid};
        foreach (var guid in guids)
        {
            _deviceNotificationListener.StartListen(guid);
            var instance = 0;
            while (Devcon.FindByInterfaceGuid(guid, out var path, out var instanceId, instance++))
            {
                DeviceNotify(EventType.DeviceArrival, path, guid);
            }
        }
    }

    private async void DeviceNotify(EventType eventType, string path, Guid guid)
    {
        RxApp.MainThreadScheduler.Schedule(() =>
        {
            try
            {
                var dev = PnPDevice.GetDeviceByInterfaceId(path,
                    eventType == EventType.DeviceArrival ? DeviceLocationFlags.Normal : DeviceLocationFlags.Phantom);
                USBPnPDevice pnpDev = new USBPnPDevice(dev, path.ToUpper());
                var vid = pnpDev.VendorId;
                var pid = pnpDev.ProductId;
                var rev = pnpDev.Revision;
                if (eventType == EventType.DeviceArrival)
                {
                    if (vid == Dfu.DfuVid && (pid == Dfu.DfuPid16U2 || pid == Dfu.DfuPid8U2))
                    {
                        _model.AddDevice(new Dfu(pnpDev));
                    }
                    else if (guid == Santroller.DeviceGuid)
                    {
                        if (!
                            USBDevice.GetDevices(guid).Any(s => s.DevicePath.ToUpper().Equals(path.ToUpper())))
                        {
                            return;
                        }
                        var udev = new USBRealDevice(pnpDev);
                        if (!udev.IsOpen) return;
                        _model.AddDevice(new Santroller(udev));
                    }
                    else if (guid == Ardwiino.DeviceGuid)
                    {
                        if (!
                            USBDevice.GetDevices(guid).Any(s => s.DevicePath.ToUpper().Equals(path.ToUpper())))
                        {
                            return;
                        }
                        var udev = new USBRealDevice(pnpDev);
                        if (!udev.IsOpen) return;
                        _model.AddDevice(new Ardwiino(udev));
                    }
                }
                else
                {
                    _model.AvailableDevices.RemoveMany(
                        _model.AvailableDevices.Items.Where(device => device.IsSameDevice(pnpDev)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        });
    }

    private void DeviceArrived(DeviceEventArgs args)
    {
        DeviceNotify(EventType.DeviceArrival, args.SymLink, args.InterfaceGuid);

    }

    private void DeviceRemoved(DeviceEventArgs args)
    {
        DeviceNotify(EventType.DeviceRemoveComplete, args.SymLink, args.InterfaceGuid);
    }

    public void Dispose()
    {
        _deviceNotificationListener.DeviceArrived -= DeviceArrived;
        _deviceNotificationListener.DeviceRemoved -= DeviceRemoved;

        _deviceNotificationListener.StopListen(DeviceInterfaceIds.UsbDevice);
    }

    public async Task RescanAsync()
    {   
        try 
        {
            var info2 = new ProcessStartInfo("powershell.exe");
            info2.ArgumentList.AddRange(new[] {"-Command", "pnputil /enum-devices /connected | findstr 1209 | ForEach-Object { pnputil /remove-device $_.Split(\":\")[1].Trim() }; pnputil /scan-devices /async"});
            info2.UseShellExecute = true;
            info2.CreateNoWindow = true;
            info2.WindowStyle = ProcessWindowStyle.Hidden;
            info2.Verb = "runas";
            var process2 = Process.Start(info2);
            if (process2 == null) return;
            await process2.WaitForExitAsync();
        } catch (Win32Exception) {
        }
    }
}