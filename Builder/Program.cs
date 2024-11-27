using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using GuitarConfigurator.NetCore;
using GuitarConfigurator.NetCore.Devices;
using Velopack;

namespace SantrollerConfiguratorBuilder.NetCore;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.FirstOrDefault() == "-Rescan")
        {
            ConfigurableUsbDeviceManager.Rescan();
            return;
        }
        if (args.FirstOrDefault() == "-Drivers")
        {
            ConfigurableUsbDeviceManager.InstallDrivers();
            return;
        }
        VelopackApp.Build().Run();
        Directory.CreateDirectory(AssetUtils.GetAppDataFolder());
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UseReactiveUI()
            .UsePlatformDetect()
            .LogToTrace();
    }
}