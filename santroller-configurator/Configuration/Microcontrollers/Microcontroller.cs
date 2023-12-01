using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public abstract class Microcontroller
{
    public abstract Board Board { get; }

    public abstract List<int> AnalogPins { get; }

    public abstract bool TwiAssignable { get; }
    public abstract bool SpiAssignable { get; }
    public abstract List<int> PwmPins { get; }
    public abstract string GenerateDigitalRead(int pin, bool invert, bool peripheral);

    public abstract string GenerateDigitalWrite(int pin, bool val, bool peripheral);

    public abstract string GenerateAnalogWrite(int pin, string val, bool peripheral);

    public abstract int GetChannel(int pin, bool reconfigurePin);

    public abstract string GenerateInit(ConfigViewModel configViewModel);

    public string GetPin(int possiblePin, bool peripheral, int selectedPin, IEnumerable<Output> outputs, bool twi,
        bool spi,
        IEnumerable<PinConfig> pinConfigs, ConfigViewModel model, bool addText)
    {
        var selectedConfig = pinConfigs.Where(s => s.Peripheral == peripheral && s.Pins.Contains(selectedPin));
        var apa102 = model.PinConfigs
            .Where(s => s.Type == ConfigViewModel.Apa102SpiType && s.Peripheral == peripheral &&
                        s.Pins.Contains(possiblePin))
            .Select(s => model.LedSpiType(peripheral)).Concat(model.PinConfigs
                .Where(s => s.Type.Contains("STP16CPC26") && s.Peripheral == peripheral &&
                            s.Pins.Contains(possiblePin))
                .Select(s => s.Type)).ToList();
        var unoMega = model.PinConfigs.Where(s =>
                s.Peripheral == peripheral &&
                (s.Type == ConfigViewModel.UnoPinTypeRx || s.Type == ConfigViewModel.UnoPinTypeTx) &&
                s.Pins.Contains(possiblePin))
            .Select(s => s.Type);

        var output = string.Join(" - ",
            outputs.Where(o =>
                    o.GetPinConfigs().Except(selectedConfig)
                        .Any(s => s.Peripheral == peripheral && s.Pins.Contains(possiblePin)))
                .Select(s => s.GetName(model.DeviceControllerType, model.LegendType, model.SwapSwitchFaceButtons))
                .Concat(apa102).Concat(unoMega));
        var ret = GetPinForMicrocontroller(possiblePin, twi, spi);
        if (!string.IsNullOrEmpty(output) && addText) return "* " + ret + " - " + output;

        return ret;
    }

    public abstract SpiConfig AssignSpiPins(ConfigViewModel model, string type, bool peripheral, bool includesMiso,
        int mosi, int miso,
        int sck, bool cpol,
        bool cpha,
        bool msbfirst,
        uint clock);

    public abstract TwiConfig AssignTwiPins(ConfigViewModel model, string type, bool peripheral, int sda, int scl,
        int clock);

    public abstract string GetPinForMicrocontroller(int pin, bool twi, bool spi);

    public abstract IEnumerable<string> GenerateAckDefines(int ack);

    public abstract List<int> SupportedAckPins();

    public abstract List<KeyValuePair<int, SpiPinType>> SpiPins();
    public abstract List<KeyValuePair<int, TwiPinType>> TwiPins();

    public abstract string GenerateAnalogRead(int pin, ConfigViewModel model, bool peripheral);

    public abstract int GetFirstAnalogPin();
    public abstract List<int> GetAllPins();
    public abstract bool FilterPin(bool isAnalog, bool isBluetooth, bool isInterrupt, int pin);

    public abstract Dictionary<int, int> GetPortsForTicking(IEnumerable<DevicePin> digital);

    public abstract void PinsFromPortMask(int port, int mask, byte pins,
        Dictionary<int, bool> digitalRaw);

    public abstract int GetAnalogMask(DevicePin devicePin);

    public abstract int GetFirstDigitalPin();
}