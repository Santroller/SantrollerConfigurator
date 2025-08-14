using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public class AvrUartConfig : UartConfig
{
    public AvrUartConfig(ConfigViewModel model, string type, bool peripheral, int tx, int rx, uint clock, bool output) : base(model, type, peripheral, tx, rx,
        clock, output)
    {
    }

    public override string Definition => Peripheral ? "SLAVE_GC_UART" : "GC_UART";
    protected override bool Reassignable => false;
}