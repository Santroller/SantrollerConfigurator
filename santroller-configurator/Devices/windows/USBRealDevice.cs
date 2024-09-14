using System;
using System.Linq;
using System.Text.RegularExpressions;
using Nefarius.Drivers.WinUSB;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace GuitarConfigurator.NetCore.Devices;

public partial class USBRealDevice : IUsbDevice
{
    private readonly USBDevice? _device;
    private readonly USBPnPDevice _pnpDevice;

    public USBRealDevice(USBPnPDevice device)
    {
        _pnpDevice = device;
        _device = device.OpenDevice();
    }

    public bool IsSameDevice(IDevice d)
    {
        return d is USBRealDevice rd && rd._pnpDevice.IsSameDevice(_pnpDevice) ||
               d is USBPnPDevice pd && pd.IsSameDevice(_pnpDevice);
    }

    public bool IsOpen => _device != null;
    public ushort VendorId => _pnpDevice.VendorId;

    public ushort ProductId => _pnpDevice.ProductId;

    public ushort Revision  => _pnpDevice.Revision;

    public string Serial => _pnpDevice.Serial;

    public string Manufacturer => _device?.Descriptor.Manufacturer ?? _pnpDevice.Manufacturer;

    public string Product => _device?.Descriptor.Product ?? _pnpDevice.Product;

    public void Open()
    {
    }

    public void Close()
    {
    }

    public void Claim()
    {
    }

    public async Task<byte[]> ReadData(ushort wValue, byte bRequest, ushort wIndex, ushort size = 128)
    {
        return _device?.ControlIn(128 | 32 | 1, bRequest, wValue, wIndex, size) ?? [];
    }

    public async Task WriteData(ushort wValue, byte bRequest, ushort wIndex, byte[] buffer)
    {
        _device?.ControlOut(0 | 32 | 1, bRequest, wValue, wIndex, buffer);
    }

    [GeneratedRegex(".+VID_(.{4})&PID_(.{4})(?:&REV_(.{4}))?")]
    private static partial Regex IdMatcher();
}