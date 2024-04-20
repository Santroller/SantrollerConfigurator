using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using Microsoft.Build.Utilities;
using static System.Diagnostics.Process;

namespace GuitarConfigurator.NetCore;

public class Builder : Task
{
    public string Parameter1 { get; set; } = "";
    public string Parameter2 { get; set; } = "";

    public override bool Execute()
    {
        var platform = "linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) platform = "windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) platform = "macos";
        if (platform == "linux")
        {
            // No idea how to fix this in rider so here we are
            if (Environment.GetEnvironmentVariable("RESHARPER_FUS_BUILD") != null)
            {
                Environment.SetEnvironmentVariable("SSH_AUTH_SOCK",
                    Path.Combine(Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR")!, "ssh-agent.socket"));
            }
        }

        if (platform == "macos" && Parameter2 != "None")
        {
            Parameter1 = Path.Combine(Parameter1, Parameter2 + ".app", "Contents", "Resources");
            Console.WriteLine(Parameter1);
        }
        Directory.CreateDirectory(Path.Combine(Parameter1, "Binaries"));

        Console.WriteLine("Copying firmware");
        CopyIfNew(Parameter1, new[] {"firmware", "firmware.version"}, new[] {"firmware", "firmware.tar.xz"});
        Console.WriteLine("Copying platformio");
        CopyIfNew(Parameter1, new[] {"libs", platform, "platformio.version"}, new[] {"libs", platform, "platformio.tar.xz"});
        return true;
    }

    private void CopyIfNew(string dir, string[] version, string[] file)
    {
        var assets = Path.Combine(dir, "Binaries");
        var versionFile = Path.Combine(assets, version.Last());
        var oldVersion = "";
        if (File.Exists(versionFile))
        {
            oldVersion = File.ReadAllText(versionFile);
        }

        CopyFile(dir, version);
        var newVersion = File.ReadAllText(versionFile);
        if (newVersion != oldVersion)
        {
            Console.WriteLine("Changed, updating");
            CopyFile(dir, file);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var info = new ProcessStartInfo("7z", $"x \"{file.Last()}\"")
                {
                    WorkingDirectory = assets
                };
                Start(info)?.WaitForExit();
                info = new ProcessStartInfo("tar", $"xf \"{file.Last().Replace("tar.xz", "tar")}\"")
                {
                    WorkingDirectory = assets
                };
                Start(info)?.WaitForExit();
                File.Delete(Path.Combine(assets, file.Last().Replace("tar.xz", "tar")));
            }
            else
            {
                var info = new ProcessStartInfo("tar", $"xf \"{file.Last()}\"")
                {
                    WorkingDirectory = assets
                };
                Start(info)?.WaitForExit();
            }

            File.Delete(Path.Combine(assets, file.Last()));
        }
        else

        {
            Console.WriteLine("No change, leaving as is");
        }
    }

    private void CopyFile(string dir, params string[] file)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null)
        {
            file = new[] {Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "artifacts"}.Concat(file)
                .ToArray();
            File.Copy(Path.Combine(file), Path.Combine(dir, "Binaries", file.Last()), true);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            file = new[] {".", "artifacts"}.Concat(file).ToArray();
            Start("scp",
                    $"sanjay@192.168.0.79:{Path.Combine(file)} {Path.Combine(dir, "Binaries", file.Last())}")
                .WaitForExit();
        }
        else
        {
            file = new[] {".", "artifacts"}.Concat(file).ToArray();
            Start("rsync",
                    $"-avPr sanjay@192.168.0.79:{Path.Combine(file)} {Path.Combine(dir, "Binaries", file.Last())}")
                .WaitForExit();
        }
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
}