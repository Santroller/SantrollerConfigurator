using System;
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
        // No idea how to fix this in rider so here we are
        if (Environment.GetEnvironmentVariable("SSH_AUTH_SOCK") == null)
        {
            Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", "/run/user/1000/ssh-agent.socket");
        }
        Console.WriteLine("Copying firmware");
        CopyFile("firmware", "firmware.tar.xz");
        CopyFile("firmware", "firmware.version");
        Console.WriteLine("Copying platformio");
        CopyFile($"libs", platform, "platformio.tar.xz");
        CopyFile($"libs", platform, "platformio.version");
        return true;
    }

    private void CopyFile(params string[] file)
    {
        file = new[] {".", "artifacts"}.Concat(file).ToArray();
        Start("rsync",
            $"-avPr sanjay@192.168.0.79:{Path.Combine(file)} {Path.Combine(Parameter2, "Assets", file.Last())}").WaitForExit();
    }
}