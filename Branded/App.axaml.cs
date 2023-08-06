using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using GuitarConfigurator.NetCore;
using GuitarConfigurator.NetCore.ViewModels;
using GuitarConfigurator.NetCore.Views;
using ReactiveUI;
using SantrollerConfiguratorBranded.NetCore.ViewModels;
using Splat;
using MainWindow = SantrollerConfiguratorBranded.NetCore.Views.MainWindow;
using MainView = SantrollerConfiguratorBranded.NetCore.Views.MainView;

namespace SantrollerConfiguratorBranded.NetCore;

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

        // Make sure we kill all python processes on exit
        Locator.CurrentMutable.RegisterConstant<IScreen>(new BrandedMainWindowViewModel());
        Locator.CurrentMutable.Register<IViewFor<ConfigViewModel>>(() => new ConfigView());
        Locator.CurrentMutable.Register<IViewFor<RestoreViewModel>>(() => new RestoreView());
        Locator.CurrentMutable.Register<IViewFor<InitialConfigViewModel>>(() => new InitialConfigureView());
        Locator.CurrentMutable.Register<IViewFor<BrandedMainViewModel>>(() => new MainView());
        lifetime.MainWindow = new MainWindow {DataContext = Locator.Current.GetService<IScreen>()};
        lifetime.MainWindow.RequestedThemeVariant = ThemeVariant.Dark;
        lifetime.Exit += (_, _) =>
        {
            PlatformIo.Exit();
            Environment.Exit(0);
        };
        base.OnFrameworkInitializationCompleted();
    }
}