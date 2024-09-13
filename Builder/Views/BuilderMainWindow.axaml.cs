using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using GuitarConfigurator.NetCore.ViewModels;
using GuitarConfigurator.NetCore.Views;
using ReactiveUI;
using SantrollerConfiguratorBuilder.NetCore.ViewModels;

namespace SantrollerConfiguratorBuilder.NetCore.Views;

public partial class BuilderMainWindow : ReactiveWindow<BuilderMainWindowViewModel>
{
    public BuilderMainWindow()
    {
        this.WhenActivated(disposables =>
        {
            disposables(ViewModel!.ShowYesNoDialog.RegisterHandler(DoShowYesNoDialogAsync));
            disposables(ViewModel!.ShowIssueDialog.RegisterHandler(DoShowIssueDialogAsync));
            disposables(ViewModel!.LoadConfig.RegisterHandler(LoadImageAsync));
            disposables(ViewModel!.SaveUf2Handler.RegisterHandler(SaveUf2Async));
            disposables(ViewModel!.SaveBinaryHandler.RegisterHandler(SaveBinaryAsync));
            ViewModel!.Begin(true);
        });
        InitializeComponent();
    }
    
    private async Task LoadImageAsync(InteractionContext<BuilderMainWindowViewModel, IStorageFile?> obj)
    {
        var file = await ((Window) VisualRoot!).StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new[] {new FilePickerFileType("Image File") {Patterns = new[] {"*.png"}}}
        });
        if (file.Count == 0)
        {
            obj.SetOutput(null);
            return;
        }
        obj.SetOutput(file[0]);
    }
    private async Task SaveUf2Async(InteractionContext<BuilderMainWindowViewModel, IStorageFile?> obj)
    {
        var file = await ((Window) VisualRoot!).StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            FileTypeChoices = new[] {new FilePickerFileType("UF2 File") {Patterns = new[] {"*.uf2"}}},
            SuggestedFileName = "firmware.uf2"
        });
        obj.SetOutput(file);
    }
    private async Task SaveBinaryAsync(InteractionContext<BuilderMainWindowViewModel, IStorageFolder?> obj)
    {
        var dir = await ((Window) VisualRoot!).StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title="Customer tool output location"
        });
        obj.SetOutput(dir.FirstOrDefault());
    }
    private async Task DoShowIssueDialogAsync(
        InteractionContext<(string _platformIOText, ConfigViewModel), RaiseIssueWindowViewModel?> interaction)
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
        InteractionContext<(string yesText, string noText, string text), AreYouSureWindowViewModel> interaction)
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