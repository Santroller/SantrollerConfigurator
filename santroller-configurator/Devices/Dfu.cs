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
using LibUsbDotNet.DeviceNotify;
using LibUsbDotNet.Main;

namespace GuitarConfigurator.NetCore.Devices;

public class Dfu : IConfigurableDevice
{
    public static readonly uint DfuPid8U2 = 0x2FF7;
    public static readonly uint DfuPid16U2 = 0x2FEF;
    public static readonly uint DfuVid = 0x03eb;

    private readonly DeviceNotifyEventArgs _args;

    private readonly string _port;

    public Dfu(DeviceNotifyEventArgs args)
    {
        _args = args;
        var pid = args.Device.IdProduct;
        _port = args.Device.Name;
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

    public bool IsSameDevice(PlatformIoPort port)
    {
        return false;
    }

    public bool IsSameDevice(string serialOrPath)
    {
        return serialOrPath == _port;
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

    public void BootloaderUsb()
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
        return _args.Device.IdProduct == DfuPid8U2 ? "8" : "16";
    }

    public string GetRestoreProcessor()
    {
        return _args.Device.IdProduct == DfuPid8U2 ? "at90usb82" : "atmega16u2";
    }

    public override string ToString()
    {
        return $"{Board.Name} ({_port})";
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
        _args.Device.Open(out var device);
        if (device != null)
        {
            var requestType = UsbCtrlFlags.Direction_In | UsbCtrlFlags.RequestType_Class |
                              UsbCtrlFlags.Recipient_Interface;

            var sp = new UsbSetupPacket(
                (byte) requestType,
                3,
                0,
                0,
                8);
            var buffer = new byte[8];
            device.ControlTransfer(ref sp, buffer, buffer.Length, out _);
            buffer = new byte[] {0x04, 0x03, 0x01, 0x00, 0x00};
            requestType = UsbCtrlFlags.Direction_Out | UsbCtrlFlags.RequestType_Class |
                          UsbCtrlFlags.Recipient_Interface;

            sp = new UsbSetupPacket(
                (byte) requestType,
                1,
                0,
                0,
                buffer.Length);
            device.ControlTransfer(ref sp, buffer, buffer.Length, out _);
            sp = new UsbSetupPacket(
                (byte) requestType,
                1,
                0,
                0,
                0);
            device.ControlTransfer(ref sp, buffer, 0, out _);
        }
#endif
    }
}