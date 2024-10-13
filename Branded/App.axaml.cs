using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using GuitarConfigurator.NetCore;
using GuitarConfigurator.NetCore.ViewModels;
using GuitarConfigurator.NetCore.Views;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
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
        var model = new BrandedMainWindowViewModel();
        Locator.CurrentMutable.RegisterConstant<IScreen>(model);
        var mutex = new Mutex(true, @"Global\SantrollerConfig", out var mutexCreated);
        
        if (!mutexCreated)
        {
            lifetime.MainWindow = new AppAlreadyOpenWindow {DataContext = Locator.Current.GetService<IScreen>()};
        }
        else
        {
            Locator.CurrentMutable.Register<IViewFor<ConfigViewModel>>(() => new ConfigView());
            Locator.CurrentMutable.Register<IViewFor<InitialConfigViewModel>>(() => new InitialConfigureView());
            Locator.CurrentMutable.Register<IViewFor<BrandedMainViewModel>>(() => new BrandedMainView());
            lifetime.MainWindow = new BrandedMainWindow {DataContext = Locator.Current.GetService<IScreen>()};
        }

        lifetime.Exit += (_, _) =>
        {
            mutex.Close();
            Environment.Exit(0);
        };
        lifetime.MainWindow.RequestedThemeVariant = ThemeVariant.Dark;
        Current!.Resources["SystemAccentColor"] = Color.Parse(model.ProgressBarPrimary);

        base.OnFrameworkInitializationCompleted();
    }
}