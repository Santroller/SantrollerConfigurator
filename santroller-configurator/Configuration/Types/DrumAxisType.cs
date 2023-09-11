using System.Collections.Generic;
using System.Linq;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum DrumAxisType
{
    Green,
    Red,
    Yellow,
    Blue,
    Orange,
    GreenCymbal,
    YellowCymbal,
    BlueCymbal,
    Kick,
    Kick2
}

public static class DrumAxisTypeMethods
{
    public static IEnumerable<DrumAxisType> RbTypes()
    {
        return new[]
        {
            DrumAxisType.Green, DrumAxisType.Red, DrumAxisType.Yellow, DrumAxisType.Blue, DrumAxisType.GreenCymbal,
            DrumAxisType.YellowCymbal, DrumAxisType.BlueCymbal, DrumAxisType.Kick, DrumAxisType.Kick2
        };
    }

    public static IEnumerable<DrumAxisType> GhTypes()
    {
        return new[]
        {
            DrumAxisType.Green,
            DrumAxisType.Red,
            DrumAxisType.Yellow,
            DrumAxisType.Blue,
            DrumAxisType.Orange,
            DrumAxisType.Kick,
            DrumAxisType.Kick2
        };
    }

    public static IEnumerable<DrumAxisType> GetTypeFor(DeviceControllerType type)
    {
        return type.IsGh() ? GhTypes() : RbTypes();
    }

    public static IEnumerable<DrumAxisType> GetInvalidTypesFor(DeviceControllerType type)
    {
        return type.IsGh() ? RbTypes() : GhTypes();
    }

    public static IEnumerable<DrumAxisType> GetDifferenceFor(DeviceControllerType type)
    {
        return GetInvalidTypesFor(type).Except(GetTypeFor(type));
    }

    public static IEnumerable<DrumAxisType> GetDifferenceInverseFor(DeviceControllerType type)
    {
        return GetTypeFor(type).Except(GetInvalidTypesFor(type));
    }
}