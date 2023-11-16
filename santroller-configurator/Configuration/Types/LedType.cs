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
    Apa102Bgr
}

public static class LedTypeMethods
{
    public static byte[] GetLedBytes(this LedType type, Color color)
    {
        return type switch
        {
            LedType.Apa102Rgb => new[] {color.R, color.G, color.B},
            LedType.Apa102Rbg => new[] {color.R, color.B, color.G},
            LedType.Apa102Grb => new[] {color.G, color.R, color.B},
            LedType.Apa102Gbr => new[] {color.G, color.B, color.R},
            LedType.Apa102Brg => new[] {color.B, color.R, color.G},
            LedType.Apa102Bgr => new[] {color.B, color.G, color.R},
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static void WriteToWriter(this LedType type, Color color, BinaryWriter writer)
    {
        switch (type)
        {
            case LedType.None:
                break;
            case LedType.Apa102Rgb:
                writer.Write((ushort) color.R);
                writer.Write((ushort) color.G);
                writer.Write((ushort) color.B);
                break;
            case LedType.Apa102Rbg:
                writer.Write((ushort) color.R);
                writer.Write((ushort) color.B);
                writer.Write((ushort) color.G);
                break;
            case LedType.Apa102Grb:
                writer.Write((ushort) color.G);
                writer.Write((ushort) color.R);
                writer.Write((ushort) color.B);
                break;
            case LedType.Apa102Gbr:
                writer.Write((ushort) color.G);
                writer.Write((ushort) color.B);
                writer.Write((ushort) color.R);
                break;
            case LedType.Apa102Brg:
                writer.Write((ushort) color.B);
                writer.Write((ushort) color.R);
                writer.Write((ushort) color.G);
                break;
            case LedType.Apa102Bgr:
                writer.Write((ushort) color.B);
                writer.Write((ushort) color.G);
                writer.Write((ushort) color.R);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public static Color ReadFromReader(this LedType type, BinaryReader reader)
    {
        if (type is LedType.None) return Colors.Black;
        var first = (byte) reader.ReadUInt16();
        var second = (byte) reader.ReadUInt16();
        var third = (byte) reader.ReadUInt16();
        return type switch
        {
            LedType.Apa102Rgb => new Color(0xFF, first, second, third),
            LedType.Apa102Rbg => new Color(0xFF, first, third, second),
            LedType.Apa102Grb => new Color(0xFF, second, first, third),
            LedType.Apa102Gbr => new Color(0xFF, third, first, second),
            LedType.Apa102Brg => new Color(0xFF, second, third, first),
            LedType.Apa102Bgr => new Color(0xFF, third, second, first),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private static IEnumerable<string> GetLedStrings(this LedType type, string r, string g, string b)
    {
        return type switch
        {
            LedType.Apa102Rgb => new[] {r, g, b},
            LedType.Apa102Rbg => new[] {r, b, g},
            LedType.Apa102Grb => new[] {g, r, b},
            LedType.Apa102Gbr => new[] {g, b, r},
            LedType.Apa102Brg => new[] {b, r, g},
            LedType.Apa102Bgr => new[] {b, g, r},
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static string GetLedAssignment(this LedType type, bool peripheral, Color color, byte index, BinaryWriter? writer)
    {
        var data = GetLedBytes(type, color);
        var variable = peripheral ? "ledStatePeripheral" : "ledState";
        return string.Join("\n",
            writer != null
                ? data.Zip(new[] {'r', 'g', 'b'}).Select(pair =>
                    $"{variable}[{index - 1}].{pair.Second} = {WriteBlob(writer, pair.First)};")
                : data.Zip(new[] {'r', 'g', 'b'})
                    .Select(pair => $"{variable}[{index - 1}].{pair.Second} = {pair.First};"));
    }

    public static string GetLedAssignment(this LedType type, bool peripheral, string r, string g, string b, byte index)
    {
        var variable = peripheral ? "ledStatePeripheral" : "ledState";
        var data = GetLedStrings(type, r, g, b);
        return string.Join("\n",
            data.Zip(new[] {'r', 'g', 'b'}).Select(pair => $"{variable}[{index - 1}].{pair.Second} = {pair.First};"));
    }

    public static string GetLedAssignment(this LedType type, bool peripheral, int index, Color on, Color off, string var,
        BinaryWriter? writer)
    {
        var variable = peripheral ? "ledStatePeripheral" : "ledState";
        var rScale = on.R - off.R;
        var gScale = on.G - off.G;
        var bScale = on.B - off.B;
        var offBytes = GetLedBytes(type, off);
        var mulStrings = GetLedStrings(type, rScale.ToString(), gScale.ToString(), bScale.ToString());
        // If the scale is zero (aka the on and off rgb values for a channel are the same) we can shortcut the lerp.
        if (writer != null)
        {
            return string.Join("\n",
                new[] {'r', 'g', 'b'}.Zip(offBytes, mulStrings).Select(pair =>
                    pair.Third == "0"
                        ? $"{variable}[{index - 1}].{pair.First} = {WriteBlob(writer, pair.Second)};"
                        : $"{variable}[{index - 1}].{pair.First} = ({var} * {pair.Third} / 255) + {WriteBlob(writer, pair.Second)};"));
        }

        return string.Join("\n",
            new[] {'r', 'g', 'b'}.Zip(offBytes, mulStrings).Select(pair =>
                pair.Third == "0"
                    ? $"{variable}[{index - 1}].{pair.First} = {pair.Second};"
                    : $"{variable}[{index - 1}].{pair.First} = ({var} * {pair.Third} / 255) + {pair.Second};"));
    }
}