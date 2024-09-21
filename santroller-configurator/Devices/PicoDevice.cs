using System.Linq;
using System.Threading.Tasks;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Devices;

public class PicoDevice : IConfigurableDevice
{
    private readonly string _path;
    private readonly Board _board;
    public PicoDevice(string path, string env)
    {
        _path = path;
        _board = Board.PicoBoards.First(s => s.Environment == env);
    }

    public bool MigrationSupported => true;


    public bool IsGeneric => false;
    public bool IsAvr => false;

    public bool IsPico => true;

    public bool Is32U4 => false;

    public bool IsSameDevice(IDevice device)
    {
        return device is PicoDevice picoDevice && picoDevice._path == _path;
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
        return new Pico(_board);
    }

    public Task<string?> GetUploadPortAsync()
    {
        return Task.FromResult((string?) _path);
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

    public void Disconnect()
    {
    }

    public string GetPath()
    {
        return _path;
    }

    public override string ToString()
    {
        return $"{_board.Name} ({_path})";
    }
}