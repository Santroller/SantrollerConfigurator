using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using Microsoft.Build.Utilities;

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
        if (Environment.GetEnvironmentVariable("SSH_AUTH_SOCK") == null)
        {
            Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", "/run/user/1000/ssh-agent.socket");
        }
        Console.WriteLine("Copying firmware");
        System.Diagnostics.Process.Start("rsync",
            $"-avPr sanjay@192.168.0.79:./artifacts/firmware/firmware.tar.xz {Path.Combine(Parameter2, "Assets", "firmware.tar.xz")}").WaitForExit();
        Console.WriteLine("Copying platformio");
        System.Diagnostics.Process.Start("rsync",$"-avPr sanjay@192.168.0.79:./artifacts/libs/{platform}/platformio.tar.xz {Path.Combine(Parameter2, "Assets", "platformio.tar.xz")}").WaitForExit(); return true;
    }

    private string GetCommit(string project)
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri("https://github.com");
        var response = client.GetAsync($"sanjay900/{project}/info/refs?service=git-upload-pack").Result;
        response.EnsureSuccessStatusCode();
        var res = response.Content.ReadAsStringAsync().Result;
        return res.Split('\n').First(s => s.EndsWith($"refs/tags/latest")).Split(' ')[0].Substring(4);
    }
}