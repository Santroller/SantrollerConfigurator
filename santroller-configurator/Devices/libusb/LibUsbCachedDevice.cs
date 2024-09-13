using LibUsbDotNet.Info;

namespace GuitarConfigurator.NetCore.Devices.libusb;

public class LibUsbCachedDevice(CachedDeviceInfo device) : LibUsbDevice(device.PortInfo)
{
    public override bool IsOpen => false;
    public override ushort VendorId => device.Descriptor.VendorId;
    public override ushort ProductId => device.Descriptor.ProductId;

    public override void Open()
    {
        
    }

    public override void Close()
    {
    }

    public override void Claim()
    {
    }

    public override byte[] ReadData(ushort wValue, byte bRequest, ushort wIndex, ushort size = 128)
    {
        return [];
    }

    public override void WriteData(ushort wValue, byte bRequest, ushort wIndex, byte[] buffer)
    {
    }
}