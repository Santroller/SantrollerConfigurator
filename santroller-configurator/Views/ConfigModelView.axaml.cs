using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Views;

public partial class ConfigModelView : ReactiveUserControl<ConfigViewModel>
{
    public ConfigModelView()
    {
        this.WhenActivated(disposables =>
        {
            disposables(ViewModel!.ShowUnpluggedDialog.RegisterHandler(DoShowUnpluggedDialogAsync));
            disposables(ViewModel!.ShowYesNoDialog.RegisterHandler(DoShowYesNoDialogAsync));
            disposables(ViewModel!.ShowResetDialog.RegisterHandler(DoShowResetDialogAsync));
            disposables(ViewModel!.ShowBindAllDialog.RegisterHandler(DoShowBindAllDialogAsync));
            disposables(ViewModel!.SaveConfig.RegisterHandler(DoSaveConfigAsync));
            disposables(ViewModel!.LoadConfig.RegisterHandler(DoLoadConfigAsync));
            disposables(ViewModel!.RegisterConnections());
            disposables(
                ViewModel!.WhenAnyValue(x => x.Device).OfType<Santroller>()
                    .ObserveOn(RxApp.MainThreadScheduler).Subscribe(s => s.StartTicking(ViewModel)));
            TopLevel.GetTopLevel(GetValue(VisualParentProperty))!.KeyDown +=
                (sender, args) => ViewModel!.OnKeyEvent(args);
            TopLevel.GetTopLevel(GetValue(VisualParentProperty))!.PointerMoved += (sender, args) =>
                ViewModel!.OnMouseEvent(args.GetCurrentPoint(GetValue(VisualParentProperty)).Position);
            TopLevel.GetTopLevel(GetValue(VisualParentProperty))!.PointerPressed += (sender, args) =>
                ViewModel!.OnMouseEvent(args.GetCurrentPoint(GetValue(VisualParentProperty)).Properties
                    .PointerUpdateKind);
            TopLevel.GetTopLevel(GetValue(VisualParentProperty))!.PointerWheelChanged +=
                (sender, args) => ViewModel!.OnMouseEvent(args);
        });
        InitializeComponent();
    }

    public ConfigViewModel Model => ViewModel!;

    private async void DoSaveConfigAsync(InteractionContext<ConfigViewModel, Unit> obj)
    {
        var extension = "." + obj.Input.Microcontroller.Board.ArdwiinoName + "config";
        var file = await ((Window) VisualRoot!).StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            ShowOverwritePrompt = true, DefaultExtension = extension, SuggestedFileName = "controller" + extension,
            FileTypeChoices = new[] {new FilePickerFileType(extension) {Patterns = new[] {"*" + extension}}}
        });
        if (file == null) return;
        await using var stream = await file.OpenWriteAsync();
        Serializer.Serialize(stream, new SerializedConfiguration(obj.Input));
    }

    private async void DoLoadConfigAsync(InteractionContext<ConfigViewModel, Unit> obj)
    {
        var extension = "." + obj.Input.Microcontroller.Board.ArdwiinoName + "config";
        var file = await ((Window) VisualRoot!).StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new[] {new FilePickerFileType(extension) {Patterns = new[] {"*" + extension}}}
        });
        if (!file.Any()) return;
        await using var stream = await file[0].OpenReadAsync();
        Serializer.Deserialize<SerializedConfiguration>(stream).LoadConfiguration(obj.Input);
    }

    private async Task DoShowUnpluggedDialogAsync(
        InteractionContext<(string yesText, string noText, string text), AreYouSureWindowViewModel> interaction)
    {
        var model = new AreYouSureWindowViewModel(ViewModel!.Main.AccentedButtonTextColor, interaction.Input.yesText, interaction.Input.noText,
            interaction.Input.text);
        var dialog = new UnpluggedWindow
        {
            DataContext = model
        };
        await dialog.ShowDialog<AreYouSureWindowViewModel?>((Window) VisualRoot!);
        interaction.SetOutput(model);
    }

    private async Task DoShowYesNoDialogAsync(
        InteractionContext<(string yesText, string noText, string text), AreYouSureWindowViewModel> interaction)
    {
        var model = new AreYouSureWindowViewModel(ViewModel!.Main.AccentedButtonTextColor, interaction.Input.yesText, interaction.Input.noText,
            interaction.Input.text);
        var dialog = new AreYouSureWindow
        {
            DataContext = model
        };
        await dialog.ShowDialog<AreYouSureWindowViewModel?>((Window) VisualRoot!);
        interaction.SetOutput(model);
    }
    
    
    private async Task DoShowResetDialogAsync(
        InteractionContext<ConfigViewModel, ResetWindowViewModel> interaction)
    {
        var model = new ResetWindowViewModel(ViewModel!.Main.AccentedButtonTextColor);
        var dialog = new ResetWindow
        {
            DataContext = model
        };
        await dialog.ShowDialog<ResetWindowViewModel?>((Window) VisualRoot!);
        interaction.SetOutput(model);
    }

    private async Task DoShowBindAllDialogAsync(
        InteractionContext<(ConfigViewModel model, Output output),
            BindAllWindowViewModel> interaction)
    {
        var model = new BindAllWindowViewModel(interaction.Input.model,
            interaction.Input.output);
        var dialog = new BindAllWindow
        {
            DataContext = model
        };
        await dialog.ShowDialog<BindAllWindowViewModel?>((Window) VisualRoot!);
        interaction.SetOutput(model);
    }
}