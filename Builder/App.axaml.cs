using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using GuitarConfigurator.NetCore;
using GuitarConfigurator.NetCore.ViewModels;
using GuitarConfigurator.NetCore.Views;
using ReactiveUI;
using SantrollerConfiguratorBuilder.NetCore.ViewModels;
using SantrollerConfiguratorBuilder.NetCore.Views;
using Splat;

namespace SantrollerConfiguratorBuilder.NetCore;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
            throw new Exception("Invalid ApplicationLifetime");

        Locator.CurrentMutable.RegisterConstant<IScreen>(new BuilderMainWindowViewModel());
        Locator.CurrentMutable.Register<IViewFor<BuilderMainViewModel>>(() => new BuilderMainView());
        Locator.CurrentMutable.Register<IViewFor<ConfigViewModel>>(() => new BuilderConfigView());
        lifetime.MainWindow = new BuilderMainWindow {DataContext = Locator.Current.GetService<IScreen>()};
        lifetime.MainWindow.RequestedThemeVariant = ThemeVariant.Dark;
        lifetime.Exit += (_, _) =>
        {
            PlatformIo.Exit();
            Environment.Exit(0);
        };
        base.OnFrameworkInitializationCompleted();
    }
}