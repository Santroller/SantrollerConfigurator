using System;
using System.Globalization;
using Avalonia.Data.Converters;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;

namespace GuitarConfigurator.NetCore;

public class PinToStringConverterPico : IValueConverter
{

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int pin) return null;
        return Pico.GetPinForPico(pin, true, false);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}