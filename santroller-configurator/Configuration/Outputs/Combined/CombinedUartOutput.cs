using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public abstract class CombinedUartOutput : CombinedOutput, IUart
{
    protected readonly UartConfig UartConfig;

    protected CombinedUartOutput(ConfigViewModel model, string uartType, bool peripheral,  uint uartFreq,
        int tx = -1, int rx = -1) : base(model)
    {
        UartType = uartType;
        BindableUart = Model.Microcontroller.UartAssignable && !model.Branded;
        var config = Model.GetUartForType(UartType, peripheral);
        UartConfig = config ?? Model.Microcontroller.AssignUartPins(model, UartType, peripheral, tx,  rx, uartFreq, false);

        this.WhenAnyValue(x => x.UartConfig.Tx).Subscribe(_ => this.RaisePropertyChanged(nameof(Tx)));
        this.WhenAnyValue(x => x.UartConfig.Rx).Subscribe(_ => this.RaisePropertyChanged(nameof(Rx)));
    }

    public bool BindableUart { get; }

    private string UartType { get; }

    public bool Peripheral => UartConfig.Peripheral;

    public int Tx
    {
        get => UartConfig.Tx;
        set => UartConfig.Tx = value;
    }

    public int Rx
    {
        get => UartConfig.Rx;
        set => UartConfig.Rx = value;
    }

    public List<int> AvailableTxPins => GetTxPins();
    public List<int> AvailableRxPins => GetRxPins();

    public List<int> SpiPins()
    {
        return [Tx, Rx];
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

    protected override IEnumerable<PinConfig> GetOwnPinConfigs()
    {
        return [UartConfig];
    }
}