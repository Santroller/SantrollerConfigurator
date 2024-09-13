namespace GuitarConfigurator.NetCore.Devices;

public interface IUsbDevice: IDevice
{
    bool IsOpen { get; }
    
    ushort VendorId { get; }
    ushort ProductId { get; }
    void Open();
    void Close();

    void Claim();
    byte[] ReadData(ushort wValue, byte bRequest, ushort wIndex, ushort size = 128);
    void WriteData(ushort wValue, byte bRequest, ushort wIndex, byte[] buffer);
}