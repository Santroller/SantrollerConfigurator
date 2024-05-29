using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Input;

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
        // Some keys have multiple names, so we can't rely on the enum name for them.
        return value switch
        {
            Key.Oem1 => Resources.KeyOemPlus,
            Key.Oem2 => Resources.KeyOemQuestion,
            Key.Oem3 => Resources.KeyOemTilde,
            Key.Oem4 => Resources.KeyOemOpenBrackets,
            Key.Oem5 => Resources.KeyOemPipe,
            Key.Oem6 => Resources.KeyOemCloseBrackets,
            Key.Return => Resources.KeyEnter,
            Key.Prior => Resources.KeyPageUp,
            Key.Next => Resources.KeyPageDown,
            _ => Resources.ResourceManager.GetString(value.GetType().Name + value, Resources.Culture) ?? ""
        };
    }
}