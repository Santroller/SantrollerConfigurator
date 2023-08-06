using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using GuitarConfigurator.NetCore.ViewModels;
using GuitarConfigurator.NetCore.Views;
using ReactiveUI;
using SantrollerConfiguratorBranded.NetCore.ViewModels;

namespace SantrollerConfiguratorBranded.NetCore.Views;

public partial class MainWindow : ReactiveWindow<BrandedMainWindowViewModel>
{
    public MainWindow()
    {
        // this.WhenActivated(disposables =>
        // {
        //     disposables(ViewModel!.ShowYesNoDialog.RegisterHandler(DoShowYesNoDialogAsync));
        //     ViewModel!.Begin();
        // });
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDevTools();
#endif
    }

    private async Task DoShowYesNoDialogAsync(
        InteractionContext<(string yesText, string noText, string text), AreYouSureWindowViewModel> interaction)
    {
        var model = new AreYouSureWindowViewModel(interaction.Input.yesText, interaction.Input.noText,
            interaction.Input.text);
        var dialog = new AreYouSureWindow
        {
            DataContext = model
        };
        await dialog.ShowDialog<AreYouSureWindowViewModel?>((Window) VisualRoot!);
        interaction.SetOutput(model);
    }
}