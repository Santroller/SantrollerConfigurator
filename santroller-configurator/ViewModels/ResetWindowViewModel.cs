using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Devices;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.ViewModels;

public enum ResetType
{
    Clear,
    Defaults,
    Cancel
}
public partial class ResetWindowViewModel : ReactiveObject
{
    [Reactive] private DeviceInputType _deviceInputType;
    public List<DeviceInputType> DeviceInputTypes { get; }

    [Reactive] private DeviceControllerType _deviceControllerType = DeviceControllerType.Gamepad;

    public List<DeviceControllerType> DeviceControllerTypes => Enum.GetValues<DeviceControllerType>().Where(s =>
        s is not (DeviceControllerType.ProGuitarMustang or DeviceControllerType.ProGuitarSquire) &&
        !s.IsFortnite()).ToList();
    [Reactive] private AccelSensorTypeMain _accelSensorTypeMain;
    
    public bool IsPico { get; }

    [Reactive] private bool _bluetoothTx;
    [Reactive] private bool _fortnite;
    [ObservableAsProperty] private bool _isBluetoothRx;
    [ObservableAsProperty] private bool _supportsFortnite;
    [ObservableAsProperty] private bool _isGuitar;
    [ObservableAsProperty] private bool _isDirect;


    public List<AccelSensorTypeMain> AccelSensorTypeMains => Enum.GetValues<AccelSensorTypeMain>().ToList();
    public readonly Interaction<Unit, Unit> CloseWindowInteraction = new();

    public string AccentedTextColor { get; }
    public ResetWindowViewModel(string accentedTextColor, IConfigurableDevice selectedDevice, DeviceInputType inputType, DeviceControllerType controllerType, bool bluetoothTx, bool fortnite)
    {
        BluetoothTx = bluetoothTx;
        Fortnite = fortnite;
        IsPico = selectedDevice.IsPico;
        DeviceInputType = inputType;
        DeviceControllerType = controllerType;
        DeviceInputTypes = Enum.GetValues<DeviceInputType>().Where(s =>
            s is not DeviceInputType.Peripheral && (
            s is not (DeviceInputType.Usb or DeviceInputType.Bluetooth) ||
            selectedDevice is {IsPico: true})).ToList();;
        AccentedTextColor = accentedTextColor;
        ClearCommand = ReactiveCommand.CreateFromObservable(() =>
        {
            Response = ResetType.Clear;
            return CloseWindowInteraction.Handle(new Unit());
        });
        DefaultCommand = ReactiveCommand.CreateFromObservable(() =>
        {
            Response = ResetType.Defaults;
            return CloseWindowInteraction.Handle(new Unit());
        });
        CancelCommand = ReactiveCommand.CreateFromObservable(() =>
        {
            Response = ResetType.Cancel;
            return CloseWindowInteraction.Handle(new Unit());
        });
        _isDirectHelper = this.WhenAnyValue(x => x.DeviceInputType)
            .Select(s => s is DeviceInputType.Direct)
            .ToProperty(this, s => s.IsDirect);
        _isBluetoothRxHelper = this.WhenAnyValue(x => x.DeviceInputType)
            .Select(s => s is DeviceInputType.Bluetooth)
            .ToProperty(this, s => s.IsBluetoothRx);
        _supportsFortniteHelper = this.WhenAnyValue(x => x.DeviceControllerType)
            .Select(s => s.Is5FretGuitar() || s.IsDrum())
            .ToProperty(this, s => s.SupportsFortnite);
        _isGuitarHelper = this.WhenAnyValue(x => x.DeviceControllerType)
            .Select(s => s.IsGuitar())
            .ToProperty(this, s => s.IsGuitar);
    }

    public ICommand ClearCommand { get; }
    public ICommand DefaultCommand { get; }
    public ICommand CancelCommand { get; }
    public ResetType Response { get; set; }
}