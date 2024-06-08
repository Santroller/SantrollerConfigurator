using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public abstract class TwiInput : Input, ITwi
{
    private readonly TwiConfig _twiConfig;

    private readonly string _twiType;

    protected TwiInput(string twiType, int twiFreq, bool peripheral, int sda, int scl,
        ConfigViewModel model) : base(model)
    {
        _twiType = twiType;
        var config = Model.GetTwiForType(_twiType, peripheral);
        _twiConfig = config ?? Model.Microcontroller.AssignTwiPins(model, _twiType, peripheral, sda, scl, twiFreq, false);


        this.WhenAnyValue(x => x._twiConfig.Scl).Subscribe(_ => this.RaisePropertyChanged(nameof(Scl)));
        this.WhenAnyValue(x => x._twiConfig.Sda).Subscribe(_ => this.RaisePropertyChanged(nameof(Sda)));
    }

    public int Sda
    {
        get => _twiConfig.Sda;
        set => _twiConfig.Sda = value;
    }

    public int Scl
    {
        get => _twiConfig.Scl;
        set => _twiConfig.Scl = value;
    }

    public override bool Peripheral => _twiConfig.Peripheral;


    public List<int> AvailableSdaPins => GetSdaPins();
    public List<int> AvailableSclPins => GetSclPins();
    public override IList<PinConfig> PinConfigs => new List<PinConfig> {_twiConfig};

    public List<int> TwiPins()
    {
        return [Sda, Scl];
    }

    private List<int> GetSdaPins()
    {
        return Model.Microcontroller.TwiPins(false)
            .Where(s => s.Value is TwiPinType.Sda)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetSclPins()
    {
        return Model.Microcontroller.TwiPins(false)
            .Where(s => s.Value is TwiPinType.Scl)
            .Select(s => s.Key).ToList();
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[] {$"{_twiType.ToUpper()}_TWI_PORT {_twiConfig.Definition}"};
    }
}