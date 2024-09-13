using LibUsbDotNet.Info;

namespace GuitarConfigurator.NetCore.Devices.libusb;

public abstract class LibUsbDevice(LocationId locationId) : IUsbDevice
{
    private LocationId LocationId { get; } = locationId;
    public abstract bool IsOpen { get; }
    public abstract ushort VendorId { get; }
    public abstract ushort ProductId { get; }
    public abstract void Open();
    public abstract void Close();
    public abstract void Claim();

    public override string ToString()
    {
        return LocationId.ToString();
    }
    public bool IsSameDevice(IDevice device)
    {
        if (device is LibUsbDevice libUsbDevice)
        {
            return libUsbDevice.LocationId == LocationId;
        }

        return false;
    }
    public abstract byte[] ReadData(ushort wValue, byte bRequest, ushort wIndex, ushort size = 128);
    public abstract void WriteData(ushort wValue, byte bRequest, ushort wIndex, byte[] buffer);
}