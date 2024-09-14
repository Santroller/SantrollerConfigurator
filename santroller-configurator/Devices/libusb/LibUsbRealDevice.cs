using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

namespace GuitarConfigurator.NetCore.Devices;

public class LibUsbRealDevice(UsbDevice d) : LibUsbDevice(d.LocationId)
{
    public override bool IsOpen => d.IsOpen;
    public override ushort VendorId => d.VendorId;
    public override ushort ProductId => d.ProductId;
    public override ushort Revision => d.Descriptor.Device;
    public override string Serial => d.Descriptor.SerialNumber;
    public override string Manufacturer => d.Descriptor.Manufacturer;
    public override string Product => d.Descriptor.Product;

    public override void Open()
    {
        if (d.IsOpen) return;
        d.Open();
    }
    public override void Close()
    {
        if (!d.IsOpen) return;
        d.Close();
    }

    public override void Claim()
    {
        if (!d.IsOpen) return;
        d.ClaimInterface(2);
    }

    public override byte[] ReadData(ushort wValue, byte bRequest, ushort wIndex, ushort size = 128)
    {
        if (!d.IsOpen) return [];
        const UsbCtrlFlags requestType = UsbCtrlFlags.Direction_In | UsbCtrlFlags.RequestType_Class |
                                         UsbCtrlFlags.Recipient_Interface;
        var buffer = new byte[size];

        var sp = new UsbSetupPacket(
            (byte) requestType,
            bRequest,
            wValue,
            2,
            buffer.Length);
        try
        {
            var length = d.ControlTransfer(sp, buffer, 0, buffer.Length);
            Array.Resize(ref buffer, length);
        }
        catch (UsbException ex)
        {
            if (ex.ErrorCode == Error.NoDevice)
            {
                Close();
            }
            Trace.TraceError($"Failed to read data from device: {ex.Message}");
            return [];
        }

        return buffer;
    }

    public override void WriteData(ushort wValue, byte bRequest, ushort wIndex, byte[] buffer)
    {
        if (!d.IsOpen) return;
        const UsbCtrlFlags requestType = UsbCtrlFlags.Direction_Out | UsbCtrlFlags.RequestType_Class |
                                         UsbCtrlFlags.Recipient_Interface;
        var sp = new UsbSetupPacket(
            (byte) requestType,
            bRequest,
            wValue,
            2,
            buffer.Length);
        try
        {
            d.ControlTransfer(sp, buffer, 0, buffer.Length);
        }
        catch (UsbException ex)
        {
            if (ex.ErrorCode == Error.NoDevice)
            {
                Close();
            }
            Trace.TraceError($"Failed to write data to device: {ex.Message}");
        }
    }
    
    public override async Task<byte[]> ReadDataAsync(ushort wValue, byte bRequest, ushort wIndex, ushort size = 128)
    {
        if (!d.IsOpen) return [];
        const UsbCtrlFlags requestType = UsbCtrlFlags.Direction_In | UsbCtrlFlags.RequestType_Class |
                                         UsbCtrlFlags.Recipient_Interface;
        var buffer = new byte[size];

        var sp = new UsbSetupPacket(
            (byte) requestType,
            bRequest,
            wValue,
            2,
            buffer.Length);
        try
        {
            var length = await d.ControlTransferAsync(sp, buffer, 0, buffer.Length);
            Array.Resize(ref buffer, length);
        }
        catch (UsbException ex)
        {
            if (ex.ErrorCode == Error.NoDevice)
            {
                Close();
            }
            Console.WriteLine($"Failed to read data from device: {ex.Message}");
            return [];
        }

        return buffer;
    }

    public override async Task WriteDataAsync(ushort wValue, byte bRequest, ushort wIndex, byte[] buffer)
    {
        if (!d.IsOpen) return;
        const UsbCtrlFlags requestType = UsbCtrlFlags.Direction_Out | UsbCtrlFlags.RequestType_Class |
                                         UsbCtrlFlags.Recipient_Interface;
        var sp = new UsbSetupPacket(
            (byte) requestType,
            bRequest,
            wValue,
            2,
            buffer.Length);
        try
        {
            await d.ControlTransferAsync(sp, buffer, 0, buffer.Length);
        }
        catch (UsbException ex)
        {
            if (ex.ErrorCode == Error.NoDevice)
            {
                Close();
            }
            Trace.TraceError($"Failed to write data to device: {ex.Message}");
        }
    }
}