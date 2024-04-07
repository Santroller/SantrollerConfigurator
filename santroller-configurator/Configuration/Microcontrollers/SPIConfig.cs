using System.Collections.Generic;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public abstract class SpiConfig : PinConfig
{
    private readonly uint _clock;
    private readonly bool _cpha;
    private readonly bool _cpol;
    private readonly bool _msbfirst;

    private int _miso;

    private int _mosi;

    private int _sck;

    protected SpiConfig(ConfigViewModel model, string type, bool peripheral, bool includesSck, bool includesMiso, int mosi, int miso, int sck, bool cpol, bool cpha,
        bool msbfirst, uint clock) : base(model, peripheral)
    {
        IncludesMiso = includesMiso;
        IncludesSck = includesSck;
        Type = type;
        _mosi = mosi;
        _miso = miso;
        _sck = sck;
        _cpol = cpol;
        _cpha = cpha;
        _msbfirst = msbfirst;
        _clock = clock;
    }

    public override string Type { get; }
    
    public bool IncludesMiso { get; }
    public bool IncludesSck { get; }

    public int Mosi
    {
        get => _mosi;
        set
        {
            if (!Reassignable) return;
            this.RaiseAndSetIfChanged(ref _mosi, value);
            Update();
        }
    }

    public int Miso
    {
        get => _miso;
        set
        {
            if (!Reassignable || !IncludesMiso) return;
            this.RaiseAndSetIfChanged(ref _miso, value);
            Update();
        }
    }

    public int Sck
    {
        get => _sck;
        set
        {
            if (!Reassignable || !IncludesSck) return;
            this.RaiseAndSetIfChanged(ref _sck, value);
            Update();
        }
    }

    public override IEnumerable<int> Pins
    {
        get
        {
            var pins = new List<int>();
            pins.Add(_mosi);
            if (IncludesMiso)
            {
                pins.Add(_miso);
            }
            if (IncludesSck)
            {
                pins.Add(_sck);
            }

            return pins;
        }
    }

    public override string Generate()
    {
        // On apa102, miso isn't used.
        var miso = IncludesMiso ? $"#define {Definition}_MISO {_miso}" : "";
        // On ws2812, sck isn't used.
        var sck = IncludesSck ? $"#define {Definition}_SCK {_sck}" : "";
        return $"""

                {miso}
                {sck}
                #define {Definition}_MOSI {_mosi}
                #define {Definition}_CPOL {(_cpol ? 1 : 0)}
                #define {Definition}_CPHA {(_cpha ? 1 : 0)}
                #define {Definition}_MSBFIRST {(_msbfirst ? 1 : 0)}
                #define {Definition}_CLOCK {_clock}
                """;
    }
}
