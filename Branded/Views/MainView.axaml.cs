using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using SantrollerConfiguratorBranded.NetCore.ViewModels;

namespace SantrollerConfiguratorBranded.NetCore.Views;

public partial class MainView : ReactiveUserControl<BrandedMainViewModel>
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