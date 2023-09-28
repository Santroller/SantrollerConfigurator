using System.Threading.Tasks;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Utils;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Devices;

public class PicoDevice : IConfigurableDevice
{
    private readonly string _path;

    public PicoDevice(string path)
    {
        _path = path;
    }

    public bool MigrationSupported => true;

    public bool IsSameDevice(PlatformIoPort port)
    {
        return false;
    }

    public bool IsGeneric()
    {
        return false;
    }

    public bool IsSameDevice(string serialOrPath)
    {
        return serialOrPath == _path;
    }

    public void Bootloader()
    {
    }


    void IConfigurableDevice.DeviceAdded(IConfigurableDevice device)
    {
    }

    public bool LoadConfiguration(ConfigViewModel model, bool merge)
    {
        return false;
    }

    public Microcontroller GetMicrocontroller(ConfigViewModel model)
    {
        return Board.FindMicrocontroller(Board.PicoBoard);
    }

    public Task<string?> GetUploadPortAsync()
    {
        return Task.FromResult((string?) _path);
    }

    public bool IsAvr()
    {
        return false;
    }

    public bool IsPico()
    {
        return true;
    }

    public void Reconnect()
    {
    }

    public void Revert()
    {
    }

    public bool HasDfuMode()
    {
        return false;
    }

    public bool Is32U4()
    {
        return false;
    }

    public void Disconnect()
    {
    }

    public string GetPath()
    {
        return _path;
    }

    public override string ToString()
    {
        return $"Raspberry Pi Pico ({_path})";
    }
}