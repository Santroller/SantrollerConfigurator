using Avalonia.ReactiveUI;
using ReactiveUI;
using SantrollerConfiguratorBranded.NetCore.ViewModels;

namespace SantrollerConfiguratorBranded.NetCore.Views;

public partial class BrandedMainWindow : ReactiveWindow<BrandedMainWindowViewModel>
{
    public BrandedMainWindow()
    {
        this.WhenActivated(disposables =>
        {
            ViewModel!.Begin(false);
        });
        InitializeComponent();
    }

}