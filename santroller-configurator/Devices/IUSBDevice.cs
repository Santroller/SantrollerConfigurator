using System.Threading.Tasks;

namespace GuitarConfigurator.NetCore.Devices;

public interface IUsbDevice: IDevice
{
    bool IsOpen { get; }
    
    ushort VendorId { get; }
    ushort ProductId { get; }
    ushort Revision { get; }
    string Serial { get; }
    string Manufacturer { get; }
    string Product { get; }
    void Open();
    void Close();

    void Claim();
    Task<byte[]> ReadDataAsync(ushort wValue, byte bRequest, ushort wIndex, ushort size = 128);
    Task WriteDataAsync(ushort wValue, byte bRequest, ushort wIndex, byte[] buffer);
    byte[] ReadData(ushort wValue, byte bRequest, ushort wIndex, ushort size = 128);
    void WriteData(ushort wValue, byte bRequest, ushort wIndex, byte[] buffer);
}