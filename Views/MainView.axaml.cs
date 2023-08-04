using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace SantrollerConfiguratorBuilder.NetCore.Views;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}