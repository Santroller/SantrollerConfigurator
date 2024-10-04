using System;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using GuitarConfigurator.NetCore.Devices;
using Velopack;

namespace GuitarConfigurator.NetCore;

public static class Program
{
#if Windows && False
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
    public static void Main(string[] args)
    {
        AllocConsole();
        Directory.CreateDirectory(AssetUtils.GetAppDataFolder());
        

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
    }
#else
    public static void Main(string[] args)
    {
        if (args.FirstOrDefault() == "-Rescan")
        {
            ConfigurableUsbDeviceManager.Rescan();
            return;
        }
        VelopackApp.Build().Run();
        Directory.CreateDirectory(AssetUtils.GetAppDataFolder());
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
    }
#endif

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UseReactiveUI()
            .UsePlatformDetect()
            .LogToTrace();
    }
}