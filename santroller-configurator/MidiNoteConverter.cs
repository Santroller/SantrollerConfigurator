using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;

namespace GuitarConfigurator.NetCore;

public class MidiNoteConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] is not int value || values[1] is not DeviceControllerType deviceType)
        {
            return null;
        }

        return MidiInput.GetNote(value, deviceType);
    }
}