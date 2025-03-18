using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
using DynamicData.Binding;
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
using ReactiveUI.SourceGenerators;
using BluetoothCombinedOutput = GuitarConfigurator.NetCore.Configuration.Outputs.Combined.BluetoothCombinedOutput;

namespace GuitarConfigurator.NetCore.ViewModels;

public partial class ConfigViewModel : ReactiveObject, IRoutableViewModel
{
    public static readonly string Apa102SpiType = "APA102";
    public static readonly string AdafruitHostType = "Adafruit Feather RP2040 USB Host Enable";
    public static readonly string Stp16SpiType = "STP16CPC26";
    public static readonly string WS2812SpiType = "WS2812";
    public static readonly string WiiOutputTwiType = "Wii Output";
    public static readonly string Ps2OutputTwiType = "Ps2 Output";
    public static readonly string Ps2OutputAckType = "Ps2 Output Acknowledge";
    public static readonly string Ps2OutputAttType = "Ps2 Output Attention";
    public static readonly string Stp16PeripheralSpiType = "Peripheral STP16CPC26";
    public static readonly string Apa102PeripheralSpiType = "Peripheral APA102";
    public static readonly string WS2812PeripheralSpiType = "Peripheral WS2812";
    public static readonly string WakeupPinType = "Pico Wakeup Pin";
    public static readonly string Stp16LeType = "STP16CPC26 Latch Enable";
    public static readonly string Stp16LePeripheralType = "Peripheral STP16CPC26 Latch Enable";
    public static readonly string Stp16OeType = "STP16CPC26 Output Enable";
    public static readonly string Stp16OePeripheralType = "Peripheral STP16CPC26 Output Enable";
    public static readonly string PeripheralTwiType = "Peripheral";
    public static readonly int PeripheralTwiClock = 500000;
    public static readonly string Mpr121TwiType = "MPR121";
    public static readonly int Mpr121TwiFreq = 400000;
    public static readonly string Max170XTwiType = "MAX170x";
    public static readonly int Max170XTwiFreq = 400000;
    public static readonly string AccelTwiType = "adxl";
    public static readonly int AccelTwiFreq = 400000;
    public static readonly string UsbHostPinTypeDm = "USB D-";
    public static readonly string UsbHostPinTypeDp = "USB D+";
    public static readonly string UnoPinTypeTx = "Uno Serial Tx Pin";
    public static readonly string UnoPinTypeRx = "Uno Serial Rx Pin";
    public static readonly int UnoPinTypeRxPin = 0;
    public static readonly int UnoPinTypeTxPin = 1;

    private bool _allExpanded;


    private SpiConfig? _ledSpiConfig;
    private SpiConfig? _ledSpiConfigPeripheral;
    private SpiConfig? _ps2OutputSpiConfig;
    private TwiConfig? _peripheralTwiConfig;
    private TwiConfig? _mpr121TwiConfig;
    private TwiConfig? _max170XTwiConfig;
    private TwiConfig? _accelTwiConfig;
    private TwiConfig? _wiiOutputTwiConfig;
    private DirectPinConfig? _ws2812Config;
    private DirectPinConfig? _ws2812ConfigPeripheral;
    private DirectPinConfig? _ps2OutputAtt;
    private DirectPinConfig? _ps2OutputAck;
    private DirectPinConfig? _stp16Oe;
    private DirectPinConfig? _stp16Le;
    private DirectPinConfig? _stp16OePeripheral;
    private DirectPinConfig? _stp16LePeripheral;
    private DirectPinConfig? _adaFruitHostPin;
    private DirectPinConfig? _sleepPin;

    [Reactive] private AccelSensorType _accelSensorType;

