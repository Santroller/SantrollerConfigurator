using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.Utils;
using ProtoBuf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.ViewModels;

public partial class ConfigViewModel : ReactiveObject, IRoutableViewModel
{
    public static readonly string Apa102SpiType = "APA102";
    public static readonly string Stp16SpiType = "STP16CPC26";
    public static readonly string Stp16PeripheralSpiType = "Peripheral STP16CPC26";
    public static readonly string Apa102PeripheralSpiType = "Peripheral APA102";
    public static readonly string Stp16LeType = "STP16CPC26 Latch Enable";
    public static readonly string Stp16LePeripheralType = "Peripheral STP16CPC26 Latch Enable";
    public static readonly string Stp16OeType = "STP16CPC26 Output Enable";
    public static readonly string Stp16OePeripheralType = "Peripheral STP16CPC26 Output Enable";
    public static readonly string PeripheralTwiType = "Peripheral";
    public static readonly int PeripheralTwiClock = 500000;
    public static readonly string UsbHostPinTypeDm = "DM";
    public static readonly string UsbHostPinTypeDp = "DP";
    public static readonly string UnoPinTypeTx = "Uno Serial Tx Pin";
    public static readonly string UnoPinTypeRx = "Uno Serial Rx Pin";
    public static readonly int UnoPinTypeRxPin = 0;
    public static readonly int UnoPinTypeTxPin = 1;

    private bool _allExpanded;


    private SpiConfig? _ledSpiConfig;
    private SpiConfig? _ledSpiConfigPeripheral;
    private TwiConfig? _peripheralTwiConfig;
    private DirectPinConfig? _stp16Oe;
    private DirectPinConfig? _stp16Le;
    private DirectPinConfig? _stp16OePeripheral;
    private DirectPinConfig? _stp16LePeripheral;

    private EmulationType _emulationType;

    private LedType _ledType;
    private LedType _ledTypePeripheral;

    private bool _disconnected;

    private readonly DirectPinConfig? _unoRx;
    private readonly DirectPinConfig? _unoTx;

    private readonly DirectPinConfig _usbHostDm;
    private readonly DirectPinConfig _usbHostDp;

    public string WarningColor => Main.ProgressBarWarning;

    public string WarningTextColor =>
        MainWindowViewModel.ShouldUseDarkTextColorForBackground(WarningColor) ? "#000000" : "#FFFFFF";

    public ConfigViewModel(MainWindowViewModel screen, IConfigurableDevice device, bool branded, bool builder = false)
    {
        _device = device;
        Main = screen;
        Branded = branded;
        Builder = builder;
        _legendType = _toolConfig.LegendType;
        Presets.AddRange(_toolConfig.Presets);
        CurrentPreset = Presets.FirstOrDefault();
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Background, Diff);
        BtRxAddr = "";
        UpdateBluetoothAddress();

        HostScreen = screen;
        Microcontroller = device.GetMicrocontroller(this);
        BindableTwi = Microcontroller.TwiAssignable && !Branded;
        BindAllCommand = ReactiveCommand.CreateFromTask(BindAllAsync);

        WriteConfigCommand = ReactiveCommand.CreateFromObservable(() =>
            {
                SetUpDiff();
                return Main.Write(this, true);
            },
            this.WhenAnyValue(x => x.Main.Working, x => x.Main.Connected, x => x.HasError)
                .ObserveOn(RxApp.MainThreadScheduler).Select(x => x is {Item1: false, Item2: true, Item3: false}));

        WriteUf2Command = ReactiveCommand.CreateFromObservable(() => Main.SaveUf2(this),
            this.WhenAnyValue(x => x.Main.Working, x => x.HasError)
                .ObserveOn(RxApp.MainThreadScheduler).Select(x => x is {Item1: false, Item2: false}));
        ResetCommand = ReactiveCommand.CreateFromTask(ResetAsync,
            this.WhenAnyValue(x => x.Main.Working, x => x.Main.Connected)
                .ObserveOn(RxApp.MainThreadScheduler).Select(x => x is {Item1: false, Item2: true}));
        GoBackCommand = ReactiveCommand.Create(GoBack, this.WhenAnyValue(x => x.Main.Working).Select(s => !s));

        SaveConfigCommand = ReactiveCommand.CreateFromObservable(() => SaveConfig.Handle(this));

        LoadConfigCommand = ReactiveCommand.CreateFromObservable(() => LoadConfig.Handle(this));
        this.WhenAnyValue(x => x.Deque, x => x.PollRate)
            .Select(GeneratePollRateLabel)
            .ToPropertyEx(this, x => x.PollRateLabel);
        this.WhenAnyValue(x => x.Mode).Select(x => x is ModeType.Advanced)
            .ToPropertyEx(this, x => x.IsAdvancedMode);
        this.WhenAnyValue(x => x.Mode).Select(x => x is ModeType.Standard)
            .ToPropertyEx(this, x => x.IsStandardMode);
        this.WhenAnyValue(x => x.PresetName).Select(x =>
                String.IsNullOrWhiteSpace(x) ? Resources.SavePresetLabel :
                Presets.Any(s => s.Item1 == x) ? string.Format(Resources.SavePresetLabel3, x) :
                string.Format(Resources.SavePresetLabel2, x))
            .ToPropertyEx(this, x => x.SavePresetLabel);
        this.WhenAnyValue(x => x.CurrentPreset).Select(x =>
                x is null ? Resources.LoadPresetLabel : string.Format(Resources.LoadPresetLabel2, x.Item1))
            .ToPropertyEx(this, x => x.LoadPresetLabel);
        this.WhenAnyValue(x => x.CurrentPreset).Select(x =>
                x is null ? Resources.DeletePresetLabel : string.Format(Resources.DeletePresetLabel2, x.Item1))
            .ToPropertyEx(this, x => x.DeletePresetLabel);
        this.WhenAnyValue(x => x.EmulationType)
            .Select(x => x is EmulationType.Bluetooth or EmulationType.BluetoothKeyboardMouse)
            .ToPropertyEx(this, x => x.IsBluetoothTx);
        this.WhenAnyValue(x => x.EmulationType, x => x.Mode, x => x.DeviceControllerType)
            .Select(x => x.Item1 is EmulationType.Controller && x.Item2 is ModeType.Standard)
            .ToPropertyEx(this, x => x.SupportsDeque);
        this.WhenAnyValue(x => x.DeviceControllerType)
            .Select(x => x is DeviceControllerType.LiveGuitar or DeviceControllerType.GuitarHeroGuitar
                or DeviceControllerType.RockBandGuitar or DeviceControllerType.FortniteGuitar
                or DeviceControllerType.FortniteGuitarStrum)
            .ToPropertyEx(this, x => x.IsGuitar);
        this.WhenAnyValue(x => x.DeviceControllerType)
            .Select(x => x is DeviceControllerType.StageKit)
            .ToPropertyEx(this, x => x.IsStageKit);
        this.WhenAnyValue(x => x.EmulationType)
            .Select(x => x is EmulationType.FortniteFestival)
            .ToPropertyEx(this, x => x.IsFortniteFestival);
        _strumDebounceDisplay = this.WhenAnyValue(x => x.StrumDebounce)
            .Select(x => x / 10.0f)
            .ToProperty(this, x => x.StrumDebounceDisplay);
        _debounceDisplay = this.WhenAnyValue(x => x.Debounce)
            .Select(x => x / 10.0f)
            .ToProperty(this, x => x.DebounceDisplay);
        this.WhenAnyValue(x => x.EmulationType)
            .Select(x => GetSimpleEmulationTypeFor(x) is EmulationType.Controller)
            .ToPropertyEx(this, x => x.IsController);
        this.WhenAnyValue(x => x.EmulationType, x => x.DeviceControllerType)
            .Select(x =>
                GetSimpleEmulationTypeFor(x.Item1) is EmulationType.Controller &&
                x.Item1 is not EmulationType.FortniteFestival)
            .ToPropertyEx(this, x => x.IsStandardController);
        this.WhenAnyValue(x => x.EmulationType, x => x.DeviceControllerType)
            .Select(x =>
                GetSimpleEmulationTypeFor(x.Item1) is EmulationType.Controller &&
                x.Item2 is DeviceControllerType.Turntable or DeviceControllerType.RockBandDrums or DeviceControllerType.RockBandGuitar or DeviceControllerType.LiveGuitar)
            .ToPropertyEx(this, x => x.IsRpcs3CompatibleController);
        this.WhenAnyValue(x => x.EmulationType)
            .Select(x => GetSimpleEmulationTypeFor(x) is EmulationType.KeyboardMouse)
            .ToPropertyEx(this, x => x.IsKeyboard);
        this.WhenAnyValue(x => x.LedType)
            .Select(x => x is LedType.Apa102Bgr or LedType.Apa102Brg or LedType.Apa102Gbr or LedType.Apa102Grb
                or LedType.Apa102Rbg or LedType.Apa102Rgb)
            .ToPropertyEx(this, x => x.IsApa102);
        this.WhenAnyValue(x => x.LedTypePeripheral, x => x.HasPeripheral)
            .Select(x => x is
            {
                Item2: true, Item1: LedType.Apa102Bgr or LedType.Apa102Brg or LedType.Apa102Gbr or LedType.Apa102Grb
                or LedType.Apa102Rbg or LedType.Apa102Rgb
            })
            .ToPropertyEx(this, x => x.IsApa102Peripheral);
        this.WhenAnyValue(x => x.LedType)
            .Select(x => x is LedType.Stp16Cpc26)
            .ToPropertyEx(this, x => x.IsStp16);
        this.WhenAnyValue(x => x.LedTypePeripheral, x => x.HasPeripheral)
            .Select(x => x is
            {
                Item2: true, Item1: LedType.Stp16Cpc26
            })
            .ToPropertyEx(this, x => x.IsStp16Peripheral);

