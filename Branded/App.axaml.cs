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
using SantrollerConfiguratorBranded.NetCore.Views;
using Splat;

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

        Locator.CurrentMutable.RegisterConstant<IScreen>(new BrandedMainWindowViewModel());
        Locator.CurrentMutable.Register<IViewFor<ConfigViewModel>>(() => new BrandedConfigView());
        Locator.CurrentMutable.Register<IViewFor<InitialConfigViewModel>>(() => new InitialConfigureView());
        Locator.CurrentMutable.Register<IViewFor<BrandedMainViewModel>>(() => new BrandedMainView());
        lifetime.MainWindow = new BrandedMainWindow {DataContext = Locator.Current.GetService<IScreen>()};
        lifetime.MainWindow.RequestedThemeVariant = ThemeVariant.Dark;lifetime.Exit += (_, _) =>
        {
            Environment.Exit(0);
        };
        base.OnFrameworkInitializationCompleted();
    }
}