    public IEnumerable<AccelSensorType> AccelSensorTypes => Enum.GetValues<AccelSensorType>();

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
            this.WhenAnyValue(x => x.Main.Working, x => x.HasError, x => x.Builder)
                .ObserveOn(RxApp.MainThreadScheduler).Select(x =>
                    x is {Item1: false, Item2: false, Item3: false} or
                        {Item1: false, Item3: true}));

        WriteUf2Command = ReactiveCommand.CreateFromObservable(() => Main.SaveUf2(this),
            this.WhenAnyValue(x => x.Main.Working)
                .ObserveOn(RxApp.MainThreadScheduler).Select(x => x is false));
        ResetCommand = ReactiveCommand.CreateFromTask(ResetAsync,
            this.WhenAnyValue(x => x.Main.Working)
                .ObserveOn(RxApp.MainThreadScheduler).Select(x => x is false));
        GoBackCommand = ReactiveCommand.Create(GoBack, this.WhenAnyValue(x => x.Main.Working).Select(s => !s));

        SaveConfigCommand = ReactiveCommand.CreateFromObservable(() => SaveConfig.Handle(this));

        LoadConfigCommand = ReactiveCommand.CreateFromObservable(() => LoadConfig.Handle(this));
        _pollRateLabelHelper = this.WhenAnyValue(x => x.Deque, x => x.PollRate, x => x.LocalDebounceMode)
            .Select(GeneratePollRateLabel)
            .ToProperty(this, x => x.PollRateLabel);
        _savePresetLabelHelper = this.WhenAnyValue(x => x.PresetName).Select(x =>
                string.IsNullOrWhiteSpace(x) ? Resources.SavePresetLabel :
                Presets.Any(s => s.Item1 == x) ? string.Format(Resources.SavePresetLabel3, x) :
                string.Format(Resources.SavePresetLabel2, x))
            .ToProperty(this, x => x.SavePresetLabel);
        _loadPresetLabelHelper = this.WhenAnyValue(x => x.CurrentPreset).Select(x =>
                x is null ? Resources.LoadPresetLabel : string.Format(Resources.LoadPresetLabel2, x.Item1))
            .ToProperty(this, x => x.LoadPresetLabel);
        _deletePresetLabelHelper = this.WhenAnyValue(x => x.CurrentPreset).Select(x =>
                x is null ? Resources.DeletePresetLabel : string.Format(Resources.DeletePresetLabel2, x.Item1))
            .ToProperty(this, x => x.DeletePresetLabel);
        _supportsDequeHelper = this.WhenAnyValue(x => x.LocalDebounceMode, x => x.DeviceControllerType)
            .Select(x => x.Item2 is DeviceControllerType.GuitarHeroGuitar or DeviceControllerType.RockBandGuitar &&
                         !x.Item1)
            .ToProperty(this, x => x.SupportsDeque);
        _minPollRateHelper = this.WhenAnyValue(x => x.Deque)
            .Select(x => x ? 1 : 0)
            .ToProperty(this, x => x.MinPollRate);
        _supportsPS4InstrumentHelper = this.WhenAnyValue(x => x.DeviceControllerType, x => x.Branded)
            .Select(x => x.Item1 is DeviceControllerType.GuitarHeroGuitar
                or DeviceControllerType.RockBandGuitar or DeviceControllerType.GuitarHeroDrums
                or DeviceControllerType.RockBandDrums && !x.Item2)
            .ToProperty(this, x => x.SupportsPS4Instrument);
        _isGuitarHelper = this.WhenAnyValue(x => x.DeviceControllerType)
            .Select(x => x.IsGuitar())
            .ToProperty(this, x => x.IsGuitar);
        _isDrumHelper = this.WhenAnyValue(x => x.DeviceControllerType)
            .Select(x => x.IsDrum())
            .ToProperty(this, x => x.IsDrum);
        _isProKeysHelper = this.WhenAnyValue(x => x.DeviceControllerType)
            .Select(x => x is DeviceControllerType.ProKeys)
            .ToProperty(this, x => x.IsProKeys);
        _isGuitarHeroGuitarHelper = this.WhenAnyValue(x => x.DeviceControllerType)
            .Select(x => x is DeviceControllerType.GuitarHeroGuitar)
            .ToProperty(this, x => x.IsGuitarHeroGuitar);
        _isTurntableHelper = this.WhenAnyValue(x => x.DeviceControllerType)
            .Select(x => x is DeviceControllerType.Turntable)
            .ToProperty(this, x => x.IsTurntable);
        _isStageKitHelper = this.WhenAnyValue(x => x.DeviceControllerType)
            .Select(x => x is DeviceControllerType.StageKit)
            .ToProperty(this, x => x.IsStageKit);
        _strumDebounceDisplay = this.WhenAnyValue(x => x.StrumDebounce)
            .Select(x => x / 10.0f)
            .ToProperty(this, x => x.StrumDebounceDisplay);
        _debounceDisplay = this.WhenAnyValue(x => x.Debounce)
            .Select(x => x / 10.0f)
            .ToProperty(this, x => x.DebounceDisplay);
        _isControllerHelper = this.WhenAnyValue(x => x.DeviceControllerType)
            .Select(x => x is not DeviceControllerType.KeyboardMouse)
            .ToProperty(this, x => x.IsController);
        _isStandardControllerHelper = this.WhenAnyValue(x => x.DeviceControllerType)
            .Select(x => x is not DeviceControllerType.KeyboardMouse)
            .ToProperty(this, x => x.IsStandardController);
        _isRpcs3CompatibleControllerHelper = this.WhenAnyValue(x => x.DeviceControllerType)
            .Select(x =>
                x is DeviceControllerType.Turntable or DeviceControllerType.RockBandDrums
                    or DeviceControllerType.RockBandGuitar or DeviceControllerType.LiveGuitar
                    or DeviceControllerType.StageKit or DeviceControllerType.ProKeys
                    or DeviceControllerType.ProGuitarMustang or DeviceControllerType.ProGuitarSquire)
            .ToProperty(this, x => x.IsRpcs3CompatibleController);
        _isKeyboardHelper = this.WhenAnyValue(x => x.DeviceControllerType)
            .Select(x => x is DeviceControllerType.KeyboardMouse)
            .ToProperty(this, x => x.IsKeyboard);
        _isApa102Helper = this.WhenAnyValue(x => x.LedType)
            .Select(x => x.IsApa102())
            .ToProperty(this, x => x.IsApa102);
        _isWs2812Helper = this.WhenAnyValue(x => x.LedType)
            .Select(x => x.IsWs2812())
            .ToProperty(this, x => x.IsWs2812);
        _isWs2812PeripheralHelper = this.WhenAnyValue(x => x.LedTypePeripheral)
            .Select(x => x.IsWs2812())
            .ToProperty(this, x => x.IsWs2812Peripheral);
        _isApa102PeripheralHelper = this.WhenAnyValue(x => x.LedTypePeripheral, x => x.HasPeripheral)
            .Select(x => x.Item2 && x.Item1.IsApa102())
            .ToProperty(this, x => x.IsApa102Peripheral);
        _isStp16Helper = this.WhenAnyValue(x => x.LedType)
            .Select(x => x is LedType.Stp16Cpc26)
            .ToProperty(this, x => x.IsStp16);
        _isStp16PeripheralHelper = this.WhenAnyValue(x => x.LedTypePeripheral, x => x.HasPeripheral)
            .Select(x => x is
            {
                Item2: true, Item1: LedType.Stp16Cpc26
            })
            .ToProperty(this, x => x.IsStp16Peripheral);

        _isIndexedLedHelper = this.WhenAnyValue(x => x.LedType)
            .Select(x => x.IsIndexed())
            .ToProperty(this, x => x.IsIndexedLed);
        _isIndexedLedPeripheralHelper = this.WhenAnyValue(x => x.LedTypePeripheral, x => x.HasPeripheral)
            .Select(x => x.Item2 && x.Item1.IsIndexed())
            .ToProperty(this, x => x.IsIndexedLedPeripheral);
        _isFortniteFestivalProHelper = Bindings.Connect()
            .QueryWhenChanged(s => s.Any(s2 => s2 is EmulationMode
            {
                Type: EmulationModeType.Fnf or EmulationModeType.FnfHid or EmulationModeType.FnfLayer
                or EmulationModeType.FnfIos
            }))
            .ToProperty(this, x => x.IsFortniteFestivalPro);
        _isBluetoothRxHelper = Bindings.Connect()
            .QueryWhenChanged(s => s.Any(s2 => s2 is BluetoothCombinedOutput))
            .ToProperty(this, x => x.IsBluetoothRx);
        _hasWiiCombinedOutputHelper = Bindings.Connect()
            .QueryWhenChanged(s => s.Any(s2 => s2 is WiiCombinedOutput))
            .ToProperty(this, x => x.HasWiiCombinedOutput);
        _hasPs2CombinedOutputHelper = Bindings.Connect()
            .QueryWhenChanged(s => s.Any(s2 => s2 is Ps2CombinedOutput))
            .ToProperty(this, x => x.HasPs2CombinedOutput);
        _hasGhwtCombinedOutputHelper = Bindings.Connect()
            .QueryWhenChanged(s => s.Any(s2 => s2 is GhwtCombinedOutput))
            .ToProperty(this, x => x.HasGhwtCombinedOutput);
        _hasCloneCombinedOutputHelper = Bindings.Connect()
            .QueryWhenChanged(s => s.Any(s2 => s2 is CloneCombinedOutput))
            .ToProperty(this, x => x.HasCloneCombinedOutput);
        _hasDjCombinedOutputHelper = Bindings.Connect()
            .QueryWhenChanged(s => s.Any(s2 => s2 is DjCombinedOutput))
            .ToProperty(this, x => x.HasDjCombinedOutput);
        _hasGh5CombinedOutputHelper = Bindings.Connect()
            .QueryWhenChanged(s => s.Any(s2 => s2 is Gh5CombinedOutput))
            .ToProperty(this, x => x.HasGh5CombinedOutput);
        _hasUsbHostCombinedOutputHelper = Bindings.Connect()
            .QueryWhenChanged(s => s.Any(s2 => s2 is UsbHostCombinedOutput))
            .ToProperty(this, x => x.HasUsbHostCombinedOutput);
        _isBluetoothHelper = this.WhenAnyValue(x => x.IsBluetoothTx, x => x.IsBluetoothRx)
            .Select(x => x.Item1 || x.Item2)
            .ToProperty(this, x => x.IsBluetooth);
        _isOrWasBluetoothHelper = this.WhenAnyValue(x => x.IsBluetooth, x => x.WasBluetooth)
            .Select(x => x.Item1 || x.Item2)
            .ToProperty(this, x => x.IsOrWasBluetooth);
        _usbHostEnabledHelper = Bindings.Connect()
            .AutoRefresh(s => s.Input)
            .QueryWhenChanged(s => s.Any(s2 =>
                s2 is UsbHostCombinedOutput || s2.Input.InnermostInputs().First().InputType is InputType.UsbHostInput ||
                s2.Input.InnermostInputs().First().InputType is InputType.MidiInput))
            .ToProperty(this, x => x.UsbHostEnabled);
        _hasMidiHelper = Bindings.Connect()
            .QueryWhenChanged(s => s.Any(s2 =>
                s2 is MidiCombinedOutput || s2.Input.InnermostInputs().First().InputType is InputType.MidiInput))
            .ToProperty(this, x => x.HasMidi);
        Bindings.Connect().TransformMany(s => s.AllDigitalOutputs).Bind(out _digitalOutputs).Subscribe();
        if (Branded)
        {
            // Filters break being able to reorder inputs, but we only need the filter for branded, which doesn't let you reorder anyways
            Bindings.Connect().Filter(x => x.IsVisible).Bind(out _outputs).Subscribe();
        }
        else
        {
            Bindings.Connect().Bind(out _outputs).Subscribe();
        }

        _deviceControllerTypes.AddRange(Enum.GetValues<DeviceControllerType>());
        _deviceControllerTypes.Connect()
            .Bind(out var controllerTypes)
            .Subscribe();
        _keys.Connect()
            .Bind(out var keys)
            .Subscribe();
        Keys = keys;
        DeviceControllerRhythmTypes = controllerTypes;
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

    private static string GeneratePollRateLabel((bool dequeue, int rate, bool localDeque) arg)
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

    [Reactive] private RolloverMode _rolloverMode;
    public IEnumerable<RolloverMode> RolloverModes => Enum.GetValues<RolloverMode>();

    [Reactive] private string? _peripheralErrorText;

    [Reactive] private string? _wiiOutputErrorText;

    [Reactive] private string? _ps2OutputErrorText;

    [Reactive] private string? _mpr121ErrorText;

    [Reactive] private string? _max170XErrorText;
    [Reactive] private string? _ledErrorText;
    [Reactive] private string? _accelErrorText;

    private readonly ReadOnlyObservableCollection<Output> _outputs;
    public ReadOnlyObservableCollection<Output> Outputs => _outputs;

    private readonly ReadOnlyObservableCollection<Output> _digitalOutputs;
    public ReadOnlyObservableCollection<Output> DigitalOutputs => _digitalOutputs;

    public bool Branded { get; }
    public bool Builder { get; }

    private SourceList<int> AllPins { get; }


    private readonly ObservableAsPropertyHelper<float> _debounceDisplay;
    private readonly ObservableAsPropertyHelper<float> _strumDebounceDisplay;
    private DeviceControllerType _deviceControllerType;

    [Reactive] private int _mpr121CapacitiveCount;

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

    [Reactive] private string _variant = "";
    [Reactive] private bool _swapSwitchFaceButtons;

    [Reactive] private bool _combinedStrumDebounce;
    [Reactive] private string? _rfErrorText;
    [Reactive] private string? _usbHostErrorText;

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
    public Interaction<ConfigViewModel, SerializedConsoleKey?> LoadNand { get; } = new();
    public Interaction<ConfigViewModel, SerializedConsoleKey[]> LoadBackup { get; } = new();
    public Interaction<ConfigViewModel, Unit> ExportBackup { get; } = new();

    public Interaction<(ConfigViewModel model, Output output),
            BindAllWindowViewModel>
        ShowBindAllDialog { get; } = new();

    public List<int> AvailableSdaPinsInput => GetSdaPins(false);
    public List<int> AvailableSclPinsInput => GetSclPins(false);
    public List<int> AvailableSdaPinsOutput => GetSdaPins(true);
    public List<int> AvailableSclPinsOutput => GetSclPins(true);

    private List<int> GetSdaPins(bool output)
    {
        return Microcontroller.TwiPins(output)
            .Where(s => s.Value is TwiPinType.Sda)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetSclPins(bool output)
    {
        return Microcontroller.TwiPins(output)
            .Where(s => s.Value is TwiPinType.Scl)
            .Select(s => s.Key).ToList();
    }

    public ICommand BindAllCommand { get; }

    public MainWindowViewModel Main { get; }

    private SourceList<DeviceControllerType> _deviceControllerTypes = new SourceList<DeviceControllerType>();

    public ReadOnlyObservableCollection<DeviceControllerType> DeviceControllerRhythmTypes { get; }

    internal SourceList<SerializedConsoleKey> _keys { get; } = new();
    [Reactive] public SerializedConsoleKey? _selectedKey;
    public ReadOnlyObservableCollection<SerializedConsoleKey> Keys { get; }

    public IEnumerable<ModeType> ModeTypes => Enum.GetValues<ModeType>();

    // Only Pico supports bluetooth
    // Festival is no longer a supported type, we use console mode bindings for it now
    public IEnumerable<EmulationType> EmulationTypes => Enum.GetValues<EmulationType>()
        .Where(type =>
            type is not EmulationType.FortniteFestival &&
            (Device.IsPico ||
             type is not (EmulationType.Bluetooth or EmulationType.BluetoothKeyboardMouse)));


    public IEnumerable<MouseMovementType> MouseMovementTypes => Enum.GetValues<MouseMovementType>();
    public IEnumerable<LegendType> LegendTypes => Enum.GetValues<LegendType>();

    public ICommand WriteConfigCommand { get; }

    public ICommand WriteUf2Command { get; }

    public ICommand SaveConfigCommand { get; }
    public ICommand LoadConfigCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand GoBackCommand { get; }

    public string LocalAddress { get; set; } = "Write config to retrieve address";

    public IEnumerable<LedType> LedTypes => Enum.GetValues<LedType>()
        .Where(s => Microcontroller is Pico || !s.IsWs2812());

    public bool BindableTwi { get; }
    [Reactive] private bool _pollExpanded;

    [Reactive] private bool _presetsExpanded;
    [Reactive] private bool _controllerConfigExpanded;
    [Reactive] private bool _bluetoothConfigExpanded;

    [Reactive] private bool _testExpanded = true;
    [Reactive] private bool _hideControllerView;
    [Reactive] private bool _ledConfigExpanded;
    [Reactive] private bool _peripheralExpanded;
    [Reactive] private bool _wiiOutputExpanded;
    [Reactive] private bool _ps2OutputExpanded;
    [Reactive] private bool _mpr121Expanded;

    [Reactive] private bool _max170XExpanded;

    [Reactive] private bool _accelExpanded;

    [Reactive] private MouseMovementType _mouseMovementType;

    [Reactive] private int _debounce;

    [Reactive] private bool _selectDpadLeftXb1;

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

    private bool _sleepEnabled = false;

    public bool SleepEnabled
    {
        get => _sleepEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _sleepEnabled, value);
            _sleepPin = value ? new DirectPinConfig(this, WakeupPinType, -1, false, DevicePinMode.PullUp) : null;
        }
    }

    [Reactive] private int _ledSleep;

    [Reactive] private int _deviceSleep;

    [Reactive] private int _strumDebounce;

    [Reactive] private int _pollRate;

    [Reactive] private int _djPollRate;

    [Reactive] private bool _djFullRange;

    [Reactive] private bool _djNavButtons;
    private double _accelFilter;

    public double AccelFilter
    {
        get => _accelFilter;
        set
        {
            this.RaiseAndSetIfChanged(ref _accelFilter, value);
            if (Device is Santroller santroller)
            {
                santroller.SetAccelFilter(value);
            }
        }
    }

    [Reactive] private bool _djSmoothing;

    [Reactive] private string _btRxAddr;

    [Reactive] private bool _classic;

    public int Ws2812Data
    {
        get => _ws2812Config?.Pin ?? 0;
        set
        {
            if (_ws2812Config == null) return;
            _ws2812Config.Pin = value;
            this.RaisePropertyChanged();
        }
    }

    public int Ws2812DataPeripheral
    {
        get => _ws2812ConfigPeripheral?.Pin ?? 0;
        set
        {
            if (_ws2812ConfigPeripheral == null) return;
            _ws2812ConfigPeripheral.Pin = value;
            this.RaisePropertyChanged();
        }
    }

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

    private byte _ledBrightnessOn;

    public byte LedBrightnessOn
    {
        get => _ledBrightnessOn;
        set
        {
            this.RaiseAndSetIfChanged(ref _ledBrightnessOn, value);
            if (Device is not Santroller santroller || (LedType is LedType.None or LedType.Stp16Cpc26 &&
                                                        LedTypePeripheral is LedType.None or LedType.Stp16Cpc26))
                return;
            santroller.SetBrightness(value);
            foreach (var output in Bindings.Items.SelectMany(binding => binding.ValidOutputs()))
            {
                if (LedType is not (LedType.None or LedType.Stp16Cpc26) && output.LedIndices.Any() &&
                    output.LedOn != Colors.Black)
                {
                    foreach (var ledIndex in output.LedIndices)
                    {
                        santroller.SetLed((byte) (ledIndex - 1), output.LedOn, value);
                    }
                }

                if (LedTypePeripheral is not (LedType.None or LedType.Stp16Cpc26) &&
                    output.LedIndicesPeripheral.Any() && output.LedOn != Colors.Black)
                {
                    foreach (var ledIndex in output.LedIndicesPeripheral)
                    {
                        santroller.SetLedPeripheral((byte) (ledIndex - 1), output.LedOn, value);
                    }
                }
            }
        }
    }

    private byte _ledBrightnessOff;

    public byte LedBrightnessOff
    {
        get => _ledBrightnessOff;
        set
        {
            this.RaiseAndSetIfChanged(ref _ledBrightnessOff, value);
            if (Device is not Santroller santroller || (LedType is LedType.None or LedType.Stp16Cpc26 &&
                                                        LedTypePeripheral is LedType.None or LedType.Stp16Cpc26))
                return;
            santroller.SetBrightness(value);
            foreach (var output in Bindings.Items.SelectMany(binding => binding.ValidOutputs()))
            {
                if (LedType is not (LedType.None or LedType.Stp16Cpc26) && output.LedIndices.Any() &&
                    output.LedOn != Colors.Black)
                {
                    foreach (var ledIndex in output.LedIndices)
                    {
                        santroller.SetLed((byte) (ledIndex - 1), output.LedOn, (byte) value);
                    }
                }

                if (LedTypePeripheral is not (LedType.None or LedType.Stp16Cpc26) &&
                    output.LedIndicesPeripheral.Any() && output.LedOn != Colors.Black)
                {
                    foreach (var ledIndex in output.LedIndicesPeripheral)
                    {
                        santroller.SetLedPeripheral((byte) (ledIndex - 1), output.LedOn, (byte) value);
                    }
                }
            }
        }
    }

    public int AccelSda
    {
        get => _accelTwiConfig?.Sda ?? 0;
        set
        {
            if (_accelTwiConfig == null) return;
            _accelTwiConfig.Sda = value;
            this.RaisePropertyChanged();
        }
    }

    public int AccelScl
    {
        get => _accelTwiConfig?.Scl ?? 0;
        set
        {
            if (_accelTwiConfig == null) return;
            _accelTwiConfig.Scl = value;
            this.RaisePropertyChanged();
        }
    }

    public int Max1704XSda
    {
        get => _max170XTwiConfig?.Sda ?? 0;
        set
        {
            if (_max170XTwiConfig == null) return;
            _max170XTwiConfig.Sda = value;
            this.RaisePropertyChanged();
        }
    }

    public int Max1704XScl
    {
        get => _max170XTwiConfig?.Scl ?? 0;
        set
        {
            if (_max170XTwiConfig == null) return;
            _max170XTwiConfig.Scl = value;
            this.RaisePropertyChanged();
        }
    }

    public int Mpr121Sda
    {
        get => _mpr121TwiConfig?.Sda ?? 0;
        set
        {
            if (_mpr121TwiConfig == null) return;
            _mpr121TwiConfig.Sda = value;
            this.RaisePropertyChanged();
        }
    }

    public int Mpr121Scl
    {
        get => _mpr121TwiConfig?.Scl ?? 0;
        set
        {
            if (_mpr121TwiConfig == null) return;
            _mpr121TwiConfig.Scl = value;
            this.RaisePropertyChanged();
        }
    }

    public int Ps2OutputAck
    {
        get => _ps2OutputAck?.Pin ?? 0;
        set
        {
            if (_ps2OutputAck == null) return;
            _ps2OutputAck.Pin = value;
            this.RaisePropertyChanged();
        }
    }

    public int Ps2OutputAtt
    {
        get => _ps2OutputAtt?.Pin ?? 0;
        set
        {
            if (_ps2OutputAtt == null) return;
            _ps2OutputAtt.Pin = value;
            this.RaisePropertyChanged();
        }
    }

    public int Ps2OutputMosi
    {
        get => _ps2OutputSpiConfig?.Mosi ?? 0;
        set
        {
            if (_ps2OutputSpiConfig == null) return;
            _ps2OutputSpiConfig.Mosi = value;
            this.RaisePropertyChanged();
        }
    }

    public int Ps2OutputMiso
    {
        get => _ps2OutputSpiConfig?.Miso ?? 0;
        set
        {
            if (_ps2OutputSpiConfig == null) return;
            _ps2OutputSpiConfig.Miso = value;
            this.RaisePropertyChanged();
        }
    }

    public int Ps2OutputSck
    {
        get => _ps2OutputSpiConfig?.Sck ?? 0;
        set
        {
            if (_ps2OutputSpiConfig == null) return;
            _ps2OutputSpiConfig.Sck = value;
            this.RaisePropertyChanged();
        }
    }

    public int WiiOutputSda
    {
        get => _wiiOutputTwiConfig?.Sda ?? 0;
        set
        {
            if (_wiiOutputTwiConfig == null) return;
            _wiiOutputTwiConfig.Sda = value;
            this.RaisePropertyChanged();
        }
    }

    public int WiiOutputScl
    {
        get => _wiiOutputTwiConfig?.Scl ?? 0;
        set
        {
            if (_wiiOutputTwiConfig == null) return;
            _wiiOutputTwiConfig.Scl = value;
            this.RaisePropertyChanged();
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

    [Reactive] private bool _apa102IsFullSize;

    public int SleepWakeUpPin
    {
        get => _sleepPin?.Pin ?? 0;
        set
        {
            if (_sleepPin == null) return;
            _sleepPin.Pin = value;
            this.RaisePropertyChanged();
        }
    }

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

    [Reactive] public bool _connected;

    [Reactive] public bool _peripheralConnected;

    [Reactive] public bool _mpr121Connected;

    [Reactive] public bool _max1704XConnected;

    [Reactive] public int _max1704XStatus;

    [Reactive] public bool _accelConnected;

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

    [Reactive] private byte _ledCount;

    [Reactive] private byte _ledCountPeripheral;

    [Reactive] private int _wtSensitivity;

    [Reactive] private bool _hasError;

    public LedType LedType
    {
        get => _ledType;

        set
        {
            if (value != _ledType)
            {
                switch (value)
                {
                    case LedType.None:
                        _ws2812Config = null;
                        _ledSpiConfig = null;
                        _stp16Le = null;
                        _stp16Oe = null;
                        break;
                    case LedType.Stp16Cpc26:
                        _ws2812Config = null;
                        _ledSpiConfig = Microcontroller.AssignSpiPins(this, Stp16SpiType, false, true, false,
                            _ledSpiConfig != null ? LedMosi : -1, -1, _ledSpiConfig != null ? LedSck : -1, false,
                            false,
                            true,
                            Math.Min(Microcontroller.Board.CpuFreq / 2, 12000000), false);
                        _stp16Le = new DirectPinConfig(this, Stp16LeType, -1, false, DevicePinMode.Output);
                        _stp16Oe = new DirectPinConfig(this, Stp16OeType, -1, false, DevicePinMode.Output);
                        this.RaisePropertyChanged(nameof(Stp16Le));
                        this.RaisePropertyChanged(nameof(Stp16Oe));
                        this.RaisePropertyChanged(nameof(LedMosi));
                        this.RaisePropertyChanged(nameof(LedSck));
                        break;
                    default:
                    {
                        if (value.IsWs2812())
                        {
                            _ledSpiConfig = null;
                            _ws2812Config = new DirectPinConfig(this, WS2812SpiType, -1, false, DevicePinMode.Skip);
                            this.RaisePropertyChanged(nameof(LedMosi));
                            this.RaisePropertyChanged(nameof(LedSck));
                            _stp16Le = null;
                            _stp16Oe = null;
                        }
                        else if (value.IsApa102())
                        {
                            _ws2812Config = null;
                            _ledSpiConfig = Microcontroller.AssignSpiPins(this, Apa102SpiType, false, true, false,
                                _ledSpiConfig != null ? LedMosi : -1, -1, _ledSpiConfig != null ? LedSck : -1, true,
                                true,
                                true,
                                Math.Min(Microcontroller.Board.CpuFreq / 2, 12000000), false);
                            this.RaisePropertyChanged(nameof(LedMosi));
                            this.RaisePropertyChanged(nameof(LedSck));
                            _stp16Le = null;
                            _stp16Oe = null;
                        }

                        break;
                    }
                }

                UpdateErrors();
            }

            this.RaiseAndSetIfChanged(ref _ledType, value);
        }
    }

    public LedType LedTypePeripheral
    {
        get => _ledTypePeripheral;

        set
        {
            if (value != _ledTypePeripheral || _ledSpiConfigPeripheral == null)
            {
                switch (value)
                {
                    case LedType.None:
                        _ledSpiConfigPeripheral = null;
                        _ws2812ConfigPeripheral = null;
                        _stp16LePeripheral = null;
                        _stp16OePeripheral = null;
                        UpdateErrors();
                        break;
                    case LedType.Stp16Cpc26:
                        _ws2812ConfigPeripheral = null;
                        _ledSpiConfigPeripheral = Microcontroller.AssignSpiPins(this, Stp16PeripheralSpiType, true,
                            true, false,
                            _ledSpiConfigPeripheral != null ? LedMosiPeripheral : -1, -1,
                            _ledSpiConfigPeripheral != null ? LedSckPeripheral : -1, false,
                            false,
                            true,
                            Math.Min(Microcontroller.Board.CpuFreq / 2, 12000000), false);
                        _stp16LePeripheral =
                            new DirectPinConfig(this, Stp16LePeripheralType, -1, true, DevicePinMode.Output);
                        _stp16OePeripheral =
                            new DirectPinConfig(this, Stp16OePeripheralType, -1, true, DevicePinMode.Output);
                        this.RaisePropertyChanged(nameof(Stp16LePeripheral));
                        this.RaisePropertyChanged(nameof(Stp16OePeripheral));
                        this.RaisePropertyChanged(nameof(LedMosiPeripheral));
                        this.RaisePropertyChanged(nameof(LedSckPeripheral));
                        UpdateErrors();
                        break;
                    default:
                    {
                        if (value.IsWs2812())
                        {
                            _ledSpiConfigPeripheral = null;
                            _stp16OePeripheral =
                                new DirectPinConfig(this, WS2812PeripheralSpiType, -1, true, DevicePinMode.Skip);
                            this.RaisePropertyChanged(nameof(LedMosiPeripheral));
                            this.RaisePropertyChanged(nameof(LedSckPeripheral));

                            _stp16LePeripheral = null;
                            _stp16OePeripheral = null;
                            UpdateErrors();
                        }
                        else if (value.IsApa102())
                        {
                            _ws2812ConfigPeripheral = null;
                            _ledSpiConfigPeripheral = Microcontroller.AssignSpiPins(this, Apa102PeripheralSpiType, true,
                                true, false,
                                _ledSpiConfigPeripheral != null ? LedMosiPeripheral : -1, -1,
                                _ledSpiConfigPeripheral != null ? LedSckPeripheral : -1, true,
                                true,
                                true,
                                Math.Min(Microcontroller.Board.CpuFreq / 2, 12000000), false);
                            this.RaisePropertyChanged(nameof(LedMosiPeripheral));
                            this.RaisePropertyChanged(nameof(LedSckPeripheral));

                            _stp16LePeripheral = null;
                            _stp16OePeripheral = null;
                            UpdateErrors();
                        }

                        break;
                    }
                }
            }

            this.RaiseAndSetIfChanged(ref _ledTypePeripheral, value);
        }
    }

    [Reactive] private bool _xInputOnWindows;

    [Reactive] private bool _ps3OnRpcs3;

    [Reactive] private bool _ps4Instruments;
    private bool _hasPeripheral;
    private bool _hasWiiOutput;
    private bool _hasPs2Output;
    private bool _hasMpr121;

    public bool HasMpr121
    {
        get => _hasMpr121;
        set
        {
            if (value)
            {
                _mpr121TwiConfig =
                    Microcontroller.AssignTwiPins(this, Mpr121TwiType, false, -1, -1, Mpr121TwiFreq, false);
            }
            else
            {
                _mpr121TwiConfig = null;
                Bindings.RemoveMany(Bindings.Items.Where(s =>
                    s.Input.InnermostInputs().Any(s2 => s2 is Mpr121Input or Mpr121SliderInput)));
            }

            this.RaisePropertyChanged(nameof(Mpr121Sda));
            this.RaisePropertyChanged(nameof(Mpr121Scl));
            this.RaiseAndSetIfChanged(ref _hasMpr121, value);
            UpdateErrors();
        }
    }

    private bool _hasAccel;

    public bool HasAccel
    {
        get => _hasAccel;
        set
        {
            if (_hasAccel == value) return;
            _accelTwiConfig =
                value
                    ? Microcontroller.AssignTwiPins(this, AccelTwiType, false, -1, -1, AccelTwiFreq, false)
                    : null;

            this.RaisePropertyChanged(nameof(AccelSda));
            this.RaisePropertyChanged(nameof(AccelScl));
            this.RaiseAndSetIfChanged(ref _hasAccel, value);
            UpdateErrors();
        }
    }

    private bool _hasMax1704X;

    public bool HasMax1704X
    {
        get => _hasMax1704X;
        set
        {
            _max170XTwiConfig =
                value
                    ? Microcontroller.AssignTwiPins(this, Max170XTwiType, false, -1, -1, Max170XTwiFreq, false)
                    : null;

            this.RaisePropertyChanged(nameof(Max1704XSda));
            this.RaisePropertyChanged(nameof(Max1704XScl));
            this.RaiseAndSetIfChanged(ref _hasMax1704X, value);
            UpdateErrors();
        }
    }

    public bool HasPeripheral
    {
        get => _hasPeripheral;
        set
        {
            if (value)
            {
                _peripheralTwiConfig =
                    Microcontroller.AssignTwiPins(this, PeripheralTwiType, false, -1, -1, PeripheralTwiClock, false);
            }
            else
            {
                Bindings.RemoveMany(Bindings.Items.Where(s =>
                    s.Input.Peripheral || s is GhwtCombinedOutput {Peripheral: true}));
                _peripheralTwiConfig = null;
                this.RaiseAndSetIfChanged(ref _hasPeripheral, value);
                UpdateErrors();
                LedTypePeripheral = LedType.None;
            }

            this.RaisePropertyChanged(nameof(PeripheralSda));
            this.RaisePropertyChanged(nameof(PeripheralScl));
            this.RaiseAndSetIfChanged(ref _hasPeripheral, value);
            UpdateErrors();
        }
    }

    public bool HasWiiOutput
    {
        get => _hasWiiOutput;
        set
        {
            _wiiOutputTwiConfig =
                value
                    ? Microcontroller.AssignTwiPins(this, WiiOutputTwiType, false, -1, -1, WiiInput.WiiTwiFreq, true)
                    : null;

            this.RaisePropertyChanged(nameof(WiiOutputSda));
            this.RaisePropertyChanged(nameof(WiiOutputScl));
            this.RaiseAndSetIfChanged(ref _hasWiiOutput, value);
            UpdateErrors();
        }
    }

    public bool HasPs2Output
    {
        get => _hasPs2Output;
        set
        {
            if (value)
            {
                _ps2OutputSpiConfig = Microcontroller.AssignSpiPins(this, Ps2OutputTwiType, false, true, true, -1, -1,
                    -1, Ps2Input.Ps2SpiCpol, Ps2Input.Ps2SpiCpha,
                    Ps2Input.Ps2SpiMsbFirst, Ps2Input.Ps2SpiFreq, true);
                _ps2OutputAck = GetPinForType(Ps2OutputAckType, false, -1, DevicePinMode.Output);
                _ps2OutputAtt = GetPinForType(Ps2OutputAttType, false, -1, DevicePinMode.PullUp);
            }
            else
            {
                _ps2OutputSpiConfig = null;
                _ps2OutputAck = null;
                _ps2OutputAtt = null;
            }

            this.RaisePropertyChanged(nameof(Ps2OutputAck));
            this.RaisePropertyChanged(nameof(Ps2OutputAtt));
            this.RaisePropertyChanged(nameof(Ps2OutputMiso));
            this.RaisePropertyChanged(nameof(Ps2OutputMosi));
            this.RaisePropertyChanged(nameof(Ps2OutputSck));
            this.RaiseAndSetIfChanged(ref _hasPs2Output, value);
            UpdateErrors();
        }
    }

    private readonly ToolConfig _toolConfig = AssetUtils.GetConfig();

    [Reactive] private string _presetName =
        "";

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
            UpdateBindings(false);
        }
    }

    public Microcontroller Microcontroller { get; private set; }
    public SourceList<Output> Bindings { get; } = new();

    [Reactive] private Tuple<string, SerializedConfiguration>? _currentPreset;
    public ObservableCollection<Tuple<string, SerializedConfiguration>> Presets { get; } = [];
    public bool BindableSpi => IsPico;

    public IDisposable RegisterConnections()
    {
        return
            Main.AvailableDevices.Connect().ObserveOn(RxApp.MainThreadScheduler).Subscribe(s =>
            {
                foreach (var change in s)
                {
                    switch (change.Reason)
                    {
                        case ListChangeReason.Add:
                            AddDevice(change.Item.Current);
                            break;
                        case ListChangeReason.Remove:
                            RemoveDevice(change.Item.Current);
                            break;
                    }
                }
            });
    }

    [ObservableAsProperty] private string? _loadPresetLabel;

    [ObservableAsProperty] private string? _deletePresetLabel;

    [ObservableAsProperty] private string? _savePresetLabel;
    private bool _localDebounceMode;

    public bool LocalDebounceMode
    {
        get => _localDebounceMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _localDebounceMode, value);
            if (value)
            {
                Deque = false;
            }
        }
    }

    [ObservableAsProperty] private bool _isGuitar;

    [ObservableAsProperty] private bool _isDrum;

    [ObservableAsProperty] private bool _isTurntable;

    [ObservableAsProperty] private bool _isProKeys;

    [ObservableAsProperty] private bool _supportsPS4Instrument;

    public bool AdafruitHost
    {
        get => _adaFruitHostPin != null;
        set
        {
            if (value)
            {
                _adaFruitHostPin = new DirectPinConfig(this, AdafruitHostType, 18, false, DevicePinMode.Output);
                UsbHostDm = 17;
            }
            else
            {
                _adaFruitHostPin = null;
            }

            this.RaisePropertyChanged();
            UpdateErrors();
        }
    }

    [ObservableAsProperty] private bool _isGuitarHeroGuitar;

    [ObservableAsProperty] private bool _isStageKit;

    [ObservableAsProperty] private bool _isController;

    [ObservableAsProperty] private bool _isStandardController;

    [ObservableAsProperty] private bool _isRpcs3CompatibleController;

    [ObservableAsProperty] private bool _isFortniteFestivalPro;

    [ObservableAsProperty] private bool _isKeyboard;

    [ObservableAsProperty] private bool _isApa102;

    [ObservableAsProperty] private bool _isWs2812;

    [ObservableAsProperty] private bool _isWs2812Peripheral;

    [ObservableAsProperty] private bool _isApa102Peripheral;

    [ObservableAsProperty] private bool _isIndexedLed;

    [ObservableAsProperty] private bool _isIndexedLedPeripheral;

    [ObservableAsProperty] private bool _isStp16;

    [ObservableAsProperty] private bool _isStp16Peripheral;

    [Reactive] private bool _isBluetoothTx;

    [ObservableAsProperty] private bool _supportsDeque;

    [ObservableAsProperty] private string? _pollRateLabel;

    [ObservableAsProperty] private bool _isBluetooth;

    [ObservableAsProperty] private bool _isOrWasBluetooth;

    [ObservableAsProperty] private bool _isBluetoothRx;

    [ObservableAsProperty] private bool _hasWiiCombinedOutput;

    [ObservableAsProperty] private bool _hasPs2CombinedOutput;

    [ObservableAsProperty] private bool _hasGhwtCombinedOutput;

    [ObservableAsProperty] private bool _hasCloneCombinedOutput;

    [ObservableAsProperty] private bool _hasDjCombinedOutput;

    [ObservableAsProperty] private bool _hasGh5CombinedOutput;

    [ObservableAsProperty] private bool _hasUsbHostCombinedOutput;

    [ObservableAsProperty] private bool _usbHostEnabled;

    [ObservableAsProperty] private bool _hasMidi;

    [ObservableAsProperty] private int _minPollRate;

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
    public bool BindableAtt => Microcontroller is not (Uno or Mega);
    public List<int> AvailableMosiPinsInput => GetMosiPins(false);
    public List<int> AvailableMisoPinsInput => GetMisoPins(false);

    public List<int> AvailableMosiPinsPs2Output =>
        Microcontroller is Pico ? AvailablePinsDigital.ToList() : GetMosiPins(true);

    public List<int> AvailableMisoPinsPs2Output =>
        Microcontroller is Pico ? AvailablePinsDigital.ToList() : GetMisoPins(true);

    public List<int> AvailableSckPinsPs2Output =>
        Microcontroller is Pico ? AvailablePinsDigital.ToList() : GetSckPins();

    public List<int> AvailableSckPins => GetSckPins();

    private List<int> GetMosiPins(bool output)
    {
        return Microcontroller.SpiPins(output)
            .Where(s => s.Value is SpiPinType.Mosi)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetMisoPins(bool output)
    {
        return Microcontroller.SpiPins(output)
            .Where(s => s.Value is SpiPinType.Miso)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetSckPins()
    {
        return Microcontroller.SpiPins(false)
            .Where(s => s.Value is SpiPinType.Sck)
            .Select(s => s.Key).ToList();
    }

    public IEnumerable<PinConfig> PinConfigs =>
        new PinConfig?[]
            {
                _ledSpiConfig, _ws2812Config, _usbHostDm, _usbHostDp, _unoRx, _unoTx, _peripheralTwiConfig,
                _ledSpiConfigPeripheral, _stp16Le, _stp16Oe, _stp16LePeripheral, _stp16OePeripheral, _mpr121TwiConfig,
                _max170XTwiConfig, _wiiOutputTwiConfig, _ps2OutputSpiConfig, _ps2OutputAck, _ps2OutputAtt,
                _adaFruitHostPin, _accelTwiConfig
            }.Where(s => s != null)
            .Cast<PinConfig>();

    public string UrlPathSegment { get; } = Guid.NewGuid().ToString()[..5];
    public IScreen HostScreen { get; }
    public bool IsPico => Device.IsPico;

    public void SetDeviceTypeWithoutUpdating(DeviceControllerType type)
    {
        this.RaiseAndSetIfChanged(ref _deviceControllerType, type, nameof(DeviceControllerType));
    }

    [ReactiveCommand]
    public async Task ImportKey()
    {
        var key = await LoadNand.Handle(this);
        if (key != null)
        {
            var old = _keys.Items.FirstOrDefault(s => s.ConsoleId == key.ConsoleId);
            if (old != null)
            {
                _keys.Remove(old);
            }

            _keys.Add(key);
        }
    }

    [ReactiveCommand]
    public void DeleteKey()
    {
        if (SelectedKey != null)
        {
            _keys.Remove(SelectedKey);
        }
    }

    [ReactiveCommand]
    public async Task ImportKeyBackup()
    {
        var keys = await LoadBackup.Handle(this);
        _keys.AddRange(keys);
    }

    [ReactiveCommand]
    public async Task ExportKeyBackup()
    {
        await ExportBackup.Handle(this);
    }

    public void UpdateBindings(bool defaults)
    {
        foreach (var binding in Bindings.Items) binding.UpdateBindings();
        if (!DeviceControllerType.Is5FretGuitar() && !IsDrum)
        {
            Bindings.RemoveMany(Bindings.Items.Where(s => s is EmulationMode
            {
                Type: EmulationModeType.Fnf or EmulationModeType.FnfHid or EmulationModeType.FnfIos
                or EmulationModeType.FnfLayer
            }));
        }

        InstrumentButtonTypeExtensions.ConvertBindings(Bindings, this, false);
        if (!IsGuitar)
        {
            Deque = false;
        }

        var (extra, types) =
            ControllerEnumConverter.FilterValidOutputs(_deviceControllerType, Bindings.Items);
        Bindings.RemoveMany(extra);
        if (Bindings.Items.Any(s => s is WiiCombinedOutput))
        {
            PollRate = 5;
        }

        if (_deviceControllerType.IsProGuitar())
        {
            if (!Bindings.Items.Any(s => s is ProGuitarCombinedOutput))
            {
                var dj = new ProGuitarCombinedOutput(this) {Expanded = defaults};
                Bindings.Add(dj);
                dj.SetOutputsOrDefaults(Array.Empty<Output>());
            }
        }
        else
        {
            Bindings.RemoveMany(Bindings.Items.Where(s => s is ProGuitarCombinedOutput));
        }

        // If the user has a ps2 or wii combined output mapped, they don't need the default bindings
        if (Bindings.Items.Any(s =>
                s is WiiCombinedOutput or Ps2CombinedOutput or UsbHostCombinedOutput
                    or BluetoothCombinedOutput)) return;


        if (_deviceControllerType == DeviceControllerType.Turntable)
        {
            if (!Bindings.Items.Any(s => s is DjCombinedOutput))
            {
                var dj = new DjCombinedOutput(this, false) {Expanded = defaults};
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
                    Bindings.Add(new ControllerButton(this, true,
                        new DirectInput(-1, false, false, DevicePinMode.PullUp, this),
                        Colors.Black, Colors.Black, [], [], [], 1,
                        buttonType, false, false, false, -1, false) {Expanded = defaults});
                    break;
                case InstrumentButtonType buttonType:
                    Bindings.Add(new GuitarButton(this, true,
                        new DirectInput(-1, false, false, DevicePinMode.PullUp, this),
                        Colors.Black, Colors.Black, [], [], [], 1,
                        buttonType, false, false, false, -1, false) {Expanded = defaults});
                    break;
                case StandardAxisType axisType:
                    Bindings.Add(new ControllerAxis(this, true,
                            new DirectInput(-1, false, false, DevicePinMode.Analog, this),
                            Colors.Black, Colors.Black, [], [], [],
                            ushort.MinValue,
                            ushort.MaxValue,
                            ushort.MaxValue / 2, 0, ushort.MaxValue, axisType, false, false, false, -1, false)
                        {Expanded = defaults});
                    break;
                case GuitarAxisType.Slider:
                    break;
                case GuitarAxisType.Tilt when defaults:
                    Input input = Main.AccelSensorTypeMain switch
                    {
                        AccelSensorTypeMain.Digital => new DigitalToAnalog(
                            new DirectInput(-1, false, false, DevicePinMode.PullUp, this), short.MaxValue, this,
                            DigitalToAnalogType.Tilt),
                        AccelSensorTypeMain.Adxl345 or AccelSensorTypeMain.Lis3dh or AccelSensorTypeMain.Mpu6050 =>
                            new AccelInput(AccelInputType.AccelX, this),
                        _ => new DirectInput(-1, false, false, DevicePinMode.Analog, this)
                    };
                    if (Main.AccelSensorTypeMain is AccelSensorTypeMain.Adxl345 or AccelSensorTypeMain.Lis3dh
                        or AccelSensorTypeMain.Mpu6050)
                    {
                        AccelSensorType = Main.AccelSensorTypeMain switch
                        {
                            AccelSensorTypeMain.Adxl345 => AccelSensorType.Adxl345,
                            AccelSensorTypeMain.Lis3dh => AccelSensorType.Lis3dh,
                            AccelSensorTypeMain.Mpu6050 => AccelSensorType.Mpu6050,
                            _ => AccelSensorType
                        };
                        HasAccel = true;
                    }

                    Bindings.Add(new GuitarAxis(this, true, input,
                        Colors.Black, Colors.Black, [], [], [],
                        ushort.MinValue,
                        ushort.MaxValue,
                        0, false, GuitarAxisType.Tilt, false, false, false, -1, false) {Expanded = defaults});
                    break;
                case GuitarAxisType axisType:
                    Bindings.Add(new GuitarAxis(this, true, new DirectInput(-1,
                            false, false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, [], [], [],
                        ushort.MinValue,
                        ushort.MaxValue,
                        0, false, axisType, false, false, false, -1, false) {Expanded = defaults});
                    break;
                case DrumAxisType axisType:
                    Bindings.Add(new DrumAxis(this, true,
                        new DirectInput(-1, false, false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, [], [], [],
                        ushort.MinValue,
                        ushort.MaxValue,
                        0, 10, axisType, false, false, false, -1, false) {Expanded = defaults});
                    break;
                case DjAxisType.EffectsKnob:
                    Bindings.Add(new DjAxis(this, true,
                        new DirectInput(-1, false, false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, [], [], [], 1, 1,
                        DjAxisType.EffectsKnob, false, false, false, -1,
                        false) {Expanded = defaults});
                    break;
                case DjAxisType axisType:
                    if (axisType is DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity) continue;
                    Bindings.Add(new DjAxis(this, true,
                        new DirectInput(-1, false, false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, [], [], [],
                        ushort.MinValue,
                        ushort.MaxValue, 0, axisType, false, false, false, -1,
                        false) {Expanded = defaults});
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
        HasWiiOutput = false;
        HasPs2Output = false;
        AccelFilter = 0.05;
        Mpr121CapacitiveCount = 0;
        Deque = false;
        AccelSensorType = AccelSensorType.Adxl345;
        LedType = LedType.None;
        LedTypePeripheral = LedType.None;
        LedCount = 1;
        LedCountPeripheral = 1;
        _deviceControllerType = Main.DeviceControllerType;
        CombinedStrumDebounce = false;
        WtSensitivity = 5;
        PollRate = 0;
        StrumDebounce = 0;
        Debounce = 10;
        DjPollRate = 5;
        DjNavButtons = false;
        DjFullRange = true;
        LedBrightnessOn = 31;
        Apa102IsFullSize = false;
        DjSmoothing = false;
        SwapSwitchFaceButtons = false;
        HasPeripheral = false;
        BtRxAddr = "";
        this.RaisePropertyChanged(nameof(DeviceControllerType));
        XInputOnWindows = true;
        Ps3OnRpcs3 = true;
        Ps4Instruments = false;
        MouseMovementType = MouseMovementType.Relative;
        IsBluetoothTx = Main.BluetoothTx;

        if (IsBluetooth)
        {
            ResetBluetoothRelated();
        }
        else
        {
            // Reset max1704x state when we disable bluetooth.
            HasMax1704X = false;
        }

        if (Main.DeviceInputType is DeviceInputType.Usb)
        {
            UsbHostDm = 3;
        }

        AdafruitHost = Main.DeviceInputType is DeviceInputType.Feather;

        ClearOutputs();
        if (Main.Fortnite)
        {
            Input defInput = Main.DeviceInputType switch
            {
                DeviceInputType.Wii => new WiiInput(WiiInputType.GuitarPlus, this, false, 18, 19),
                DeviceInputType.Ps2 => new Ps2Input(Ps2InputType.GuitarStart, this, false, 4, 3, 6, 10, 7),
                DeviceInputType.Usb or DeviceInputType.Feather => new UsbHostInput(UsbHostInputType.Start, this),
                DeviceInputType.Bluetooth => new BluetoothInput(UsbHostInputType.Start, this),
                _ => new DirectInput(-1, false, false, DevicePinMode.PullUp, this)
            };

            Input defInput2 = Main.DeviceInputType switch
            {
                DeviceInputType.Wii => new WiiInput(WiiInputType.GuitarMinus, this, false, 18, 19),
                DeviceInputType.Ps2 => new Ps2Input(Ps2InputType.GuitarSelect, this, false, 4, 3, 6, 10, 7),
                DeviceInputType.Usb or DeviceInputType.Feather => new UsbHostInput(UsbHostInputType.Back, this),
                DeviceInputType.Bluetooth => new BluetoothInput(UsbHostInputType.Back, this),
                _ => new DirectInput(-1, false, false, DevicePinMode.PullUp, this)
            };

            Bindings.Add(new EmulationMode(this, true, defInput, EmulationModeType.Fnf)
            {
                Expanded = true
            });
            Bindings.Add(new EmulationMode(this, true,
                new MacroInput(defInput, defInput2, this), EmulationModeType.FnfLayer)
            {
                Expanded = true
            });
            Bindings.Add(new EmulationMode(this, true, defInput2, EmulationModeType.FnfIos)
            {
                Expanded = true
            });
        }

        switch (Main.DeviceInputType)
        {
            case DeviceInputType.Direct:
                if (_deviceControllerType is DeviceControllerType.KeyboardMouse) return;

                foreach (var type in Enum.GetValues<StandardAxisType>())
                {
                    if (ControllerEnumConverter.Convert(type, _deviceControllerType, LegendType, SwapSwitchFaceButtons)
                            .Length == 0) continue;
                    var isTrigger = type is StandardAxisType.LeftTrigger or StandardAxisType.RightTrigger;
                    int min = isTrigger ? ushort.MinValue : short.MinValue;
                    int max = isTrigger ? ushort.MaxValue : short.MaxValue;
                    Bindings.Add(new ControllerAxis(this, true,
                        new DirectInput(-1, false, false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, [], [], [],
                        min, max, (max + min) / 2, 0,
                        ushort.MaxValue, type, false, false, false, -1, false) {Expanded = true});
                }

                foreach (var type in Enum.GetValues<StandardButtonType>())
                {
                    if (ControllerEnumConverter.Convert(type, _deviceControllerType, LegendType, SwapSwitchFaceButtons)
                            .Length == 0) continue;
                    Bindings.Add(new ControllerButton(this, true,
                        new DirectInput(-1, false, false, DevicePinMode.PullUp, this),
                        Colors.Black, Colors.Black, [], [], [], 1,
                        type,
                        false, false, false, -1, false) {Expanded = true});
                }

                break;
            case DeviceInputType.Wii:
                var output = new WiiCombinedOutput(this, false, 18, 19)
                {
                    Expanded = true
                };
                Bindings.Add(output);
                output.SetOutputsOrDefaults(Array.Empty<Output>());
                break;
            case DeviceInputType.Ps2:
                var ps2Output = new Ps2CombinedOutput(this, false, 4, 3, 6, 10, 7)
                {
                    Expanded = true
                };
                Bindings.Add(ps2Output);
                ps2Output.SetOutputsOrDefaults(Array.Empty<Output>());
                break;
            case DeviceInputType.Usb:
            case DeviceInputType.Feather:
                var usbOutput = new UsbHostCombinedOutput(this)
                {
                    Expanded = true
                };
                Bindings.Add(usbOutput);
                usbOutput.SetOutputsOrDefaults(Array.Empty<Output>());
                break;
            case DeviceInputType.Bluetooth:
                var bluetoothOutput = new BluetoothCombinedOutput(this)
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

        BluetoothConfigExpanded = true;
        AccelExpanded = true;
        PeripheralExpanded = true;
        PollExpanded = true;
        ControllerConfigExpanded = true;
        LedConfigExpanded = true;


        UpdateBindings(true);
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

        Bindings.RemoveMany(Bindings.Items.Where(s => s is BluetoothCombinedOutput));
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

    public static string WriteBlob(BinaryWriter writer, double data)
    {
        var pos = writer.BaseStream.Length;
        if ((pos & 3) != 0)
        {
            var align = 4 - (pos & 3);
            for (var i = 0; i < align; i++)
            {
                writer.Write((byte) 0);
                pos += 1;
            }
        }

        writer.Write(data);
        return $"read_double({pos})";
    }

    public static string WriteBlob(BinaryWriter writer, bool data)
    {
        return WriteBlob(writer, data ? (byte) 1 : (byte) 0);
    }

    public static string WriteBlob(BinaryWriter writer, int data)
    {
        var pos = writer.BaseStream.Length;
        // uint16_t needs to be 2-byte aligned
        if ((pos & 1) != 0)
        {
            writer.Write((byte) 0);
            pos += 1;
        }

        writer.Write((short) data);
        return $"read_int16({pos})";
    }

    public static string WriteBlob(BinaryWriter writer, uint data)
    {
        var pos = writer.BaseStream.Length;
        // uint16_t needs to be 2-byte aligned
        if ((pos & 1) != 0)
        {
            writer.Write((byte) 0);
            pos += 1;
        }

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
            // TODO: make it posible to do pullup + wakeup
            writer = new BinaryWriter(blobStream);
            config += $$"""
                        #define CONFIGURABLE_BLOBS
                        #define CONFIGURATION_LEN {{WriteBlob(writer, configLength)}}
                        #define SWAP_SWITCH_FACE_BUTTONS {{WriteBlob(writer, SwapSwitchFaceButtons)}}
                        #define WINDOWS_USES_XINPUT {{WriteBlob(writer, XInputOnWindows && IsStandardController)}}
                        #define WINDOWS_TURNTABLE_FULLRANGE {{WriteBlob(writer, XInputOnWindows && DjFullRange)}}
                        #define RPCS3_COMPAT {{WriteBlob(writer, Ps3OnRpcs3 && IsRpcs3CompatibleController)}}
                        #define INPUT_QUEUE {{WriteBlob(writer, Deque)}}
                        #define WS2812W {{LedType.IsWs2812W().ToString().ToLower()}}
                        #define WS2812W_PERIPHERAL {{LedTypePeripheral.IsWs2812W().ToString().ToLower()}}
                        #define POLL_RATE {{WriteBlob(writer, (byte) PollRate)}}
                        #define INPUT_DJ_TURNTABLE_POLL_RATE {{WriteBlob(writer, (byte) DjPollRate)}}
                        #define INPUT_DJ_TURNTABLE_SMOOTHING {{WriteBlob(writer, DjSmoothing)}}
                        #define WT_SENSITIVITY {{WriteBlob(writer, WtSensitivity)}}
                        #define LED_BRIGHTNESS {{WriteBlob(writer, LedBrightnessOn)}}
                        #define LOW_PASS_ALPHA {{WriteBlob(writer, AccelFilter)}}
                        #define DJ_NAV_BUTTONS {{WriteBlob(writer, DjNavButtons)}}
                        #define COMBINED_DEBOUNCE {{WriteBlob(writer, CombinedStrumDebounce)}}
                        #define SLEEP_PIN {{WriteBlob(writer, SleepEnabled ? SleepWakeUpPin : -1)}}
                        #define SLEEP_INACTIVITY_TIMEOUT_MS {{WriteBlob(writer, DeviceSleep * 1000)}}
                        #define SLEEP_ACTIVE_HIGH {{WriteBlob(writer, false)}}
                        #define RGB_INACTIVITY_TIMEOUT_MS {{WriteBlob(writer, LedSleep * 1000)}}
                        """;

            if (IsBluetoothRx)
            {
                // Add space for null terminator
                var addr = new byte[Santroller.BtAddressLength + 1];
                // If we have a valid bluetooth address, write it
                if (BtRxAddr.Length != 0 && BtRxAddr.Contains("("))
                {
                    Array.Copy(Encoding.UTF8.GetBytes(BtRxAddr.Substring(BtRxAddr.Length - Santroller.BtAddressLength,
                        Santroller.BtAddressLength - 1)), addr, Santroller.BtAddressLength - 1);
                }
                else if (BtRxAddr.Length != 0 && BtRxAddr.Contains(":"))
                {
                    Array.Copy(Encoding.UTF8.GetBytes(BtRxAddr), addr, Santroller.BtAddressLength - 1);
                }

                config += $"""

                           #define BT_ADDR {WriteBlob(writer, addr)}
                           """;
                if (Classic)
                {
                    config += """

                              #define BLUETOOTH_RX_CLASSIC
                              """;
                }
                else
                {
                    config += """

                              #define BLUETOOTH_RX_BLE
                              """;
                }
            }
        }
        else
        {
            config += $$"""
                        #define CONFIGURATION_LEN {{configLength}}
                        #define SWAP_SWITCH_FACE_BUTTONS {{(!SwapSwitchFaceButtons).ToString().ToLower()}}
                        #define WINDOWS_USES_XINPUT {{(XInputOnWindows && IsStandardController).ToString().ToLower()}}
                        #define WINDOWS_TURNTABLE_FULLRANGE {{(XInputOnWindows && DjFullRange).ToString().ToLower()}}
                        #define RPCS3_COMPAT {{(Ps3OnRpcs3 && IsRpcs3CompatibleController).ToString().ToLower()}}
                        #define INPUT_QUEUE {{Deque.ToString().ToLower()}}
                        #define WS2812W {{LedType.IsWs2812W().ToString().ToLower()}}
                        #define WS2812W_PERIPHERAL {{LedTypePeripheral.IsWs2812W().ToString().ToLower()}}
                        #define POLL_RATE {{PollRate}}
                        #define WT_SENSITIVITY {{WtSensitivity}}
                        #define INPUT_DJ_TURNTABLE_POLL_RATE {{DjPollRate * 1000}}
                        #define INPUT_DJ_TURNTABLE_SMOOTHING {{DjSmoothing.ToString().ToLower()}}
                        #define LED_BRIGHTNESS {{LedBrightnessOn}}
                        #define LOW_PASS_ALPHA {{AccelFilter.ToString(CultureInfo.GetCultureInfo("en"))}}
                        #define DJ_NAV_BUTTONS {{DjNavButtons.ToString().ToLower()}}
                        #define COMBINED_DEBOUNCE {{CombinedStrumDebounce.ToString().ToLower()}}
                        #define SLEEP_PIN {{(SleepEnabled ? SleepWakeUpPin : -1)}}
                        #define SLEEP_INACTIVITY_TIMEOUT_MS {{DeviceSleep * 1000}}
                        #define SLEEP_ACTIVE_HIGH {{false.ToString().ToLower()}}
                        #define RGB_INACTIVITY_TIMEOUT_MS {{LedSleep * 1000}}
                        """;
            if (IsBluetoothRx)
            {
                if (BtRxAddr.Contains('('))
                {
                    config += $"""

                               #define BT_ADDR "{BtRxAddr.Substring(BtRxAddr.Length - Santroller.BtAddressLength,
                                   Santroller.BtAddressLength - 1)}"
                               """;
                }
                else if (BtRxAddr.Contains(':'))
                {
                    config += $"""

                               #define BT_ADDR "{BtRxAddr}"
                               """;
                }

                if (Classic)
                {
                    config += """

                              #define BLUETOOTH_RX_CLASSIC
                              """;
                }
                else
                {
                    config += """

                              #define BLUETOOTH_RX_BLE
                              """;
                }
            }
        }

        config += $"""

                   #define ABSOLUTE_MOUSE_COORDS {(MouseMovementType == MouseMovementType.Absolute).ToString().ToLower()}
                   #define ARDWIINO_BOARD "{Microcontroller.Board.ArdwiinoName}"
                   #define DEVICE_TYPE {(byte) DeviceControllerType}
                   #define PS4_INSTRUMENT {(Ps4Instruments && UsbHostEnabled && DeviceControllerType is DeviceControllerType.GuitarHeroDrums or DeviceControllerType.GuitarHeroGuitar or DeviceControllerType.RockBandDrums or DeviceControllerType.RockBandGuitar).ToString().ToLower()}
                   #define PRO_GUITAR {Outputs.Any(s => s is ProGuitarCombinedOutput).ToString().ToLower()}
                   """;

        if (IsBluetoothTx)
        {
            config += $"""

                       #define BLUETOOTH_TX true
                       """;
        }

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

                            {Microcontroller.GenerateDigitalWrite(Stp16Le, false, false, IsBluetooth)};
                            {Microcontroller.GenerateDigitalWrite(Stp16Oe, false, false, IsBluetooth)};
                            """;
            }

            if (IsStp16Peripheral)
            {
                ledInit += $"""

                            {Microcontroller.GenerateDigitalWrite(Stp16LePeripheral, false, true, IsBluetooth)};
                            {Microcontroller.GenerateDigitalWrite(Stp16OePeripheral, false, true, IsBluetooth)};
                            """;
            }

            ledInit = GenerateLedInit() + "\\\n\t" + GenerateTick(ConfigField.InitLed, writer) + "\\\n\t" +
                      FixNewlines(ledInit);
            config += "\n";
            var debounces = CalculateDebounceTicks(writer);
            config += $$"""
                        #define USB_HOST_STACK {{UsbHostEnabled.ToString().ToLower()}}
                        #define USB_HOST_DP_PIN {{UsbHostDp}}
                        #define DIGITAL_COUNT {{debounces.Item1}}
                        #define LED_DEBOUNCE_COUNT {{debounces.Item2}}
                        #define HAS_LED_OUTPUT {{Bindings.Items.SelectMany(s => s.Outputs.Items).Any(s => s.OutputEnabled).ToString().ToLower()}}
                        #define LED_COUNT {{(LedType.IsApa102() ? LedCount : 0)}}
                        #define LED_COUNT_PERIPHERAL {{(LedTypePeripheral.IsApa102() ? LedCountPeripheral : 0)}}
                        #define LED_COUNT_STP {{(LedType is LedType.Stp16Cpc26 ? LedCount : 0)}}
                        #define LED_COUNT_PERIPHERAL_STP {{(LedTypePeripheral is LedType.Stp16Cpc26 ? LedCountPeripheral : 0)}}
                        #define LED_COUNT_WS2812 {{(LedType.IsWs2812() ? LedCount : 0)}}
                        #define LED_COUNT_PERIPHERAL_WS2812 {{(LedTypePeripheral.IsWs2812() ? LedCountPeripheral : 0)}}
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
                        #define TICK_OG_XBOX \
                            {{GenerateTick(ConfigField.Xbox, writer)}}
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
                        #define HANDLE_RUMBLE_EXPANDED \
                            {{GenerateTick(ConfigField.RumbleLedExpanded, writer)}}
                        #define HANDLE_KEYBOARD_LED \
                            {{GenerateTick(ConfigField.KeyboardLed, writer)}}
                        #define PIN_INIT_PERIPHERAL \
                            {{GenerateInitPeripheral()}}
                        #define PIN_INIT \
                            {{GenerateInit()}}
                        #define LED_INIT \
                            {{ledInit}}
                        """;
            if (HasWiiOutput)
            {
                config += $"""
                                           
                           #define TICK_WII \
                             {GenerateTick(ConfigField.Wii, writer)}
                           """;
            }

            if (HasPs2Output)
            {
                config += $"""
                                           
                           #define TICK_PS2 \
                             {GenerateTick(ConfigField.Ps2, writer)}
                           """;
            }

            if (HasPs2Output && !IsPico)
            {
                config += $"""

                           #define PS2_ATT {Ps2OutputAtt}
                           #define PS2_OUTPUT_ACK_SET() {Microcontroller.GenerateDigitalWrite(Ps2OutputAck, true, false, IsBluetooth)}
                           #define PS2_OUTPUT_ACK_CLEAR() {Microcontroller.GenerateDigitalWrite(Ps2OutputAck, false, false, IsBluetooth)}
                           #define PS2_OUTPUT_ATT_READ() {Microcontroller.GenerateDigitalRead(Ps2OutputAtt, false, false)}
                           #define PS2_OUTPUT_SPI_PORT {_ps2OutputSpiConfig!.Definition}
                           """;
            }

            if (_wiiOutputTwiConfig != null)
            {
                config += $"""

                           #define WII_OUTPUT_TWI_PORT {_wiiOutputTwiConfig.Definition}
                           """;
            }

            if (_accelTwiConfig != null)
            {
                config += $"""

                           #define ACCEL_TWI_PORT {_accelTwiConfig.Definition}
                           """;
            }

            if (_hasAccel)
            {
                config += $"""

                           #define ACCEL_TYPE {(int) _accelSensorType}
                           """;
            }
            else
            {
                config += """

                          #define ACCEL_TYPE 0
                          """;
            }

            var keyboardTick = GenerateTick(ConfigField.Keyboard, writer);
            if (IsKeyboard || IsFortniteFestivalPro)
            {
                if (keyboardTick.Length != 0)
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
            if (consumerTick.Length != 0)
                config += $"""

                           #define TICK_CONSUMER \
                               {consumerTick}
                           """;

            var mouseTick = GenerateTick(ConfigField.Mouse, writer);
            if (mouseTick.Length != 0)
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

            if (IsWs2812)
            {
                config += $"""

                           #define TICK_LED \
                               {GenerateWs2812LedTick()}
                           """;
            }

            if (IsWs2812Peripheral)
            {
                config += $"""

                           #define TICK_LED_PERIPHERAL \
                               {GenerateWs2812PeripheralLedTick()}
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

            var ledCount = Outputs.SelectMany(s => s.Outputs.Items).SelectMany(s => s.LedIndicesMpr121)
                .DefaultIfEmpty<byte>(0).Max();
            config += $"""

                       #define LED_COUNT_MPR121 {ledCount}
                       """;

            var ledTick = GenerateTick(ConfigField.StrobeLed, writer);
            if (ledTick.Length != 0)
            {
                config += $"""

                           #define TICK_LED_STROBE \
                               {ledTick}
                           """;
            }


            if (IsFortniteFestivalPro)
            {
                var festivalTick = GenerateTick(ConfigField.Festival, writer);
                if (festivalTick.Length != 0)
                {
                    config += $"""

                               #define TICK_FESTIVAL \
                                   {festivalTick}
                               """;
                }
            }

            var ledTickBt = GenerateTick(ConfigField.BluetoothLed, writer);
            if (ledTickBt.Length != 0)
            {
                config += $"""

                           #define TICK_LED_BLUETOOTH \
                               {ledTickBt}
                           """;
            }

            var fnfDetTick = GenerateTick(ConfigField.DetectionFestival, writer);
            if (fnfDetTick.Length != 0)
            {
                config += $"""

                           #define TICK_DETECTION_FESTIVAL \
                               {fnfDetTick}
                           """;
            }

            var offLed = GenerateTick(ConfigField.OffLed, writer);
            if (offLed.Length != 0)
                config += $"""

                           #define HANDLE_LED_RUMBLE_OFF \
                               {offLed}
                           """;

            var actualPinConfigs = GetPinConfigs();
            var twiConfigs = actualPinConfigs.OfType<TwiConfig>().GroupBy(s => s.Definition);
            foreach (var pinConfigse in twiConfigs)
            {
                if (pinConfigse.Any() && pinConfigse.Any(s => s.Type == WiiInput.WiiTwiType))
                {
                    config += """

                              #define WII_SHARED
                              """;
                }

                var min = pinConfigse.MinBy(s => s.Clock);
                actualPinConfigs.RemoveMany(pinConfigse);
                actualPinConfigs.Add(min!);
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

            if (_max170XTwiConfig != null)
            {
                config += $"\n#define MAX1704X_TWI_PORT {_max170XTwiConfig.Definition}";
            }

            if (_mpr121TwiConfig != null)
            {
                // GPIO bit 0 is actually sensor 4
                // Capacitive touch overrides GPIO so exclude capacitive when calculating GPIO
                config += $"""

                           #define MPR121_TWI_PORT {_mpr121TwiConfig.Definition}
                           #define MPR121_TOUCHPADS {Mpr121CapacitiveCount}
                           #define MPR121_DDR {outputs.SelectMany(s => s.LedIndicesMpr121).Select(s => 1 << s - 4).DefaultIfEmpty(0).Aggregate((acc, s) => acc | s)}
                           #define MPR121_ENABLE {outputs.SelectMany(s => s.LedIndicesMpr121.Select(led => (int) led).Concat(s.Input.InnermostInputs().Where(input => input is Mpr121Input or Mpr121SliderInput).SelectMany(input => input switch
                           {
                               Mpr121Input mpr121Input => [mpr121Input.Input],
                               Mpr121SliderInput mpr121SliderInput => mpr121SliderInput.MappedInputs,
                               _ => Array.Empty<int>()
                           }))).Where(s => s >= Mpr121CapacitiveCount && s >= 4).Select(s => 1 << s - 4).DefaultIfEmpty(0).Aggregate((acc, s) => acc | s)}
                           """;
                ;
            }

            if (_ledSpiConfig != null)
            {
                config += $"\n#define APA102_SPI_PORT {_ledSpiConfig.Definition}";
            }

            if (_ws2812Config != null)
            {
                config += $"\n#define WS2812_PIN {_ws2812Config.Pin}";
            }

            if (Outputs.Any(output => output.Input.InnermostInputs().Any(input => input is MultiplexerInput
                {
                    MultiplexerType: MultiplexerType.EightChannelSlow or MultiplexerType.SixteenChannelSlow
                })))
            {
                config += "\n#define CD4051BE";
            }

            if (Keys.Count == 0) return config;
            if (writer != null)
            {
                config += $$"""

                            #define KV_KEY_ARRAY {{WriteBlob(writer, Keys.SelectMany(s => s.Combined).ToArray())}}
                            #define KV_KEY_SIZE {{WriteBlob(writer, (byte) Keys.Count)}}
                            """;
            }
            else
            {
                config += $$"""

                            #define KV_KEY_ARRAY {{{string.Join(",", Keys.Select(s => s.Format()))}}}
                            #define KV_KEY_SIZE {{Keys.Count}}
                            """;
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
                      #define TICK_OG_XBOX
                      #define DIGITAL_COUNT 0
                      #define LED_DEBOUNCE_COUNT 0
                      #define LED_COUNT 0
                      #define LED_COUNT_MPR121 0
                      #define LED_COUNT_PERIPHERAL 0
                      #define HANDLE_AUTH_LED
                      #define HANDLE_PLAYER_LED
                      #define HANDLE_LIGHTBAR_LED
                      #define HANDLE_RUMBLE
                      #define HANDLE_RUMBLE_EXPANDED
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
        var ret = "";
        if (_adaFruitHostPin != null)
        {
            ret += $"""

                    {Microcontroller.GenerateDigitalWrite(_adaFruitHostPin.Pin, true, false, IsBluetooth)};
                    """;
        }

        return FixNewlines(Microcontroller.GenerateLedInit(this) + ret);
    }

    private string GenerateInit()
    {
        var ret = "";
        if (_adaFruitHostPin != null)
        {
            ret += $"""

                    {Microcontroller.GenerateDigitalWrite(_adaFruitHostPin.Pin, true, false, IsBluetooth)};
                    """;
        }

        return FixNewlines(Microcontroller.GenerateInit(this) + ret);
    }

    private string GenerateInitPeripheral()
    {
        return FixNewlines(GetPinConfigs().OfType<DirectPinConfig>()
            .Where(s => s.PinMode != DevicePinMode.Skip && s.Peripheral).Aggregate("",
                (current, pin) => current + $"\nslavePinMode({pin.Pin},{(byte) pin.PinMode});"));
    }

    public PinConfig[] UsbHostPinConfigs()
    {
        return UsbHostEnabled ? [_usbHostDm, _usbHostDp] : [];
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
    public void RemoveMax1704X()
    {
        HasMax1704X = false;
    }

    [RelayCommand]
    public void RemoveAccel()
    {
        HasAccel = false;
        Bindings.RemoveMany(Bindings.Items.Where(s => s.Input.InnermostInputs().Any(s2 => s2 is AccelInput)));
    }

    [RelayCommand]
    public void RemoveMpr121()
    {
        HasMpr121 = false;
    }

    [RelayCommand]
    public void RemovePeripheral()
    {
        HasPeripheral = false;
    }

    [RelayCommand]
    public void RemoveBtTxCommand()
    {
        IsBluetoothTx = false;
    }

    [RelayCommand]
    public void RemoveWiiOutput()
    {
        HasWiiOutput = false;
    }

    [RelayCommand]
    public void RemovePs2Output()
    {
        HasPs2Output = false;
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
                Main.DeviceControllerType = response.DeviceControllerType;
                Main.DeviceInputType = response.DeviceInputType;
                Main.Fortnite = response.Fortnite;
                Main.BluetoothTx = response.BluetoothTx;
                SetDefaults();
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
            Bindings.Add(new KeyboardButton(this, true, new DirectInput(0, false, false, DevicePinMode.PullUp, this),
                Colors.Black, Colors.Black, [], [], [], 1, Key.Space,
                false, false, false, -1));

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
        var strings = LedType.GetLedStrings("brightness", "r", "g", "b").ToArray();
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

                 spi_transfer(APA102_SPI_PORT, ledState[{i}].{strings[0]} | 0xE0);
                 spi_transfer(APA102_SPI_PORT, ledState[{i}].{strings[1]});
                 spi_transfer(APA102_SPI_PORT, ledState[{i}].{strings[2]});
                 spi_transfer(APA102_SPI_PORT, ledState[{i}].{strings[3]});
                 """;
        }

        ret +=
            """

            spi_transfer(APA102_SPI_PORT, 0x00);
            spi_transfer(APA102_SPI_PORT, 0x00);
            spi_transfer(APA102_SPI_PORT, 0x00);
            spi_transfer(APA102_SPI_PORT, 0x00);
            """;

        for (var i = 0; i <= ledMax; i += 16)
        {
            ret += """

                   spi_transfer(APA102_SPI_PORT, 0xff);
                   """;
        }

        return FixNewlines(ret);
    }

    private string GenerateWs2812LedTick()
    {
        var outputs = Bindings.Items.SelectMany(binding => binding.ValidOutputs()).ToList();
        if (!outputs.Any(s => s.LedIndices.Any())) return "";
        if (LedType == LedType.None) return "";
        var ret = "";
        var ledMax = LedCount;
        var strings = LedType.GetLedStrings("brightness", "r", "g", "b").ToArray();
        for (var i = 0; i < ledMax; i++)
        {
            ret +=
                $"""

                 putWs2812(ledState[{i}].{strings[0]}, ledState[{i}].{strings[1]}, ledState[{i}].{strings[2]});
                 """;
        }

        return FixNewlines(ret);
    }

    private string GenerateWs2812PeripheralLedTick()
    {
        var outputs = Bindings.Items.SelectMany(binding => binding.ValidOutputs()).ToList();
        if (!outputs.Any(s => s.LedIndices.Any())) return "";
        if (LedTypePeripheral == LedType.None) return "";
        var ret = "";
        var ledMax = LedCount;
        for (var i = 0; i < ledMax; i++)
        {
            ret +=
                $"""

                 slaveWriteLED(ledState[{i}].r);
                 slaveWriteLED(ledState[{i}].g);
                 slaveWriteLED(ledState[{i}].b);
                 """;
        }

        return FixNewlines(ret);
    }

    private string GenerateApa102LedPeripheralTick()
    {
        var outputs = Bindings.Items.SelectMany(binding => binding.ValidOutputs()).ToList();
        if (!outputs.Any(s => s.LedIndicesPeripheral.Any())) return "";
        if (LedTypePeripheral == LedType.None) return "";
        var strings = LedTypePeripheral.GetLedStrings("brightness", "r", "g", "b").ToArray();
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

                 slaveWriteLED(ledStatePeripheral[{i}].{strings[0]} | 0xE0);
                 slaveWriteLED(ledStatePeripheral[{i}].{strings[1]});
                 slaveWriteLED(ledStatePeripheral[{i}].{strings[2]});
                 slaveWriteLED(ledStatePeripheral[{i}].{strings[3]});
                 """;
        }

        ret +=
            """

            slaveWriteLED(0x00);
            slaveWriteLED(0x00);
            slaveWriteLED(0x00);
            slaveWriteLED(0x00);
            """;
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

             {Microcontroller.GenerateDigitalWrite(Stp16Le, true, false, IsBluetooth)};
             delayMicroseconds(10);
             {Microcontroller.GenerateDigitalWrite(Stp16Le, false, false, IsBluetooth)};
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

             {Microcontroller.GenerateDigitalWrite(Stp16LePeripheral, true, true, IsBluetooth)};
             delayMicroseconds(10);
             {Microcontroller.GenerateDigitalWrite(Stp16LePeripheral, false, true, IsBluetooth)};
             """;

        return FixNewlines(ret);
    }

    private string ComputeLedsStp16(bool peripheral,
        Dictionary<byte, List<(Output, int)>> debouncesRelatedToLed,
        Dictionary<byte, List<OutputAxis>> analogRelatedToLed, BinaryWriter? writer)
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
                        $"bit_write({analogLedOutput.Input.Generate(writer)}, {variable}[{index / 8}],{index % 8});");
            }

            if (analog.Length == 0)
            {
                analog = $"bit_clear({variable}[{index / 8}],{index % 8});";
            }

            ret += $$"""

                     if (!bit_check({{variable}}Select[{{index / 8}}],{{index % 8}})) {
                     """;
            ret += string.Join(" else ", relatedOutputs.DistinctBy(tuple => tuple.Item1).Select(tuple =>
            {
                var ifStatement = $"ledDebounce[{tuple.Item2}]";
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
                    current +
                    $"bit_write({analogLedOutput.Input.Generate(writer)}, {variable}[{index / 8}],{index % 8});");

            ret += "}";
        }

        return ret;
    }

    private string ComputeLedsWs2812(ConfigField mode, bool peripheral,
        Dictionary<byte, List<(Output, int)>> debouncesRelatedToLed,
        Dictionary<byte, List<OutputAxis>> analogRelatedToLed, BinaryWriter? writer)
    {
        var ret = "";
        var type = peripheral ? LedTypePeripheral : LedType;
        var variable = peripheral ? "ledStatePeripheral" : "ledState";
        if (mode != ConfigField.Shared || type is LedType.None) return "";

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
                        var generated = $"({analogLedOutput.Input.Generate(writer)})";
                        var isI2C = analogLedOutput.Input is DjInput
                        {
                            Input: DjInputType.LeftTurntable or DjInputType.RightTurntable
                        };
                        ledReadCheck = analogLedOutput.Input.Generate(writer);
                        if (analogLedOutput.InputIsUint)
                        {
                            ledReadCheck = $"({generated} - INT16_MAX)";
                        }
                        else
                        {
                            generated = $"({generated} + INT16_MAX)";
                        }

                        ledRead = isI2C
                            ? $"handle_calibration_turntable_ps3_i2c(0, {analogLedOutput.Input.Generate(writer)},{multiplier})"
                            : $"handle_calibration_turntable_ps3(0, {generated},{multiplier})";
                    }

                    if (analogLedOutput is DjAxis {Type: DjAxisType.EffectsKnob})
                    {
                        var generated = $"({analogLedOutput.Input.Generate(writer)})";
                        if (!analogLedOutput.InputIsUint)
                        {
                            generated = $"({generated} + INT16_MAX)";
                        }

                        ledRead = $"(({generated} >> 8))";
                    }

                    if (analogLedOutput.Input is DigitalToAnalog dta)
                    {
                        var on = dta.On >> 8;
                        if (dta.Type != DigitalToAnalogType.Trigger)
                        {
                            on += sbyte.MaxValue;
                        }

                        ledRead = $"(({ledRead}) ? {on} : 0)";
                    }

                    // Now we have the value, calibrated as a uint8_t
                    // Only apply analog colours if non zero when conflicting with digital, so that the digital off states override
                    analog +=
                        $$"""
                          led_tmp = {{ledRead}};
                          if({{ledReadCheck}}) {
                              {{type.GetLedAssignment(peripheral, led, analogLedOutput.LedOn, analogLedOutput.LedOff, LedBrightnessOn, LedBrightnessOff, "led_tmp", writer)}}
                          } else {
                              {{type.GetLedAssignment(peripheral, relatedOutputs.First().Item1.LedOff, led, LedBrightnessOff, writer)}}
                          }
                          """;
                }
            }

            if (analog.Length == 0)
            {
                analog = type.GetLedAssignment(peripheral, relatedOutputs.First().Item1.LedOff, led, LedBrightnessOff,
                    writer);
            }

            ret += $$"""

                     if ({{variable}}[{{led - 1}}].select == 0) {
                     """;
            ret += string.Join(" else ", relatedOutputs.DistinctBy(tuple => tuple.Item1).Select(tuple =>
            {
                var ifStatement = $"ledDebounce[{tuple.Item2}]";
                return $$"""
                         
                             if ({{ifStatement}}) {
                                 {{type.GetLedAssignment(peripheral, tuple.Item1.LedOn, led, LedBrightnessOn, writer)}}
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
                    var generated = $"({analogLedOutput.Input.Generate(writer)})";
                    var isI2C = analogLedOutput.Input is DjInput
                    {
                        Input: DjInputType.LeftTurntable or DjInputType.RightTurntable
                    };
                    if (!analogLedOutput.InputIsUint)
                    {
                        generated = $"({generated} + INT16_MAX)";
                    }

                    ledRead = isI2C
                        ? $"handle_calibration_turntable_ps3_i2c(0, {analogLedOutput.Input.Generate(writer)},{multiplier})"
                        : $"handle_calibration_turntable_ps3(0, {generated},{multiplier})";
                }

                if (analogLedOutput is DjAxis {Type: DjAxisType.EffectsKnob})
                {
                    var generated = $"({analogLedOutput.Input.Generate(writer)})";
                    if (!analogLedOutput.InputIsUint)
                    {
                        generated = $"({generated} + INT16_MAX)";
                    }

                    ledRead = $"(({generated} >> 8))";
                }

                if (analogLedOutput.Input is DigitalToAnalog dta)
                {
                    var on = dta.On >> 8;
                    if (dta.Type != DigitalToAnalogType.Trigger)
                    {
                        on += sbyte.MaxValue;
                    }

                    ledRead = $"(({ledRead}) ? {on} : 0)";
                }

                // Now we have the value, calibrated as a uint8_t
                ret +=
                    $"led_tmp = {ledRead};{type.GetLedAssignment(peripheral, led, analogLedOutput.LedOn, analogLedOutput.LedOff, LedBrightnessOn, LedBrightnessOff, "led_tmp", writer)}";
            }

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
            return ComputeLedsStp16(peripheral, debouncesRelatedToLed, analogRelatedToLed, writer);

        if (type.IsWs2812())
        {
            return ComputeLedsWs2812(mode, peripheral, debouncesRelatedToLed, analogRelatedToLed, writer);
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
                        analogLedOutput.GenerateAssignment(analogLedOutput.Trigger ? "0" : "128", ConfigField.Ps3,
                            false, false, false, false, writer);

                    var ledReadCheck = "led_tmp";
                    if (!analogLedOutput.Trigger)
                    {
                        ledReadCheck = "(led_tmp != 128)";
                    }

                    // Turntable velocities are different to most axis, as they don't use standard calibration.
                    if (analogLedOutput is DjAxis
                        {
                            Type: DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity
                        } djAxis)
                    {
                        var multiplier = djAxis.LedMultiplier;
                        var generated = $"({analogLedOutput.Input.Generate(writer)})";
                        var isI2C = analogLedOutput.Input is DjInput
                        {
                            Input: DjInputType.LeftTurntable or DjInputType.RightTurntable
                        };
                        ledReadCheck = analogLedOutput.Input.Generate(writer);
                        if (analogLedOutput.InputIsUint)
                        {
                            ledReadCheck = $"({generated} - INT16_MAX)";
                        }
                        else
                        {
                            generated = $"({generated} + INT16_MAX)";
                        }

                        ledRead = isI2C
                            ? $"handle_calibration_turntable_ps3_i2c(0, {analogLedOutput.Input.Generate(writer)},{multiplier})"
                            : $"handle_calibration_turntable_ps3(0, {generated},{multiplier})";
                    }

                    if (analogLedOutput is DjAxis {Type: DjAxisType.EffectsKnob})
                    {
                        var generated = $"({analogLedOutput.Input.Generate(writer)})";
                        if (!analogLedOutput.InputIsUint)
                        {
                            generated = $"({generated} + INT16_MAX)";
                        }

                        ledRead = $"(({generated} >> 8))";
                    }

                    if (analogLedOutput.Input is DigitalToAnalog dta)
                    {
                        var on = dta.On >> 8;
                        if (dta.Type != DigitalToAnalogType.Trigger)
                        {
                            on += sbyte.MaxValue;
                        }

                        ledRead = $"(({ledRead}) ? {on} : 0)";
                    }

                    // Now we have the value, calibrated as a uint8_t
                    // Only apply analog colours if non zero when conflicting with digital, so that the digital off states override
                    analog +=
                        $$"""
                          led_tmp = {{ledRead}};
                          if({{ledReadCheck}}) {
                              {{type.GetLedAssignment(peripheral, led, analogLedOutput.LedOn, analogLedOutput.LedOff, LedBrightnessOn, LedBrightnessOff, "led_tmp", writer)}}
                          } else {
                              {{type.GetLedAssignment(peripheral, relatedOutputs.First().Item1.LedOff, led, LedBrightnessOff, writer)}}
                          }
                          """;
                }
            }

            if (analog.Length == 0)
            {
                analog = type.GetLedAssignment(peripheral, relatedOutputs.First().Item1.LedOff, led, LedBrightnessOff,
                    writer);
            }

            ret += $$"""

                     if ({{variable}}[{{led - 1}}].select == 0) {
                     """;
            ret += string.Join(" else ", relatedOutputs.DistinctBy(tuple => tuple.Item1).Select(tuple =>
            {
                var ifStatement = $"ledDebounce[{tuple.Item2}]";
                return $$"""
                         
                             if ({{ifStatement}}) {
                                 {{type.GetLedAssignment(peripheral, tuple.Item1.LedOn, led, LedBrightnessOn, writer)}}
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
                    analogLedOutput.GenerateAssignment(analogLedOutput.Trigger ? "0" : "128", ConfigField.Ps3, false,
                        false, false, false, writer);
                // Turntable velocities are different to most axis, as they don't use standard calibration.
                if (analogLedOutput is DjAxis
                    {
                        Type: DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity
                    } djAxis)
                {
                    var multiplier = djAxis.LedMultiplier;
                    var generated = $"({analogLedOutput.Input.Generate(writer)})";
                    var isI2C = analogLedOutput.Input is DjInput
                    {
                        Input: DjInputType.LeftTurntable or DjInputType.RightTurntable
                    };
                    if (!analogLedOutput.InputIsUint)
                    {
                        generated = $"({generated} + INT16_MAX)";
                    }

                    ledRead = isI2C
                        ? $"handle_calibration_turntable_ps3_i2c(0, {analogLedOutput.Input.Generate(writer)},{multiplier})"
                        : $"handle_calibration_turntable_ps3(0, {generated},{multiplier})";
                }

                if (analogLedOutput is DjAxis {Type: DjAxisType.EffectsKnob})
                {
                    var generated = $"({analogLedOutput.Input.Generate(writer)})";
                    if (!analogLedOutput.InputIsUint)
                    {
                        generated = $"({generated} + INT16_MAX)";
                    }

                    ledRead = $"(({generated} >> 8))";
                }

                if (analogLedOutput.Input is DigitalToAnalog dta)
                {
                    var on = dta.On >> 8;
                    if (dta.Type != DigitalToAnalogType.Trigger)
                    {
                        on += sbyte.MaxValue;
                    }

                    ledRead = $"(({ledRead}) ? {on} : 0)";
                }

                // Now we have the value, calibrated as a uint8_t
                ret +=
                    $"led_tmp = {ledRead};{type.GetLedAssignment(peripheral, led, analogLedOutput.LedOn, analogLedOutput.LedOff, LedBrightnessOn, LedBrightnessOff, "led_tmp", writer)}";
            }

            ret += "}";
        }

        return ret;
    }

    private string ComputeLedsPin(ConfigField mode, bool peripheral,
        Dictionary<int, List<(Output, int)>> debouncesRelatedToLed,
        Dictionary<int, List<OutputAxis>> analogRelatedToLed, BinaryWriter? writer)
    {
        if (mode != ConfigField.Shared) return "";
        var ret = "";
        // Handle leds, including when multiple leds are assigned to a single output.
        foreach (var (pin, relatedOutputs) in debouncesRelatedToLed)
        {
            var analog = "";
            if (analogRelatedToLed.TryGetValue(pin, out var analogLedOutputs))
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
                        var generated = $"({analogLedOutput.Input.Generate(writer)})";
                        var isI2C = analogLedOutput.Input is DjInput
                        {
                            Input: DjInputType.LeftTurntable or DjInputType.RightTurntable
                        };
                        ledReadCheck = analogLedOutput.Input.Generate(writer);
                        if (analogLedOutput.InputIsUint)
                        {
                            ledReadCheck = $"({generated} - INT16_MAX)";
                        }
                        else
                        {
                            generated = $"({generated} + INT16_MAX)";
                        }

                        ledRead = isI2C
                            ? $"handle_calibration_turntable_ps3_i2c(0, {analogLedOutput.Input.Generate(writer)},{multiplier})"
                            : $"handle_calibration_turntable_ps3(0, {generated},{multiplier})";
                    }

                    if (analogLedOutput is DjAxis {Type: DjAxisType.EffectsKnob})
                    {
                        var generated = $"({analogLedOutput.Input.Generate(writer)})";
                        if (!analogLedOutput.InputIsUint)
                        {
                            generated = $"({generated} + INT16_MAX)";
                        }

                        ledRead = $"(({generated} >> 8))";
                    }

                    if (analogLedOutput.Input is DigitalToAnalog dta)
                    {
                        var on = dta.On >> 8;
                        if (dta.Type != DigitalToAnalogType.Trigger)
                        {
                            on += sbyte.MaxValue;
                        }

                        ledRead = $"(({ledRead}) ? {on} : 0)";
                    }

                    // Now we have the value, calibrated as a uint8_t
                    // Only apply analog colours if non zero when conflicting with digital, so that the digital off states override
                    analog +=
                        $$"""
                          if ({{ledReadCheck}}) {
                            led_tmp = {{ledRead}};
                            {{Microcontroller.GenerateAnalogWrite(pin, $"{(analogLedOutput.OutputInverted ? "(255-" : "(")}led_tmp)", peripheral)}};
                          }
                          """;
                }
            }

            // If there are any analog outputs here, then we need to convert the matching digital writes to analog writes
            if (analog.Length != 0)
            {
                ret += string.Join(" else ", relatedOutputs.DistinctBy(tuple => tuple.Item1).Select(tuple =>
                {
                    var ifStatement = $"ledDebounce[{tuple.Item2}]";
                    return $$"""

                             if ({{ifStatement}}) {
                                 {{Microcontroller.GenerateAnalogWrite(pin, (tuple.Item1.OutputInverted ? 0 : 255).ToString(), peripheral)}};
                             }
                             """;
                }));
                ret += $$"""
                         else {
                             {{analog}}
                         }
                         """;
            }
            else
            {
                // Otherwise just do digital writes
                ret += string.Join(" else ", relatedOutputs.DistinctBy(tuple => tuple.Item1).Select(tuple =>
                {
                    var ifStatement = $"ledDebounce[{tuple.Item2}]";
                    return $$"""

                             if ({{ifStatement}}) {
                                 {{Microcontroller.GenerateDigitalWrite(pin, !tuple.Item1.OutputInverted, peripheral, IsBluetooth)}};
                             }
                             """;
                }));
                ret += $$"""
                         else {
                             {{Microcontroller.GenerateDigitalWrite(pin, relatedOutputs.First().Item1.OutputInverted, peripheral, IsBluetooth)}};
                         }
                         """;
            }
        }

        foreach (var (pin, analogLedOutputs) in analogRelatedToLed)
        {
            if (debouncesRelatedToLed.ContainsKey(pin)) continue;
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
                    var generated = $"({analogLedOutput.Input.Generate(writer)})";
                    var isI2C = analogLedOutput.Input is DjInput
                    {
                        Input: DjInputType.LeftTurntable or DjInputType.RightTurntable
                    };
                    if (!analogLedOutput.InputIsUint)
                    {
                        generated = $"({generated} + INT16_MAX)";
                    }

                    ledRead = isI2C
                        ? $"handle_calibration_turntable_ps3_i2c(0, {analogLedOutput.Input.Generate(writer)},{multiplier})"
                        : $"handle_calibration_turntable_ps3(0, {generated},{multiplier})";
                }

                if (analogLedOutput is DjAxis {Type: DjAxisType.EffectsKnob})
                {
                    var generated = $"({analogLedOutput.Input.Generate(writer)})";
                    if (!analogLedOutput.InputIsUint)
                    {
                        generated = $"({generated} + INT16_MAX)";
                    }

                    ledRead = $"(({generated} >> 8))";
                }


                if (analogLedOutput.Input is DigitalToAnalog dta)
                {
                    var on = dta.On >> 8;
                    if (dta.Type != DigitalToAnalogType.Trigger)
                    {
                        on += sbyte.MaxValue;
                    }

                    ledRead = $"(({ledRead}) ? {on} : 0)";
                }

                // Now we have the value, calibrated as a uint8_t
                ret +=
                    $"led_tmp = {ledRead};{Microcontroller.GenerateAnalogWrite(pin, $"{(analogLedOutput.OutputInverted ? "(255-" : "(")}led_tmp)", peripheral)};";
            }
        }

        return ret;
    }

    private string ComputeLedsMpr121(ConfigField mode, Dictionary<byte, List<(Output, int)>> debouncesRelatedToLed,
        Dictionary<byte, List<OutputAxis>> analogRelatedToLed, BinaryWriter? writer)
    {
        if (mode != ConfigField.Shared) return "";
        var ret = "";
        var variable = "ledStateMpr121";
        // Handle leds, including when multiple leds are assigned to a single output.
        foreach (var (led, relatedOutputs) in debouncesRelatedToLed)
        {
            // MPR121 leds starts at index 4.
            var index = led - 4;
            var analog = "";
            if (analogRelatedToLed.TryGetValue(led, out var analogLedOutputs))
            {
                analog = analogLedOutputs.Aggregate(analog,
                    (current, analogLedOutput) =>
                        current +
                        $"bit_write({analogLedOutput.Input.Generate(writer)}, {variable},{index % 8});");
            }

            if (analog.Length == 0)
            {
                analog = $"bit_clear({variable},{index % 8});";
            }

            ret += $$"""

                     if (!bit_check({{variable}}Select,{{index % 8}})) {
                     """;
            ret += string.Join(" else ", relatedOutputs.DistinctBy(tuple => tuple.Item1).Select(tuple =>
            {
                var ifStatement = $"ledDebounce[{tuple.Item2}]";
                return $$"""
                         
                             if ({{ifStatement}}) {
                                 bit_set({{variable}},{{index % 8}});
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

                     if (!bit_check({{variable}}Select,{{index % 8}})) {
                     """;
            ret = analogLedOutputs.Aggregate(ret,
                (current, analogLedOutput) =>
                    current + $"bit_write({analogLedOutput.Input.Generate(writer)}, {variable},{index % 8});");

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
        Dictionary<string, int> ledDebounces = new();
        var strumIndices = new List<int>();

        var generatedMode = DeviceControllerType is DeviceControllerType.KeyboardMouse
            ? ConfigField.Keyboard
            : ConfigField.Shared;

        // Pass 1: work out debounces and map inputs to debounces
        var macros = new Dictionary<string, List<(int, Input)>>();
        foreach (var outputByType in outputsByType)
        {
            foreach (var output in outputByType)
            {
                var generatedInput = output.Input.Generate(writer);
                var generatedOutput = output.GenerateOutput(generatedMode);
                var pro = IsFortniteFestivalPro && output is GuitarAxis
                {
                    Type: GuitarAxisType.Tilt or GuitarAxisType.Whammy
                };
                if (output is not OutputButton and not DrumAxis and not EmulationMode && !pro) continue;

                if (output.Input is MacroInput)
                {
                    foreach (var input in output.Input.Inputs())
                    {
                        var gen = input.Generate(writer);
                        macros.TryAdd(gen, []);
                        macros[gen].AddRange(output.Input.Inputs().Where(s => s != input).Select(s => (0, s)));
                    }
                }

                debounces.TryAdd(generatedOutput, debounces.Count);
                ledDebounces.TryAdd(generatedInput, ledDebounces.Count);
                if (output is GuitarButton
                    {
                        IsStrum: true
                    })
                    strumIndices.Add(debounces[generatedOutput]);
            }
        }

        foreach (var (key, value) in macros)
        {
            var list2 = new List<(int, Input)>();
            foreach (var (_, input) in value)
            {
                var gen = input.Generate(writer);
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

        var debouncesRelatedToLedPin = new Dictionary<int, List<(Output, int)>>();
        var analogRelatedToLedPin = new Dictionary<int, List<OutputAxis>>();
        var debouncesRelatedToLedPeripheralPin = new Dictionary<int, List<(Output, int)>>();
        var analogRelatedToLedPeripheralPin = new Dictionary<int, List<OutputAxis>>();

        var debouncesRelatedToLedMpr121 = new Dictionary<byte, List<(Output, int)>>();
        var analogRelatedToLedMpr121 = new Dictionary<byte, List<OutputAxis>>();
        var ret = "";
        if (mode == ConfigField.Keyboard && IsFortniteFestivalPro)
        {
            ret += """
                   tiltActive = false;
                   """;
        }

        var enabled = new Dictionary<Output, string>();

        // Handle most mappings
        ret += outputsByType
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
                            var generatedInput = input.Generate(writer);
                            var generatedOutput = output.GenerateOutput(generatedMode);
                            var index = 0;
                            var ledIndex = 0;

                            var pro = IsFortniteFestivalPro && output is GuitarAxis
                            {
                                Type: GuitarAxisType.Tilt or GuitarAxisType.Whammy
                            };
                            if (pro)
                            {
                                index = debounces[generatedOutput];
                                ledIndex = ledDebounces[generatedInput];
                            }

                            if (output is OutputButton or DrumAxis or EmulationMode)
                            {
                                index = debounces[generatedOutput];
                                ledIndex = ledDebounces[generatedInput];
                                if (output.OutputPinConfig != null)
                                {
                                    if (output.OutputPinConfig.Peripheral)
                                    {
                                        if (!debouncesRelatedToLedPeripheralPin.ContainsKey(output.OutputPin))
                                            debouncesRelatedToLedPeripheralPin[output.OutputPin] =
                                                [];
                                        debouncesRelatedToLedPeripheralPin[output.OutputPin].Add((output, ledIndex));
                                    }
                                    else
                                    {
                                        if (!debouncesRelatedToLedPin.ContainsKey(output.OutputPin))
                                            debouncesRelatedToLedPin[output.OutputPin] = [];
                                        debouncesRelatedToLedPin[output.OutputPin].Add((output, ledIndex));
                                    }
                                }

                                foreach (var led in output.LedIndices)
                                {
                                    if (!debouncesRelatedToLed.ContainsKey(led))
                                        debouncesRelatedToLed[led] = [];

                                    debouncesRelatedToLed[led].Add((output, ledIndex));
                                }

                                foreach (var led in output.LedIndicesMpr121)
                                {
                                    if (!debouncesRelatedToLedMpr121.ContainsKey(led))
                                        debouncesRelatedToLedMpr121[led] = [];

                                    debouncesRelatedToLedMpr121[led].Add((output, ledIndex));
                                }

                                foreach (var led in output.LedIndicesPeripheral)
                                {
                                    if (!debouncesRelatedToLedPeripheral.ContainsKey(led))
                                        debouncesRelatedToLedPeripheral[led] = [];

                                    debouncesRelatedToLedPeripheral[led].Add((output, ledIndex));
                                }
                            }

                            if (output is OutputAxis axis)
                            {
                                foreach (var led in output.LedIndices)
                                {
                                    if (!analogRelatedToLed.ContainsKey(led))
                                        analogRelatedToLed[led] = [];

                                    analogRelatedToLed[led].Add(axis);
                                }

                                foreach (var led in output.LedIndicesMpr121)
                                {
                                    if (!analogRelatedToLedMpr121.ContainsKey(led))
                                        analogRelatedToLedMpr121[led] = [];

                                    analogRelatedToLedMpr121[led].Add(axis);
                                }

                                foreach (var led in output.LedIndicesPeripheral)
                                {
                                    if (!analogRelatedToLedPeripheral.ContainsKey(led))
                                        analogRelatedToLedPeripheral[led] = [];

                                    analogRelatedToLedPeripheral[led].Add(axis);
                                }

                                if (output.OutputPinConfig != null)
                                {
                                    if (output.OutputPinConfig.Peripheral)
                                    {
                                        if (!analogRelatedToLedPeripheralPin.ContainsKey(output.OutputPin))
                                            analogRelatedToLedPeripheralPin[output.OutputPin] = [];
                                        analogRelatedToLedPeripheralPin[output.OutputPin].Add(axis);
                                    }
                                    else
                                    {
                                        if (!analogRelatedToLedPin.ContainsKey(output.OutputPin))
                                            analogRelatedToLedPin[output.OutputPin] = [];
                                        analogRelatedToLedPin[output.OutputPin].Add(axis);
                                    }
                                }
                            }

                            var generated = output.Generate(mode, index, ledIndex, "", "", strumIndices, combined,
                                macros,
                                writer);
                            if (writer != null && generated.Length != 0)
                            {
                                if (!enabled.TryGetValue(output, out var blob))
                                {
                                    blob = WriteBlob(writer, output.Enabled);
                                    enabled[output] = blob;
                                }

                                generated = $$"""
                                              if ({{blob}}) {
                                                  {{generated}}
                                              }
                                              """;
                            }
                            else
                            {
                                if (!output.Enabled) generated = "";
                            }

                            return new Tuple<Input, string>(input, generated);
                        })
                        .Where(s => !string.IsNullOrEmpty(s.Item2))
                        .Distinct().ToList(), mode);
            });
        ret += ComputeLeds(mode, false, debouncesRelatedToLed, analogRelatedToLed, writer);
        ret += ComputeLeds(mode, true, debouncesRelatedToLedPeripheral, analogRelatedToLedPeripheral, writer);
        ret += ComputeLedsPin(mode, false, debouncesRelatedToLedPin, analogRelatedToLedPin, writer);
        ret += ComputeLedsPin(mode, true, debouncesRelatedToLedPeripheralPin, analogRelatedToLedPeripheralPin, writer);
        ret += ComputeLedsMpr121(mode, debouncesRelatedToLedMpr121, analogRelatedToLedMpr121, writer);
        if (mode == ConfigField.Keyboard && IsFortniteFestivalPro)
        {
            ret += """
                   if (!tiltActive && (millis() - lastTilt) > 1000) {
                      lastTilt = 0;
                   }
                   """;
        }

        return FixNewlines(ret);
    }

    private (int, int) CalculateDebounceTicks(BinaryWriter? writer)
    {
        var generatedMode = DeviceControllerType is DeviceControllerType.KeyboardMouse
            ? ConfigField.Keyboard
            : ConfigField.Shared;
        var outputs = Bindings.Items.SelectMany(binding => binding.ValidOutputs()).ToList();
        var outputsByType = outputs
            .GroupBy(s => s.Input.InnermostInputs().First().GetType()).ToList();
        Dictionary<string, int> debounces = new();
        Dictionary<string, int> ledDebounces = new();

        foreach (var outputByType in outputsByType)
        {
            foreach (var output in outputByType)
            {
                var generatedInput = output.Input.Generate(writer);
                var generatedOutput = output.GenerateOutput(generatedMode);
                var pro = IsFortniteFestivalPro && output is GuitarAxis
                {
                    Type: GuitarAxisType.Tilt or GuitarAxisType.Whammy
                };
                if (output is not OutputButton and not DrumAxis and not EmulationMode && !pro) continue;


                debounces.TryAdd(generatedOutput, debounces.Count);
                ledDebounces.TryAdd(generatedInput, ledDebounces.Count);
            }
        }

        return (debounces.Count, ledDebounces.Count);
    }

    public List<PinConfig> GetPinConfigs()
    {
        return Bindings.Items.SelectMany(s => s.GetPinConfigs()).Concat(PinConfigs).Distinct().ToList();
    }

    public string LedSpiType(bool peripheral)
    {
        if (peripheral)
        {
            if (IsApa102Peripheral)
                return Apa102PeripheralSpiType;
            if (IsWs2812Peripheral)
                return WS2812PeripheralSpiType;
            return Stp16PeripheralSpiType;
        }

        if (IsApa102)
            return Apa102SpiType;
        if (IsWs2812)
            return WS2812SpiType;
        return Stp16SpiType;
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

                if (!twi)
                {
                    pins2.AddRange(pinConfig.Pins);
                }
            }

            if (skip)
            {
                break;
            }

            if (!pins.ContainsKey(binding.LocalisedName)) pins[binding.LocalisedName] = [];
            pins[binding.LocalisedName].AddRange(pins2);
        }

        if ((Main.IsUno || Main.IsMega) && !peripheral)
        {
            pins[UnoPinTypeTx] = [UnoPinTypeTxPin];
            pins[UnoPinTypeRx] = [UnoPinTypeRxPin];
        }

        if (IsIndexedLed && _ledSpiConfig != null && type != LedSpiType(false) && !peripheral)
            pins[LedSpiType(false)] = _ledSpiConfig.Pins.ToList();

        if (IsIndexedLedPeripheral && _ledSpiConfigPeripheral != null && type != LedSpiType(true) && peripheral)
        {
            pins[LedSpiType(true)] = _ledSpiConfigPeripheral.Pins.ToList();
        }

        if (IsIndexedLed && _ws2812Config != null && type != LedSpiType(false) && !peripheral)
            pins[LedSpiType(false)] = _ws2812Config.Pins.ToList();

        if (IsIndexedLedPeripheral && _ws2812ConfigPeripheral != null && type != LedSpiType(true) && peripheral)
        {
            pins[LedSpiType(true)] = _ws2812ConfigPeripheral.Pins.ToList();
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
            pins["USB Host"] = [UsbHostDm, UsbHostDp];

        if (_adaFruitHostPin != null && type != AdafruitHostType && !peripheral)
            pins["Adafruit USB Host Enable"] = [_adaFruitHostPin.Pin];

        if (_peripheralTwiConfig != null && type != PeripheralTwiType && !twi && !peripheral)
        {
            pins[PeripheralTwiType] = _peripheralTwiConfig.Pins.ToList();
        }

        if (_max170XTwiConfig != null && type != Max170XTwiType && !twi && !peripheral)
        {
            pins[Max170XTwiType] = _max170XTwiConfig.Pins.ToList();
        }

        if (_mpr121TwiConfig != null && type != Mpr121TwiType && !twi && !peripheral)
        {
            pins[Mpr121TwiType] = _mpr121TwiConfig.Pins.ToList();
        }

        return pins;
    }

    public void UpdateErrors()
    {
        var foundError = false;
        foreach (var output in Bindings.Items.SelectMany(s => s.Outputs.Items))
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

        AccelErrorText = null;
        if (_accelTwiConfig?.ErrorText != null)
        {
            foundError = true;
            AccelErrorText = _accelTwiConfig.ErrorText;
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


        if (HasWiiOutput && _wiiOutputTwiConfig?.ErrorText != null)
        {
            foundError = true;
            WiiOutputErrorText = _wiiOutputTwiConfig.ErrorText;
        }
        else
        {
            WiiOutputErrorText = null;
        }

        switch (HasPs2Output)
        {
            case true when _ps2OutputSpiConfig?.ErrorText != null:
                foundError = true;
                Ps2OutputErrorText = _ps2OutputSpiConfig.ErrorText;
                break;
            case true when _ps2OutputAtt?.ErrorText != null:
                foundError = true;
                Ps2OutputErrorText = _ps2OutputAtt.ErrorText;
                break;
            case true when _ps2OutputAck?.ErrorText != null:
                foundError = true;
                Ps2OutputErrorText = _ps2OutputAck.ErrorText;
                break;
            default:
                Ps2OutputErrorText = null;
                break;
        }

        if (HasMpr121 && _mpr121TwiConfig?.ErrorText != null)
        {
            foundError = true;
            Mpr121ErrorText = _mpr121TwiConfig.ErrorText;
        }
        else
        {
            Mpr121ErrorText = null;
        }

        if (HasMax1704X && _max170XTwiConfig?.ErrorText != null)
        {
            foundError = true;
            Max170XErrorText = _max170XTwiConfig.ErrorText;
        }
        else
        {
            Max170XErrorText = null;
        }

        HasError = foundError;
        Main.ShowError = foundError;
    }

    public List<PinConfig> LedPinConfigs()
    {
        List<PinConfig> configs = [];
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
                UpdateBluetoothAddress();
            }

            Device.DeviceAdded(device);
        });
    }

    public void RemoveDevice(IConfigurableDevice device)
    {
        if (_disconnected || Main.Working || Device is not Santroller old ||
            !device.IsSameDevice(old)) return;
        old.StopTicking();
        Main.SetDifference(false);
        _disconnected = true;
        if (Builder) return;
        ShowUnpluggedDialog.Handle(("", "", "")).ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => Main.GoBack.Execute(new Unit()));
    }

    public void Update(byte[] btRaw, bool peripheralConnected, bool mpr121Connected, bool max1270XConnected,
        byte[] max1270XRaw, bool accelConnected)
    {
        if (IsBluetoothTx && btRaw.Length != 0)
        {
            Connected = btRaw[0] != 0;
        }

        PeripheralConnected = peripheralConnected;

        Mpr121Connected = mpr121Connected;
        Max1704XConnected = max1270XConnected;
        AccelConnected = accelConnected;
        if (max1270XRaw.Length != 0)
        {
            Max1704XStatus = max1270XRaw[0];
        }
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
    public LedType LastLedType { get; set; } = LedType.None;
    public LedType LastLedTypePeripheral { get; set; } = LedType.None;
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

    [RelayCommand]
    public void ReturnAndWrite()
    {
        Main.SaveConfiguration();
        GoBackCommand.Execute(null);
    }

    public void SetUpDiff()
    {
        WasBluetooth = IsBluetooth;
        LastLedType = LedType;
        LastLedTypePeripheral = LedTypePeripheral;
        var lastConfig = new SerializedConfiguration(this);
        using var outputStream = new MemoryStream();
        Serializer.Serialize(outputStream, lastConfig);
        _lastConfig = outputStream.ToArray();
        _currentConfig = new SerializedConfiguration(this);
        _currentConfigData = new byte[_lastConfig.Length];
        _timer.Start();
    }
}