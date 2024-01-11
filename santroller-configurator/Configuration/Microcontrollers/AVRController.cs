using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public abstract class AvrController : Microcontroller
{
    public enum AvrPinMode
    {
        Input,
        InputPullup,
        Output
    }

    public override bool TwiAssignable => false;
    public override bool SpiAssignable => false;

    protected abstract int PinA0 { get; }
    protected abstract int SpiMiso { get; }

    protected abstract int SpiMosi { get; }
    protected abstract int SpiSck { get; }
    protected abstract int SpiCSn { get; }
    protected abstract int I2CSda { get; }
    protected abstract int I2CScl { get; }

    public abstract int PinCount { get; }

    protected abstract char[] PortNames { get; }
    protected abstract Dictionary<(char, int), int> PinByMask { get; }

    public override string GenerateDigitalRead(int pin, bool invert, bool peripheral)
    {
        // Invert on pullup
        return invert ? $"(PIN{GetPort(pin)} & (1 << {GetIndex(pin)})) == 0" : $"PIN{GetPort(pin)} & ({1 << GetIndex(pin)})";
    }

    public override string GenerateDigitalWrite(int pin, bool val, bool peripheral)
    {
        return val ? $"PORT{GetPort(pin)} |= {1 << GetIndex(pin)}" : $"PORT{GetPort(pin)} &= ~{(1 << GetIndex(pin))}";
    }

    public abstract int GetIndex(int pin);
    public abstract char GetPort(int pin);

    public abstract AvrPinMode? ForcedMode(int pin);


    public override SpiConfig AssignSpiPins(ConfigViewModel model, string type,bool peripheral,  bool includesMiso, int mosi, int miso, int sck, bool cpol,
        bool cpha,
        bool msbfirst,
        uint clock)
    {
        return new AvrSpiConfig(model, type, peripheral, includesMiso, SpiMosi, SpiMiso, SpiSck, SpiCSn, cpol, cpha, msbfirst, clock);
    }

    public override string GenerateAnalogWrite(int pin, string val, bool peripheral)
    {
        return $"analogWrite({pin}, {val})";
    }

    public override TwiConfig AssignTwiPins(ConfigViewModel model, string type,bool peripheral,  int sda, int scl, int clock)
    {
        return new AvrTwiConfig(model, type, peripheral, I2CSda, I2CScl, clock);
    }

    public override List<KeyValuePair<int, SpiPinType>> SpiPins()
    {
        return new List<KeyValuePair<int, SpiPinType>>
        {
            new(SpiCSn, SpiPinType.CSn),
            new(SpiMiso, SpiPinType.Miso),
            new(SpiMosi, SpiPinType.Mosi),
            new(SpiSck, SpiPinType.Sck)
        };
    }

    public override List<KeyValuePair<int, TwiPinType>> TwiPins()
    {
        return new List<KeyValuePair<int, TwiPinType>>
        {
            new(I2CScl, TwiPinType.Scl),
            new(I2CSda, TwiPinType.Sda)
        };
    }

    public override void PinsFromPortMask(int port, int mask, byte pins,
        Dictionary<int, bool> digitalRaw)
    {
        var portChar = PortNames[port];
        for (var i = 0; i < 8; i++)
        {
            if ((mask & (1 << i)) == 0) continue;
            digitalRaw[PinByMask[(portChar, i)]] = (pins & (1 << i)) != 0;
        }
    }

    public override int GetAnalogMask(DevicePin devicePin)
    {
        return 1 << GetIndex(devicePin.Pin);
    }

    public override Dictionary<int, int> GetPortsForTicking(IEnumerable<DevicePin> digital)
    {
        var maskByPort = new Dictionary<int, int>();
        foreach (var devicePin in digital)
        {
            var port = PortNames.IndexOf(GetPort(devicePin.Pin));
            var mask = 1 << GetIndex(devicePin.Pin);
            maskByPort[port] = mask | maskByPort.GetValueOrDefault(port, 0);
        }

        return maskByPort;
    }

    public override string GenerateInit(ConfigViewModel configViewModel)
    {
        // DDRx 1 = output, 0 = input
        // PORTx input 1= pullup, 0 = floating
        var ddrByPort = new Dictionary<char, int>();
        var portByPort = new Dictionary<char, int>();
        var pins = configViewModel.GetPinConfigs().OfType<DirectPinConfig>().ToList();
        var write = "";
        if (configViewModel.GetPinConfigs().OfType<SpiConfig>().Any())
        {
            pins.Add(new DirectPinConfig(configViewModel, "mosi", SpiMosi, false, DevicePinMode.Output));
            pins.Add(new DirectPinConfig(configViewModel, "miso", SpiMiso, false, DevicePinMode.PullUp));
            pins.Add(new DirectPinConfig(configViewModel, "sck", SpiSck, false, DevicePinMode.Output));
            pins.Add(new DirectPinConfig(configViewModel, "ss", SpiCSn, false, DevicePinMode.Output));
            write = GenerateDigitalWrite(SpiCSn, true, false);
        }
        foreach (var pin in pins)
        {
            if (pin.PinMode == DevicePinMode.Skip || pin.Peripheral || pin.Type == "led_output") continue;
            var port = GetPort(pin.Pin);
            var idx = GetIndex(pin.Pin);
            var currentPort = portByPort.GetValueOrDefault(port, 0);
            var currentDdr = ddrByPort.GetValueOrDefault(port, 0);
            switch (pin.PinMode)
            {
                case DevicePinMode.Output:
                    currentDdr |= 1 << idx;
                    break;
                case DevicePinMode.PullUp:
                    currentPort |= 1 << idx;
                    break;
            }

            portByPort[port] = currentPort;
            ddrByPort[port] = currentDdr;
        }

        for (var i = 0; i < PinCount; i++)
        {
            var force = ForcedMode(i);
            var port = GetPort(i);
            var idx = GetIndex(i);

            if (ForcedMode(i) is null) continue;
            var currentPort = portByPort.GetValueOrDefault(port, 0);
            var currentDdr = ddrByPort.GetValueOrDefault(port, 0);
            switch (force)
            {
                case AvrPinMode.InputPullup:
                    currentPort |= 1 << idx;
                    break;
                case AvrPinMode.Output:
                    currentDdr |= 1 << idx;
                    break;
            }
            portByPort[port] = currentPort;
            ddrByPort[port] = currentDdr;
        }

        return $"""
               uint8_t oldSREG = SREG;
               cli();
               {write};
               {string.Join("\n", ddrByPort.Select(port => $"DDR{port.Key} = {port.Value};"))}
               {string.Join("\n", portByPort.Select(port => $"PORT{port.Key} = {port.Value};"))}
               SREG = oldSREG;
               """;
    }
    
    public override string GenerateLedInit(ConfigViewModel configViewModel)
    {
        // DDRx 1 = output, 0 = input
        // PORTx input 1= pullup, 0 = floating
        var ddrByPort = new Dictionary<char, int>();
        var portByPort = new Dictionary<char, int>();
        var pins = configViewModel.GetPinConfigs().OfType<DirectPinConfig>().ToList();
        var write = "";
        if (configViewModel.GetPinConfigs().OfType<SpiConfig>().Any())
        {
            pins.Add(new DirectPinConfig(configViewModel, "mosi", SpiMosi, false, DevicePinMode.Output));
            pins.Add(new DirectPinConfig(configViewModel, "miso", SpiMiso, false, DevicePinMode.PullUp));
            pins.Add(new DirectPinConfig(configViewModel, "sck", SpiSck, false, DevicePinMode.Output));
            pins.Add(new DirectPinConfig(configViewModel, "ss", SpiCSn, false, DevicePinMode.Output));
            write = GenerateDigitalWrite(SpiCSn, true, false);
        }
        foreach (var pin in pins)
        {
            if (pin.PinMode == DevicePinMode.Skip || pin.Peripheral)
            {
                continue;
            }
            var port = GetPort(pin.Pin);
            var idx = GetIndex(pin.Pin);
            var currentPort = portByPort.GetValueOrDefault(port, 0);
            var currentDdr = ddrByPort.GetValueOrDefault(port, 0);
            switch (pin.PinMode)
            {
                case DevicePinMode.Output:
                    currentDdr |= 1 << idx;
                    break;
                case DevicePinMode.PullUp:
                    currentPort |= 1 << idx;
                    break;
            }

            portByPort[port] = currentPort;
            ddrByPort[port] = currentDdr;
        }

        for (var i = 0; i < PinCount; i++)
        {
            var force = ForcedMode(i);
            var port = GetPort(i);
            var idx = GetIndex(i);

            if (ForcedMode(i) is null) continue;
            var currentPort = portByPort.GetValueOrDefault(port, 0);
            var currentDdr = ddrByPort.GetValueOrDefault(port, 0);
            switch (force)
            {
                case AvrPinMode.InputPullup:
                    currentPort |= 1 << idx;
                    break;
                case AvrPinMode.Output:
                    currentDdr |= 1 << idx;
                    break;
            }

            portByPort[port] = currentPort;
            ddrByPort[port] = currentDdr;
        }

        return $"""
               uint8_t oldSREG = SREG;
               cli();
               {write};
               {string.Join("\n", ddrByPort.Select(port => $"DDR{port.Key} = {port.Value};"))}
               {string.Join("\n", portByPort.Select(port => $"PORT{port.Key} = {port.Value};"))}
               SREG = oldSREG;
               """;
    }

    public override string GenerateAnalogRead(int pin, ConfigViewModel model, bool peripheral)
    {
        var pins = model.GetPinConfigs().OfType<DirectPinConfig>().Where(config => config.PinMode is DevicePinMode.Analog)
            .Select(s => s.Pin).Distinct().Order();
        return $"adc({pins.IndexOf(pin)})";
    }

    protected virtual string? AnalogName(int pin)
    {
        return pin < PinA0 ? null : $" / A{pin - PinA0}";
    }

    public override string GetPinForMicrocontroller(int pin, bool spi, bool twi)
    {
        var ret = $"D{pin}";
        var analogName = AnalogName(pin);
        if (analogName != null) ret += analogName;

        if (pin == SpiCSn) ret += " / SPI CS";

        if (pin == SpiMiso) ret += " / SPI MISO";

        if (pin == SpiMosi) ret += " / SPI MOSI";

        if (pin == SpiSck) ret += " / SPI CLK";

        if (pin == I2CScl) ret += " / I2C SCL";

        if (pin == I2CSda) ret += " / I2C SDA";

        return ret;
    }

    public override IEnumerable<string> GenerateAckDefines(int ack)
    {
        var interrupt = GetInterruptForPin(ack);
        return new List<string> {$"INTERRUPT_PS2_ACK INT{interrupt}", $"INTERRUPT_PS2_ACK_VECT INT{interrupt}_vect", $"INTERRUPT_PS2_ACK_EICRA _BV(ISC{interrupt}0) | _BV(ISC{interrupt}1)"};
    }

    protected abstract int GetInterruptForPin(int ack);

    public override int GetFirstAnalogPin()
    {
        return PinA0;
    }
}
