using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public abstract class SpiInput : Input, ISpi
{
    private readonly SpiConfig _spiConfig;

    private readonly string _spiType;

    protected SpiInput(string spiType, bool peripheral,  uint spiFreq, bool cpol,
        bool cpha, bool msbFirst, ConfigViewModel model, int miso = -1, int mosi = -1,
        int sck = -1) : base(model)
    {
        _spiType = spiType;
        var config = Model.GetSpiForType(_spiType, peripheral);
        _spiConfig = config ?? Model.Microcontroller.AssignSpiPins(model, _spiType, peripheral, true, mosi, miso, sck, cpol, cpha,
            msbFirst, spiFreq);

        this.WhenAnyValue(x => x._spiConfig.Miso).Subscribe(_ => this.RaisePropertyChanged(nameof(Miso)));
        this.WhenAnyValue(x => x._spiConfig.Mosi).Subscribe(_ => this.RaisePropertyChanged(nameof(Mosi)));
        this.WhenAnyValue(x => x._spiConfig.Sck).Subscribe(_ => this.RaisePropertyChanged(nameof(Sck)));
    }

    public int Mosi
    {
        get => _spiConfig.Mosi;
        set => _spiConfig.Mosi = value;
    }

    public int Miso
    {
        get => _spiConfig.Miso;
        set => _spiConfig.Miso = value;
    }

    public override bool Peripheral => _spiConfig.Peripheral;

    public int Sck
    {
        get => _spiConfig.Sck;
        set => _spiConfig.Sck = value;
    }

    public List<int> AvailableMosiPins => GetMosiPins();
    public List<int> AvailableMisoPins => GetMisoPins();
    public List<int> AvailableSckPins => GetSckPins();

    public override IList<PinConfig> PinConfigs => new List<PinConfig> {_spiConfig};

    public List<int> SpiPins()
    {
        return new List<int> {Mosi, Miso, Sck};
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[] {$"{_spiType.ToUpper()}_SPI_PORT {_spiConfig.Definition}"};
    }

    private List<int> GetMosiPins()
    {
        return Model.Microcontroller.SpiPins()
            .Where(s => s.Value is SpiPinType.Mosi)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetMisoPins()
    {
        return Model.Microcontroller.SpiPins()
            .Where(s => s.Value is SpiPinType.Miso)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetSckPins()
    {
        return Model.Microcontroller.SpiPins()
            .Where(s => s.Value is SpiPinType.Sck)
            .Select(s => s.Key).ToList();
    }
}