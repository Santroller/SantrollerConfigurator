using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
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
            ((INotifyCollectionChanged) ViewModel.Outputs).CollectionChanged += OutputsChanged;
        });
        InitializeComponent();
    }

    private void OutputsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            Dispatcher.UIThread.Post(OutputsContainer.ContainerFromItem(e.NewItems![0]!)!.BringIntoView,
                DispatcherPriority.Background);
        }
    }

    public ConfigViewModel Model => ViewModel!;

    private async void DoSaveConfigAsync(IInteractionContext<ConfigViewModel, Unit> obj)
    {
        obj.SetOutput(new Unit());
        var extension = "." + obj.Input.Microcontroller.Board.ArdwiinoName + "config";
        var file = await ((Window) VisualRoot!).StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            ShowOverwritePrompt = true, DefaultExtension = extension, SuggestedFileName = "controller" + extension,
            FileTypeChoices = [new FilePickerFileType(extension) {Patterns = ["*" + extension]}]
        });
        if (file == null) return;
        await using var stream = await file.OpenWriteAsync();
        Serializer.Serialize(stream, new SerializedConfiguration(obj.Input));
    }

    private async void DoLoadConfigAsync(IInteractionContext<ConfigViewModel, Unit> obj)
    {
        obj.SetOutput(new Unit());
        var extension = "." + obj.Input.Microcontroller.Board.ArdwiinoName + "config";
        var file = await ((Window) VisualRoot!).StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(extension) {Patterns = ["*" + extension]}]
        });
        if (file.Count == 0) return;
        await using var stream = await file[0].OpenReadAsync();
        Serializer.Deserialize<SerializedConfiguration>(stream).LoadConfiguration(obj.Input);
    }

    public async Task DoShowUnpluggedDialogAsync(
        IInteractionContext<(string yesText, string noText, string text), AreYouSureWindowViewModel> interaction)
    {
        var model = new AreYouSureWindowViewModel(ViewModel!.Main.AccentedButtonTextColor, interaction.Input.yesText,
            interaction.Input.noText,
            interaction.Input.text);
        var dialog = new UnpluggedWindow
        {
            DataContext = model
        };
        await dialog.ShowDialog<AreYouSureWindowViewModel?>((Window) VisualRoot!);
        interaction.SetOutput(model);
    }

    private async Task DoShowYesNoDialogAsync(
        IInteractionContext<(string yesText, string noText, string text), AreYouSureWindowViewModel> interaction)
    {
        var model = new AreYouSureWindowViewModel(ViewModel!.Main.AccentedButtonTextColor, interaction.Input.yesText,
            interaction.Input.noText,
            interaction.Input.text);
        var dialog = new AreYouSureWindow
        {
            DataContext = model
        };
        await dialog.ShowDialog<AreYouSureWindowViewModel?>((Window) VisualRoot!);
        interaction.SetOutput(model);
    }


    private async Task DoShowResetDialogAsync(
        IInteractionContext<ConfigViewModel, ResetWindowViewModel> interaction)
    {
        var deviceInputType = ViewModel!.Main.DeviceInputType;
        if (ViewModel!.Bindings.Items.Any(s => s.Outputs.Items.Any(s2 => s2.Input.InputType is InputType.WiiInput)))
        {
            deviceInputType = DeviceInputType.Wii;
        }
        if (ViewModel!.Bindings.Items.Any(s => s.Outputs.Items.Any(s2 => s2.Input.InputType is InputType.Ps2Input)))
        {
            deviceInputType = DeviceInputType.Ps2;
        }
        if (ViewModel!.UsbHostEnabled)
        {
            deviceInputType = DeviceInputType.Usb;
        }
        if (ViewModel!.IsBluetoothRx)
        {
            deviceInputType = DeviceInputType.Bluetooth;
        }
        if (ViewModel!.AdafruitHost)
        {
            deviceInputType = DeviceInputType.Feather;
        }
        var model = new ResetWindowViewModel(ViewModel!.Main.AccentedButtonTextColor, interaction.Input.Device,
            deviceInputType, ViewModel!.DeviceControllerType, ViewModel!.IsBluetoothTx,
            ViewModel!.Bindings.Items.Any(s => s is EmulationMode
            {
                Type: EmulationModeType.Fnf or EmulationModeType.FnfHid or EmulationModeType.FnfIos
                or EmulationModeType.FnfLayer
            }));
        var dialog = new ResetWindow
        {
            DataContext = model
        };
        await dialog.ShowDialog<ResetWindowViewModel?>((Window) VisualRoot!);
        interaction.SetOutput(model);
    }

    private async Task DoShowBindAllDialogAsync(
        IInteractionContext<(ConfigViewModel model, Output output),
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

    private void ReloadPinTemplates(object? sender, EventArgs _)
    {
        // We need to refresh item templates for pin dropdowns so they are always up to date
        if (sender is not ComboBox comboBox) return;
        var t = comboBox.ItemTemplate;
        comboBox.ItemTemplate = null;
        comboBox.ItemTemplate = t;
    }
}