#if Windows

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.ComponentModel;
using DynamicData;
using GuitarConfigurator.NetCore.ViewModels;
using LibUsbDotNet;
using LibUsbDotNet.DeviceNotify;
using LibUsbDotNet.DeviceNotify.Info;
using LibUsbDotNet.Main;
using LibUsbDotNet.WinUsb;
using Nefarius.Utilities.DeviceManagement.PnP;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Devices;

public class ConfigurableUsbDeviceManager
{
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
            while (Devcon.FindByInterfaceGuid(guid, out var path,
                       out var instanceId, instance++))
            {
                DeviceNotify(EventType.DeviceArrival, path, guid);
            }
        }
    }

    private async void DeviceNotify(EventType eventType, string path, Guid guid)
    {
        RxApp.MainThreadScheduler.Schedule(() =>
        {
            var ids = UsbSymbolicName.Parse(path);
            if (eventType == EventType.DeviceArrival)
            {
                var vid = ids.Vid;
                var pid = ids.Pid;
                var serial = ids.SerialNumber;
                if (vid == Dfu.DfuVid && (pid == Dfu.DfuPid16U2 || pid == Dfu.DfuPid8U2))
                {
                    _model.AddDevice(
                        new Dfu(new RegDeviceNotifyInfoEventArgs(new RegDeviceNotifyInfo(path,
                            PnPDevice.GetInstanceIdFromInterfaceId(path), serial))));
                }
                else if (guid == Santroller.DeviceGuid)
                {
                    WinUsbDevice.Open(path, out var dev);
                    if (dev == null) return;
                    var revision = (ushort) dev.Info.Descriptor.BcdDevice;
                    serial = dev.Info.SerialString;
                    // If a device gets disconnected just after connection (aka when swapping to xinput)
                    // Then we dont get a serial string.
                    if (string.IsNullOrEmpty(serial))
                    {
                        return;
                    }

                    _model.AddDevice(new Santroller(path, dev, serial, revision, dev.Info.ProductString,
                        dev.Info.ManufacturerString));
                }
                else if (guid == Ardwiino.DeviceGuid)
                {
                    WinUsbDevice.Open(path, out var dev);
                    if (dev == null) return;
                    var revision = (ushort) dev.Info.Descriptor.BcdDevice;
                    _model.AddDevice(new Ardwiino(path, dev, serial, revision));
                }
            }
            else
            {
                var serial = ids.SerialNumber;
                _model.AvailableDevices.RemoveMany(
                    _model.AvailableDevices.Items.Where(device =>
                        device.IsSameDevice(path) || device.IsSameDevice(serial) ||
                        device.IsSameDevice(PnPDevice.GetInstanceIdFromInterfaceId(path))));
            }
        });
    }

    private class RegDeviceNotifyInfoEventArgs : DeviceNotifyEventArgs
    {
        internal RegDeviceNotifyInfoEventArgs(IUsbDeviceNotifyInfo info)
        {
            Device = info;
        }
    }

    private class RegDeviceNotifyInfo : IUsbDeviceNotifyInfo
    {
        private readonly string _path;
        private readonly string _instanceId;
        private readonly string _serialNumber;

        public RegDeviceNotifyInfo(string path, string instanceId, string serialNumber)
        {
            _path = path;
            _instanceId = instanceId;
            _serialNumber = serialNumber;
        }

        public UsbSymbolicName SymbolicName => UsbSymbolicName.Parse(_instanceId);

        public string Name => _instanceId;

        public Guid ClassGuid => DeviceInterfaceIds.UsbDevice;

        public int IdVendor => SymbolicName.Vid;

        public int IdProduct => SymbolicName.Pid;

        public string SerialNumber => _serialNumber;

        public bool Open(out UsbDevice usbDevice)
        {
            WinUsbDevice.Open(_path, out var winUsbDevice);
            usbDevice = winUsbDevice;
            return winUsbDevice != null && winUsbDevice.Open();
        }
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
#endif