        this.WhenAnyValue(x => x.LedType)
            .Select(x => x is LedType.Apa102Bgr or LedType.Apa102Brg or LedType.Apa102Gbr or LedType.Apa102Grb
                or LedType.Apa102Rbg or LedType.Apa102Rgb or LedType.Stp16Cpc26)
            .ToPropertyEx(this, x => x.IsIndexedLed);
        this.WhenAnyValue(x => x.LedTypePeripheral, x => x.HasPeripheral)
            .Select(x => x is
            {
                Item2: true, Item1: LedType.Apa102Bgr or LedType.Apa102Brg or LedType.Apa102Gbr or LedType.Apa102Grb
                or LedType.Apa102Rbg or LedType.Apa102Rgb or LedType.Stp16Cpc26
            })
            .ToPropertyEx(this, x => x.IsIndexedLedPeripheral);
        Bindings.Connect()
            .QueryWhenChanged(s => s.Any(s2 => s2 is BluetoothOutput))
            .ToPropertyEx(this, x => x.IsBluetoothRx);
        Bindings.Connect()
            .QueryWhenChanged(s => s.Any(s2 => s2 is WiiCombinedOutput))
            .ToPropertyEx(this, x => x.HasWiiCombinedOutput);
        Bindings.Connect()
            .QueryWhenChanged(s => s.Any(s2 => s2 is Ps2CombinedOutput))
            .ToPropertyEx(this, x => x.HasPs2CombinedOutput);
        Bindings.Connect()
            .QueryWhenChanged(s => s.Any(s2 => s2 is GhwtCombinedOutput))
            .ToPropertyEx(this, x => x.HasGhwtCombinedOutput);
        Bindings.Connect()
            .QueryWhenChanged(s => s.Any(s2 => s2 is CloneCombinedOutput))
            .ToPropertyEx(this, x => x.HasCloneCombinedOutput);
        Bindings.Connect()
            .QueryWhenChanged(s => s.Any(s2 => s2 is DjCombinedOutput))
            .ToPropertyEx(this, x => x.HasDjCombinedOutput);
        Bindings.Connect()
            .QueryWhenChanged(s => s.Any(s2 => s2 is Gh5CombinedOutput))
            .ToPropertyEx(this, x => x.HasGh5CombinedOutput);
        Bindings.Connect()
            .QueryWhenChanged(s => s.Any(s2 => s2 is UsbHostCombinedOutput))
            .ToPropertyEx(this, x => x.HasUsbHostCombinedOutput);
        this.WhenAnyValue(x => x.IsBluetoothTx, x => x.IsBluetoothRx)
            .Select(x => x.Item1 || x.Item2)
            .ToPropertyEx(this, x => x.IsBluetooth);
        this.WhenAnyValue(x => x.IsBluetooth, x => x.WasBluetooth)
            .Select(x => x.Item1 || x.Item2)
            .ToPropertyEx(this, x => x.IsOrWasBluetooth);
        Bindings.Connect()
            .Filter(s => s.IsVisible)
            .Bind(out var outputs)
            .Subscribe();
        _deviceControllerTypes.AddRange(Enum.GetValues<DeviceControllerType>());
        _deviceControllerTypes.Connect().Filter(
                this.WhenAnyValue(s => s.EmulationType)
                    .Select(s =>
                        new Func<DeviceControllerType, bool>(type =>
                            s is EmulationType.FortniteFestival == type.IsFortnite())))
            .Bind(out var controllerTypes)
            .Subscribe();
        DeviceControllerRhythmTypes = controllerTypes;
        Outputs = outputs;
        AllPins = new SourceList<int>();
        AllPins.AddRange(Microcontroller.GetAllPins());
        AllPins.Connect().Filter(this.WhenAnyValue(s => s.IsBluetooth)
                .Select(s => new Func<int, bool>(pin => Microcontroller.FilterPin(false, s, true, pin))))
            .Bind(out var interruptPins)
            .Subscribe();
        AllPins.Connect().Filter(this.WhenAnyValue(s => s.IsBluetooth)
                .Select(s => new Func<int, bool>(pin => Microcontroller.FilterPin(false, s, false, pin))))
            .Bind(out var digitalPins)
            .Subscribe();
        AllPins.Connect().Filter(this.WhenAnyValue(s => s.IsBluetooth)
                .Select(s => new Func<int, bool>(pin => Microcontroller.FilterPin(true, s, false, pin))))
            .Bind(out var analogPins)
            .Subscribe();
        AvailablePinsAnalog = analogPins;
        AvailablePinsDigital = digitalPins;
        AvailablePinsInterrupt = interruptPins;
        _usbHostDm = new DirectPinConfig(this, UsbHostPinTypeDm, -1, false, DevicePinMode.Skip);
        _usbHostDp = new DirectPinConfig(this, UsbHostPinTypeDp, -1, false, DevicePinMode.Skip);
        if (!device.LoadConfiguration(this, false))
        {
            SetDefaults();
            Main.Message = "Building";
            Main.Progress = 0;
            // Write the full config, bluetooth has zero config so we can actually properly write it 
            Main.Write(this, true);
            SetUpDiff();
        }

