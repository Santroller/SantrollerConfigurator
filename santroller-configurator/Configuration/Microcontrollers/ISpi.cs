using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public interface ISpi: IInput;

public static class SpiMethods
{
    public static List<int> GetMosiPins(this ISpi iSpi)
    {
        return iSpi.Model.Microcontroller.SpiPins(false)
            .Where(s => s.Value is SpiPinType.Mosi)
            .Select(s => s.Key).ToList();
    }

    public static List<int> GetMisoPins(this ISpi iSpi)
    {
        return iSpi.Model.Microcontroller.SpiPins(false)
            .Where(s => s.Value is SpiPinType.Miso)
            .Select(s => s.Key).ToList();
    }

    public static List<int> GetSckPins(this ISpi iSpi)
    {
        return iSpi.Model.Microcontroller.SpiPins(false)
            .Where(s => s.Value is SpiPinType.Sck)
            .Select(s => s.Key).ToList();
    }
}