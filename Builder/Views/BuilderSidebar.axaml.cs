using Avalonia.ReactiveUI;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace SantrollerConfiguratorBuilder.NetCore.Views;

public partial class BuilderSidebarView : ReactiveUserControl<ConfigViewModel>
{
    public BuilderSidebarView()
    {
        this.WhenActivated(disposables => { });
        InitializeComponent();
    }
    public ConfigViewModel Model => ViewModel!;
}