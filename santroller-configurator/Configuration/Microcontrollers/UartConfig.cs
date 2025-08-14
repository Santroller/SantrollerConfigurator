using System;
using System.Collections.Generic;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public abstract class UartConfig : PinConfig
{
    private readonly uint _clock;

    private int _tx;

    private int _rx;

    protected UartConfig(ConfigViewModel model, string type, bool peripheral, int tx, int rx, uint clock, bool output) : base(model, peripheral)
    {
        Type = type;
        _tx = tx;
        _rx = rx;
        _clock = clock;
    }

    public override string Type { get; }

    public int Tx
    {
        get => _tx;
        set
        {
            if (!Reassignable) return;
            this.RaiseAndSetIfChanged(ref _tx, value);
            Update();
        }
    }

    public int Rx
    {
        get => _rx;
        set
        {
            if (!Reassignable) return;
            this.RaiseAndSetIfChanged(ref _rx, value);
            Update();
        }
    }

    public override IEnumerable<int> Pins => [_tx, _rx];

    public override string Generate()
    {
        return $"""

                #define {Definition}_TX {_tx}
                #define {Definition}_RX {_rx}
                #define {Definition}_CLOCK {_clock}
                """;
    }
}