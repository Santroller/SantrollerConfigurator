using Avalonia.ReactiveUI;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace SantrollerConfiguratorBranded.NetCore.Views;

public partial class BrandedSidebarView : ReactiveUserControl<ConfigViewModel>
{
    public BrandedSidebarView()
    {
        this.WhenActivated(disposables => { });
        InitializeComponent();
    }
    public ConfigViewModel Model => ViewModel!;
}