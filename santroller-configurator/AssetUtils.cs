using System;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Platform;
using GuitarConfigurator.NetCore.Utils;
using ProtoBuf;

namespace GuitarConfigurator.NetCore;

public class AssetUtils
{
    public delegate void ExtractionProgress(float progress);

    public static async Task ExtractFileAsync(string file, string location)
    {
        await using var f = File.OpenWrite(location);
        var assemblyName = typeof(AssetUtils).Assembly.GetName().Name!;
        var uri = new Uri($"avares://{assemblyName}/Assets/{file}");
        await using var target = AssetLoader.Open(uri);
        await target.CopyToAsync(f).ConfigureAwait(false);
    }

    public static async Task<string> ReadFileAsync(string file)
    {
        var assemblyName = typeof(AssetUtils).Assembly.GetName().Name!;
        var uri = new Uri($"avares://{assemblyName}/Assets/{file}");
        await using var target = AssetLoader.Open(uri);
        var reader = new StreamReader(target);
        return await reader.ReadToEndAsync();
    }


    public static string GetAppDataFolder()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            folder = "/Users/Shared/Library/Application Support";
        }

        var path = Path.Combine(folder, "SantrollerConfigurator");

        return path;
    }

    public static ToolConfig GetConfig()
    {
        var configFile = Path.Combine(GetAppDataFolder(), "config.bin");
        if (!File.Exists(configFile))
        {
            return new ToolConfig();
        }

        try
        {
            using var outputStream = new FileStream(configFile, FileMode.Open);
            return Serializer.Deserialize<ToolConfig>(outputStream);
        }
        catch (Exception)
        {
            // Config is broken, reset to defaults so the tool doesnt crash.
            return new ToolConfig();
        }
    }

    public static void SaveConfig(ToolConfig config)
    {
        var configFile = Path.Combine(GetAppDataFolder(), "config.bin");
        using var outputStream = new FileStream(configFile, FileMode.OpenOrCreate);
        Serializer.Serialize(outputStream, config);
    }
    public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }
}

   