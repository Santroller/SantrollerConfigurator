using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore;

public class PinToStringConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] is not int || values[1] is not ConfigViewModel ||
            values[3] is not (Output or Input or ConfigViewModel)) return null;
        var peripheral = values.Count >= 6 && (bool?)values[5] == true;
        var pin = (int) values[0]!;
        var selectedPin = -1;
        if (values[2] is not null) selectedPin = (int) values[2]!;
        var model = (ConfigViewModel) values[1]!;
        var microcontroller = model.Microcontroller;
        var twi = values[3] is ITwi || values.Count >= 7 && (bool?)values[6] == false;
        var spi = values[3] is ISpi || values.Count >= 7 && (bool?)values[6] == true;
        var outputMode = values.Count >= 8 && (bool?)values[7] == true;
        var configs = values[3] switch
        {
            Input input => input.PinConfigs,
            Output output => output.GetPinConfigs(),
            ConfigViewModel => model.PinConfigs,
            _ => new List<PinConfig>()
        };
        
        peripheral = values[3] switch
        {
            Input input => input.Peripheral,
            Output output => output.Input.Peripheral,
            _ => peripheral
        };
        
        return microcontroller.GetPin(pin, peripheral, selectedPin, model.Bindings.Items, twi, spi, configs, model,
            values[4] is ComboBoxItem, outputMode);
    }
}