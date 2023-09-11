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
        const string firmwareUrl = "https://github.com/sanjay900/Santroller/releases/download/latest/firmware.tar.xz";
        var platformIoUrl =
            $"https://github.com/sanjay900/SantrollerLibs/releases/download/latest/platformio-{platform}.tar.xz";
        var firmwareFileLoc = Path.Combine(Parameter2, "Assets", "firmware.version");
        var platformioFileLoc = Path.Combine(Parameter2, "Assets", "platformio.version");
        var firmwareCommit = GetCommit("Santroller");
        var platformIoCommit = GetCommit("SantrollerLibs");
        var firmwareChanged =
            !File.Exists(firmwareFileLoc) || !File.ReadAllText(firmwareFileLoc).Equals(firmwareCommit);
        var platformioChanged = !File.Exists(platformioFileLoc) ||
                                !File.ReadAllText(platformioFileLoc).Equals(platformIoCommit);
        var webClient = new HttpClient();
        webClient.Timeout = TimeSpan.FromMinutes(120);
        if (firmwareChanged)
        {
            Console.WriteLine("Builder: Downloading Santroller firmware...");
            var result = webClient.GetByteArrayAsync(firmwareUrl).Result;
            File.WriteAllBytes(Path.Combine(Parameter2, "Assets", "firmware.tar.xz"), result);
            File.WriteAllText(firmwareFileLoc, firmwareCommit);
        }

        if (!platformioChanged) return true;
        Console.WriteLine("Builder: Downloading SantrollerLibs...");
        var result2 = webClient.GetByteArrayAsync(platformIoUrl).Result;
        File.WriteAllBytes(Path.Combine(Parameter2, "Assets", "platformio.tar.xz"), result2);
        File.WriteAllText(platformioFileLoc, platformIoCommit);

        return true;
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