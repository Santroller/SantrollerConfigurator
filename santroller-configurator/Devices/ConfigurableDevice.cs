using System.Threading.Tasks;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Utils;
using GuitarConfigurator.NetCore.ViewModels;
using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;

namespace GuitarConfigurator.NetCore.Devices;

public interface IConfigurableDevice: IDevice
{
    public bool MigrationSupported { get; }

    public void Bootloader();

    public void DeviceAdded(IConfigurableDevice device);

    public Microcontroller GetMicrocontroller(ConfigViewModel model);

    public bool LoadConfiguration(ConfigViewModel model, bool merge);

    public Task<string?> GetUploadPortAsync();
    public bool IsGeneric();
    public bool IsPico();
    void Reconnect();
    bool HasDfuMode();
    bool Is32U4();
    void Disconnect();
}