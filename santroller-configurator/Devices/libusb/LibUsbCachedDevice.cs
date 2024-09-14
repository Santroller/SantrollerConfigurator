using System;
using System.Threading.Tasks;
using LibUsbDotNet.Info;

namespace GuitarConfigurator.NetCore.Devices;

public class LibUsbCachedDevice(CachedDeviceInfo device) : LibUsbDevice(device.PortInfo)
{
    public override bool IsOpen => false;
    public override ushort VendorId => device.Descriptor.VendorId;
    public override ushort ProductId => device.Descriptor.ProductId;
    public override ushort Revision => device.Descriptor.Device;
    public override string Serial => device.Descriptor.SerialNumber;
    public override string Manufacturer => device.Descriptor.Manufacturer;
    public override string Product => device.Descriptor.Product;

    public override void Open()
    {
        
    }

    public override void Close()
    {
    }

    public override void Claim()
    {
    }

    public override Task<byte[]> ReadDataAsync(ushort wValue, byte bRequest, ushort wIndex, ushort size = 128)
    {
        return Task.FromResult(Array.Empty<byte>());
    }

    public override Task WriteDataAsync(ushort wValue, byte bRequest, ushort wIndex, byte[] buffer)
    {
        return Task.FromResult(0);
    }

    public override byte[] ReadData(ushort wValue, byte bRequest, ushort wIndex, ushort size = 128)
    {
        return [];
    }

    public override void WriteData(ushort wValue, byte bRequest, ushort wIndex, byte[] buffer)
    {
    }
}