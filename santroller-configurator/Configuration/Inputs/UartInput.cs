using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public abstract class UartInput : Input, IUart
{
    private readonly UartConfig _uartConfig;

    private readonly string _uartType;

    protected UartInput(string spiType, bool peripheral, int tx, int rx, uint clock, ConfigViewModel model) : base(model)
    {
        _uartType = spiType;
        var config = Model.GetUartForType(_uartType, peripheral);
        _uartConfig = config ?? Model.Microcontroller.AssignUartPins(model, _uartType, peripheral, tx, rx, clock, false);

        this.WhenAnyValue(x => x._uartConfig.Tx).Subscribe(_ => this.RaisePropertyChanged(nameof(Tx)));
        this.WhenAnyValue(x => x._uartConfig.Rx).Subscribe(_ => this.RaisePropertyChanged(nameof(Rx)));
    }

    public int Tx
    {
        get => _uartConfig.Tx;
        set => _uartConfig.Tx = value;
    }

    public int Rx
    {
        get => _uartConfig.Rx;
        set => _uartConfig.Rx = value;
    }

    public override bool Peripheral => _uartConfig.Peripheral;


    public List<int> AvailableTxPins => GetTxPins();
    public List<int> AvailableRxPins => GetRxPins();

    public override IList<PinConfig> PinConfigs => new List<PinConfig> {_uartConfig};

    public List<int> UartPins()
    {
        return [Tx, Rx];
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return [$"{_uartType.ToUpper()}_UART_PORT {_uartConfig.Definition}"];
    }

    private List<int> GetTxPins()
    {
        return Model.Microcontroller.UartPins(false)
            .Where(s => s.Value is UartPinType.Tx)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetRxPins()
    {
        return Model.Microcontroller.UartPins(false)
            .Where(s => s.Value is UartPinType.Rx)
            .Select(s => s.Key).ToList();
    }
}