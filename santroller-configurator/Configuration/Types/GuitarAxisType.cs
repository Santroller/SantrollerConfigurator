using System;
using System.Collections.Generic;
using System.Linq;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum GuitarAxisType
{
    Whammy,
    Pickup,
    Slider,
    Tilt
}

public static class GuitarAxisTypeMethods
{
    public static IEnumerable<GuitarAxisType> RbTypes()
    {
        return new[]
        {
            GuitarAxisType.Pickup, GuitarAxisType.Tilt, GuitarAxisType.Whammy
        };
    }
    public static IEnumerable<GuitarAxisType> GhlTypes()
    {
        return new[]
        {
            GuitarAxisType.Tilt, GuitarAxisType.Whammy
        };
    }
    public static IEnumerable<GuitarAxisType> GuitarPraiseTypes()
    {
        return new[]
        {
            GuitarAxisType.Whammy
        };
    }

    public static IEnumerable<GuitarAxisType> GhTypes()
    {
        return new[]
        {
            GuitarAxisType.Slider, GuitarAxisType.Tilt, GuitarAxisType.Whammy
        };
    }

    public static IEnumerable<GuitarAxisType> GetTypeFor(DeviceControllerType deviceControllerType)
    {
        switch (deviceControllerType)
        {
            case DeviceControllerType.GuitarPraiseGuitar:
                return GuitarPraiseTypes();
            case DeviceControllerType.LiveGuitar:
                return GhlTypes();
        }

        if (deviceControllerType.IsFortnite())
        {
            return Array.Empty<GuitarAxisType>();
        }
        return deviceControllerType.IsGh()
            ? GhTypes()
            : RbTypes();
    }

    public static IEnumerable<GuitarAxisType> GetDifferenceFor(DeviceControllerType deviceControllerType)
    {
        return Enum.GetValues<GuitarAxisType>()
            .Except(GetTypeFor(deviceControllerType));
    }
}