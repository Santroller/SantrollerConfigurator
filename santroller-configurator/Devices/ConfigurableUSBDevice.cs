using System;
using System.Threading.Tasks;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.ViewModels;
using Version = SemanticVersioning.Version;

namespace GuitarConfigurator.NetCore.Devices;

public abstract class ConfigurableUsbDevice : IConfigurableDevice
{
    protected readonly IUsbDevice UsbDevice;
    public readonly Version Version;
    private TaskCompletionSource<string?>? _bootloaderPath;
    private string? _lastBootloaderPath;

    protected ConfigurableUsbDevice(IUsbDevice usbDevice)
    {
        UsbDevice = usbDevice;
        Version = new Version((usbDevice.Revision >> 8) & 0xff, (usbDevice.Revision >> 4) & 0xf, usbDevice.Revision & 0xf);
    }

    public Board Board { get; set; }
    public string Serial => UsbDevice.Serial;
    public string Manufacturer => UsbDevice.Manufacturer;
    public string Product => UsbDevice.Product;

    public IConfigurableDevice? BootloaderDevice { get; private set; }

    public abstract bool MigrationSupported { get; }

    public abstract void Bootloader();

    public bool IsSameDevice(IDevice device)
    {
        return device.IsSameDevice(UsbDevice);
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
        else if (Board.HasUsbmcu && device is Arduino {Board.HasUsbmcu: true} arduino)
        {
            _bootloaderPath?.TrySetResult(arduino.GetSerialPort());
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
        UsbDevice.Close();
    }

    public bool IsPico()
    {
        return Board.IsPico();
    }

    public abstract bool LoadConfiguration(ConfigViewModel model, bool merge);

    public void Close()
    {
        UsbDevice.Close();
    }
    public Task<byte[]> ReadDataAsync(ushort wValue, byte bRequest, ushort size = 128)
    {
        return UsbDevice.ReadDataAsync(wValue, bRequest, 2, size);
    }


    public Task WriteDataAsync(ushort wValue, byte bRequest, byte[] buffer)
    {
        return UsbDevice.WriteDataAsync(wValue, bRequest, 2, buffer);
    }
    public byte[] ReadData(ushort wValue, byte bRequest, ushort size = 128)
    {
        return UsbDevice.ReadData(wValue, bRequest, 2, size);
    }


    public void WriteData(ushort wValue, byte bRequest, byte[] buffer)
    {
        UsbDevice.WriteData(wValue, bRequest, 2, buffer);
    }
}