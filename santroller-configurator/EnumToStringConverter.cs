using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace GuitarConfigurator.NetCore;

public class EnumToStringConverter : IValueConverter
{
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null ? null : Convert(value);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public static string Convert(object value)
    {
        return Resources.ResourceManager.GetString(value.GetType().Name + value) ?? "";
    }
}