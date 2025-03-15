using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media;
using static GuitarConfigurator.NetCore.ViewModels.ConfigViewModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum LedType
{
    None,
    Apa102Rgb,
    Apa102Rbg,
    Apa102Grb,
    Apa102Gbr,
    Apa102Brg,
    Apa102Bgr,
    Stp16Cpc26,
    Ws2812Rgb,
    Ws2812Rbg,
    Ws2812Grb,
    Ws2812Gbr,
    Ws2812Brg,
    Ws2812Bgr,
    Ws2812Rgbw,
    Ws2812Rbgw,
    Ws2812Grbw,
    Ws2812Gbrw,
    Ws2812Brgw,
    Ws2812Bgrw
}

public static class LedTypeMethods
{
    public static bool IsWs2812(this LedType type)
    {
        return type.IsWs2812W() || type is LedType.Ws2812Rgb or LedType.Ws2812Rbg or LedType.Ws2812Grb or LedType.Ws2812Gbr
            or LedType.Ws2812Brg or LedType.Ws2812Bgr;
    }
    public static bool IsWs2812W(this LedType type)
    {
        return type is LedType.Ws2812Rgbw or LedType.Ws2812Rbgw or LedType.Ws2812Grbw or LedType.Ws2812Gbrw
            or LedType.Ws2812Brgw or LedType.Ws2812Bgrw;
    }

    public static bool IsApa102(this LedType type)
    {
        return type is LedType.Apa102Rgb or LedType.Apa102Rbg or LedType.Apa102Grb or LedType.Apa102Gbr
            or LedType.Apa102Brg or LedType.Apa102Bgr;
    }

    public static bool IsIndexed(this LedType type)
    {
        return type.IsWs2812() || type.IsApa102() || type == LedType.Stp16Cpc26;
    }

    private static readonly byte[] Ws2812Bits = [0x88, 0x8C, 0xC8, 0xCC];

    public static byte[] GetLedBytes(this LedType type, Color color, byte brightness)
    {
        if (type.IsApa102())
        {
            return [brightness, color.R, color.G, color.B];
        }

        if (type.IsWs2812())
        {
            return [color.R, color.G, color.B];
        }

        throw new ArgumentOutOfRangeException(nameof(type), type, null);
    }

