using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace GuitarConfigurator.NetCore;

public class SidebarHeightConverter : IValueConverter
{
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double number) return null;
        return number > 600;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}