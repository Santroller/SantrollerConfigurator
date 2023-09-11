using System;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public class DevicePin
{
    public DevicePin(int pin, DevicePinMode pinMode)
    {
        Pin = pin;
        PinMode = pinMode;
    }

    public int Pin { get; }
    public DevicePinMode PinMode { get; }

    private bool Equals(DevicePin other)
    {
        return Pin == other.Pin && PinMode == other.PinMode;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((DevicePin) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Pin, (int) PinMode);
    }
}