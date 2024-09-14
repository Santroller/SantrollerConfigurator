using System.Threading.Tasks;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Devices;

public class EmptyDevice: IConfigurableDevice
{
    public bool MigrationSupported => false;
    public bool IsSameDevice(IDevice device)
    {
        return false;
    }

    public void Bootloader()
    {
    }

    public void DeviceAdded(IConfigurableDevice device)
    {
    }

    public Microcontroller GetMicrocontroller(ConfigViewModel model)
    {
        return new Pico(Board.PicoBoard);
    }

    public bool LoadConfiguration(ConfigViewModel model, bool merge)
    {
        return true;
    }

    public Task<string?> GetUploadPortAsync()
    {
        return Task.FromResult<string?>(null);
    }

    public bool IsGeneric()
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
}