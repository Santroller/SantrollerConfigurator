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

    public static IEnumerable<GuitarAxisType> GhTypes()
    {
        return new[]
        {
            GuitarAxisType.Slider, GuitarAxisType.Tilt, GuitarAxisType.Whammy
        };
    }

    public static IEnumerable<GuitarAxisType> GetTypeFor(DeviceControllerType deviceControllerType)
    {
        if (deviceControllerType == DeviceControllerType.LiveGuitar)
        {
            return GhlTypes();
        }
        return deviceControllerType.IsGh()
            ? GhTypes()
            : RbTypes();
    }

    public static IEnumerable<GuitarAxisType> GetInvalidTypesFor(DeviceControllerType deviceControllerType)
    {
        if (deviceControllerType == DeviceControllerType.LiveGuitar)
        {
            return RbTypes().Concat(GhTypes());
        }
        return deviceControllerType.IsGh()
            ? RbTypes()
            : GhTypes();
    }

    public static IEnumerable<GuitarAxisType> GetDifferenceFor(DeviceControllerType deviceControllerType)
    {
        return GetInvalidTypesFor(deviceControllerType)
            .Except(GetTypeFor(deviceControllerType));
    }

    public static IEnumerable<GuitarAxisType> GetDifferenceInverseFor(DeviceControllerType deviceControllerType)
    {
        return GetTypeFor(deviceControllerType)
            .Except(GetInvalidTypesFor(deviceControllerType));
    }
}