using System.Linq;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public class PicoUartConfig : UartConfig
{
    public PicoUartConfig(ConfigViewModel model, string type,bool peripheral,  int tx, int rx, uint clock, bool output) :
        base(model, type, peripheral, tx, rx, clock, output)
    {
    }

    public int Index => Pico.UartIndexByPin[Tx];
    protected override bool Reassignable => true;
    public override string Definition => Peripheral ? $"UART_SLAVE_{Index}": $"UART_{Index}";

    protected override string? CalculateError()
    {
        var ret = base.CalculateError();
        if (ret != null) return ret;
        if (Pico.UartIndexByPin[Tx] != Pico.UartIndexByPin[Rx])
        {
            return Resources.DifferentSPIGroup;
        }
        var ret2 = Model.Bindings.Items
            .Where(output => output.GetPinConfigs().OfType<PicoUartConfig>().Any(s => s != this && s.Index == Index && s.Peripheral == Peripheral))
            .Select(output => string.Format(Resources.UARTGroup, output.LocalisedName, Index)).ToList();
        return ret2.Count != 0 ? string.Join(", ", ret2) : null;
    }
}