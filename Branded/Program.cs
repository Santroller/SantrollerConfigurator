using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using GuitarConfigurator.NetCore.Devices;

namespace SantrollerConfiguratorBranded.NetCore;

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