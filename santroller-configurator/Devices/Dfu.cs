using System;
#if Windows
using System.Diagnostics;
using System.IO;
#endif
using System.Threading.Tasks;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Utils;
using GuitarConfigurator.NetCore.ViewModels;
using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

namespace GuitarConfigurator.NetCore.Devices;

public class Dfu : IConfigurableDevice
{
    public static readonly uint DfuPid8U2 = 0x2FF7;
    public static readonly uint DfuPid16U2 = 0x2FEF;
    public static readonly uint DfuVid = 0x03eb;

    private readonly IUsbDevice _device;

    public Dfu(IUsbDevice device)
    {
        _device = device;
        var pid = device.VendorId;
        foreach (var board in Board.Boards)
            if (board.ProductIDs.Contains((uint) pid) && board.HasUsbmcu)
            {
                Board = board;
                Console.WriteLine(Board.Environment);
                return;
            }

        throw new InvalidOperationException("Not expected");
    }

    public Board Board { get; }

    public bool MigrationSupported => true;

    public bool IsSameDevice(IDevice device)
    {
        return _device.IsSameDevice(device);
    }


    public void DeviceAdded(IConfigurableDevice device)
    {
    }

    public Microcontroller GetMicrocontroller(ConfigViewModel model)
    {
        var board = Board;
        if (Board.ArdwiinoName == "usb")
            switch (model.Main.UnoMegaType)
            {
                case UnoMegaType.Uno:
                    board = Board.Uno;
                    break;
                case UnoMegaType.MegaAdk:
                    board = Board.MegaBoards[1];
                    break;
                case UnoMegaType.Mega:
                    board = Board.MegaBoards[0];
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        return Board.FindMicrocontroller(board);
    }

    public bool LoadConfiguration(ConfigViewModel model, bool merge)
    {
        return false;
    }

    public Task<string?> GetUploadPortAsync()
    {
        return Task.FromResult<string?>(null);
    }

    public bool IsAvr()
    {
        return true;
    }

    public void Bootloader()
    {
    }

    public bool IsPico()
    {
        return false;
    }

    public void Reconnect()
    {
    }

    public void Revert()
    {
    }

    public bool HasDfuMode()
    {
        return true;
    }

    public bool Is32U4()
    {
        return false;
    }

    public void Disconnect()
    {
    }

    public bool IsGeneric()
    {
        return false;
    }

    public string GetRestoreSuffix()
    {
        return _device.ProductId == DfuPid8U2 ? "8" : "16";
    }

    public string GetRestoreProcessor()
    {
        return _device.ProductId == DfuPid8U2 ? "at90usb82" : "atmega16u2";
    }

    public override string ToString()
    {
        return $"{Board.Name} ({_device})";
    }

    public void Launch()
    {
#if Windows
        var mcu = _args.Device.IdProduct == 0x2FF7 ? "at90usb82" : "atmega16u2";
        
        var appdataFolder = AssetUtils.GetAppDataFolder();
        var dfuExecutable = Path.Combine(appdataFolder, "platformio", "dfu-programmer.exe");
        var process = new Process();
        process.StartInfo.FileName = dfuExecutable;
        process.StartInfo.Arguments = $"{mcu} launch --no-reset";
        process.Start();
        process.WaitForExit();
#else
        _device.Open();
        if (!_device.IsOpen) return;
        _device.ReadData(0, 3, 0, 8);
        
        _device.WriteData(0, 1, 0, [0x04, 0x03, 0x01, 0x00, 0x00]);
        _device.WriteData(0, 1, 0, []);
#endif
    }
}