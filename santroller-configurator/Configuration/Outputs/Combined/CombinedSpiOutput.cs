using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public abstract class CombinedSpiOutput : CombinedOutput, ISpi
{
    protected readonly SpiConfig SpiConfig;

    protected CombinedSpiOutput(ConfigViewModel model, string spiType, bool peripheral,  uint spiFreq,
        bool cpol,
        bool cpha, bool msbFirst, string name, int miso = -1, int mosi = -1, int sck = -1) : base(model)
    {
        SpiType = spiType;
        BindableSpi = Model.Microcontroller.SpiAssignable && !model.Branded;
        BindableAtt = Model.Microcontroller is not (Uno or Mega) && !model.Branded;
        var config = Model.GetSpiForType(SpiType, peripheral);
        SpiConfig = config ?? Model.Microcontroller.AssignSpiPins(model, SpiType, peripheral, true,  true, mosi, miso, sck, cpol, cpha,
            msbFirst, spiFreq, false);

        this.WhenAnyValue(x => x.SpiConfig.Miso).Subscribe(_ => this.RaisePropertyChanged(nameof(Miso)));
        this.WhenAnyValue(x => x.SpiConfig.Mosi).Subscribe(_ => this.RaisePropertyChanged(nameof(Mosi)));
        this.WhenAnyValue(x => x.SpiConfig.Sck).Subscribe(_ => this.RaisePropertyChanged(nameof(Sck)));
    }

    public bool BindableSpi { get; }
    public bool BindableAtt { get; }

    private string SpiType { get; }

    public bool Peripheral => SpiConfig.Peripheral;

    public int Mosi
    {
        get => SpiConfig.Mosi;
        set => SpiConfig.Mosi = value;
    }

    public int Miso
    {
        get => SpiConfig.Miso;
        set => SpiConfig.Miso = value;
    }

    public int Sck
    {
        get => SpiConfig.Sck;
        set => SpiConfig.Sck = value;
    }

    public List<int> AvailableMosiPins => GetMosiPins();
    public List<int> AvailableMisoPins => GetMisoPins();
    public List<int> AvailableSckPins => GetSckPins();

    public List<int> SpiPins()
    {
        return [Mosi, Miso, Sck];
    }

    private List<int> GetMosiPins()
    {
        return Model.Microcontroller.SpiPins(false)
            .Where(s => s.Value is SpiPinType.Mosi)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetMisoPins()
    {
        return Model.Microcontroller.SpiPins(false)
            .Where(s => s.Value is SpiPinType.Miso)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetSckPins()
    {
        return Model.Microcontroller.SpiPins(false)
            .Where(s => s.Value is SpiPinType.Sck)
            .Select(s => s.Key).ToList();
    }

    protected override IEnumerable<PinConfig> GetOwnPinConfigs()
    {
        return [SpiConfig];
    }
}