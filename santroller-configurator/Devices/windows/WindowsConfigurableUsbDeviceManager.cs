using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using GuitarConfigurator.NetCore.ViewModels;
using Microsoft.Win32;
using Nefarius.Drivers.WinUSB;
using Nefarius.Utilities.DeviceManagement.Drivers;
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
        
        var key =
            Registry.CurrentUser.OpenSubKey(@"System\CurrentControlSet\Control\MediaProperties\PrivateProperties\Joystick\OEM\VID_1209&PID_2882", true);
        if (key != null) {
            if (key.GetValue("OEMName") != null) {
                key.DeleteValue("OEMName");
            }
            key.Close();
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
    
    
    public async Task<bool> CheckDrivers()
    {
        return DriverStore.ExistingDrivers.Any(s => s.Contains("atmel_usb_dfu"));
    }

    public async Task InvokeRescan()
    {   
        // Start app as admin and then call rescan function and wait for it to exit
        try 
        {
            var info2 = new ProcessStartInfo(Environment.ProcessPath!, "-Rescan")
            {
                UseShellExecute = true,
                Verb = "runas"
            };
            var process2 = Process.Start(info2);
            if (process2 == null) return;
            await process2.WaitForExitAsync();
        } catch (Win32Exception) {
        }
    }
    
    public async Task InvokeDriverInstall()
    {   
        // Start app as admin and then call rescan function and wait for it to exit
        try 
        {
            var info2 = new ProcessStartInfo(Environment.ProcessPath!, "-Drivers")
            {
                UseShellExecute = true,
                Verb = "runas"
            };
            var process2 = Process.Start(info2);
            if (process2 == null) return;
            await process2.WaitForExitAsync();
        } catch (Win32Exception e) {
            Trace.WriteLine(e);
            Console.WriteLine(e);
        }
    }

    public static void Rescan()
    {
        
        foreach (var guid in (Guid[])[DeviceInterfaceIds.UsbDevice, Ardwiino.DeviceGuid, Santroller.DeviceGuid, DeviceInterfaceIds.HidDevice, DeviceInterfaceIds.XUsbDevice])
        {
            var instance = 0;
            while (Devcon.FindByInterfaceGuid(guid, out var path,
                       out var instanceId, instance++, false))
            {
                if (!path.Contains("VID_1209&PID_2882")) continue;
                var usbDevice = PnPDevice
                    .GetDeviceByInterfaceId(path, DeviceLocationFlags.Phantom);
                try
                {
                    usbDevice.Uninstall();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
    }
    public static void InstallDrivers()
    {
        var driverFolder = Path.Combine(PlatformIo.GetAssetDir(), "platformio", "drivers");
        Devcon.Install(Path.Combine(driverFolder, "atmel_usb_dfu.inf"), out var rebootRequired);
        Trace.WriteLine(driverFolder);
    }
}