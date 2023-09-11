using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;

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