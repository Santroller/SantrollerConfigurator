using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using MouseButton = Avalonia.Input.MouseButton;

namespace GuitarConfigurator.NetCore;

public class InputImageConverter : IMultiValueConverter
{
    private static readonly Dictionary<object, Bitmap> Icons = new();

    private string GetPath(object type, LegendType legendType, bool swapSwitchFaceButtons)
    {
        switch (legendType)
        {
            case LegendType.Xbox:
                return $"Xbox360/{type}";
            case LegendType.PlayStation:
                return $"PS2/{type}";
            case LegendType.Switch:
                if (!swapSwitchFaceButtons)
                {
                    type = type switch
                    {
                        StandardButtonType.X => StandardButtonType.Y,
                        StandardButtonType.Y => StandardButtonType.X,
                        StandardButtonType.A => StandardButtonType.B,
                        StandardButtonType.B => StandardButtonType.A,
                        _ => type
                    };
                }

                return $"Switch/Switch{type}";
        }

        return "";
    }
    
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] is not Enum) return null;
        if (values[1] is not DeviceControllerType deviceControllerType) return null;
        if (values[2] is not LegendType legendType) return null;
        if (values[3] is not bool swapSwitchFaceButtons) return null;
        var name = values[0]!.GetType().Name + values[0] + deviceControllerType + legendType + swapSwitchFaceButtons;
        if (Icons.TryGetValue(name, out var image)) return image;

        var path = values[0] switch
        {
            EmptyType.Empty => "Generic",
            SimpleType type => "Combined/" + type switch
            {
                SimpleType.WiiInputSimple => "Wii",
                SimpleType.Ps2InputSimple => "PS2",
                SimpleType.WtNeckSimple => "GHWT",
                SimpleType.Gh5NeckSimple => "GH5",
                SimpleType.DjTurntableSimple => "DJ",
                SimpleType.UsbHost => "Usb",
                SimpleType.Bluetooth => "Bluetooth",
                SimpleType.Rumble => "Rumble",
                SimpleType.ConsoleMode => "Console",
                SimpleType.Reset => "Reset",
                _ => null
            },
            
            InputType.AnalogPinInput or InputType.AnalogPeripheralInput => "Combined/Analog",
            InputType.MultiplexerInput or InputType.MultiplexerPeripheralInput => "Combined/Multiplexer",
            InputType.DigitalPinInput or InputType.DigitalPeripheralInput => "Combined/Digital",
            InputType.MacroInput => "Combined/Macro",
            InputType.ConstantInput => "Combined/Constant",
            InputType.WiiInput => "Combined/Wii",
            InputType.Ps2Input => "Combined/PS2",
            InputType.TurntableInput => "Combined/DJ",
            InputType.WtNeckInput => "Combined/GHWT",
            InputType.Gh5NeckInput => "Combined/GH5",
            InputType.UsbHostInput => "Combined/Usb",
            DpadType type => (type.ToString().StartsWith("Ps2") ? "PS2/DPad" : "Wii/ClassicDPad") +
                             type.ToString()[3..],
            UsbHostInputType type => $"Combined/Usb/{type}",
            Key => "Keyboard",
            MouseAxis => "Mouse",
            MouseButton => "Mouse",
            DjAxisType type => "DJ/" + type.ToString().Replace("Left","").Replace("Right",""),
            DjInputType type => "DJ/" + type.ToString().Replace("Left","").Replace("Right",""),
            Ps2InputType type => "PS2/" + type.ToString().Replace("Dualshock2",""),
            WiiInputType type => "Wii/" + type,
            DrumAxisType type => deviceControllerType + "/" + type,
            GuitarAxisType type => deviceControllerType + "/" + type,
            InstrumentButtonType type => deviceControllerType + "/" + type,
            Gh5NeckInputType type => "GuitarHero/" + type,
            GhWtInputType type => "GuitarHero/" + type,
            EmulationModeType type => type switch
            {
                EmulationModeType.XboxOne => "Combined/XboxOne",
                EmulationModeType.Xbox360 => "Combined/Xbox360",
                EmulationModeType.Wii => "Combined/Wii",
                EmulationModeType.Ps3 => "Combined/PS3",
                EmulationModeType.Ps4Or5 => "Combined/PS4",
                EmulationModeType.Switch => "Combined/Switch",
                _ => throw new ArgumentOutOfRangeException()
            },
            StandardButtonType type => GetPath(type, legendType, swapSwitchFaceButtons),
            StandardAxisType type => GetPath(type, legendType, swapSwitchFaceButtons),
            Ps3AxisType type => "PS2/" + type.ToString().Replace("Pressure",""),
            _ => null
        };
        try
        {
            var asset = AssetLoader.Open(new Uri($"avares://SantrollerConfigurator/Assets/Icons/{path}.png"));
            var bitmap = new Bitmap(asset);
            Icons.Add(name, bitmap);
            return bitmap;
        }
        catch (FileNotFoundException)
        {
            var asset = AssetLoader.Open(new Uri($"avares://SantrollerConfigurator/Assets/Icons/Generic.png"));
            var bitmap = new Bitmap(asset);
            Icons.Add(name, bitmap);
            return bitmap;
        }
    }
}