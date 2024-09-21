using System.Threading.Tasks;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Utils;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Devices;

public interface IConfigurableDevice: IDevice
{
    public bool MigrationSupported { get; }

    public void Bootloader();

    public void DeviceAdded(IConfigurableDevice device);

    public Microcontroller GetMicrocontroller(ConfigViewModel model);

    public bool LoadConfiguration(ConfigViewModel model, bool merge);

    public Task<string?> GetUploadPortAsync();
    public bool IsGeneric { get; }
    public bool IsPico { get; }
    public bool Is32U4 { get; }
    void Reconnect();
    bool HasDfuMode();
    void Disconnect();
}