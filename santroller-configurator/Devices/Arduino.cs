using System;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Utils;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Devices;

public class Arduino : IConfigurableDevice
{
    private readonly bool _generic;
    private TaskCompletionSource<string?>? _arduino32U4Path;
    private string _port;

    public Arduino(PlatformIoPort port)
    {
        DfuDetected = new Subject<bool>();
        _port = port.Port;
        foreach (var board in Board.Boards)
        {
            if (!board.ProductIDs.Contains(port.Pid)) continue;
            Board = board;
            Is32U4Bootloader = Board.Name.Contains("Bootloader Mode");
            MigrationSupported = true;
            return;
        }

        // Handle any other RP2040 based boards we don't already have code for, using just the generic rp2040 board in this case
        if (port.Vid == Board.RaspberryPiVendorId)
        {
            Board = Board.PicoBoard;
            MigrationSupported = true;
            return;
        }

        // Really, really old ardwiinos had a serial protocol that response to a couple of commands for retrieving data.
        if (port is {Vid: 0x1209, Pid: 0x2882})
        {
            MigrationSupported = false;

            var serial = new SerialPort(port.Port, 115200);
            serial.Open();
            serial.Write("i\x06\n");
            var boardName = serial.ReadLine().Trim();
            serial.DiscardInBuffer();
            serial.Write("i\x04\n");
            var boardFreqStr = serial.ReadLine().Replace("UL", "");
            var boardFreq = uint.Parse(boardFreqStr);
            var tmp = Board.FindBoard(boardName, boardFreq);
            Board = new Board(boardName, $"Ardwiino - {tmp.Name} - pre 4.3.7", boardFreq, tmp.Environment,
                tmp.ProductIDs, tmp.HasUsbmcu);
            serial.Close();
        }
        else
        {
            Board = Board.Generic;
            MigrationSupported = true;
            _generic = true;
        }
    }

    public bool Is32U4Bootloader { get; private set; }

    public Board Board { get; set; }

    public Subject<bool> DfuDetected { get; }

    public bool MigrationSupported { get; }

    public bool IsSameDevice(PlatformIoPort port)
    {
        return port.Port == _port;
    }

    public bool IsSameDevice(string serialOrPath)
    {
        return false;
    }

    public void Bootloader()
    {
        if (Is32U4() && Is32U4Bootloader) return;
        var serial = new SerialPort(_port, 1200);
        serial.Open();
        serial.Close();
    }

    public void BootloaderUsb()
    {
        // Automagically handled by pio
    }

    public Microcontroller GetMicrocontroller(ConfigViewModel model)
    {
        if (model.Main.IsDfu || model.Main.IsMega || model.Main.IsUno)
        {
            var unoMegaType = model.Main.UnoMegaType;
            Board = unoMegaType switch
            {
                UnoMegaType.Uno => Board.Uno,
                UnoMegaType.MegaAdk => Board.MegaBoards[1],
                UnoMegaType.Mega => Board.MegaBoards[0],
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        if (model.Main.Is32U4)
        {
            var u4Mode = model.Main.Arduino32U4Type;
            Board = u4Mode switch
            {
                Arduino32U4Type.ProMicro => Board.Atmega32U4Boards[2],
                Arduino32U4Type.Leonardo => Board.Atmega32U4Boards[4],
                Arduino32U4Type.Micro => Board.Atmega32U4Boards[1],
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        if (model.Main.IsGeneric)
            Board = model.Main.AvrType switch
            {
                AvrType.ProMicro => Board.Atmega32U4Boards[2],
                AvrType.Leonardo => Board.Atmega32U4Boards[4],
                AvrType.Micro => Board.Atmega32U4Boards[1],
                AvrType.Uno => Board.Uno,
                AvrType.Mega => Board.MegaBoards[0],
                AvrType.MegaAdk => Board.MegaBoards[1],
                _ => throw new ArgumentOutOfRangeException()
            };

        return Board.FindMicrocontroller(Board);
    }

    public void Reconnect()
    {
    }

    public void Revert()
    {
    }

    public bool HasDfuMode()
    {
        return Board.HasUsbmcu;
    }

    public bool LoadConfiguration(ConfigViewModel model, bool merge)
    {
        return false;
    }

    public Task<string?> GetUploadPortAsync()
    {
        if (IsPico())
        {
            _arduino32U4Path = new TaskCompletionSource<string?>();
            Bootloader();
            return _arduino32U4Path.Task;
        }


        return Task.FromResult((string?) GetSerialPort());
    }

    public bool IsAvr()
    {
        return true;
    }

    public void DeviceAdded(IConfigurableDevice device)
    {
        switch (device)
        {
            case Arduino arduino when Is32U4() && arduino.Is32U4():
                Console.WriteLine("Found device with port: " + arduino.GetSerialPort());
                _port = arduino.GetSerialPort();
                Board = arduino.Board;
                Is32U4Bootloader = true;
                break;
            case PicoDevice pico when IsPico() && _arduino32U4Path != null:
                Console.WriteLine("Found device with port: " + pico.GetPath());
                _port = pico.GetPath();
                _arduino32U4Path.TrySetResult(pico.GetPath());
                _arduino32U4Path = null;
                break;
            case Dfu when !Is32U4():
                DfuDetected.OnNext(true);
                break;
        }
    }

    public bool IsPico()
    {
        return Board.IsPico();
    }

    public bool Is32U4()
    {
        return Board.Is32U4();
    }

    public void Disconnect()
    {
        
    }

    public bool IsGeneric()
    {
        return _generic;
    }

    public string GetSerialPort()
    {
        return _port;
    }

    public override string ToString()
    {
        return $"{Board.Name} ({_port})";
    }

    public bool IsUno()
    {
        return Board.Uno.Name == Board.Name;
    }

    public bool IsMega()
    {
        return Board.MegaBoards.Contains(Board);
    }
}