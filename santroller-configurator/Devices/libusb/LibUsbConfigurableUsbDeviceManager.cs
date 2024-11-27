using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DynamicData;
using GuitarConfigurator.NetCore.ViewModels;
using LibUsbDotNet.LibUsb;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Devices;

public class ConfigurableUsbDeviceManager
{
    private const string UdevFile = "68-santroller.rules";
    private const string UdevPath = $"/usr/lib/udev/rules.d/{UdevFile}";
    private readonly MainWindowViewModel _model;
    private readonly UsbContext _context = new UsbContext();
    public ConfigurableUsbDeviceManager(MainWindowViewModel model)
    {
        _model = model;
        _context.RegisterHotPlug();
    }

    public void Register()
    {
        _context.DeviceEvent += OnDeviceNotify;
        _context.StartHandlingEvents();
        
        foreach (var dev in _context.List()) OnDeviceNotify(null, new DeviceArrivedEventArgs(dev as UsbDevice));
    }

    public void Dispose()
    {
        _context.DeviceEvent -= OnDeviceNotify;
    }


    private void OnDeviceNotify(object? sender, DeviceEventArgs e)
    {
        RxApp.MainThreadScheduler.Schedule(() =>
        {
            switch (e)
            {
                case DeviceArrivedEventArgs arrivedEventArgs:
                    UsbDevice device = arrivedEventArgs.Device;
                    var vid = device.VendorId;
                    var pid = device.ProductId;
                    if (vid == Dfu.DfuVid && (pid == Dfu.DfuPid16U2 || pid == Dfu.DfuPid8U2))
                    {
                        _model.AddDevice(new Dfu(new LibUsbRealDevice(device)));
                    }
                    else if (Ardwiino.HardwareIds.Contains((vid, pid)))
                    {
                        try
                        {
                            device.Open();
                            if (!device.IsOpen)
                            {
                                break;
                            }

                            var info = device.Info;
                            var revision = info.Device;
                            var product = info.Product ?? "Santroller";
                            var manufacturer = info.Manufacturer ?? "sanjay900";
                            var serial = info.SerialNumber?.Split("\0", 2)[0] ?? "";
                            // All our devices have a serial number specified, so skip devices that don't have one
                            if (string.IsNullOrEmpty(serial))
                            {
                                return;
                            }

                            switch (product)
                            {
                                case "Ardwiino" when _model.Programming:
                                case "Ardwiino" when revision == Ardwiino.SerialArdwiinoRevision:
                                    return;
                                case "Ardwiino":
                                    _model.AddDevice(new Ardwiino(new LibUsbRealDevice(device)));
                                    break;
                                default:
                                    // Branded devices can have any name.
                                    _model.AddDevice(new Santroller(new LibUsbRealDevice(device)));
                                    break;
                            }
                        }
                        catch (UsbException ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                    break;
                case DeviceLeftEventArgs deviceLeftEventArgs:
                    var infoLeave = deviceLeftEventArgs.DeviceInfo;
                    _model.AvailableDevices.RemoveMany(
                        _model.AvailableDevices.Items.Where(dev => dev.IsSameDevice(new LibUsbCachedDevice(infoLeave))));
                    break;
            }
        });
    }

    public async Task<bool> CheckDrivers()
    {
        return !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || File.Exists(UdevPath) &&
            await File.ReadAllTextAsync(UdevPath) == await AssetUtils.ReadFileAsync(UdevFile);
    }

    public static void Rescan()
    {
              
    }

    public static void InstallDrivers()
    {
              
    }
    
    public async Task InvokeRescan()
    {
        
    }
    
    public async Task InvokeDriverInstall()
    {   
        // Just copy the file to install it, using pkexec for admin
        var appdataFolder = AssetUtils.GetAppDataFolder();
        var rules = Path.Combine(appdataFolder, UdevFile);
        await AssetUtils.ExtractFileAsync(UdevFile, rules);
        var info = new ProcessStartInfo("pkexec");
        info.ArgumentList.AddRange(["cp", rules, UdevPath]);
        info.UseShellExecute = true;
        var process = Process.Start(info);
        if (process == null) return;
        await process.WaitForExitAsync();
        // And then reload rules and trigger
        info = new ProcessStartInfo("pkexec");
        info.ArgumentList.AddRange(["udevadm", "control", "--reload-rules"]);
        info.UseShellExecute = true;
        process = Process.Start(info);
        if (process == null) return;
        await process.WaitForExitAsync();

        info = new ProcessStartInfo("pkexec");
        info.ArgumentList.AddRange(["udevadm", "trigger"]);
        info.UseShellExecute = true;
        process = Process.Start(info);
        if (process == null) return;
        await process.WaitForExitAsync();
    }
}