    public static byte[] TranslateLedBytes(this LedType type, byte[] led)
    {
        var brightness = led[0];
        var r = led[1];
        var g = led[2];
        var b = led[3];
        return type switch
        {
            LedType.Apa102Rgb => [brightness, r, g, b],
            LedType.Apa102Rbg => [brightness, r, b, g],
            LedType.Apa102Grb => [brightness, g, r, b],
            LedType.Apa102Gbr => [brightness, g, b, r],
            LedType.Apa102Brg => [brightness, b, r, g],
            LedType.Apa102Bgr => [brightness, b, g, r],
            LedType.Ws2812Rgb or LedType.Ws2812Rgbw => [r, g, b],
            LedType.Ws2812Rbg or LedType.Ws2812Rbgw => [r, b, g],
            LedType.Ws2812Grb or LedType.Ws2812Grbw => [g, r, b],
            LedType.Ws2812Gbr or LedType.Ws2812Gbrw => [g, b, r],
            LedType.Ws2812Brg or LedType.Ws2812Brgw => [b, r, g],
            LedType.Ws2812Bgr or LedType.Ws2812Bgrw => [b, g, r],
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static IEnumerable<string> GetLedStrings(this LedType type, string brightness, string r, string g, string b)
    {
        return type switch
        {
            LedType.Apa102Rgb => [brightness, r, g, b],
            LedType.Apa102Rbg => [brightness, r, b, g],
            LedType.Apa102Grb => [brightness, g, r, b],
            LedType.Apa102Gbr => [brightness, g, b, r],
            LedType.Apa102Brg => [brightness, b, r, g],
            LedType.Apa102Bgr => [brightness, b, g, r],
            LedType.Ws2812Rgb or LedType.Ws2812Rgbw => [r, g, b],
            LedType.Ws2812Rbg or LedType.Ws2812Rbgw => [r, b, g],
            LedType.Ws2812Grb or LedType.Ws2812Grbw => [g, r, b],
            LedType.Ws2812Gbr or LedType.Ws2812Gbrw => [g, b, r],
            LedType.Ws2812Brg or LedType.Ws2812Brgw => [b, r, g],
            LedType.Ws2812Bgr or LedType.Ws2812Bgrw => [b, g, r],
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static string GetLedAssignment(this LedType type, bool peripheral, Color color, byte index, byte brightness,
        BinaryWriter? writer)
    {
        var variable = peripheral ? "ledStatePeripheral" : "ledState";
        if (type.IsWs2812())
        {
            return writer != null
                ? $"""
                        {variable}[{index - 1}].r = {WriteBlob(writer, color.R)};
                        {variable}[{index - 1}].g = {WriteBlob(writer, color.G)};
                        {variable}[{index - 1}].b = {WriteBlob(writer, color.B)};
                   """
                : $"""
                        {variable}[{index - 1}].r = {color.R};
                        {variable}[{index - 1}].g = {color.G};
                        {variable}[{index - 1}].b = {color.B};
                   """;
        }

        return writer != null
            ? $"""
                    {variable}[{index - 1}].brightness = {WriteBlob(writer, brightness)};
                    {variable}[{index - 1}].r = {WriteBlob(writer, color.R)};
                    {variable}[{index - 1}].g = {WriteBlob(writer, color.G)};
                    {variable}[{index - 1}].b = {WriteBlob(writer, color.B)};
               """
            : $"""
                    {variable}[{index - 1}].brightness = {brightness};
                    {variable}[{index - 1}].r = {color.R};
                    {variable}[{index - 1}].g = {color.G};
                    {variable}[{index - 1}].b = {color.B};
               """;
    }

    public static string GetLedAssignment(this LedType type, bool peripheral, string brightness, string r, string g,
        string b, byte index)
    {
        var variable = peripheral ? "ledStatePeripheral" : "ledState";
        if (type.IsWs2812())
        {
            return $"""
                         {variable}[{index - 1}].r = {r};
                         {variable}[{index - 1}].g = {g};
                         {variable}[{index - 1}].b = {b};

                    """;
        }

        return $"""
                     {variable}[{index - 1}].brightness = {brightness};
                     {variable}[{index - 1}].r = {r};
                     {variable}[{index - 1}].g = {g};
                     {variable}[{index - 1}].b = {b};
                """;
    }

    public static string GetLedAssignment(this LedType type, bool peripheral, int index, Color on, Color off,
        byte brightnessOn, byte brightnessOff, string var,
        BinaryWriter? writer)
    {
        var variable = peripheral ? "ledStatePeripheral" : "ledState";
        var r = $"({var} * {on.R - off.R} / 255) + {off.R}";
        var g = $"({var} * {on.G - off.G} / 255) + {off.G}";
        var b = $"({var} * {on.B - off.B} / 255) + {off.B}";
        var brightness = $"({var} * {brightnessOn - brightnessOff} / 255) + {brightnessOff}";
        if (writer != null)
        {
            var rOnBlob = WriteBlob(writer, on.R);
            var gOnBlob = WriteBlob(writer, on.G);
            var bOnBlob = WriteBlob(writer, on.G);
            var rOffBlob = WriteBlob(writer, off.R);
            var gOffBlob = WriteBlob(writer, off.G);
            var bOffBlob = WriteBlob(writer, off.G);
            r = $"({var} * {rOnBlob} - {rOffBlob} / 255) + {rOffBlob}";
            g = $"({var} * {gOnBlob} - {gOffBlob} / 255) + {gOffBlob}";
            b = $"({var} * {bOnBlob} - {bOffBlob} / 255) + {bOffBlob}";
        }
        else
        {
            // If the on and off rgb values for a channel are the same we can shortcut the lerp.
            // but only if the led colours aren't blobs
            if (on.R == off.R)
            {
                r = off.R.ToString();
            }

            if (on.G == off.G)
            {
                g = off.G.ToString();
            }

            if (on.B == off.B)
            {
                b = off.B.ToString();
            }
        }

        if (type.IsWs2812())
        {
            return $"""
                         {variable}[{index - 1}].r = {r};
                         {variable}[{index - 1}].g = {g};
                         {variable}[{index - 1}].b = {b};

                    """;
        }

        return $"""
                     {variable}[{index - 1}].brightness = {brightness};
                     {variable}[{index - 1}].r = {r};
                     {variable}[{index - 1}].g = {g};
                     {variable}[{index - 1}].b = {b};
                """;
    }
}