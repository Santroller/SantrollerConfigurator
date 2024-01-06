using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        this.WhenActivated(disposables =>
        {
            disposables(ViewModel!.ShowYesNoDialog.RegisterHandler(DoShowYesNoDialogAsync));
            disposables(ViewModel!.ShowIssueDialog.RegisterHandler(DoShowIssueDialogAsync));
            ViewModel!.Begin(true);
        });
        InitializeComponent();
    }

    private async Task DoShowIssueDialogAsync(
        IInteractionContext<(string _platformIOText, ConfigViewModel), RaiseIssueWindowViewModel?> interaction)
    {
        var model = new RaiseIssueWindowViewModel(interaction.Input);
        var dialog = new RaiseIssueWindow
        {
            DataContext = model
        };
        var result = await dialog.ShowDialog<RaiseIssueWindowViewModel?>((Window) VisualRoot!);
        interaction.SetOutput(result);
    }

    private async Task DoShowYesNoDialogAsync(
        IInteractionContext<(string yesText, string noText, string text), AreYouSureWindowViewModel> interaction)
    {
        var model = new AreYouSureWindowViewModel(ViewModel!.AccentedButtonTextColor, interaction.Input.yesText, interaction.Input.noText,
            interaction.Input.text);
        var dialog = new AreYouSureWindow
        {
            DataContext = model
        };
        await dialog.ShowDialog<AreYouSureWindowViewModel?>((Window) VisualRoot!);
        interaction.SetOutput(model);
    }
}