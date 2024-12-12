using System.Linq;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public class PicoTwiConfig : TwiConfig
{
    public PicoTwiConfig(ConfigViewModel model, string type,bool peripheral,  int sda, int scl, int clock, bool output) : base(model, type, peripheral, sda, scl,
        clock, output)
    {
    }

    public int Index => Sda == -1 ? 0 : Pico.TwiIndexByPin[Sda];
    public override string Definition => Peripheral ? $"TWI_SLAVE_{Index}": $"TWI_{Index}";
    protected override bool Reassignable => true;

    protected override string? CalculateError()
    {
        var ret = base.CalculateError();
        if (ret != null) return ret;
        if (Pico.TwiIndexByPin[Sda] != Pico.TwiIndexByPin[Scl])
        {
            return Resources.DifferentI2CGroup;
        }
        var ret2 = Model.Bindings.Items
            .Where(output => output.GetPinConfigs().OfType<PicoTwiConfig>().Any(s => s != this && s.Index == Index && !(s.Sda == Sda && s.Scl == Scl) && s.Peripheral == Peripheral))
            .Select(output => string.Format(Resources.I2CGroup, output.LocalisedName, Index))
            .ToList();
        return ret2.Count != 0 ? string.Join(", ", ret2) : null;
    }
}