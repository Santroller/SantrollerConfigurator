namespace GuitarConfigurator.NetCore.Devices;

public interface IDevice
{
    bool IsSameDevice(IDevice device);
    
}