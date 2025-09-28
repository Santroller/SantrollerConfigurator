using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using GuitarConfigurator.NetCore.ViewModels;
using GuitarConfigurator.NetCore.Views;
using ReactiveUI;
using SantrollerConfiguratorBranded.NetCore.ViewModels;

namespace SantrollerConfiguratorBranded.NetCore.Views;

public partial class BrandedMainWindow : ReactiveWindow<BrandedMainWindowViewModel>
{
    public BrandedMainWindow()
    {
        this.WhenActivated(disposables =>
        {
            ViewModel!.ShowInformationDialog.RegisterHandler(DoShowInformationDialogAsync).DisposeWith(disposables);
            ViewModel!.Begin(false).DisposeWith(disposables);
        });
        InitializeComponent();
    }

    private async Task DoShowInformationDialogAsync(
        IInteractionContext<string, InformationWindowViewModel> interaction)
    {
        var model = new InformationWindowViewModel(ViewModel!.AccentedButtonTextColor, interaction.Input);
        var dialog = new InformationWindow
        {
            DataContext = model
        };
        await dialog.ShowDialog<InformationWindowViewModel?>((Window) VisualRoot!);
        interaction.SetOutput(model);
    }

}