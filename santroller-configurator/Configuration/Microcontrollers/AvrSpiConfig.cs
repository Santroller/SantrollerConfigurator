using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public class AvrSpiConfig : SpiConfig
{
    private readonly int _ss;

    public AvrSpiConfig(ConfigViewModel model, string type, bool peripheral, bool includesSck, bool includesMiso, int mosi, int miso,
        int sck, int ss, bool cpol, bool cpha,
        bool msbfirst, uint clock, bool output) : base(model, type, peripheral, includesSck, includesMiso, mosi, miso, sck, cpol, cpha, msbfirst,
        clock, output)
    {
        _ss = ss;
    }

    public override string Definition => Peripheral ? "SLAVE_GC_SPI" : "GC_SPI";
    protected override bool Reassignable => false;
    public override IEnumerable<int> Pins => base.Pins.Concat(new List<int> {_ss}).ToList();
}