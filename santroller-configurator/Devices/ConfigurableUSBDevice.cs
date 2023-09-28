using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Utils;
using GuitarConfigurator.NetCore.ViewModels;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using Version = SemanticVersioning.Version;

namespace GuitarConfigurator.NetCore.Devices;

public abstract class ConfigurableUsbDevice : IConfigurableDevice
{
    public readonly UsbDevice Device;
    public readonly string Path;
    public readonly string Serial;
    public readonly Version Version;
    private TaskCompletionSource<string?>? _bootloaderPath;
    private string? _lastBootloaderPath;

    protected ConfigurableUsbDevice(UsbDevice device, string path, string serial, ushort version)
    {
        Device = device;
        Path = path;
        Serial = serial;
        Version = new Version((version >> 8) & 0xff, (version >> 4) & 0xf, version & 0xf);
    }

    public Board Board { get; set; }

    public IConfigurableDevice? BootloaderDevice { get; private set; }

    public abstract bool MigrationSupported { get; }

    public abstract void Bootloader();

    public bool IsSameDevice(PlatformIoPort port)
    {
        return false;
    }

    public bool IsSameDevice(string serialOrPath)
    {
        return Serial == serialOrPath || Path == serialOrPath;
    }

    public void DeviceAdded(IConfigurableDevice device)
    {
        if (Board.Is32U4() && device is Arduino arduino2 && (arduino2.Board.Is32U4() || arduino2.Board.IsGeneric()))
        {
            _bootloaderPath?.TrySetResult(arduino2.GetSerialPort());
            if (arduino2.Is32U4Bootloader || arduino2.Board.IsGeneric())
            {
                _lastBootloaderPath = arduino2.GetSerialPort();
            }
        }
        else if (device is PicoDevice pico && Board.IsPico())
        {
            _bootloaderPath?.TrySetResult(pico.GetPath());
        }
        else if (Board.HasUsbmcu && device is Dfu {Board.HasUsbmcu: true} dfu)
        {
            BootloaderDevice = dfu;
            _bootloaderPath?.TrySetResult(dfu.Board.Environment);
        }
    }

    public abstract Microcontroller GetMicrocontroller(ConfigViewModel model);

    public async Task<string?> GetUploadPortAsync()
    {
        if (!Board.ArdwiinoName.Contains("pico") && !Board.HasUsbmcu && !Is32U4()) return null;
        if (_lastBootloaderPath != null)
        {
            return _lastBootloaderPath;
        }

        _bootloaderPath = new TaskCompletionSource<string?>();
        Bootloader();
        return await _bootloaderPath.Task;
    }

    public bool IsAvr()
    {
        return Board.IsAvr();
    }

    public bool IsGeneric()
    {
        return Board.IsGeneric();
    }

    public void Reconnect()
    {
    }

    public abstract void Revert();

    public bool HasDfuMode()
    {
        return Board.HasUsbmcu;
    }

    public bool Is32U4()
    {
        return Board.Is32U4();
    }

    public virtual void Disconnect()
    {
        if (Device.IsOpen)
        {
            Device.Close();
        }
    }

    public bool IsPico()
    {
        return Board.IsPico();
    }

    public abstract bool LoadConfiguration(ConfigViewModel model, bool merge);

    public byte[] ReadData(ushort wValue, byte bRequest, ushort size = 128)
    {
        if (!Device.IsOpen) return Array.Empty<byte>();
        const UsbCtrlFlags requestType = UsbCtrlFlags.Direction_In | UsbCtrlFlags.RequestType_Class |
                                         UsbCtrlFlags.Recipient_Interface;
        var buffer = new byte[size];

        var sp = new UsbSetupPacket(
            (byte) requestType,
            bRequest,
            wValue,
            2,
            buffer.Length);

        if (!Device.ControlTransfer(ref sp, buffer, buffer.Length, out var length))
        {
            var firstError = UsbDevice.LastErrorString;
            if (!Device.ControlTransfer(ref sp, buffer, buffer.Length, out length))
            {
                Trace.TraceError($"Failed to read data from device: {UsbDevice.LastErrorString}");
                return Array.Empty<byte>();
            }

            Trace.TraceWarning($"Failed to read data from device (retry succeeded): {firstError}");
        }

        Array.Resize(ref buffer, length);
        return buffer;
    }


    public void WriteData(ushort wValue, byte bRequest, byte[] buffer)
    {
        if (!Device.IsOpen) return;
        var requestType = UsbCtrlFlags.Direction_Out | UsbCtrlFlags.RequestType_Class |
                          UsbCtrlFlags.Recipient_Interface;
        var sp = new UsbSetupPacket(
            (byte) requestType,
            bRequest,
            wValue,
            2,
            buffer.Length);
        if (Device.ControlTransfer(ref sp, buffer, buffer.Length, out _)) return;
        var firstError = UsbDevice.LastErrorString;
        if (!Device.ControlTransfer(ref sp, buffer, buffer.Length, out _))
        {
            Trace.TraceError($"Failed to write data to device: {UsbDevice.LastErrorString}");
            return;
        }

        Trace.TraceWarning($"Failed to write data to device (retry succeeded): {firstError}");
    }
}