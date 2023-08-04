#if !DEBUG
using System;
#endif
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;

namespace GuitarConfigurator.NetCore;

public static class Program
{
    public static void Main(string[] args)
    {
#if !DEBUG
        var tr1 = new TextWriterTraceListener(Console.Out);
        Trace.Listeners.Add(tr1);
#endif
        Directory.CreateDirectory(AssetUtils.GetAppDataFolder());
        var tr2 = new TextWriterTraceListener(File.CreateText(Path.Combine(AssetUtils.GetAppDataFolder(),
            "build.log")));
        Trace.Listeners.Add(tr2);
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