using System.Linq;
using System.Reactive.Concurrency;
using DynamicData;
using GuitarConfigurator.NetCore.ViewModels;
using LibUsbDotNet.LibUsb;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Devices.libusb;

public class ConfigurableUsbDeviceManager
{
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
                                _model.AddDevice(new Ardwiino(new LibUsbRealDevice(device), serial,
                                    revision));
                                break;
                            default:
                                // Branded devices can have any name.
                                _model.AddDevice(new Santroller(new LibUsbRealDevice(device), serial,
                                    revision, product, manufacturer));
                                break;
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
}