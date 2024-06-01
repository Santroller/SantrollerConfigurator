using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public class AvrTwiConfig : TwiConfig
{
    public AvrTwiConfig(ConfigViewModel model, string type, bool peripheral, int sda, int scl, int clock, bool output) : base(model, type, peripheral, sda, scl,
        clock, output)
    {
    }

    public override string Definition => Peripheral ? "SLAVE_GC_TWI" : "GC_TWI";
    protected override bool Reassignable => false;
}