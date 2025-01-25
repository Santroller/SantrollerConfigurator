using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public class Pico : Microcontroller
{
    private const int GpioCount = 30;
    public const int PinA0 = 26;

    public static readonly Dictionary<int, SpiPinType> SpiTypesByPin = new()
    {
        {0, SpiPinType.Miso},
        {1, SpiPinType.CSn},
        {2, SpiPinType.Sck},
        {3, SpiPinType.Mosi},
        {4, SpiPinType.Miso},
        {5, SpiPinType.CSn},
        {6, SpiPinType.Sck},
        {7, SpiPinType.Mosi},
        {19, SpiPinType.Mosi},
        {18, SpiPinType.Sck},
        {17, SpiPinType.CSn},
        {16, SpiPinType.Miso},
        {8, SpiPinType.Miso},
        {9, SpiPinType.CSn},
        {10, SpiPinType.Sck},
        {11, SpiPinType.Mosi},
        {12, SpiPinType.Miso},
        {13, SpiPinType.CSn},
        {14, SpiPinType.Sck},
        {15, SpiPinType.Mosi}
    };

    public static readonly Dictionary<int, int> SpiIndexByPin = new()
    {
        {0, 0},
        {1, 0},
        {2, 0},
        {3, 0},
        {4, 0},
        {5, 0},
        {6, 0},
        {7, 0},
        {19, 0},
        {18, 0},
        {17, 0},
        {16, 0},
        {8, 1},
        {9, 1},
        {10, 1},
        {11, 1},
        {12, 1},
        {13, 1},
        {14, 1},
        {15, 1}
    };

    public static readonly Dictionary<int, int> TwiIndexByPin = new()
    {
        {0, 0},
        {1, 0},
        {2, 1},
        {3, 1},
        {4, 0},
        {5, 0},
        {6, 1},
        {7, 1},
        {8, 0},
        {9, 0},
        {10, 1},
        {11, 1},
        {12, 0},
        {13, 0},
        {14, 1},
        {15, 1},
        {16, 0},
        {17, 0},
        {18, 1},
        {19, 1},
        {20, 0},
        {21, 0},
        {26, 1},
        {27, 1}
    };

    public static readonly Dictionary<int, TwiPinType> TwiTypeByPin = new()
    {
        {0, TwiPinType.Sda},
        {1, TwiPinType.Scl},
        {2, TwiPinType.Sda},
        {3, TwiPinType.Scl},
        {4, TwiPinType.Sda},
        {5, TwiPinType.Scl},
        {6, TwiPinType.Sda},
        {7, TwiPinType.Scl},
        {8, TwiPinType.Sda},
        {9, TwiPinType.Scl},
        {10, TwiPinType.Sda},
        {11, TwiPinType.Scl},
        {12, TwiPinType.Sda},
        {13, TwiPinType.Scl},
        {14, TwiPinType.Sda},
        {15, TwiPinType.Scl},
        {16, TwiPinType.Sda},
        {17, TwiPinType.Scl},
        {18, TwiPinType.Sda},
        {19, TwiPinType.Scl},
        {20, TwiPinType.Sda},
        {21, TwiPinType.Scl},
        {26, TwiPinType.Sda},
        {27, TwiPinType.Scl}
    };

    public Pico(Board board)
    {
        Board = board;
    }

    public override bool TwiAssignable => true;
    public override bool SpiAssignable => true;

    public override Board Board { get; }

    public override List<int> AnalogPins => [26, 27, 28, 29];
    public List<int> BluetoothPins => [23, 24, 29];

    // All pins support pwm
    public override List<int> PwmPins { get; } =
        Enumerable.Range(0, GpioCount).ToList();

    public override int GetFirstAnalogPin()
    {
        return PinA0;
    }

    public override string GenerateAnalogRead(int pin, ConfigViewModel model, bool peripheral)
    {
        return peripheral ? $"slaveReadAnalog({pin - PinA0})" : $"adc({pin - PinA0})";
    }

    public override string GenerateDigitalRead(int pin, bool invert, bool peripheral)
    {
        // Invert on pullup
        if (peripheral)
        {
            return invert ? $"(((slave_digital & (1 << {pin})) == 0) && slave_initted)" : $"((slave_digital & (1 << {pin})) && slave_initted)";
        }

        return invert ? $"(sio_hw->gpio_in & (1 << {pin})) == 0" : $"sio_hw->gpio_in & (1 << {pin})";
    }

    public override string GenerateDigitalWrite(int pin, bool val, bool peripheral, bool picow)
    {
        if (peripheral)
        {
            return $"slaveWriteDigital({pin}, {val.ToString().ToLower()})";
        }

        if (picow && pin == 25)
        {
            return $"cyw43_arch_gpio_put(0, {val.ToString().ToLower()});";
        }

        return val ? $"sio_hw->gpio_set = {1 << pin}" : $"sio_hw->gpio_clr = {1 << pin}";
    }

    public override string GenerateAnalogWrite(int pin, string val, bool peripheral)
    {
        return peripheral ? $"slaveWriteAnalog({pin}, {val})" : $"analogWrite({pin}, {val})";
    }


    public override SpiConfig AssignSpiPins(ConfigViewModel model, string type, bool peripheral, bool includesSck, bool includesMiso,
        int mosi, int miso,
        int sck, bool cpol,
        bool cpha,
        bool msbfirst,
        uint clock, bool output)
    {
        return new PicoSpiConfig(model, type, peripheral, includesSck, includesMiso, mosi, miso, sck, cpol, cpha, msbfirst, clock, output);
    }

    public override TwiConfig AssignTwiPins(ConfigViewModel model, string type, bool peripheral, int sda, int scl,
        int clock, bool output)
    {
        return new PicoTwiConfig(model, type, peripheral, sda, scl, clock, output);
    }

    public override IEnumerable<string> GeneratePs2Defines(int ack, string prefix)
    {
        return Array.Empty<string>();
    }

    public override List<int> SupportedAckPins()
    {
        return Enumerable.Range(0, GpioCount).ToList();
    }

    public override List<KeyValuePair<int, SpiPinType>> SpiPins(bool output)
    {
        if (output)
        {
            // Pico SPI flips Mosi and Miso in output mode
            return SpiTypesByPin.Select(s => new KeyValuePair<int, SpiPinType>(s.Key, s.Value switch
            {
                SpiPinType.Mosi => SpiPinType.Miso,
                SpiPinType.Miso => SpiPinType.Mosi,
                _ => s.Value
            })).ToList();
        }
        return SpiTypesByPin.ToList();
    }

    public override List<KeyValuePair<int, TwiPinType>> TwiPins(bool output)
    {
        return TwiTypeByPin.ToList();
    }

    public override string GenerateInit(ConfigViewModel configViewModel)
    {
        var ret = "";
        var pins = configViewModel.GetPinConfigs().OfType<DirectPinConfig>();
        foreach (var devicePin in pins)
        {
            if (devicePin.Pin == 25 && configViewModel.IsBluetooth)
            {
                continue;
            }
            if (devicePin.PinMode == DevicePinMode.Skip || devicePin.Peripheral || devicePin.Type == "led_output" || devicePin.Type.Contains("Ps2 Output")) continue;
            switch (devicePin.PinMode)
            {
                case DevicePinMode.Analog:
                    ret += $"\nadc_gpio_init({devicePin.Pin});";
                    continue;
                default:
                    var up = devicePin.PinMode is DevicePinMode.BusKeep or DevicePinMode.PullUp;
                    var down = devicePin.PinMode is DevicePinMode.BusKeep or DevicePinMode.PullDown;
                    ret += "\n";
                    ret += $"""
                            gpio_init({devicePin.Pin});
                            gpio_set_dir({devicePin.Pin},{(devicePin.PinMode == DevicePinMode.Output).ToString().ToLower()});
                            gpio_set_pulls({devicePin.Pin},{up.ToString().ToLower()},{down.ToString().ToLower()});
                            """;
                    continue;
            }
        }

        return ret;
    }
    
    public override string GenerateLedInit(ConfigViewModel configViewModel)
    {
        var ret = "";
        var pins = configViewModel.GetPinConfigs().OfType<DirectPinConfig>();
        foreach (var devicePin in pins)
        {
            if (devicePin.Pin == 25 && configViewModel.IsBluetooth)
            {
                continue;
            }
            if (devicePin.PinMode == DevicePinMode.Skip || devicePin.Peripheral || devicePin.Type.Contains("Ps2 Output")) continue;
            switch (devicePin.PinMode)
            {
                case DevicePinMode.Analog:
                    ret += $"\nadc_gpio_init({devicePin.Pin});";
                    continue;
                default:
                    var up = devicePin.PinMode is DevicePinMode.BusKeep or DevicePinMode.PullUp;
                    var down = devicePin.PinMode is DevicePinMode.BusKeep or DevicePinMode.PullDown;
                    ret += "\n";
                    ret += $"""
                            gpio_init({devicePin.Pin});
                            gpio_set_dir({devicePin.Pin},{(devicePin.PinMode == DevicePinMode.Output).ToString().ToLower()});
                            gpio_set_pulls({devicePin.Pin},{up.ToString().ToLower()},{down.ToString().ToLower()});
                            """;
                    continue;
            }
        }

        return ret;
    }

    public override int GetChannel(int pin, bool reconfigurePin)
    {
        var chan = pin - PinA0;
        if (reconfigurePin) chan |= 1 << 7;
        return chan;
    }

    public static string GetPinForPico(int pin, bool twi, bool spi, bool outputMode)
    {
        var ret = $"GP{pin}";
        if (twi && TwiIndexByPin.TryGetValue(pin, out var value))
            ret += $" / TWI{value} {TwiTypeByPin[pin].ToString().ToUpper()}";

        if (spi && SpiIndexByPin.TryGetValue(pin, out var value1))
        {
            // MOSI and MISO are swapped for output mode
            var type = SpiTypesByPin[pin];
            type = type switch
            {
                SpiPinType.Mosi when outputMode => SpiPinType.Miso,
                SpiPinType.Miso when outputMode => SpiPinType.Mosi,
                _ => type
            };
            ret += $" / SPI{value1} {type.ToString().ToUpper()}";
        }

        switch (pin)
        {
            case >= 26:
                ret += $" / ADC{pin - 26}";
                break;
            case 23:
                ret += " Pico Power Supply Power Select";
                break;
            case 24:
                ret += " VBUS sense";
                break;
            case 25:
                ret += " Pico Onboard LED";
                break;
        }

        return ret;
    }

    public override string GetPinForMicrocontroller(int pin, bool twi, bool spi, bool outputMode)
    {
        return GetPinForPico(pin, twi, spi, outputMode);
    }

    public override List<int> GetAllPins()
    {
        return PwmPins;
    }

    public override bool FilterPin(bool isAnalog, bool isBluetooth, bool isInterrupt, int pin)
    {
        if (isBluetooth && BluetoothPins.Contains(pin))
        {
            return false;
        }

        return !isAnalog || AnalogPins.Contains(pin);
    }

    public override void PinsFromPortMask(int port, int mask, byte pins,
        Dictionary<int, bool> digitalRaw)
    {
        for (var i = 0; i < 8; i++)
            if ((mask & (1 << i)) != 0)
                digitalRaw[port * 8 + i] = (pins & (1 << i)) != 0;
    }

    public override int GetAnalogMask(DevicePin devicePin)
    {
        return 0;
    }

    public override int GetFirstDigitalPin()
    {
        return 0;
    }

    public override Dictionary<int, int> GetPortsForTicking(IEnumerable<DevicePin> digital)
    {
        Dictionary<int, int> ports = new();
        foreach (var devicePin in digital)
        {
            var port = devicePin.Pin / 8;
            var mask = 1 << (devicePin.Pin % 8);
            mask |= ports.GetValueOrDefault(port, 0);
            ports[port] = mask;
        }

        return ports;
    }
}