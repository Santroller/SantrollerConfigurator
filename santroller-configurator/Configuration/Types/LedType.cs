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
    Ws2812,
    Ws2812W
}

public static class LedTypeMethods
{
    private static readonly byte[] Ws2812Bits = [0x88, 0x8C, 0xC8, 0xCC];

    public static byte[] GetLedBytes(this LedType type, Color color, byte brightness)
    {
        return type switch
        {
            LedType.Apa102Rgb or LedType.Apa102Rbg or LedType.Apa102Grb or LedType.Apa102Gbr or LedType.Apa102Brg or LedType.Apa102Bgr =>
                [brightness, color.R, color.G, color.B],
            LedType.Ws2812 or LedType.Ws2812W =>
            [
                Ws2812Bits[(color.R >> 6) & 0x3], Ws2812Bits[(color.R >> 4) & 0x3], Ws2812Bits[(color.R >> 2) & 0x3],
                Ws2812Bits[color.R & 0x3], Ws2812Bits[(color.G >> 6) & 0x3], Ws2812Bits[(color.G >> 4) & 0x3],
                Ws2812Bits[(color.G >> 2) & 0x3], Ws2812Bits[color.G & 0x3], Ws2812Bits[(color.B >> 6) & 0x3],
                Ws2812Bits[(color.B >> 4) & 0x3], Ws2812Bits[(color.B >> 2) & 0x3], Ws2812Bits[color.B & 0x3]
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
    public static byte[] TranslateLedBytes(this LedType type, byte[] led)
    {
        if (type is LedType.Ws2812 or LedType.Ws2812W)
        {
            return led;
        }
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
            LedType.Apa102Bgr => new[] {brightness, b, g, r},
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static string GetLedAssignment(this LedType type, bool peripheral, Color color, byte index, byte brightness,
        BinaryWriter? writer)
    {
        var variable = peripheral ? "ledStatePeripheral" : "ledState";
        if (type is LedType.Ws2812 or LedType.Ws2812W)
        {
            return writer == null
                ? $"""
                        {variable}[{index - 1}].r[0] = {Ws2812Bits[(color.R >> 6) & 0x3]};
                        {variable}[{index - 1}].r[1] = {Ws2812Bits[(color.R >> 4) & 0x3]};
                        {variable}[{index - 1}].r[2] = {Ws2812Bits[(color.R >> 2) & 0x3]};
                        {variable}[{index - 1}].r[3] = {Ws2812Bits[color.R & 0x3]};
                        {variable}[{index - 1}].g[0] = {Ws2812Bits[(color.G >> 6) & 0x3]};
                        {variable}[{index - 1}].g[1] = {Ws2812Bits[(color.G >> 4) & 0x3]};
                        {variable}[{index - 1}].g[2] = {Ws2812Bits[(color.G >> 2) & 0x3]};
                        {variable}[{index - 1}].g[3] = {Ws2812Bits[color.G & 0x3]};
                        {variable}[{index - 1}].b[0] = {Ws2812Bits[(color.B >> 6) & 0x3]};
                        {variable}[{index - 1}].b[1] = {Ws2812Bits[(color.B >> 4) & 0x3]};
                        {variable}[{index - 1}].b[2] = {Ws2812Bits[(color.B >> 2) & 0x3]};
                        {variable}[{index - 1}].b[3] = {Ws2812Bits[color.B & 0x3]};

                   """
                : $"""
                        {variable}[{index - 1}].r[0] = {WriteBlob(writer, Ws2812Bits[(color.R >> 6) & 0x3])};
                        {variable}[{index - 1}].r[1] = {WriteBlob(writer, Ws2812Bits[(color.R >> 4) & 0x3])};
                        {variable}[{index - 1}].r[2] = {WriteBlob(writer, Ws2812Bits[(color.R >> 2) & 0x3])};
                        {variable}[{index - 1}].r[3] = {WriteBlob(writer, Ws2812Bits[color.R & 0x3])};
                        {variable}[{index - 1}].g[0] = {WriteBlob(writer, Ws2812Bits[(color.G >> 6) & 0x3])};
                        {variable}[{index - 1}].g[1] = {WriteBlob(writer, Ws2812Bits[(color.G >> 4) & 0x3])};
                        {variable}[{index - 1}].g[2] = {WriteBlob(writer, Ws2812Bits[(color.G >> 2) & 0x3])};
                        {variable}[{index - 1}].g[3] = {WriteBlob(writer, Ws2812Bits[color.G & 0x3])};
                        {variable}[{index - 1}].b[0] = {WriteBlob(writer, Ws2812Bits[(color.B >> 6) & 0x3])};
                        {variable}[{index - 1}].b[1] = {WriteBlob(writer, Ws2812Bits[(color.B >> 4) & 0x3])};
                        {variable}[{index - 1}].b[2] = {WriteBlob(writer, Ws2812Bits[(color.B >> 2) & 0x3])};
                        {variable}[{index - 1}].b[3] = {WriteBlob(writer, Ws2812Bits[color.B & 0x3])};

                   """;
        }

        return writer != null ? $"""
                                      {variable}[{index - 1}].brightness = {WriteBlob(writer, brightness)};
                                      {variable}[{index - 1}].r = {WriteBlob(writer, color.R)};
                                      {variable}[{index - 1}].g = {WriteBlob(writer, color.G)};
                                      {variable}[{index - 1}].b = {WriteBlob(writer, color.B)};
                                 """ : $"""
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
        if (type is LedType.Ws2812 or LedType.Ws2812W)
        {
            return $"""
                         {variable}[{index - 1}].r[0] = ws2812_bits[({r} >> 6) & 0x3];
                         {variable}[{index - 1}].r[1] = ws2812_bits[({r} >> 4) & 0x3];
                         {variable}[{index - 1}].r[2] = ws2812_bits[({r} >> 2) & 0x3];
                         {variable}[{index - 1}].r[3] = ws2812_bits[{r} & 0x3];
                         {variable}[{index - 1}].g[0] = ws2812_bits[({g} >> 6) & 0x3];
                         {variable}[{index - 1}].g[1] = ws2812_bits[({g} >> 4) & 0x3];
                         {variable}[{index - 1}].g[2] = ws2812_bits[({g} >> 2) & 0x3];
                         {variable}[{index - 1}].g[3] = ws2812_bits[{g} & 0x3];
                         {variable}[{index - 1}].b[0] = ws2812_bits[({b} >> 6) & 0x3];
                         {variable}[{index - 1}].b[1] = ws2812_bits[({b} >> 4) & 0x3];
                         {variable}[{index - 1}].b[2] = ws2812_bits[({b} >> 2) & 0x3];
                         {variable}[{index - 1}].b[3] = ws2812_bits[({b} & 0x3];

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

        if (type is LedType.Ws2812 or LedType.Ws2812W)
        {
            return $"""
                         {variable}[{index - 1}].r[0] = ws2812_bits[(({r}) >> 6) & 0x3];
                         {variable}[{index - 1}].r[1] = ws2812_bits[(({r}) >> 4) & 0x3];
                         {variable}[{index - 1}].r[2] = ws2812_bits[(({r}) >> 2) & 0x3];
                         {variable}[{index - 1}].r[3] = ws2812_bits[({r}) & 0x3];
                         {variable}[{index - 1}].g[0] = ws2812_bits[(({g}) >> 6) & 0x3];
                         {variable}[{index - 1}].g[1] = ws2812_bits[(({g}) >> 4) & 0x3];
                         {variable}[{index - 1}].g[2] = ws2812_bits[(({g}) >> 2) & 0x3];
                         {variable}[{index - 1}].g[3] = ws2812_bits[({g}) & 0x3];
                         {variable}[{index - 1}].b[0] = ws2812_bits[(({b}) >> 6) & 0x3];
                         {variable}[{index - 1}].b[1] = ws2812_bits[(({b}) >> 4) & 0x3];
                         {variable}[{index - 1}].b[2] = ws2812_bits[(({b}) >> 2) & 0x3];
                         {variable}[{index - 1}].b[3] = ws2812_bits[({b}) & 0x3];

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