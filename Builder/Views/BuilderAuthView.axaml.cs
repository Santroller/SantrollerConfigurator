using Avalonia.ReactiveUI;
using ReactiveUI;
using SantrollerConfiguratorBuilder.NetCore.ViewModels;

namespace SantrollerConfiguratorBuilder.NetCore.Views;

public partial class BuilderAuthView : ReactiveUserControl<BuilderAuthViewModel>
{
    public BuilderAuthView()
    {
        this.WhenActivated(disposables =>
        {
            ViewModel!.SetupCommand.Execute(null);
        });
        InitializeComponent();
    }
}