        if (Main is {IsUno: false, IsMega: false}) return;
        _unoRx = new DirectPinConfig(this, UnoPinTypeRx, UnoPinTypeRxPin, false, DevicePinMode.Output);
        _unoTx = new DirectPinConfig(this, UnoPinTypeTx, UnoPinTypeTxPin, false, DevicePinMode.Output);
    }

    public void UpdateBluetoothAddress()
    {
        if (Device is Santroller santroller)
            LocalAddress = santroller.GetBluetoothAddress();
    }

    private static string GeneratePollRateLabel((bool dequeue, int rate) arg)
    {
        var rate = Math.Floor((1f / Math.Max(arg.rate, 1)) * 1000);
        return arg.dequeue ? $"Dequeue Rate ({rate}+ fps required)" : $"Poll Rate (0 for fastest speed) ({rate}hz)";
    }

    private IConfigurableDevice _device;

    public IConfigurableDevice Device
    {
        get => _device;
        set
        {
            if (Builder)
            {
                if (_device is Santroller santroller)
                {
                    santroller.StopTicking();
                }

                Main.ShowError = false;
                Main.Complete(100);
                Main.DeviceNotProgrammed = value is not (Santroller or EmptyDevice);
                Main.SetDifference(false);
                if (value is Santroller s)
                {
                    Microcontroller = value.GetMicrocontroller(this);
                    s.StartTicking(this);
                }
            }

            this.RaiseAndSetIfChanged(ref _device, value);
        }
    }

    [Reactive] public RolloverMode RolloverMode { get; set; }
    public IEnumerable<RolloverMode> RolloverModes => Enum.GetValues<RolloverMode>();

    [Reactive] public string? PeripheralErrorText { get; set; }
    [Reactive] public string? LedErrorText { get; set; }

    public ReadOnlyObservableCollection<Output> Outputs { get; }

    public bool Branded { get; }
    public bool Builder { get; }

    private SourceList<int> AllPins { get; }


    private readonly ObservableAsPropertyHelper<float> _debounceDisplay;
    private readonly ObservableAsPropertyHelper<float> _strumDebounceDisplay;
    private DeviceControllerType _deviceControllerType;

    public float DebounceDisplay
    {
        get => _debounceDisplay.Value;
        set => Debounce = (int) (value * 10);
    }

    public float StrumDebounceDisplay
    {
        get => _strumDebounceDisplay.Value;
        set => StrumDebounce = (int) (value * 10);
    }

    [Reactive] public string Variant { get; set; } = "";
    [Reactive] public bool SwapSwitchFaceButtons { get; set; }

    [Reactive] public bool CombinedStrumDebounce { get; set; }
    [Reactive] public string? RfErrorText { get; set; }
    [Reactive] public string? UsbHostErrorText { get; set; }

    public bool AllExpanded
    {
        get => _allExpanded;
        set
        {
            this.RaiseAndSetIfChanged(ref _allExpanded, value);
            if (value)
                ExpandAll();
            else
                CollapseAll();
        }
    }

    public Interaction<(string yesText, string noText, string text), AreYouSureWindowViewModel>
        ShowYesNoDialog { get; } = new();

    public Interaction<ConfigViewModel, ResetWindowViewModel>
        ShowResetDialog { get; } = new();

    public Interaction<(string yesText, string noText, string text), AreYouSureWindowViewModel>
        ShowUnpluggedDialog { get; } =
        new();

    public Interaction<ConfigViewModel, Unit> SaveConfig { get; } = new();
    public Interaction<ConfigViewModel, Unit> LoadConfig { get; } = new();

    public Interaction<(ConfigViewModel model, Output output),
            BindAllWindowViewModel>
        ShowBindAllDialog { get; } = new();

    public List<int> AvailableSdaPins => GetSdaPins();
    public List<int> AvailableSclPins => GetSclPins();

    private List<int> GetSdaPins()
    {
        return Microcontroller.TwiPins()
            .Where(s => s.Value is TwiPinType.Sda)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetSclPins()
    {
        return Microcontroller.TwiPins()
            .Where(s => s.Value is TwiPinType.Scl)
            .Select(s => s.Key).ToList();
    }

    public ICommand BindAllCommand { get; }

    public MainWindowViewModel Main { get; }

    private SourceList<DeviceControllerType> _deviceControllerTypes = new SourceList<DeviceControllerType>();

    public ReadOnlyObservableCollection<DeviceControllerType> DeviceControllerRhythmTypes { get; }

    public IEnumerable<ModeType> ModeTypes => Enum.GetValues<ModeType>();

    // Only Pico supports bluetooth
    public IEnumerable<EmulationType> EmulationTypes => Enum.GetValues<EmulationType>()
        .Where(type =>
            Device.IsPico() ||
            type is not (EmulationType.Bluetooth or EmulationType.BluetoothKeyboardMouse));


    public IEnumerable<MouseMovementType> MouseMovementTypes => Enum.GetValues<MouseMovementType>();
    public IEnumerable<LegendType> LegendTypes => Enum.GetValues<LegendType>();

    public ICommand WriteConfigCommand { get; }

    public ICommand WriteUf2Command { get; }

    public ICommand SaveConfigCommand { get; }
    public ICommand LoadConfigCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand GoBackCommand { get; }

    public string LocalAddress { get; set; } = "Write config to retrieve address";

    public List<int> AvailableApaMosiPins => Microcontroller.SpiPins()
        .Where(s => s.Value is SpiPinType.Mosi)
        .Select(s => s.Key).ToList();

    public List<int> AvailableApaSckPins => Microcontroller.SpiPins()
        .Where(s => s.Value is SpiPinType.Sck)
        .Select(s => s.Key).ToList();

    public IEnumerable<LedType> LedTypes => Enum.GetValues<LedType>();

    public bool BindableTwi { get; }
    [Reactive] public bool PollExpanded { get; set; }

    [Reactive] public bool PresetsExpanded { get; set; }
    [Reactive] public bool ControllerConfigExpanded { get; set; }
    [Reactive] public bool BluetoothConfigExpanded { get; set; }
    [Reactive] public bool LedConfigExpanded { get; set; }
    [Reactive] public bool PeripheralExpanded { get; set; }

    [Reactive] public MouseMovementType MouseMovementType { get; set; }

    public ModeType Mode
    {
        get => _mode;
        set
        {
            this.RaiseAndSetIfChanged(ref _mode, value);
            if (value is ModeType.Advanced)
            {
                Deque = false;
            }
        }
    }

    [Reactive] public int Debounce { get; set; }

    private ModeType _mode;
    private bool _deque;

    public bool Deque
    {
        get => _deque;
        set
        {
            if (value == _deque) return;
            this.RaiseAndSetIfChanged(ref _deque, value);
            if (value)
            {
                // If we have enabled deque, then make sure the poll rate and debounce are above the min
                PollRate = Math.Max(1, PollRate);
                Debounce = Math.Max(5, Debounce);
            }
            else
            {
                // If we have disabled deque, then round to the nearest whole debounce
                Debounce = (int) (Math.Round(Debounce / 10.0f) * 10);
            }
        }
    }

    [Reactive] public int StrumDebounce { get; set; }

    [Reactive] public int PollRate { get; set; }
    [Reactive] public int DjPollRate { get; set; }
    [Reactive] public bool DjSmoothing { get; set; }

    [Reactive] public string BtRxAddr { get; set; }

    public int LedMosi
    {
        get => _ledSpiConfig?.Mosi ?? 0;
        set
        {
            if (_ledSpiConfig == null) return;
            _ledSpiConfig.Mosi = value;
            this.RaisePropertyChanged();
        }
    }

    public int LedSck
    {
        get => _ledSpiConfig?.Sck ?? 0;
        set
        {
            if (_ledSpiConfig == null) return;
            _ledSpiConfig.Sck = value;
            this.RaisePropertyChanged();
        }
    }

    private int _ledBrightness;

    public int LedBrightness
    {
        get => _ledBrightness;
        set
        {
            this.RaiseAndSetIfChanged(ref _ledBrightness, value);
            if (Device is not Santroller santroller || (LedType is LedType.None or LedType.Stp16Cpc26 && LedTypePeripheral is LedType.None or LedType.Stp16Cpc26)) return;
            santroller.SetBrightness(value);
            foreach (var output in Bindings.Items.SelectMany(binding => binding.ValidOutputs()))
            {
                if (LedType is not (LedType.None or LedType.Stp16Cpc26) && output.LedIndices.Any() && output.LedOn != Colors.Black)
                {
                    foreach (var ledIndex in output.LedIndices)
                    {
                        santroller.SetLed((byte) (ledIndex - 1), LedType.GetLedBytes(output.LedOn));
                    }
                }
                if (LedTypePeripheral is not (LedType.None or LedType.Stp16Cpc26) && output.LedIndicesPeripheral.Any() && output.LedOn != Colors.Black)
                {
                    foreach (var ledIndex in output.LedIndicesPeripheral)
                    {
                        santroller.SetLedPeripheral((byte) (ledIndex - 1), LedTypePeripheral.GetLedBytes(output.LedOn));
                    }
                }
            }
        }
    }

    public int PeripheralSda
    {
        get => _peripheralTwiConfig?.Sda ?? 0;
        set
        {
            if (_peripheralTwiConfig == null) return;
            _peripheralTwiConfig.Sda = value;
            this.RaisePropertyChanged();
        }
    }

    public int PeripheralScl
    {
        get => _peripheralTwiConfig?.Scl ?? 0;
        set
        {
            if (_peripheralTwiConfig == null) return;
            _peripheralTwiConfig.Scl = value;
            this.RaisePropertyChanged();
        }
    }

    public int Stp16Oe
    {
        get => _stp16Oe?.Pin ?? 0;
        set
        {
            if (_stp16Oe == null) return;
            _stp16Oe.Pin = value;
            this.RaisePropertyChanged();
        }
    }

    public int Stp16Le
    {
        get => _stp16Le?.Pin ?? 0;
        set
        {
            if (_stp16Le == null) return;
            _stp16Le.Pin = value;
            this.RaisePropertyChanged();
        }
    }

    public int Stp16OePeripheral
    {
        get => _stp16OePeripheral?.Pin ?? 0;
        set
        {
            if (_stp16OePeripheral == null) return;
            _stp16OePeripheral.Pin = value;
            this.RaisePropertyChanged();
        }
    }
    
    [Reactive]
    public bool Apa102IsFullSize { get; set; }

    public int Stp16LePeripheral
    {
        get => _stp16LePeripheral?.Pin ?? 0;
        set
        {
            if (_stp16LePeripheral == null) return;
            _stp16LePeripheral.Pin = value;
            this.RaisePropertyChanged();
        }
    }

    public int LedMosiPeripheral
    {
        get => _ledSpiConfigPeripheral?.Mosi ?? 0;
        set
        {
            if (_ledSpiConfigPeripheral == null) return;
            _ledSpiConfigPeripheral!.Mosi = value;
            this.RaisePropertyChanged();
        }
    }

    public int LedSckPeripheral
    {
        get => _ledSpiConfigPeripheral?.Sck ?? 0;
        set
        {
            if (_ledSpiConfigPeripheral == null) return;
            _ledSpiConfigPeripheral!.Sck = value;
            this.RaisePropertyChanged();
        }
    }

    [Reactive] public bool Connected { get; set; }
    [Reactive] public bool PeripheralConnected { get; set; }


    public int UsbHostDm
    {
        get => _usbHostDm.Pin;
        set
        {
            _usbHostDm.Pin = value;
            _usbHostDp.Pin = value - 1;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(UsbHostDp));
            UpdateErrors();
        }
    }

    public int UsbHostDp
    {
        get => _usbHostDp.Pin;
        set
        {
            _usbHostDp.Pin = value;
            _usbHostDm.Pin = value + 1;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(UsbHostDm));
            UpdateErrors();
        }
    }

    [Reactive] public byte LedCount { get; set; }
    [Reactive] public byte LedCountPeripheral { get; set; }

    [Reactive] public int WtSensitivity { get; set; }

    [Reactive] public bool HasError { get; set; }

    public LedType LedType
    {
        get => _ledType;
        set
        {
            if (value == LedType.None)
            {
                _ledSpiConfig = null;
                UpdateErrors();
            }

            if (value != _ledType)
            {
                if (value == LedType.Stp16Cpc26)
                {
                    _ledSpiConfig = Microcontroller.AssignSpiPins(this, Apa102SpiType, false, false,
                        _ledSpiConfig != null ? LedMosi : -1, -1, _ledSpiConfig != null ? LedSck : -1, false,
                        false,
                        true,
                        Math.Min(Microcontroller.Board.CpuFreq / 2, 12000000));
                    _stp16Le = new DirectPinConfig(this, Stp16LeType, -1, false, DevicePinMode.Output);
                    _stp16Oe = new DirectPinConfig(this, Stp16OeType, -1, false, DevicePinMode.Output);
                    this.RaisePropertyChanged(nameof(Stp16Le));
                    this.RaisePropertyChanged(nameof(Stp16Oe));
                    this.RaisePropertyChanged(nameof(LedMosi));
                    this.RaisePropertyChanged(nameof(LedSck));
                    UpdateErrors();
                }
                else
                {
                    if (value != LedType.None)
                    {
                        _ledSpiConfig = Microcontroller.AssignSpiPins(this, Apa102SpiType, false, false,
                            _ledSpiConfig != null ? LedMosi : -1, -1, _ledSpiConfig != null ? LedSck : -1, true,
                            true,
                            true,
                            Math.Min(Microcontroller.Board.CpuFreq / 2, 12000000));
                        this.RaisePropertyChanged(nameof(LedMosi));
                        this.RaisePropertyChanged(nameof(LedSck));
                    }

                    _stp16Le = null;
                    _stp16Oe = null;
                    UpdateErrors();
                }
            }

            this.RaiseAndSetIfChanged(ref _ledType, value);
        }
    }

    public LedType LedTypePeripheral
    {
        get => _ledTypePeripheral;
        set
        {
            if (value == LedType.None)
            {
                _ledSpiConfigPeripheral = null;
            }
            else if (_ledTypePeripheral == LedType.None)
            {
                _ledSpiConfigPeripheral = Microcontroller.AssignSpiPins(this, Apa102PeripheralSpiType, true, false, -1,
                    -1, -1,
                    true, true,
                    true,
                    Math.Min(Microcontroller.Board.CpuFreq / 2, 12000000));
                this.RaisePropertyChanged(nameof(LedMosiPeripheral));
                this.RaisePropertyChanged(nameof(LedSckPeripheral));
                UpdateErrors();
            }

            if (value != _ledTypePeripheral)
            {
                if (value == LedType.Stp16Cpc26)
                {
                    _stp16LePeripheral =
                        new DirectPinConfig(this, Stp16LePeripheralType, -1, true, DevicePinMode.Output);
                    _stp16OePeripheral =
                        new DirectPinConfig(this, Stp16OePeripheralType, -1, true, DevicePinMode.Output);
                    this.RaisePropertyChanged(nameof(Stp16LePeripheral));
                    this.RaisePropertyChanged(nameof(Stp16OePeripheral));
                    UpdateErrors();
                }
                else
                {
                    _stp16LePeripheral = null;
                    _stp16OePeripheral = null;
                    UpdateErrors();
                }
            }

            this.RaiseAndSetIfChanged(ref _ledTypePeripheral, value);
        }
    }

    private bool _hasPeripheral;

    [Reactive] public bool XInputOnWindows { get; set; }
    
    [Reactive] public bool Ps3OnRpcs3 { get; set; }

    [Reactive] public bool XInputAuth { get; set; }

    public bool HasPeripheral
    {
        get => _hasPeripheral;
        set
        {
            if (!value)
            {
                Bindings.RemoveMany(Bindings.Items.Where(s =>
                    s.Input.Peripheral || s is GhwtCombinedOutput {Peripheral: true}));
                _peripheralTwiConfig = null;
                this.RaiseAndSetIfChanged(ref _hasPeripheral, value);
                UpdateErrors();
                LedTypePeripheral = LedType.None;
            }

            _peripheralTwiConfig =
                value
                    ? Microcontroller.AssignTwiPins(this, PeripheralTwiType, false, -1, -1, PeripheralTwiClock)
                    : null;
            this.RaisePropertyChanged(nameof(PeripheralSda));
            this.RaisePropertyChanged(nameof(PeripheralScl));
            this.RaiseAndSetIfChanged(ref _hasPeripheral, value);
            UpdateErrors();
        }
    }

    private readonly ToolConfig _toolConfig = AssetUtils.GetConfig();

    [Reactive] public string PresetName { get; set; } = "";

    private LegendType _legendType;

    public LegendType LegendType
    {
        get => _legendType;
        set
        {
            this.RaiseAndSetIfChanged(ref _legendType, value);
            _toolConfig.LegendType = value;
            AssetUtils.SaveConfig(_toolConfig);
        }
    }


    public DeviceControllerType DeviceControllerType
    {
        get => _deviceControllerType;
        set
        {
            this.RaiseAndSetIfChanged(ref _deviceControllerType, value);
            UpdateBindings();
        }
    }


    public EmulationType EmulationType
    {
        get => _emulationType;
        set => _ = SetDefaultBindingsAsync(value);
    }

    public Microcontroller Microcontroller { get; private set; }

    public SourceList<Output> Bindings { get; } = new();

    [Reactive] public Tuple<string, SerializedConfiguration>? CurrentPreset { get; set; }

    public ObservableCollection<Tuple<string, SerializedConfiguration>> Presets { get; } = new();
    public bool BindableSpi => IsPico;

    public IDisposable RegisterConnections()
    {
        return
            Main.AvailableDevices.Connect().ObserveOn(RxApp.MainThreadScheduler).Subscribe(s =>
            {
                foreach (var change in s)
                    switch (change.Reason)
                    {
                        case ListChangeReason.Add:
                            AddDevice(change.Item.Current);
                            break;
                        case ListChangeReason.Remove:
                            RemoveDevice(change.Item.Current);
                            break;
                    }
            });
    }

    // ReSharper disable UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] public string? LoadPresetLabel { get; }
    [ObservableAsProperty] public string? DeletePresetLabel { get; }
    [ObservableAsProperty] public string? SavePresetLabel { get; }
    [ObservableAsProperty] public bool IsStandardMode { get; }
    [ObservableAsProperty] public bool IsAdvancedMode { get; }
    [ObservableAsProperty] public bool IsGuitar { get; }
    [ObservableAsProperty] public bool IsStageKit { get; }
    [ObservableAsProperty] public bool IsController { get; }
    [ObservableAsProperty] public bool IsStandardController { get; }
    
    [ObservableAsProperty] public bool IsRpcs3CompatibleController { get; }
    [ObservableAsProperty] public bool IsFortniteFestival { get; }
    [ObservableAsProperty] public bool IsKeyboard { get; }
    [ObservableAsProperty] public bool IsApa102 { get; }
    [ObservableAsProperty] public bool IsApa102Peripheral { get; }

    [ObservableAsProperty] public bool IsIndexedLed { get; }
    [ObservableAsProperty] public bool IsIndexedLedPeripheral { get; }
    [ObservableAsProperty] public bool IsStp16 { get; }
    [ObservableAsProperty] public bool IsStp16Peripheral { get; }
    [ObservableAsProperty] public bool IsBluetoothTx { get; }
    [ObservableAsProperty] public bool SupportsDeque { get; }
    [ObservableAsProperty] public string? PollRateLabel { get; }
    [ObservableAsProperty] public bool IsBluetooth { get; }
    [ObservableAsProperty] public bool IsOrWasBluetooth { get; }
    [ObservableAsProperty] public bool IsBluetoothRx { get; }
    [ObservableAsProperty] public bool HasWiiCombinedOutput { get; }
    [ObservableAsProperty] public bool HasPs2CombinedOutput { get; }
    [ObservableAsProperty] public bool HasGhwtCombinedOutput { get; }
    [ObservableAsProperty] public bool HasCloneCombinedOutput { get; }
    [ObservableAsProperty] public bool HasDjCombinedOutput { get; }
    [ObservableAsProperty] public bool HasGh5CombinedOutput { get; }
    [ObservableAsProperty] public bool HasUsbHostCombinedOutput { get; }

    public bool UsbHostEnabled => Bindings.Items.Any(x =>
        x is UsbHostCombinedOutput ||
        x.Outputs.Items.Any(x2 => x2.Input.InnermostInputs().First().InputType is InputType.UsbHostInput));

    // ReSharper enable UnassignedGetOnlyAutoProperty

    private static readonly Dictionary<object, int> TypeOrder =
        Enum.GetValues<InstrumentButtonType>().Cast<object>()
            .Concat(Enum.GetValues<DjInputType>().Cast<object>())
            .Concat(Enum.GetValues<StandardButtonType>().Cast<object>())
            .Concat(Enum.GetValues<DrumAxisType>().Cast<object>())
            .Concat(Enum.GetValues<GuitarAxisType>().Cast<object>())
            .Concat(Enum.GetValues<DjAxisType>().Cast<object>())
            .Concat(Enum.GetValues<Ps3AxisType>().Cast<object>())
            .Concat(Enum.GetValues<StandardAxisType>().Cast<object>()).Select((s, index) => new {s, index})
            .ToDictionary(x => x.s, x => x.index);


    public ReadOnlyObservableCollection<int> AvailablePinsDigital { get; private set; }
    public ReadOnlyObservableCollection<int> AvailablePinsInterrupt { get; private set; }
    public ReadOnlyObservableCollection<int> AvailablePinsAnalog { get; private set; }

    // Since DM and DP need to be next to eachother, you cannot use pins at the far ends
    public List<int> AvailablePinsDm => AvailablePinsDigital.Skip(1).ToList();
    public List<int> AvailablePinsDp => AvailablePinsDigital.SkipLast(1).ToList();

    public IEnumerable<PinConfig> PinConfigs =>
        new PinConfig?[]
            {
                _ledSpiConfig, _usbHostDm, _usbHostDp, _unoRx, _unoTx, _peripheralTwiConfig,
                _ledSpiConfigPeripheral, _stp16Le, _stp16Oe, _stp16LePeripheral, _stp16OePeripheral
            }.Where(s => s != null)
            .Cast<PinConfig>();

    public string UrlPathSegment { get; } = Guid.NewGuid().ToString()[..5];

    public IScreen HostScreen { get; }
    public bool IsPico => Device.IsPico();

    public void SetDeviceTypeAndRhythmTypeWithoutUpdating(DeviceControllerType type, EmulationType emulationType)
    {
        this.RaiseAndSetIfChanged(ref _deviceControllerType, type, nameof(DeviceControllerType));
        this.RaiseAndSetIfChanged(ref _emulationType, emulationType, nameof(EmulationType));
    }

    public void UpdateBindings()
    {
        foreach (var binding in Bindings.Items) binding.UpdateBindings();
        InstrumentButtonTypeExtensions.ConvertBindings(Bindings, this, false);
        if (!IsGuitar || EmulationType is EmulationType.FortniteFestival)
        {
            Deque = false;
        }

        if (EmulationType is EmulationType.FortniteFestival)
        {
            Bindings.RemoveMany(Bindings.Items.Where(s => s is Led));
        }

        var (extra, types) =
            ControllerEnumConverter.FilterValidOutputs(_deviceControllerType, Bindings.Items);
        Bindings.RemoveMany(extra);

        // If the user has a ps2 or wii combined output mapped, they don't need the default bindings
        if (Bindings.Items.Any(s =>
                s is WiiCombinedOutput or Ps2CombinedOutput or UsbHostCombinedOutput or BluetoothOutput)) return;


        if (_deviceControllerType == DeviceControllerType.Turntable)
        {
            if (!Bindings.Items.Any(s => s is DjCombinedOutput))
            {
                var dj = new DjCombinedOutput(this, false);
                Bindings.Add(dj);
                dj.SetOutputsOrDefaults(Array.Empty<Output>());
            }
        }
        else
        {
            Bindings.RemoveMany(Bindings.Items.Where(s => s is DjCombinedOutput));
        }

        if (!_deviceControllerType.Is5FretGuitar() && _deviceControllerType.IsDrum())
            Bindings.RemoveMany(Bindings.Items.Where(s => s is EmulationMode {Type: EmulationModeType.Wii}));

        if (_deviceControllerType is DeviceControllerType.Turntable)
            Bindings.RemoveMany(Bindings.Items.Where(s => s is EmulationMode
            {
                Type: EmulationModeType.Ps4Or5 or EmulationModeType.XboxOne
            }));

        if (_deviceControllerType.IsDrum())
        {
            IEnumerable<DrumAxisType> difference =
                DrumAxisTypeMethods.GetDifferenceFor(_deviceControllerType).ToHashSet();
            Bindings.RemoveMany(Bindings.Items.Where(s => s is DrumAxis axis && difference.Contains(axis.Type)));
        }
        else
        {
            Bindings.RemoveMany(Bindings.Items.Where(s => s is DrumAxis));
        }

        if (_deviceControllerType.IsGuitar())
        {
            IEnumerable<GuitarAxisType> difference = GuitarAxisTypeMethods
                .GetDifferenceFor(_deviceControllerType).ToHashSet();
            Bindings.RemoveMany(Bindings.Items.Where(s => s is GuitarAxis axis && difference.Contains(axis.Type)));
        }
        else
        {
            Bindings.RemoveMany(Bindings.Items.Where(s => s is GuitarAxis));
        }

        if (_deviceControllerType is not DeviceControllerType.RockBandGuitar)
            Bindings.RemoveMany(Bindings.Items.Where(s => s is EmulationMode {Type: EmulationModeType.Wii}));

        if (!_deviceControllerType.IsGuitar())
            Bindings.RemoveMany(Bindings.Items.Where(s => s is GuitarButton));

        foreach (var type in types)
            switch (type)
            {
                case StandardButtonType buttonType:
                    Bindings.Add(new ControllerButton(this,
                        new DirectInput(-1, false, false, DevicePinMode.PullUp, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), 1, buttonType, false));
                    break;
                case InstrumentButtonType buttonType:
                    Bindings.Add(new GuitarButton(this,
                        new DirectInput(-1, false, false, DevicePinMode.PullUp, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), 1, buttonType, false));
                    break;
                case StandardAxisType axisType:
                    Bindings.Add(new ControllerAxis(this,
                        new DirectInput(-1, false, false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), ushort.MinValue,
                        ushort.MaxValue,
                        0, ushort.MaxValue, axisType, false));
                    break;
                case GuitarAxisType.Slider:
                    break;
                case GuitarAxisType axisType:
                    Bindings.Add(new GuitarAxis(this, new DirectInput(-1,
                            false, false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), ushort.MinValue,
                        ushort.MaxValue,
                        0, false, axisType, false));
                    break;
                case DrumAxisType axisType:
                    Bindings.Add(new DrumAxis(this,
                        new DirectInput(-1, false, false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), ushort.MinValue,
                        ushort.MaxValue,
                        0, 10, axisType, false));
                    break;
                case DjAxisType.EffectsKnob:
                    Bindings.Add(new DjAxis(this,
                        new DirectInput(-1, false, false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), 1, 1,
                        DjAxisType.EffectsKnob,
                        false));
                    break;
                case DjAxisType axisType:
                    if (axisType is DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity) continue;
                    Bindings.Add(new DjAxis(this,
                        new DirectInput(-1, false, false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), ushort.MinValue,
                        ushort.MaxValue, 0, axisType,
                        false));
                    break;
            }

        Bindings.Edit(s =>
        {
            var sorted = s.OrderBy(s2 => TypeOrder.GetValueOrDefault(s2.GetOutputType())).ToList();
            s.Clear();
            s.AddRange(sorted);
        });
    }

    public void SetDefaults()
    {
        ClearOutputs();
        Deque = false;
        LedType = LedType.None;
        LedTypePeripheral = LedType.None;
        LedCount = 1;
        LedCountPeripheral = 1;
        _deviceControllerType = DeviceControllerType.Gamepad;
        CombinedStrumDebounce = false;
        WtSensitivity = 5;
        PollRate = 0;
        StrumDebounce = 0;
        Debounce = 10;
        DjPollRate = 10;
        LedBrightness = 31;
        Apa102IsFullSize = false;
        DjSmoothing = false;
        SwapSwitchFaceButtons = false;
        HasPeripheral = false;
        BtRxAddr = "";
        this.RaisePropertyChanged(nameof(DeviceControllerType));
        this.RaisePropertyChanged(nameof(EmulationType));
        XInputOnWindows = true;
        Ps3OnRpcs3 = true;
        XInputAuth = true;
        MouseMovementType = MouseMovementType.Relative;

        switch (Main.DeviceInputType)
        {
            case DeviceInputType.Direct:
                _ = SetDefaultBindingsAsync(EmulationType);
                break;
            case DeviceInputType.Wii:
                var output = new WiiCombinedOutput(this, false)
                {
                    Expanded = true
                };
                Bindings.Add(output);
                output.SetOutputsOrDefaults(Array.Empty<Output>());
                break;
            case DeviceInputType.Ps2:
                var ps2Output = new Ps2CombinedOutput(this, false)
                {
                    Expanded = true
                };
                Bindings.Add(ps2Output);
                ps2Output.SetOutputsOrDefaults(Array.Empty<Output>());
                break;
            case DeviceInputType.Usb:
                var usbOutput = new UsbHostCombinedOutput(this)
                {
                    Expanded = true
                };
                Bindings.Add(usbOutput);
                usbOutput.SetOutputsOrDefaults(Array.Empty<Output>());
                break;
            case DeviceInputType.Bluetooth:
                var bluetoothOutput = new BluetoothOutput(this)
                {
                    Expanded = true
                };
                Bindings.Add(bluetoothOutput);
                bluetoothOutput.SetOutputsOrDefaults(Array.Empty<Output>());
                break;
            case DeviceInputType.Peripheral:
                HasPeripheral = true;
                PeripheralScl = Main.PeripheralScl;
                PeripheralSda = Main.PeripheralSda;
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }


        UpdateBindings();
        UpdateErrors();
    }

    private async Task SetDefaultBindingsAsync(EmulationType emulationType)
    {
        if (emulationType is EmulationType.Bluetooth or EmulationType.BluetoothKeyboardMouse)
        {
            ResetBluetoothRelated();
        }

        if (DeviceControllerType.IsDrum() && emulationType == EmulationType.FortniteFestival)
        {
            _emulationType = emulationType;
            this.RaisePropertyChanged(nameof(EmulationType));
            DeviceControllerType = DeviceControllerType.FortniteDrums;
            return;
        }

        if (emulationType == EmulationType.FortniteFestival)
        {
            _emulationType = emulationType;
            this.RaisePropertyChanged(nameof(EmulationType));
            DeviceControllerType = DeviceControllerType.FortniteGuitar;
            return;
        }

        if (DeviceControllerType.IsGuitar() && emulationType == EmulationType.Controller)
        {
            _emulationType = emulationType;
            this.RaisePropertyChanged(nameof(EmulationType));
            DeviceControllerType = DeviceControllerType.GuitarHeroGuitar;
            return;
        }

        if (DeviceControllerType.IsDrum() && emulationType == EmulationType.Controller)
        {
            _emulationType = emulationType;
            this.RaisePropertyChanged(nameof(EmulationType));
            DeviceControllerType = DeviceControllerType.GuitarHeroDrums;
            return;
        }

        // If going from say bluetooth controller to standard controller, the pin bindings can stay
        if (GetSimpleEmulationTypeFor(EmulationType) == GetSimpleEmulationTypeFor(emulationType))
        {
            _emulationType = emulationType;
            this.RaisePropertyChanged(nameof(EmulationType));
            UpdateErrors();
            return;
        }

        if (Bindings.Items.Any())
        {
            var yesNo = await ShowYesNoDialog.Handle(("Clear", "Cancel",
                "The following action will clear all your bindings, are you sure you want to do this?")).ToTask();
            if (!yesNo.Response)
            {
                var last = _emulationType;
                _emulationType = emulationType;
                this.RaisePropertyChanged(nameof(EmulationType));
                _emulationType = last;
                this.RaisePropertyChanged(nameof(EmulationType));
                return;
            }
        }

        _emulationType = emulationType;
        this.RaisePropertyChanged(nameof(EmulationType));
        ClearOutputs();

        if (GetSimpleEmulationType() is EmulationType.KeyboardMouse) return;

        foreach (var type in Enum.GetValues<StandardAxisType>())
        {
            if (!ControllerEnumConverter.Convert(type, _deviceControllerType, LegendType, SwapSwitchFaceButtons)
                    .Any()) continue;
            var isTrigger = type is StandardAxisType.LeftTrigger or StandardAxisType.RightTrigger;
            Bindings.Add(new ControllerAxis(this,
                new DirectInput(-1, false, false, DevicePinMode.Analog, this),
                Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(),
                isTrigger ? ushort.MinValue : short.MinValue,
                isTrigger ? ushort.MaxValue : short.MaxValue, 0,
                ushort.MaxValue, type, false));
        }

        foreach (var type in Enum.GetValues<StandardButtonType>())
        {
            if (!ControllerEnumConverter.Convert(type, _deviceControllerType, LegendType, SwapSwitchFaceButtons)
                    .Any()) continue;
            Bindings.Add(new ControllerButton(this,
                new DirectInput(-1, false, false, DevicePinMode.PullUp, this),
                Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), 1, type, false));
        }

        UpdateErrors();
    }

    public void ResetBluetoothRelated()
    {
        // When changing to bluetooth mode, reset any pins related to bluetooth.
        foreach (var pinConfig in GetPinConfigs())
        {
            if (pinConfig is not DirectPinConfig direct) continue;
            if (direct.Pin is 23 or 24 or 25 or 29)
            {
                direct.Pin = -1;
            }
        }

        Bindings.RemoveMany(Bindings.Items.Where(s => s is BluetoothOutput));
    }

    public static string WriteBlob(BinaryWriter writer, byte[] data)
    {
        var pos = writer.BaseStream.Length;
        writer.Write(data);
        return $"config_blobs[{pos}]";
    }

    public static string WriteBlob(BinaryWriter writer, byte data)
    {
        var pos = writer.BaseStream.Length;
        writer.Write(data);
        return $"config_blobs[{pos}]";
    }

    public static string WriteBlob(BinaryWriter writer, bool data)
    {
        return WriteBlob(writer, data ? 1 : 0);
    }

    public static string WriteBlob(BinaryWriter writer, int data)
    {
        var pos = writer.BaseStream.Length;
        writer.Write((short) data);
        return $"read_int16({pos})";
    }

    public static string WriteBlob(BinaryWriter writer, uint data)
    {
        var pos = writer.BaseStream.Length;
        writer.Write((ushort) data);
        return $"read_uint16({pos})";
    }

    public string Generate(MemoryStream? blobStream)
    {
        if (Device is Santroller santroller)
        {
            santroller.StopTicking();
        }

        BinaryWriter? writer = null;
        var outputs = Bindings.Items.SelectMany(binding => binding.Outputs.Items).ToList();
        var inputs = outputs.SelectMany(binding => binding.Input.InnermostInputs()).ToList();
        var directInputs = inputs.OfType<DirectInput>().ToList();
        string config;
        int configLength;
        using (var outputStream = new MemoryStream())
        {
            using (var compressStream = new BrotliStream(outputStream, CompressionLevel.SmallestSize))
            {
                Serializer.Serialize(compressStream, new SerializedConfiguration(this));
            }

            config =
                $"#define CONFIGURATION {{{string.Join(",", outputStream.ToArray().Select(b => "0x" + b.ToString("X")))}}}";
            config += "\n";
            configLength = outputStream.ToArray().Length;
        }

        if (blobStream != null)
        {
            writer = new BinaryWriter(blobStream);
            config += $"""
                       #define CONFIGURABLE_BLOBS
                       #define CONFIGURATION_LEN {WriteBlob(writer, configLength)}
                       #define SWAP_SWITCH_FACE_BUTTONS {WriteBlob(writer, SwapSwitchFaceButtons)}
                       #define WINDOWS_USES_XINPUT {WriteBlob(writer, XInputOnWindows && IsStandardController)}
                       #define RPCS3_COMPAT {WriteBlob(writer, Ps3OnRpcs3 && IsRpcs3CompatibleController)}
                       #define XINPUT_AUTH {WriteBlob(writer, XInputAuth && UsbHostEnabled)}
                       #define INPUT_QUEUE {WriteBlob(writer, Deque)}
                       #define POLL_RATE {WriteBlob(writer, (byte) PollRate)}
                       #define INPUT_DJ_TURNTABLE_POLL_RATE {WriteBlob(writer, (byte) DjPollRate)}
                       #define INPUT_DJ_TURNTABLE_SMOOTHING {WriteBlob(writer, DjSmoothing)}
                       #define WT_SENSITIVITY {WriteBlob(writer, WtSensitivity)}
                       #define LED_BRIGHTNESS {WriteBlob(writer, LedBrightness)}
                       """;

            if (IsBluetoothRx)
            {
                var addr = new byte[Santroller.BtAddressLength];
                // If we have a valid bluetooth address, write it
                if (BtRxAddr.Any() && BtRxAddr.Contains(":"))
                {
                    addr = Encoding.UTF8.GetBytes(BtRxAddr);
                }

                config += $"""

                           #define BT_ADDR {WriteBlob(writer, addr)}
                           """;
            }
        }
        else
        {
            config += $"""
                       #define CONFIGURATION_LEN {configLength}
                       #define SWAP_SWITCH_FACE_BUTTONS {(!SwapSwitchFaceButtons).ToString().ToLower()}
                       #define WINDOWS_USES_XINPUT {(XInputOnWindows && IsStandardController).ToString().ToLower()}
                       #define RPCS3_COMPAT {(Ps3OnRpcs3 && IsRpcs3CompatibleController).ToString().ToLower()}
                       #define XINPUT_AUTH {(XInputAuth && UsbHostEnabled).ToString().ToLower()}
                       #define INPUT_QUEUE {Deque.ToString().ToLower()}
                       #define POLL_RATE {PollRate}
                       #define WT_SENSITIVITY {WtSensitivity}
                       #define INPUT_DJ_TURNTABLE_POLL_RATE {DjPollRate * 1000}
                       #define INPUT_DJ_TURNTABLE_SMOOTHING {DjSmoothing.ToString().ToLower()}
                       #define LED_BRIGHTNESS {LedBrightness}
                       """;
            if (BtRxAddr.Any() && BtRxAddr.Contains(":"))
            {
                config += $"""

                           #define BT_ADDR "{BtRxAddr}"
                           """;
            }
        }

        config += "\n";
        config += $"""
                   #define ABSOLUTE_MOUSE_COORDS {(MouseMovementType == MouseMovementType.Absolute).ToString().ToLower()}
                   #define ARDWIINO_BOARD "{Microcontroller.Board.ArdwiinoName}"
                   #define EMULATION_TYPE {GetEmulationType()}
                   #define DEVICE_TYPE {(byte) DeviceControllerType}
                   """;

        // Actually write the config as configured
        if (!HasError)
        {
            // Sort by pin index, and then map to adc number and turn into an array
            var analogPins = directInputs.Where(s => s.IsAnalog).OrderBy(s => s.PinConfig.Pin)
                .Select(s => Microcontroller.GetChannel(s.PinConfig.Pin, false).ToString()).Distinct().ToList();
            var ledInit = "";
            if (IsStp16)
            {
                ledInit += $"""

                            {Microcontroller.GenerateDigitalWrite(Stp16Le, false, false)};
                            {Microcontroller.GenerateDigitalWrite(Stp16Oe, false, false)};
                            """;
            }

            if (IsStp16Peripheral)
            {
                ledInit += $"""

                            {Microcontroller.GenerateDigitalWrite(Stp16LePeripheral, false, true)};
                            {Microcontroller.GenerateDigitalWrite(Stp16OePeripheral, false, true)};
                            """;
            }

            ledInit = GenerateLedInit() + "\\\n\t" + GenerateTick(ConfigField.InitLed, writer) + "\\\n\t" +
                      FixNewlines(ledInit);
            config += "\n";
            config += $$"""
                        #define USB_HOST_STACK {{UsbHostEnabled.ToString().ToLower()}}
                        #define USB_HOST_DP_PIN {{UsbHostDp}}
                        #define DIGITAL_COUNT {{CalculateDebounceTicks()}}
                        #define LED_COUNT {{(LedType is not (LedType.None or LedType.Stp16Cpc26) ? LedCount : 0)}}
                        #define LED_COUNT_PERIPHERAL {{(LedTypePeripheral is not (LedType.None or LedType.Stp16Cpc26) ? LedCountPeripheral : 0)}}
                        #define LED_COUNT_STP {{(LedType is LedType.Stp16Cpc26 ? LedCount : 0)}}
                        #define LED_COUNT_PERIPHERAL_STP {{(LedTypePeripheral is LedType.Stp16Cpc26 ? LedCountPeripheral : 0)}}
                        #define ADC_PINS {{{string.Join(",", analogPins)}}}
                        #define ADC_COUNT {{analogPins.Count}}
                        #define TICK_SHARED \
                            {{GenerateTick(ConfigField.Shared, writer)}}
                        #define TICK_DETECTION \
                            {{GenerateTick(ConfigField.Detection, writer)}}
                        #define TICK_RESET \
                            {{GenerateTick(ConfigField.Reset, writer)}}
                        #define TICK_PS3 \
                            {{GenerateTick(ConfigField.Ps3, writer)}}
                        #define TICK_PS3_WITHOUT_CAPTURE \
                            {{GenerateTick(ConfigField.Ps3WithoutCapture, writer)}}
                        #define TICK_PC \
                            {{GenerateTick(ConfigField.Universal, writer)}}
                        #define TICK_PS4 \
                            {{GenerateTick(ConfigField.Ps4, writer)}}
                        #define TICK_XINPUT \
                            {{GenerateTick(ConfigField.Xbox360, writer)}}
                        #define TICK_XBOX_ONE \
                            {{GenerateTick(ConfigField.XboxOne, writer)}}
                        #define HANDLE_AUTH_LED \
                            {{GenerateTick(ConfigField.AuthLed, writer)}}
                        #define HANDLE_PLAYER_LED \
                            {{GenerateTick(ConfigField.PlayerLed, writer)}}
                        #define HANDLE_LIGHTBAR_LED \
                            {{GenerateTick(ConfigField.LightBarLed, writer)}}
                        #define HANDLE_RUMBLE \
                            {{GenerateTick(ConfigField.RumbleLed, writer)}}
                        #define HANDLE_KEYBOARD_LED \
                            {{GenerateTick(ConfigField.KeyboardLed, writer)}}
                        #define PIN_INIT_PERIPHERAL \
                            {{GenerateInitPeripheral()}}
                        #define PIN_INIT \
                            {{GenerateInit()}}
                        #define LED_INIT \
                            {{ledInit}}
                        """;

            var keyboardTick = GenerateTick(ConfigField.Keyboard, writer);
            if (IsKeyboard || IsFortniteFestival)
            {
                if (keyboardTick.Any())
                {
                    switch (RolloverMode)
                    {
                        case RolloverMode.Nkro:
                            config += $"""

                                       #define TICK_NKRO \
                                           {keyboardTick}
                                       """;
                            break;
                        case RolloverMode.SixKro:
                            config += $"""

                                       #define TICK_SIXKRO \
                                           {keyboardTick}
                                       """;
                            break;
                    }
                }
                else
                {
                    config += $"""

                               #define TICK_NKRO
                               """;
                }
            }

            var consumerTick = GenerateTick(ConfigField.Consumer, writer);
            if (consumerTick.Any())
                config += $"""

                           #define TICK_CONSUMER \
                               {consumerTick}
                           """;

            var mouseTick = GenerateTick(ConfigField.Mouse, writer);
            if (mouseTick.Any())
                config += $"""

                           #define TICK_MOUSE \
                               {mouseTick}
                           """;

            if (IsApa102)
            {
                config += $"""

                           #define TICK_LED \
                               {GenerateApa102LedTick()}
                           """;
            }

            if (IsApa102Peripheral)
            {
                config += $"""

                           #define TICK_LED_PERIPHERAL \
                               {GenerateApa102LedPeripheralTick()}
                           """;
            }

            if (IsStp16)
            {
                config += $"""

                           #define TICK_LED \
                               {GenerateStp16LedTick()}
                           """;
            }

            if (IsStp16Peripheral)
            {
                config += $"""

                           #define TICK_LED_PERIPHERAL \
                               {GenerateStp16LedPeripheralTick()}
                           """;
            }

            var ledTick = GenerateTick(ConfigField.StrobeLed, writer);
            if (ledTick.Any())
            {
                config += $"""

                           #define TICK_LED_STROBE \
                               {ledTick}
                           """;
            }


            var offLed = GenerateTick(ConfigField.OffLed, writer);
            if (offLed.Any())
                config += $"""

                           #define HANDLE_LED_RUMBLE_OFF \
                               {offLed}
                           """;
            if (EmulationType is EmulationType.Bluetooth or EmulationType.BluetoothKeyboardMouse)
            {
                config += $"""

                           #define BLUETOOTH_TX {(EmulationType is EmulationType.Bluetooth or EmulationType.BluetoothKeyboardMouse).ToString().ToLower()}
                           """;
            }

            var actualPinConfigs = GetPinConfigs();
            if (_peripheralTwiConfig != null)
            {
                // If the peripheral is on the same pins as something else, then use the config from the other device.
                if (actualPinConfigs.Any(s =>
                        s is TwiConfig && s != _peripheralTwiConfig &&
                        s.Pins.Intersect(_peripheralTwiConfig.Pins).Any()))
                {
                    actualPinConfigs.Remove(_peripheralTwiConfig);
                }
            }

            var test = string.Join("\n", inputs.SelectMany(s => s.RequiredDefines()).Distinct()
                .Select(define => $"#define {define}"));
            config += $"""

                       {actualPinConfigs.Aggregate("", (current, pinConfig) => current + pinConfig.Generate())}
                       {test}
                       """;
            if (_peripheralTwiConfig != null)
            {
                config += $"\n#define SLAVE_TWI_PORT {_peripheralTwiConfig.Definition}";
            }

            if (_ledSpiConfig != null)
            {
                config += $"\n#define APA102_SPI_PORT {_ledSpiConfig.Definition}";
            }
        }
        else
        {
            // Write an empty config - the config at this point is likely invalid and won't compile
            config += """

                      #define USB_HOST_STACK false
                      #define USB_HOST_DP_PIN 0
                      #define TICK_SHARED
                      #define TICK_DETECTION
                      #define TICK_PC
                      #define TICK_PS3
                      #define TICK_PS3_WITHOUT_CAPTURE
                      #define TICK_PS4
                      #define TICK_XINPUT
                      #define TICK_RESET
                      #define TICK_XBOX_ONE
                      #define DIGITAL_COUNT 0
                      #define LED_COUNT 0
                      #define LED_COUNT_PERIPHERAL 0
                      #define HANDLE_AUTH_LED
                      #define HANDLE_PLAYER_LED
                      #define HANDLE_LIGHTBAR_LED
                      #define HANDLE_RUMBLE
                      #define HANDLE_KEYBOARD_LED
                      #define ADC_PINS {}
                      #define ADC_COUNT 0
                      #define PIN_INIT
                      #define PIN_INIT_PERIPHERAL
                      #define LED_INIT
                      """;
        }

        return config;
    }

    private string GenerateLedInit()
    {
        return FixNewlines(Microcontroller.GenerateLedInit(this));
    }

    private string GenerateInit()
    {
        return FixNewlines(Microcontroller.GenerateInit(this));
    }

    private string GenerateInitPeripheral()
    {
        return FixNewlines(GetPinConfigs().OfType<DirectPinConfig>()
            .Where(s => s.PinMode != DevicePinMode.Skip && s.Peripheral).Aggregate("",
                (current, pin) => current + $"\nslavePinMode({pin.Pin},{(byte) pin.PinMode});"));
    }

    public PinConfig[] UsbHostPinConfigs()
    {
        return UsbHostEnabled ? new PinConfig[] {_usbHostDm, _usbHostDp} : Array.Empty<PinConfig>();
    }

    private byte GetEmulationType()
    {
        return (byte) GetSimpleEmulationType();
    }

    private EmulationType GetSimpleEmulationTypeFor(EmulationType type)
    {
        switch (type)
        {
            case EmulationType.Bluetooth:
            case EmulationType.Controller:
            case EmulationType.FortniteFestival:
                return EmulationType.Controller;
            case EmulationType.KeyboardMouse:
            case EmulationType.BluetoothKeyboardMouse:
                return EmulationType.KeyboardMouse;
            default:
                return EmulationType;
        }
    }

    public EmulationType GetSimpleEmulationType()
    {
        return GetSimpleEmulationTypeFor(EmulationType);
    }

    private async Task BindAllAsync()
    {
        foreach (var binding in Bindings.Items)
        {
            if (binding.Input.InnermostInputs().First() is not DirectInput) continue;
            var response = await ShowBindAllDialog.Handle((this, binding)).ToTask();
            if (!response.Response) return;
        }
    }

    public void RemoveOutput(Output output)
    {
        if (Bindings.Remove(output))
        {
            UpdateErrors();
            return;
        }

        foreach (var binding in Bindings.Items) binding.Outputs.Remove(output);

        UpdateErrors();
    }

    [RelayCommand]
    public void LoadPreset()
    {
        CurrentPreset!.Item2.LoadConfiguration(this);
        Main.Write(this, true);
    }

    [RelayCommand]
    public void DeletePreset()
    {
        Presets.Remove(CurrentPreset!);
        var test = PresetName;
        PresetName = "";
        PresetName = test;
        CurrentPreset = Presets.FirstOrDefault();
        _toolConfig.Presets.Clear();
        _toolConfig.Presets.AddRange(Presets);
        AssetUtils.SaveConfig(_toolConfig);
    }

    [RelayCommand]
    public void SavePreset()
    {
        var config = new SerializedConfiguration(this);
        Presets.RemoveMany(Presets.Where(s => s.Item1 == PresetName));
        Presets.Add(new(PresetName, config));
        var test = PresetName;
        PresetName = "";
        PresetName = test;
        CurrentPreset ??= Presets.First();
        _toolConfig.Presets.Clear();
        _toolConfig.Presets.AddRange(Presets);
        AssetUtils.SaveConfig(_toolConfig);
    }

    [RelayCommand]
    public void ClearOutputs()
    {
        Bindings.Clear();
        UpdateErrors();
    }

    [RelayCommand]
    public void ExpandAll()
    {
        foreach (var binding in Bindings.Items) binding.Expanded = true;
        PeripheralExpanded = true;
        PollExpanded = true;
        PresetsExpanded = true;
        LedConfigExpanded = true;
        ControllerConfigExpanded = true;
        BluetoothConfigExpanded = true;
    }

    [RelayCommand]
    public void CollapseAll()
    {
        foreach (var binding in Bindings.Items) binding.Expanded = false;
        PeripheralExpanded = false;
        PollExpanded = false;
        PresetsExpanded = false;
        LedConfigExpanded = false;
        ControllerConfigExpanded = false;
        BluetoothConfigExpanded = false;
    }

    [RelayCommand]
    public async Task ResetWithConfirmationAsync()
    {
        var response = await ShowResetDialog.Handle(this);
        switch (response.Response)
        {
            case ResetType.Clear:
                Bindings.Clear();
                UpdateErrors();
                break;
            case ResetType.Defaults:
                Bindings.Clear();
                Deque = false;
                LedType = LedType.None;
                LedTypePeripheral = LedType.None;
                LedCount = 1;
                LedCountPeripheral = 1;
                CombinedStrumDebounce = false;
                WtSensitivity = 5;
                PollRate = 0;
                StrumDebounce = 0;
                Debounce = 10;
                DjPollRate = 10;
                DjSmoothing = false;
                SwapSwitchFaceButtons = false;
                HasPeripheral = false;
                BtRxAddr = "";
                XInputOnWindows = true;
                MouseMovementType = MouseMovementType.Relative;
                UpdateBindings();
                UpdateErrors();
                break;
            case ResetType.Cancel:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task ResetAsync()
    {
        var yesNo = await ShowYesNoDialog.Handle(("Revert", "Cancel",
                "The following action will revert your device back to an Arduino, are you sure you want to do this?"))
            .ToTask();
        if (!yesNo.Response) return;
        if (Device is not Santroller device) return;
        if (Builder)
        {
            device.Revert();
            return;
        } 
        await Main.RevertCommand.Execute(device);
    }

    [RelayCommand]
    public void AddOutput()
    {
        if (IsController)
            Bindings.Add(new EmptyOutput(this));
        else if (IsKeyboard)
            Bindings.Add(new KeyboardButton(this, new DirectInput(0, false, false, DevicePinMode.PullUp, this),
                Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), 1, Key.Space));

        UpdateErrors();
    }

    private static string FixNewlines(string code)
    {
        return NewlineRegex().Replace(code.Replace("\r", "").Trim(), "").Replace("\n", "\\\n    ");
    }

    private string GenerateApa102LedTick()
    {
        var outputs = Bindings.Items.SelectMany(binding => binding.ValidOutputs()).ToList();
        if (!outputs.Any(s => s.LedIndices.Any())) return "";
        if (LedType == LedType.None) return "";
        var ret = "";
        var ledMax = LedCount;
        ret +=
            """

            spi_transfer(APA102_SPI_PORT, 0x00);
            spi_transfer(APA102_SPI_PORT, 0x00);
            spi_transfer(APA102_SPI_PORT, 0x00);
            spi_transfer(APA102_SPI_PORT, 0x00);
            """;
        for (var i = 0; i < ledMax; i++)
        {
            ret +=
                $"""

                 spi_transfer(APA102_SPI_PORT, brightness | 0xE0);
                 spi_transfer(APA102_SPI_PORT, ledState[{i}].r);
                 spi_transfer(APA102_SPI_PORT, ledState[{i}].g);
                 spi_transfer(APA102_SPI_PORT, ledState[{i}].b);
                 """;
        }

        for (var i = 0; i <= ledMax; i += 16)
        {
            ret += """

                   spi_transfer(APA102_SPI_PORT, 0xff);
                   """;
        }

        return FixNewlines(ret);
    }

    private string GenerateApa102LedPeripheralTick()
    {
        var outputs = Bindings.Items.SelectMany(binding => binding.ValidOutputs()).ToList();
        if (!outputs.Any(s => s.LedIndicesPeripheral.Any())) return "";
        if (LedTypePeripheral == LedType.None) return "";
        var ret = "";
        var ledMax = LedCountPeripheral;
        ret +=
            """

            slaveWriteLED(0x00);
            slaveWriteLED(0x00);
            slaveWriteLED(0x00);
            slaveWriteLED(0x00);
            """;
        for (var i = 0; i < ledMax; i++)
        {
            ret +=
                $"""

                 slaveWriteLED(brightness | 0xE0);
                 slaveWriteLED(ledStatePeripheral[{i}].r);
                 slaveWriteLED(ledStatePeripheral[{i}].g);
                 slaveWriteLED(ledStatePeripheral[{i}].b);
                 """;
        }

        for (var i = 0; i <= ledMax; i += 16)
        {
            ret += """

                   slaveWriteLED(0xff);
                   """;
        }


        return FixNewlines(ret);
    }

    private string GenerateStp16LedTick()
    {
        var outputs = Bindings.Items.SelectMany(binding => binding.ValidOutputs()).ToList();
        if (!outputs.Any(s => s.LedIndices.Any())) return "";
        if (LedType == LedType.None) return "";
        var ret = "";
        for (var i = 0; i < Math.Ceiling(LedCount / 8f); i++)
        {
            ret +=
                $"""

                 spi_transfer(APA102_SPI_PORT, ledState[{i}]);
                 """;
        }

        ret +=
            $"""

             {Microcontroller.GenerateDigitalWrite(Stp16Le, true, false)};
             delayMicroseconds(10);
             {Microcontroller.GenerateDigitalWrite(Stp16Le, false, false)};
             """;


        return FixNewlines(ret);
    }

    private string GenerateStp16LedPeripheralTick()
    {
        var outputs = Bindings.Items.SelectMany(binding => binding.ValidOutputs()).ToList();
        if (!outputs.Any(s => s.LedIndicesPeripheral.Any())) return "";
        if (LedTypePeripheral == LedType.None) return "";
        var ret = "";
        for (var i = 0; i < Math.Ceiling(LedCountPeripheral / 8f); i++)
        {
            ret +=
                $"""

                 slaveWriteLED(ledStatePeripheral[{i}]);
                 """;
        }

        ret +=
            $"""

             {Microcontroller.GenerateDigitalWrite(Stp16LePeripheral, true, true)};
             delayMicroseconds(10);
             {Microcontroller.GenerateDigitalWrite(Stp16LePeripheral, false, true)};
             """;

        return FixNewlines(ret);
    }

    private string ComputeLedsStp16(bool peripheral,
        Dictionary<byte, List<(Output, int)>> debouncesRelatedToLed,
        Dictionary<byte, List<OutputAxis>> analogRelatedToLed)
    {
        var ret = "";
        var variable = peripheral ? "ledStatePeripheral" : "ledState";
        var count = peripheral ? LedCountPeripheral : LedCount;
        // Handle leds, including when multiple leds are assigned to a single output.
        foreach (var (led, relatedOutputs) in debouncesRelatedToLed)
        {
            var index = led - 1;
            var analog = "";
            if (analogRelatedToLed.TryGetValue(led, out var analogLedOutputs))
            {
                analog = analogLedOutputs.Aggregate(analog,
                    (current, analogLedOutput) =>
                        current +
                        $"bit_write({analogLedOutput.Input.Generate()}, {variable}[{index / 8}],{index % 8});");
            }

            if (!analog.Any())
            {
                analog = $"bit_clear({variable}[{index / 8}],{index % 8});";
            }

            ret += $$"""

                     if (!bit_check({{variable}}Select[{{index / 8}}],{{index % 8}})) {
                     """;
            ret += string.Join(" else ", relatedOutputs.DistinctBy(tuple => tuple.Item1).Select(tuple =>
            {
                var ifStatement = $"debounce[{tuple.Item2}]";
                return $$"""
                         
                             if ({{ifStatement}}) {
                                 bit_set({{variable}}[{{index / 8}}],{{index % 8}});
                             }
                         """;
            }));
            ret += $$"""
                         else {
                             {{analog}}
                         }
                     }
                     """;
        }

        foreach (var (led, analogLedOutputs) in analogRelatedToLed)
        {
            if (debouncesRelatedToLed.ContainsKey(led)) continue;
            var index = led - 1;
            ret += $$"""

                     if (!bit_check({{variable}}Select[{{index / 8}}],{{index % 8}})) {
                     """;
            ret = analogLedOutputs.Aggregate(ret,
                (current, analogLedOutput) =>
                    current + $"bit_write({analogLedOutput.Input.Generate()}, {variable}[{index / 8}],{index % 8});");

            ret += "}";
        }

        return ret;
    }

    private string ComputeLeds(ConfigField mode, bool peripheral,
        Dictionary<byte, List<(Output, int)>> debouncesRelatedToLed,
        Dictionary<byte, List<OutputAxis>> analogRelatedToLed, BinaryWriter? writer)
    {
        var ret = "";
        var type = peripheral ? LedTypePeripheral : LedType;
        var variable = peripheral ? "ledStatePeripheral" : "ledState";
        if (mode != ConfigField.Shared || type is LedType.None) return "";
        if (type == LedType.Stp16Cpc26)
        {
            return ComputeLedsStp16(peripheral, debouncesRelatedToLed, analogRelatedToLed);
        }

        // Handle leds, including when multiple leds are assigned to a single output.
        foreach (var (led, relatedOutputs) in debouncesRelatedToLed)
        {
            var analog = "";
            if (analogRelatedToLed.TryGetValue(led, out var analogLedOutputs))
            {
                foreach (var analogLedOutput in analogLedOutputs)
                {
                    var ledRead =
                        analogLedOutput.GenerateAssignment("0", ConfigField.Ps3, false, true, false, false, null);

                    var ledReadCheck = "led_tmp";
                    // Turntable velocities are different to most axis, as they don't use standard calibration.
                    if (analogLedOutput is DjAxis
                        {
                            Type: DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity
                        } djAxis)
                    {
                        var multiplier = djAxis.LedMultiplier;
                        var generated = $"({analogLedOutput.Input.Generate()})";
                        var isI2C = analogLedOutput.Input is DjInput
                        {
                            Input: DjInputType.LeftTurntable or DjInputType.RightTurntable
                        };
                        ledReadCheck = analogLedOutput.Input.Generate();
                        if (analogLedOutput.InputIsUint)
                        {
                            ledReadCheck = $"({generated} - INT16_MAX)";
                        }
                        else
                        {
                            generated = $"({generated} + INT16_MAX)";
                        }

                        ledRead = isI2C
                            ? $"handle_calibration_turntable_ps3_i2c(0, {analogLedOutput.Input.Generate()},{multiplier})"
                            : $"handle_calibration_turntable_ps3(0, {generated},{multiplier})";
                    }

                    if (analogLedOutput is DjAxis {Type: DjAxisType.EffectsKnob})
                    {
                        var generated = $"({analogLedOutput.Input.Generate()})";
                        if (!analogLedOutput.InputIsUint)
                        {
                            generated = $"({generated} + INT16_MAX)";
                        }

                        ledRead = $"(({generated} >> 8))";
                    }

                    // Now we have the value, calibrated as a uint8_t
                    // Only apply analog colours if non zero when conflicting with digital, so that the digital off states override
                    analog +=
                        $$"""
                          led_tmp = {{ledRead}};
                          if({{ledReadCheck}}) {
                              {{type.GetLedAssignment(peripheral, led, analogLedOutput.LedOn, analogLedOutput.LedOff, "led_tmp", writer)}}
                          } else {
                              {{type.GetLedAssignment(peripheral, relatedOutputs.First().Item1.LedOff, led, writer)}}
                          }
                          """;
                }
            }

            if (!analog.Any())
            {
                analog = type.GetLedAssignment(peripheral, relatedOutputs.First().Item1.LedOff, led, writer);
            }

            ret += $$"""

                     if ({{variable}}[{{led - 1}}].select == 0) {
                     """;
            ret += string.Join(" else ", relatedOutputs.DistinctBy(tuple => tuple.Item1).Select(tuple =>
            {
                var ifStatement = $"debounce[{tuple.Item2}]";
                return $$"""
                         
                             if ({{ifStatement}}) {
                                 {{type.GetLedAssignment(peripheral, tuple.Item1.LedOn, led, writer)}}
                             }
                         """;
            }));
            ret += $$"""
                         else {
                             {{analog}}
                         }
                     }
                     """;
        }

        foreach (var (led, analogLedOutputs) in analogRelatedToLed)
        {
            if (debouncesRelatedToLed.ContainsKey(led)) continue;
            ret += $$"""

                     if ({{variable}}[{{led - 1}}].select == 0) {
                     """;
            foreach (var analogLedOutput in analogLedOutputs)
            {
                var ledRead =
                    analogLedOutput.GenerateAssignment("0", ConfigField.Ps3, false, true, false, false, writer);
                // Turntable velocities are different to most axis, as they don't use standard calibration.
                if (analogLedOutput is DjAxis
                    {
                        Type: DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity
                    } djAxis)
                {
                    var multiplier = djAxis.LedMultiplier;
                    var generated = $"({analogLedOutput.Input.Generate()})";
                    var isI2C = analogLedOutput.Input is DjInput
                    {
                        Input: DjInputType.LeftTurntable or DjInputType.RightTurntable
                    };
                    if (!analogLedOutput.InputIsUint)
                    {
                        generated = $"({generated} + INT16_MAX)";
                    }

                    ledRead = isI2C
                        ? $"handle_calibration_turntable_ps3_i2c(0, {analogLedOutput.Input.Generate()},{multiplier})"
                        : $"handle_calibration_turntable_ps3(0, {generated},{multiplier})";
                }

                if (analogLedOutput is DjAxis {Type: DjAxisType.EffectsKnob})
                {
                    var generated = $"({analogLedOutput.Input.Generate()})";
                    if (!analogLedOutput.InputIsUint)
                    {
                        generated = $"({generated} + INT16_MAX)";
                    }

                    ledRead = $"(({generated} >> 8))";
                }

                // Now we have the value, calibrated as a uint8_t
                ret +=
                    $"led_tmp = {ledRead};{type.GetLedAssignment(peripheral, led, analogLedOutput.LedOn, analogLedOutput.LedOff, "led_tmp", writer)}";
            }

            ret += "}";
        }

        return ret;
    }

    private string GenerateTick(ConfigField mode, BinaryWriter? writer)
    {
        var outputs = Bindings.Items.SelectMany(binding => binding.ValidOutputs()).ToList();

        var outputsByType = outputs
            .GroupBy(s => s.Input.InnermostInputs().First().GetType()).ToList();
        var combined = DeviceControllerType.IsGuitar() && CombinedStrumDebounce;
        Dictionary<string, int> debounces = new();
        var strumIndices = new List<int>();

        // Pass 1: work out debounces and map inputs to debounces
        var inputs = new Dictionary<string, List<int>>();
        var macros = new Dictionary<string, List<(int, Input)>>();
        foreach (var outputByType in outputsByType)
        {
            foreach (var output in outputByType)
            {
                var generatedInput = output.Input.Generate();
                if (output is not OutputButton and not DrumAxis and not EmulationMode) continue;

                if (output.Input is MacroInput)
                {
                    foreach (var input in output.Input.Inputs())
                    {
                        var gen = input.Generate();
                        macros.TryAdd(gen, new List<(int, Input)>());
                        macros[gen].AddRange(output.Input.Inputs().Where(s => s != input).Select(s => (0, s)));
                    }
                }

                debounces.TryAdd(generatedInput, debounces.Count);
                if (output is GuitarButton
                    {
                        IsStrum: true
                    })
                    strumIndices.Add(debounces[generatedInput]);

                if (!inputs.ContainsKey(generatedInput)) inputs[generatedInput] = new List<int>();

                inputs[generatedInput].Add(debounces[generatedInput]);
            }
        }

        foreach (var (key, value) in macros)
        {
            var list2 = new List<(int, Input)>();
            foreach (var (_, input) in value)
            {
                var gen = input.Generate();
                if (debounces.TryGetValue(gen, out var debounce))
                {
                    list2.Add((debounce, input));
                }

                macros[key] = list2;
            }
        }

        var debouncesRelatedToLed = new Dictionary<byte, List<(Output, int)>>();
        var analogRelatedToLed = new Dictionary<byte, List<OutputAxis>>();
        var debouncesRelatedToLedPeripheral = new Dictionary<byte, List<(Output, int)>>();
        var analogRelatedToLedPeripheral = new Dictionary<byte, List<OutputAxis>>();
        // Handle most mappings
        var ret = outputsByType
            .Aggregate("", (current, group) =>
            {
                return current + group
                    .First().Input.InnermostInputs().First()
                    .GenerateAll(group
                        // DigitalToAnalog and MacroInput need to be handled last
                        .OrderByDescending(s => s.Input is DigitalToAnalog or MacroInput ? 0 : 1)
                        // And then we need to make sure any strum ouputs are first, so that the fortnite strum code works
                        .ThenBy(s => s is GuitarButton {IsStrum: true} ? 0 : 1)
                        .Select(s =>
                        {
                            var input = s.Input;
                            var output = s;
                            var generatedInput = input.Generate();
                            var index = 0;
                            if (output is OutputButton or DrumAxis or EmulationMode)
                            {
                                index = debounces[generatedInput];

                                foreach (var led in output.LedIndices)
                                {
                                    if (!debouncesRelatedToLed.ContainsKey(led))
                                        debouncesRelatedToLed[led] = new List<(Output, int)>();

                                    debouncesRelatedToLed[led].Add((output, index));
                                }

                                foreach (var led in output.LedIndicesPeripheral)
                                {
                                    if (!debouncesRelatedToLedPeripheral.ContainsKey(led))
                                        debouncesRelatedToLedPeripheral[led] = new List<(Output, int)>();

                                    debouncesRelatedToLedPeripheral[led].Add((output, index));
                                }
                            }

                            if (output is OutputAxis axis)
                            {
                                foreach (var led in output.LedIndices)
                                {
                                    if (!analogRelatedToLed.ContainsKey(led))
                                        analogRelatedToLed[led] = new List<OutputAxis>();

                                    analogRelatedToLed[led].Add(axis);
                                }

                                foreach (var led in output.LedIndicesPeripheral)
                                {
                                    if (!analogRelatedToLedPeripheral.ContainsKey(led))
                                        analogRelatedToLedPeripheral[led] = new List<OutputAxis>();

                                    analogRelatedToLedPeripheral[led].Add(axis);
                                }
                            }

                            var generated = output.Generate(mode, index, "", "", strumIndices, combined, macros,
                                writer);

                            return new Tuple<Input, string>(input, generated);
                        })
                        .Where(s => !string.IsNullOrEmpty(s.Item2))
                        .Distinct().ToList(), mode);
            });
        ret += ComputeLeds(mode, false, debouncesRelatedToLed, analogRelatedToLed, writer);
        ret += ComputeLeds(mode, true, debouncesRelatedToLedPeripheral, analogRelatedToLedPeripheral, writer);
        return FixNewlines(ret);
    }

    private int CalculateDebounceTicks()
    {
        var outputs = Bindings.Items.SelectMany(binding => binding.ValidOutputs()).ToList();
        var outputsByType = outputs
            .GroupBy(s => s.Input.InnermostInputs().First().GetType()).ToList();
        Dictionary<string, int> debounces = new();

        foreach (var outputByType in outputsByType)
        {
            foreach (var output in outputByType)
            {
                var generatedInput = output.Input.Generate();
                if (output is not OutputButton and not DrumAxis and not EmulationMode) continue;


                debounces.TryAdd(generatedInput, debounces.Count);
            }
        }

        return debounces.Count;
    }

    public List<PinConfig> GetPinConfigs()
    {
        return Bindings.Items.SelectMany(s => s.GetPinConfigs()).Concat(PinConfigs).Distinct().ToList();
    }

    public string LedSpiType(bool peripheral)
    {
        if (peripheral)
        {
            return IsApa102Peripheral ? Apa102PeripheralSpiType : Stp16PeripheralSpiType;
        }

        return IsApa102 ? Apa102SpiType : Stp16SpiType;
    }

    public Dictionary<string, List<int>> GetPins(string type, bool twi, bool spi, bool peripheral)
    {
        var pins = new Dictionary<string, List<int>>();
        foreach (var binding in Bindings.Items)
        {
            var pins2 = new List<int>();
            var configs = binding.GetPinConfigs();
            var skip = false;
            foreach (var pinConfig in configs)
            {
                //Exclude digital or analog pins (which use a guid containing a -)
                if (pinConfig.Type == type || (type.Contains('-') && pinConfig.Type.Contains('-')) ||
                    pinConfig.Peripheral != peripheral)
                {
                    skip = true;
                    break;
                }

                if (!twi || type != PeripheralTwiType)
                {
                    pins2.AddRange(pinConfig.Pins);
                }
            }

            if (skip)
            {
                break;
            }

            if (!pins.ContainsKey(binding.LocalisedName)) pins[binding.LocalisedName] = new List<int>();
            pins[binding.LocalisedName].AddRange((pins2));
        }

        if ((Main.IsUno || Main.IsMega) && !peripheral)
        {
            pins[UnoPinTypeTx] = new List<int> {UnoPinTypeTxPin};
            pins[UnoPinTypeRx] = new List<int> {UnoPinTypeRxPin};
        }

        if (IsIndexedLed && _ledSpiConfig != null && type != Apa102SpiType && !peripheral)
            pins[LedSpiType(false)] = _ledSpiConfig.Pins.ToList();

        if (IsIndexedLedPeripheral && _ledSpiConfigPeripheral != null && type != Apa102PeripheralSpiType && peripheral)
        {
            pins[LedSpiType(true)] = _ledSpiConfigPeripheral.Pins.ToList();
        }

        if (IsStp16 && _stp16Le != null && type != Stp16LeType && !peripheral)
        {
            pins[Stp16LeType] = _stp16Le.Pins.ToList();
        }

        if (IsStp16 && _stp16Oe != null && type != Stp16OeType && !peripheral)
        {
            pins[Stp16OeType] = _stp16Oe.Pins.ToList();
        }

        if (IsStp16Peripheral && _stp16LePeripheral != null && type != Stp16LePeripheralType && peripheral)
        {
            pins[Stp16LePeripheralType] = _stp16LePeripheral.Pins.ToList();
        }

        if (IsStp16Peripheral && _stp16OePeripheral != null && type != Stp16OePeripheralType && peripheral)
        {
            pins[Stp16OePeripheralType] = _stp16OePeripheral.Pins.ToList();
        }

        if (UsbHostEnabled && type != UsbHostPinTypeDm && type != UsbHostPinTypeDp && !peripheral)
            pins["USB Host"] = new List<int> {UsbHostDm, UsbHostDp};

        if (_peripheralTwiConfig != null && type != PeripheralTwiType && !twi && !peripheral)
        {
            pins[PeripheralTwiType] = _peripheralTwiConfig.Pins.ToList();
        }

        return pins;
    }

    public void UpdateErrors()
    {
        var foundError = false;
        foreach (var output in Bindings.Items)
        {
            output.UpdateErrors();
            if (!string.IsNullOrEmpty(output.ErrorText)) foundError = true;
        }

        LedErrorText = null;
        if (_ledSpiConfig?.ErrorText != null)
        {
            foundError = true;
            LedErrorText = _ledSpiConfig.ErrorText;
        }

        if (_stp16Le?.ErrorText != null)
        {
            foundError = true;
            LedErrorText = _stp16Le.ErrorText;
        }

        if (_stp16Oe?.ErrorText != null)
        {
            foundError = true;
            LedErrorText = _stp16Oe.ErrorText;
        }

        if (HasPeripheral && _ledSpiConfigPeripheral?.ErrorText != null)
        {
            foundError = true;
            if (LedErrorText != null && LedErrorText != _ledSpiConfigPeripheral.ErrorText)
            {
                LedErrorText += " " + _ledSpiConfigPeripheral.ErrorText;
            }
            else
            {
                LedErrorText = _ledSpiConfigPeripheral.ErrorText;
            }
        }

        if (HasPeripheral && _peripheralTwiConfig?.ErrorText != null)
        {
            foundError = true;
            PeripheralErrorText = _peripheralTwiConfig.ErrorText;
        }
        else
        {
            PeripheralErrorText = null;
        }

        HasError = foundError;
        Main.ShowError = foundError;
    }

    public List<PinConfig> LedPinConfigs()
    {
        List<PinConfig> configs = new List<PinConfig>();
        if (_ledSpiConfig != null)
        {
            configs.Add(_ledSpiConfig);
        }

        if (_ledSpiConfigPeripheral != null)
        {
            configs.Add(_ledSpiConfigPeripheral);
        }

        return configs;
    }

    public void GoBack()
    {
        if (Device is Santroller santroller)
        {
            santroller.StopTicking();
        }

        Main.ShowError = false;
        Main.SetDifference(false);
        Main.Complete(100);
        Main.GoBack.Execute();
    }

    public void AddDevice(IConfigurableDevice device)
    {
        if (_disconnected) return;
        RxApp.MainThreadScheduler.Schedule(() =>
        {
            Console.WriteLine(Resources.AddDeviceMessage, Device, device);
            if (device is Santroller santroller && Main.Working)
            {
                Main.Complete(100);
                Device = device;
                Microcontroller = device.GetMicrocontroller(this);
                Main.SetDifference(false);
                santroller.StartTicking(this);
            }

            Device.DeviceAdded(device);
        });
    }

    public void RemoveDevice(IConfigurableDevice device)
    {
        if (_disconnected || Main.Working || Device is not Santroller old ||
            (!device.IsSameDevice(old.Serial) && !device.IsSameDevice(old.Path))) return;
        old.StopTicking();
        Main.SetDifference(false);
        _disconnected = true;
        if (Builder) return;
        ShowUnpluggedDialog.Handle(("", "", "")).ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => Main.GoBack.Execute(new Unit()));
    }

    public void Update(byte[] btRaw, bool peripheralConnected)
    {
        if (IsBluetoothTx && btRaw.Any())
        {
            Connected = btRaw[0] != 0;
        }

        PeripheralConnected = peripheralConnected;
    }

    public TwiConfig? GetTwiForType(string twiType, bool peripheral)
    {
        return Bindings.Items.Select(binding => binding.GetPinConfigs())
            .Select(configs => configs.OfType<TwiConfig>()
                .FirstOrDefault(s => s.Type == twiType && s.Peripheral == peripheral))
            .FirstOrDefault(found => found != null);
    }

    public SpiConfig? GetSpiForType(string spiType, bool peripheral)
    {
        return Bindings.Items.Select(binding => binding.GetPinConfigs())
            .Select(configs => configs.OfType<SpiConfig>()
                .FirstOrDefault(s => s.Type == spiType && s.Peripheral == peripheral))
            .FirstOrDefault(found => found != null);
    }

    public DirectPinConfig GetPinForType(string pinType, bool peripheral, int fallbackPin, DevicePinMode fallbackMode)
    {
        return Bindings.Items.Select(binding => binding.GetPinConfigs())
                   .Select(configs =>
                       configs.OfType<DirectPinConfig>()
                           .FirstOrDefault(s => s.Type == pinType && s.Peripheral == peripheral))
                   .FirstOrDefault(found => found != null) ??
               new DirectPinConfig(this, pinType, fallbackPin, peripheral, fallbackMode);
    }

    public void MoveUp(Output output)
    {
        var index = Bindings.Items.IndexOf(output);
        Bindings.Move(index, index - 1);
    }

    public void MoveDown(Output output)
    {
        var index = Bindings.Items.IndexOf(output);
        Bindings.Move(index, index + 1);
    }

    // Capture any input events (such as pointer or keyboard) - used for detecting the last input
    public readonly Subject<object> KeyOrPointerEvent = new();

    public void OnKeyEvent(KeyEventArgs args)
    {
        KeyOrPointerEvent.OnNext(args);
    }

    public void OnMouseEvent(PointerEventArgs args)
    {
        KeyOrPointerEvent.OnNext(args);
    }

    public void OnMouseEvent(PointerUpdateKind args)
    {
        KeyOrPointerEvent.OnNext(args);
    }

    public void OnMouseEvent(Point args)
    {
        KeyOrPointerEvent.OnNext(args);
    }

    [GeneratedRegex("^\\s+$[\\r\\n]*", RegexOptions.Multiline)]
    private static partial Regex NewlineRegex();

    private byte[]? _lastConfig;
    private byte[]? _currentConfigData;
    private SerializedConfiguration? _currentConfig;
    private readonly DispatcherTimer _timer;
    public bool WasBluetooth { get; set; }

    private void Diff(object? sender, EventArgs e)
    {
        if (_disconnected || (Main.Router.NavigationStack.Last() != this &&
                              Main.Router.NavigationStack.Last() is not InitialConfigViewModel))
        {
            _timer.Stop();
            Main.ShowError = false;
            Main.ProgressbarColor = Main.ProgressBarPrimary;
            Main.SetDifference(false);
            return;
        }

        if (_currentConfig == null || _lastConfig == null || _currentConfigData == null) return;
        _currentConfig.Update(this, Bindings.Items, false);
        using var outputStream = new MemoryStream(_currentConfigData);
        try
        {
            Serializer.Serialize(outputStream, _currentConfig);
            Main.SetDifference(!_currentConfigData.AsSpan().SequenceEqual(_lastConfig.AsSpan()));
        }
        catch (Exception)
        {
            Main.SetDifference(true);
        }
    }

    public void SetUpDiff()
    {
        WasBluetooth = IsBluetooth;
        var lastConfig = new SerializedConfiguration(this);
        using var outputStream = new MemoryStream();
        Serializer.Serialize(outputStream, lastConfig);
        _lastConfig = outputStream.ToArray();
        _currentConfig = new SerializedConfiguration(this);
        _currentConfigData = new byte[_lastConfig.Length];
        _timer.Start();
    }
}