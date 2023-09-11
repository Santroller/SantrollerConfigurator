using System;
using System.Collections;
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
        var copyFunc = CopyFile;
        if (platform == "linux")
        {
            // No idea how to fix this in rider so here we are
            if (Environment.GetEnvironmentVariable("RESHARPER_FUS_BUILD") != null)
            {
                Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", Path.Combine(Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR")!, "ssh-agent.socket"));
            }
            else
            {
                copyFunc = CopyFileRunner;
            }
        }

        Console.WriteLine("Copying firmware");
        copyFunc("firmware", "firmware.tar.xz");
        copyFunc("firmware", "firmware.version");
        Console.WriteLine("Copying platformio");
        copyFunc("libs", platform, "platformio.tar.xz");
        copyFunc("libs", platform, "platformio.version");
        return true;
    }

    private void CopyFile(params string[] file)
    {
        file = new[] {".", "artifacts"}.Concat(file).ToArray();
        Console.WriteLine($"calling rsync: rsync -avPr sanjay@192.168.0.79:{Path.Combine(file)} {Path.Combine(Parameter2, "Assets", file.Last())}");
        Start("rsync",
            $"-avPr sanjay@192.168.0.79:{Path.Combine(file)} {Path.Combine(Parameter2, "Assets", file.Last())}").WaitForExit();
    }
    private void CopyFileRunner(params string[] file)
    {
        file = new[] {Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "artifacts"}.Concat(file).ToArray();
        File.Copy(Path.Combine(file), Path.Combine(Parameter2, "Assets", file.Last()));
    }
}