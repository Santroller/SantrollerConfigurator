using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using AsmResolver;
using AsmResolver.IO;
using AsmResolver.PE;
using AsmResolver.PE.File;
using AsmResolver.PE.Win32Resources.Builder;
using AsmResolver.PE.Win32Resources.Icon;
using Avalonia;
using Avalonia.Media.Imaging;
using GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using ProtoBuf;

namespace SantrollerConfiguratorBuilder.NetCore;

public static class ExecutableUtils
{

    public static void UpdateIconEntryIcon(Bitmap img, IconEntry icon)
    {
        img = img.CreateScaledBitmap(new PixelSize(128, 128));
        using var msImg = new MemoryStream();
        img.Save(msImg);
        icon.PixelData = new DataSegment(msImg.ToArray());
        icon.Height = 128;
        icon.Width = 128;
        icon.ColorCount = 0;
        icon.Reserved = 0;
        icon.Planes = 0;
        icon.BitsPerPixel = 32;
        icon.PixelData.UpdateOffsets(new RelocationParameters());
    }

    private static byte[] GetIcnsIconType(int width, bool isScale2X)
    {
        var iconType = width switch
        {
            16 => isScale2X ? null : "icp4"u8,
            32 => isScale2X ? "ic11"u8 : "icp5"u8,
            64 => isScale2X ? "ic12"u8 : "icp6"u8,
            128 => isScale2X ? null : "ic07"u8,
            256 => isScale2X ? "ic13"u8 : "ic08"u8,
            512 => isScale2X ? "ic14"u8 : "ic09"u8,
            _ => "ic10"u8
        };

        return iconType.ToArray();
    }

    private static byte[] GetBigEndianBytes(int value)
    {
        var bytes = BitConverter.GetBytes(value);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);

        return bytes;
    }

    public static async Task RenameDirectoryInZip(string oldName, string newName, ZipArchive archive)
    {
        foreach (var oldEntry in archive.Entries.ToList())
        {
            if (oldEntry.Name.StartsWith("._") || oldEntry.Name.EndsWith(".pdb") || oldEntry.Name.EndsWith(".json") || (oldEntry.FullName.Contains("MacOS") && oldEntry.FullName.EndsWith(".icns")))
            {
                oldEntry.Delete();
                continue;
            }
            
            var newEntry = archive.CreateEntry(oldEntry.FullName.Replace(oldName, newName));
            await using var oldStream = oldEntry.Open();
            await using var newStream = newEntry.Open();
            await oldStream.CopyToAsync(newStream);
            oldStream.Close();
            newEntry.ExternalAttributes = oldEntry.ExternalAttributes;
            oldEntry.Delete();
        }
    }

    public static async Task UpdatePlist(string name, string outputFileName)
    {
        await using var info = File.Open(outputFileName, FileMode.Open);
        await using var stream = new MemoryStream();
        await using var infoWriter = new StreamWriter(stream);
        var infoReader = new StreamReader(info);
        while (!infoReader.EndOfStream)
        {
            var line = await infoReader.ReadLineAsync();
            await infoWriter.WriteLineAsync(line);
            if (line!.Contains("CFBundleName"))
            {
                await infoReader.ReadLineAsync();
                await infoWriter.WriteLineAsync($"<string>{name}</string>");
            }
            else if (line.Contains("CFBundleDisplayName"))
            {
                await infoReader.ReadLineAsync();
                await infoWriter.WriteLineAsync($"<string>{name}</string>");
            }
        }

        info.SetLength(0);
        info.Seek(0, SeekOrigin.Begin);
        await infoWriter.FlushAsync();
        stream.Seek(0, SeekOrigin.Begin);
        await stream.CopyToAsync(info);
        info.Close();
    }

    public static async Task OverwriteIcns(Bitmap img, string outputFileName)
    {
        await using var outputFile = File.OpenWrite(outputFileName);
        var icnsData = new List<byte>();
        var sizeAll = 0;

        img = img.CreateScaledBitmap(new PixelSize(512, 512));
        using var msImg = new MemoryStream();
        img.Save(msImg);
        msImg.Seek(0, SeekOrigin.Begin);
        var iconType = GetIcnsIconType(512, false);
        var sizeIcon = 4 + 4 + Convert.ToInt32(msImg.Length);
        icnsData.AddRange(GetBigEndianBytes(sizeIcon));
        msImg.CopyTo(outputFile);
        sizeAll += 4 + 4 + sizeIcon;

        outputFile.Write("icns"u8);
        sizeAll = 4 + 4 + sizeAll;
        var sizeAllArray = GetBigEndianBytes(sizeAll);
        outputFile.Write(sizeAllArray);
        outputFile.Write(iconType);
        outputFile.Write(icnsData.ToArray());
    }

    public static async Task AppendConfig(Stream output, BrandedConfigurationStore config)
    {
        var len = output.Length;
        await using var windowsWriter = new BinaryWriter(output);
        Serializer.SerializeWithLengthPrefix(output, new SerialisedBrandedConfigurationStore(config),
            PrefixStyle.Base128);
        windowsWriter.Write((int) len);
    }
}