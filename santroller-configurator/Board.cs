using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;

namespace GuitarConfigurator.NetCore;

public struct Board
{
    public const short RaspberryPiVendorId = 0x2e8a;
    public string ArdwiinoName { get; }
    public string Name { get; }
    public string Environment { get; }

    public uint CpuFreq { get; }
    public List<uint> ProductIDs { get; }

    public bool HasUsbmcu { get; }

    public bool MultipleFrequencies { get; }

    public Board(string ardwiinoName, string name, uint cpuFreq, string environment, List<uint> productIDs,
        bool hasUsbmcu, bool multipleFrequencies = false)
    {
        ArdwiinoName = ardwiinoName;
        Name = name;
        Environment = environment;
        ProductIDs = productIDs;
        CpuFreq = cpuFreq;
        HasUsbmcu = hasUsbmcu;
        MultipleFrequencies = multipleFrequencies;
    }

    public static readonly List<string> PicoArdwiinoNames = new()
    {
        "pico",
        "picow",
        "0xcb_helios",
        "adafruit_feather_rp2040",
        "adafruit_itsybitsy_rp2040",
        "adafruit_qtpy_rp2040",
        "adafruit_trinkey_qt2040",
        "adafruit_feather",
        "adafruit_feather_scorpio",
        "adafruit_feather_dvi",
        "arduino_nano_rp2040_connect",
        "adafruit_itsybitsy",
        "adafruit_qtpy",
        "adafruit_stemmafriend",
        "adafruit_trinkeyrp2040qt",
        "adafruit_macropad2040",
        "adafruit_kb2040",
        "arduino_nano_connect",
        "bridgetek_idm2040-7a",
        "cytron_maker_nano_rp2040",
        "cytron_maker_pi_rp2040",
        "datanoisetv_picoadk",
        "flyboard2040_core",
        "dfrobot_beetle_rp2040",
        "electroniccats_huntercat_nfc",
        "extelec_rc2040",
        "challenger_2040_lte",
        "challenger_2040_lora",
        "challenger_2040_subghz",
        "challenger_2040_wifi",
        "challenger_2040_wifi_ble",
        "challenger_nb_2040_wifi",
        "challenger_2040_sdrtc",
        "challenger_2040_nfc",
        "ilabs_rpico32",
        "melopero_cookie_rp2040",
        "melopero_shake_rp2040",
        "nullbits_bit_c_pro",
        "pimoroni_pga2040",
        "pimoroni_interstate75_rp2040",
        "pimoroni_keybow2040",
        "pimoroni_picolipo_4mb",
        "pimoroni_picolipo_16mb",
        "pimoroni_picosystem_rp2040",
        "pimoroni_plasma2040",
        "pimoroni_tiny2040",
        "pybstick26_rp2040",
        "solderparty_rp2040_stamp",
        "sparkfun_promicrorp2040",
        "sparkfun_micromod_rp2040",
        "vgaboard_rp2040",
        "sparkfun_thingplusrp2040",
        "upesy_rp2040_devkit",
        "seeed_xiao_rp2040",
        "vccgnd_yd_rp2040",
        "viyalab_mizu",
        "waveshare_rp2040_zero",
        "waveshare_rp2040_one",
        "waveshare_rp2040_plus_4mb",
        "waveshare_rp2040_plus_16mb",
        "waveshare_rp2040_lcd_0_96",
        "waveshare_rp2040_lcd_0.96",
        "waveshare_rp2040_lcd_1_28",
        "wiznet_5100s_evb_pico",
        "wiznet_wizfi360_evb_pico",
        "wiznet_5500_evb_pico"
    };

    public static readonly Board Generic = new("generic", "Generic Serial Device", 0, "generic", new List<uint>(),
        false);

    public static readonly Board[] Atmega32U4Boards =
    {
        new("a-micro", "Arduino Micro in Bootloader Mode", 16000000, "arduino_micro_16",
            new List<uint> {0x0037, 0x0237},
            false, true),
        new("a-micro", "Arduino Micro", 16000000, "arduino_micro_16", new List<uint> {0x8037, 0x8237}, false, true),
        new("micro", "Sparkfun Pro Micro 3.3V", 8000000, "sparkfun_promicro_8", new List<uint> {0x9204}, false, true),
        new("micro", "Sparkfun Pro Micro 5V", 16000000, "sparkfun_promicro_16", new List<uint> {0x9206}, false, true),
        new("leonardo", "Arduino Leonardo", 16000000, "arduino_leonardo_16", new List<uint> {0x8036, 0x800c}, false,
            true),
        new("leonardo", "Arduino Leonardo 3.3V", 8000000, "arduino_leonardo_8", new List<uint>(), false, true),
        new("leonardo", "Arduino Micro / Pro Micro / Leonardo in Bootloader Mode", 16000000, "sparkfun_promicro_8",
            new List<uint> {0x0036}, false, true),
        new("micro", "Arduino Pro Micro in Bootloader Mode", 8000000, "sparkfun_promicro_8",
            new List<uint> {0x9203, 0x9207},
            false, true),
        new("micro", "Arduino Pro Micro in Bootloader Mode", 16000000, "sparkfun_promicro_16", new List<uint> {0x9205},
            false, true)
    };

    public static readonly Board PicoBoard = new("pico", "Raspberry PI Pico", 125000000, "pico",
        new List<uint> {0x000a}, false);

    public static readonly Board[] MegaBoards =
    {
        new("mega2560", "Arduino Mega 2560", 16000000, "arduino_mega_2560", new List<uint> {0x0010, 0x0042}, true),
        new("megaadk", "Arduino Mega ADK", 16000000, "arduino_mega_adk", new List<uint> {0x003f, 0x0044}, true)
    };

    public static readonly Board DfuBoard = new("usb", "Arduino Uno / Mega / Mega ADK in DFU mode", 0, "",
        new List<uint> {0x2FF7, 0x2FEF}, true);

    public static readonly Board Uno = new("uno", "Arduino Uno", 16000000, "arduino_uno",
        new List<uint> {0x0043, 0x0001, 0x0243}, true);

    public static readonly Board[] Boards = MegaBoards
        .Concat(Atmega32U4Boards)
        .Concat(new[] {Uno, DfuBoard, PicoBoard})
        .ToArray();

    public static Board FindBoard(string ardwiinoName, uint cpuFreq)
    {
        if (PicoArdwiinoNames.Contains(ardwiinoName))
        {
            return PicoBoard;
        }

        foreach (var board in Boards)
            if (board.ArdwiinoName == ardwiinoName &&
                (!board.MultipleFrequencies || board.CpuFreq == cpuFreq))
                return board;

        return Generic;
    }

    public static Microcontroller FindMicrocontroller(Board board)
    {
        if (Atmega32U4Boards.Contains(board)) return new Micro(board);

        if (board.Name == Uno.Name) return new Uno(board);

        if (MegaBoards.Contains(board)) return new Mega(board);

        if (board.Name == PicoBoard.Name) return new Pico(board);

        throw new NotSupportedException("Not sure how we got here");
    }

    public bool IsAvr()
    {
        return Atmega32U4Boards.Contains(this) || Name == Uno.Name || MegaBoards.Contains(this);
    }

    public bool IsPico()
    {
        return PicoBoard.Name == Name;
    }

    public bool IsGeneric()
    {
        return Generic.Name == Name;
    }

    public bool Is32U4()
    {
        return Atmega32U4Boards.Contains(this);
    }
}