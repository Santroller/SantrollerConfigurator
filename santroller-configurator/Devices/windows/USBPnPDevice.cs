using System;
using System.Linq;
using System.Text.RegularExpressions;
using Nefarius.Drivers.WinUSB;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace GuitarConfigurator.NetCore.Devices;

public partial class USBPnPDevice : IUsbDevice
{
    private readonly PnPDevice _device;
    private readonly string _path;

    public USBPnPDevice(PnPDevice device, string path)
    {
        _device = device;
        _path = path;
        var hardwareId = device.HardwareIds?.FirstOrDefault() ?? "";
        var ids = IdMatcher().Match(hardwareId).Groups.Values.ToList();
        VendorId = Convert.ToUInt16(ids[1].Value, 16);
        ProductId = Convert.ToUInt16(ids[2].Value, 16);
        Revision = Convert.ToUInt16(ids[3].Value, 16);
        Serial = device.DeviceId.Split("\\").LastOrDefault() ?? "";
        Manufacturer = device.GetProperty<string>(DevicePropertyKey.Device_Manufacturer) ?? "";
        Product = device.GetProperty<string>(DevicePropertyKey.Device_Model) ?? "";
        // For composite devices, get the parent device as that has the actual serial number
        if (_device.DeviceId.Contains("MI_", StringComparison.CurrentCultureIgnoreCase))
        {
            Serial = device.GetProperty<string>(DevicePropertyKey.Device_Parent)?.Split("\\").LastOrDefault() ?? "";
        }
    }

    public USBDevice OpenDevice()
    {
        return USBDevice.GetSingleDeviceByPath(_path);
    }

    public bool IsSameDevice(IDevice d)
    {
        return d is USBPnPDevice pd && pd._device.InstanceId == _device.InstanceId ||
               d is USBRealDevice rd && rd.IsSameDevice(this);
    }

    public bool IsOpen => false;
    public ushort VendorId { get; }

    public ushort ProductId { get; }

    public ushort Revision { get; }
    public string Serial { get; }
    public string Manufacturer { get; }
    public string Product { get; }

    public void Open()
    {
    }

    public void Close()
    {
    }

    public void Claim()
    {
    }

    public byte[] ReadData(ushort wValue, byte bRequest, ushort wIndex, ushort size = 128)
    {
        return [];
    }

    public void WriteData(ushort wValue, byte bRequest, ushort wIndex, byte[] buffer)
    {
    }

    public override string ToString()
    {
        return $"{nameof(VendorId)}: {VendorId}, {nameof(ProductId)}: {ProductId}, {nameof(Revision)}: {Revision}, {nameof(Serial)}: {Serial}, {nameof(Manufacturer)}: {Manufacturer}, {nameof(Product)}: {Product}";
    }

    [GeneratedRegex(".+VID_(.{4})&PID_(.{4})(?:&REV_(.{4}))?")]
    private static partial Regex IdMatcher();
}