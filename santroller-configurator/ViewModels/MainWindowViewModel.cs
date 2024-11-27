using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.Utils;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Velopack;
using Velopack.Sources;

namespace GuitarConfigurator.NetCore.ViewModels;

public partial class MainWindowViewModel : ReactiveObject, IScreen, IDisposable
{
    private static readonly Regex VersionRegex = new("v\\d+\\.\\d+\\.\\d+$");
    private readonly HashSet<string> _currentDrives = [];
    private readonly HashSet<string> _currentDrivesTemp = [];
    private readonly HashSet<string> _currentPorts = [];
    public string ProgressBarError;
    public string ProgressBarPrimary;
    public string ProgressBarWarning;
    private readonly Timer _timer = new();
    private bool hasLibUsb = true;
    private UpdateManager? _mgr = null;
    private UpdateInfo? _updateInfo;

    private readonly ToolConfig _toolConfig = AssetUtils.GetConfig();
    public virtual string ToolName => Resources.ToolName + " - v" + GitVersionInformation.SemVer;
    public virtual string AlreadyOpenMessage => $"You already have a copy of {Resources.ToolName} running, exiting.";


    private readonly SourceList<DeviceInputType> _allDeviceInputTypes = new();
    private readonly ConfigurableUsbDeviceManager? _manager;
    private readonly bool _picoOnly;
    public bool LibUsbMissing = false;
    public bool Builder { get; }
    public bool Windows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public MainWindowViewModel(bool builder, bool branded, bool picoOnly, string primary = "#FF0078D7",
        string warning = "#FFd7cb00",
        string error = "red")
    {
        Builder = builder;
        Logo = new Bitmap(AssetLoader.Open(new Uri("avares://SantrollerConfigurator/Assets/Icons/logo.png")));
        ProgressBarError = error;
        ProgressBarPrimary = primary;
        ProgressBarWarning = warning;
        _picoOnly = picoOnly;
        SelectedLanguage = _toolConfig.Language;

        Message = Resources.ConnectedMessage;
        GoBack = ReactiveCommand.CreateFromObservable(() =>
            Router.NavigateAndReset.Execute(Router.NavigationStack.First()));
        ProgressbarColor = ProgressBarPrimary;
        Working = true;
        _allDeviceInputTypes.AddRange(Enum.GetValues<DeviceInputType>());
        _allDeviceInputTypes
            .Connect()
            .Filter(this.WhenAnyValue(s => s.SelectedDevice).Select(CreateFilter))
            .Bind(out var deviceInputTypes).Subscribe();
        DeviceInputTypes = deviceInputTypes;
        try
        {
            _manager = new ConfigurableUsbDeviceManager(this);
        }
        catch (DllNotFoundException e)
        {
            Message =
                Resources.LibUsbMissingMessage;
            ProgressbarColor = ProgressBarError;
            Progress = 100;
            hasLibUsb = false;
            Console.WriteLine(e);
        }

        if (!branded)
        {
            _ = CheckForUpdates();
        }

        ConfigureCommand = ReactiveCommand.CreateFromObservable(
            () => Router.Navigate.Execute(new ConfigViewModel(this, SelectedDevice!, false))
        );
        InitialConfigureCommand = ReactiveCommand.CreateFromObservable(
            () => Router.Navigate.Execute(new InitialConfigViewModel(this,
                new ConfigViewModel(this, SelectedDevice!, false)))
        );
        RevertCommand = ReactiveCommand.CreateFromObservable<Santroller, IRoutableViewModel>(
            device => Router.Navigate.Execute(new RestoreViewModel(this, device))
        );
        AvailableDevices.Connect().Bind(out var devices).Subscribe();
        AvailableDevices.Connect().Subscribe(s =>
        {
            IConfigurableDevice? item = null;
            if (AvailableDevices.Items.Any()) item = AvailableDevices.Items.First();

            foreach (var change in s)
            {
                SelectedDevice = change.Reason switch
                {
                    ListChangeReason.Add when SelectedDevice == null => change.Item.Current,
                    ListChangeReason.Remove when SelectedDevice == change.Item.Current => item,
                    ListChangeReason.Remove when SelectedDevice == null => item,
                    _ => SelectedDevice
                };
                if (change.Reason == ListChangeReason.Remove)
                {
                    change.Item.Current.Disconnect();
                }
            }
        });
        Devices = devices;
        Router.Navigate.Execute(new MainViewModel(this));
        _needsTiltHelper = this.WhenAnyValue(x => x.DeviceInputType, x => x.DeviceControllerType)
            .Select(s => s.Item1 is DeviceInputType.Direct or DeviceInputType.Wii && s.Item2.IsGuitar())
            .ToProperty(this, s => s.NeedsTilt);

        _hasSidebarHelper = Router.CurrentViewModel
            .Select(s => s is ConfigViewModel)
            .ToProperty(this, s2 => s2.HasSidebar);
        _progressBarShouldUseDarkTextColorHelper = this.WhenAnyValue(x => x.ProgressbarColor, x => x.Progress)
            .Select(x => ShouldUseDarkTextColorForBackground(x.Item1) && x.Item2 > 45)
            .ToProperty(this, x => x.ProgressBarShouldUseDarkTextColor);
        _accentedButtonTextColorHelper = this.WhenAnyValue(x => x.ProgressbarColor)
            .Select(ShouldUseDarkTextColorForBackground)
            .Select(x => x ? "#FF000000" : "#FFFFFFFF")
            .ToProperty(this, x => x.AccentedButtonTextColor);
        _migrationSupportedHelper = this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s?.MigrationSupported != false)
            .ToProperty(this, s => s.MigrationSupported);
        _connectedHelper = this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s != null)
            .ToProperty(this, s => s.Connected);
        _isPicoHelper = this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s is {IsPico: true})
            .ToProperty(this, s => s.IsPico);
        _is32U4Helper = this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s is Arduino {Is32U4: true})
            .ToProperty(this, s => s.Is32U4);
        _supportsFortniteHelper = this.WhenAnyValue(x => x.DeviceControllerType)
            .Select(s => s.Is5FretGuitar() || s.IsDrum())
            .ToProperty(this, s => s.SupportsFortnite);
        _isGuitarHelper = this.WhenAnyValue(x => x.DeviceControllerType)
            .Select(s => s.IsGuitar())
            .ToProperty(this, s => s.IsGuitar);
        _isUnoMegaHelper = this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s is Dfu or Arduino {IsUno: true} or Arduino {IsMega: true})
            .ToProperty(this, s => s.IsUnoMega);
        _isUnoHelper = this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s is Arduino {IsUno: true})
            .ToProperty(this, s => s.IsUno);
        _isMegaHelper = this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s is Arduino {IsMega: true})
            .ToProperty(this, s => s.IsMega);
        _isDfuHelper = this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s is Dfu)
            .ToProperty(this, s => s.IsDfu);
        _isGenericHelper = this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s is {IsGeneric: true})
            .ToProperty(this, s => s.IsGeneric);
        _newDeviceHelper = this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s is not (null or Ardwiino or Santroller))
            .ToProperty(this, s => s.NewDevice);
        _newDeviceOrArdwiinoHelper = this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s is not Santroller)
            .ToProperty(this, s => s.NewDeviceOrArdwiino);
        _isPeripheralHelper = this.WhenAnyValue(x => x.DeviceInputType)
            .Select(s => s is DeviceInputType.Peripheral)
            .ToProperty(this, s => s.IsPeripheral);
        _isDirectHelper = this.WhenAnyValue(x => x.DeviceInputType)
            .Select(s => s is DeviceInputType.Direct)
            .ToProperty(this, s => s.IsDirect);
        _isBluetoothRxHelper = this.WhenAnyValue(x => x.DeviceInputType)
            .Select(s => s is DeviceInputType.Bluetooth)
            .ToProperty(this, s => s.IsBluetoothRx);
        _isFeatherHelper = this.WhenAnyValue(x => x.DeviceInputType)
            .Select(s => s is DeviceInputType.Feather)
            .ToProperty(this, s => s.IsFeather);
        _readyToConfigureHelper = this.WhenAnyValue(x => x.SelectedDevice, x => x.Installed, x => x.IsPeripheral,
                x => x.PeripheralErrorText, x => x.AdvancedBranded)
            .Select(s =>
                s is {Item1: not null, Item2: true} and ({Item1: not Santroller {IsSantroller: false}} or {Item5: true})
                    and ({Item3: false} or {Item4: null}))
            .ToProperty(this, s => s.ReadyToConfigure);
        _isSantrollerHelper = this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s is not Santroller {IsSantroller: false})
            .ToProperty(this, s => s.IsSantroller);
        _isPicoCdcHelper = this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s is Arduino {Board.IsPico: true})
            .ToProperty(this, s => s.IsPicoCdc);
        // Make sure that the selected device input type is reset so that we don't end up doing something invalid
        this.WhenAnyValue(s => s.SelectedDevice).Subscribe(_ =>
        {
            DeviceInputType = DeviceInputType.Direct;
            this.RaisePropertyChanged(nameof(DeviceInputType));
        });
    }
    
    public void Close()
    {
        Environment.Exit(0);
    }

    public async Task CheckForUpdates()
    {
        GithubSource source;
        if (Builder)
        {
            var file = Path.Combine(AssetUtils.GetAppDataFolder(), "auth");
            if (!File.Exists(file))
            {
                return;
            }

            var tokens = await File.ReadAllTextAsync(file);
            var accessToken = HttpUtility.ParseQueryString(tokens).Get("access_token")!;
            source = new GithubSource("https://github.com/Santroller/SantrollerConfiguratorBinaries", accessToken,
                false);
        }
        else
        {
            source = new GithubSource("https://github.com/Santroller/Santroller", null, false);
        }

        _mgr = new(source);
        if (!_mgr.IsInstalled) return;
        try
        {
            _updateInfo = await _mgr.CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        UpdateMessage = _updateInfo == null
            ? Resources.UpToDate
            : string.Format(Resources.NewVersion, _updateInfo.TargetFullRelease.Version);
        HasUpdate = _updateInfo != null;
    }

    public bool Programming { get; private set; }
    public ReadOnlyObservableCollection<DeviceInputType> DeviceInputTypes { get; }

    [Reactive] private DeviceControllerType _deviceControllerType = DeviceControllerType.Gamepad;
    [Reactive] private AccelSensorTypeMain _accelSensorTypeMain;

    public List<AccelSensorTypeMain> AccelSensorTypeMains => Enum.GetValues<AccelSensorTypeMain>().ToList();

    public List<DeviceControllerType> DeviceControllerTypes => Enum.GetValues<DeviceControllerType>().ToList();

    public Interaction<(string yesText, string noText, string text), AreYouSureWindowViewModel>
        ShowYesNoDialog { get; } = new();

    public Interaction<string, InformationWindowViewModel>
        ShowInformationDialog { get; } = new();

    public Interaction<(string _platformIOText, ConfigViewModel), RaiseIssueWindowViewModel?>
        ShowIssueDialog { get; } =
        new();

    public ReactiveCommand<Unit, IRoutableViewModel> ConfigureCommand { get; }
    public ReactiveCommand<Santroller, IRoutableViewModel> RevertCommand { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> InitialConfigureCommand { get; }

    // The command that navigates a user back.
    public ReactiveCommand<Unit, IRoutableViewModel> GoBack { get; }

    public SourceList<IConfigurableDevice> AvailableDevices { get; } = new();

    public ReadOnlyObservableCollection<IConfigurableDevice> Devices { get; }

    private Language _language = Language.En;

    public IEnumerable<Language> Languages => Enum.GetValues<Language>();

    public Language SelectedLanguage
    {
        set
        {
            if (_language != value)
            {
                switch (value)
                {
                    case Language.En:
                        Resources.Culture = new CultureInfo("en");
                        break;
                    case Language.Es:
                        Resources.Culture = new CultureInfo("es-ES");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (Router.NavigationStack.Any())
                {
                    Router.NavigateAndReset.Execute(Router.NavigationStack.First());
                    _toolConfig.Language = value;
                    AssetUtils.SaveConfig(_toolConfig);
                }
            }

            this.RaiseAndSetIfChanged(ref _language, value);
        }
        get => _language;
    }

    private static Func<DeviceInputType, bool> CreateFilter(IConfigurableDevice? s)
    {
        return type =>
            type is not (DeviceInputType.Usb or DeviceInputType.Bluetooth or DeviceInputType.Peripheral
                or DeviceInputType.Feather) ||
            s is PicoDevice;
    }

    public static bool ShouldUseDarkTextColorForBackground(string color)
    {
        var c = Color.Parse(color);
        return c.R * 0.299 + c.G * 0.587 + c.B * 0.114 > 140;
    }

    public void AddDevice(IConfigurableDevice device)
    {
        if (_picoOnly && !device.IsPico) return;
        AvailableDevices.Add(device);
    }

    public async Task Begin(bool platformIo)
    {
        if (!hasLibUsb)
        {
            return;
        }

        _timer.Elapsed += DevicePoller_Tick;
        _timer.AutoReset = false;
        StartWorking();
        if (platformIo)
        {
            Pio.InitialisePlatformIo().Subscribe(UpdateProgress, ex =>
            {
                Complete(100);
                ProgressbarColor = ProgressBarError;
                Message = ex.Message;
            }, () =>
            {
                Complete(100);
                Installed = true;
                _manager?.Register();
                _timer.Start();
            });
            await InstallDependenciesAsync();
        }
        else
        {
            Complete(100);
            Installed = true;
            _manager?.Register();
            _timer.Start();
        }
    }

    [ObservableAsProperty] private bool _migrationSupported;
    [ObservableAsProperty] private bool _isPico;
    [ObservableAsProperty] private bool _is32U4;
    [ObservableAsProperty] private bool _isUno;
    [ObservableAsProperty] private bool _isUnoMega;
    [ObservableAsProperty] private bool _isMega;
    [ObservableAsProperty] private bool _isDfu;
    [ObservableAsProperty] private bool _isGeneric;
    [ObservableAsProperty] private bool _isGuitar;
    [ObservableAsProperty] private bool _needsTilt;
    [ObservableAsProperty] private bool _isDirect;
    [ObservableAsProperty] private bool _newDevice;
    [ObservableAsProperty] private bool _newDeviceOrArdwiino;
    [ObservableAsProperty] private bool _isPeripheral;
    [ObservableAsProperty] private bool _isBluetoothRx;
    [ObservableAsProperty] private bool _isFeather;
    [ObservableAsProperty] private bool _supportsFortnite;

    [ObservableAsProperty] private bool _connected;

    [ObservableAsProperty] private bool _hasSidebar;

    [ObservableAsProperty] private bool _progressBarShouldUseDarkTextColor;
    [ObservableAsProperty] private string _accentedButtonTextColor = "";

    [ObservableAsProperty] private bool _readyToConfigure;
    [ObservableAsProperty] private bool _isSantroller;
    [ObservableAsProperty] private bool _isPicoCdc;

    public IEnumerable<Arduino32U4Type> Arduino32U4Types => Enum.GetValues<Arduino32U4Type>();
    public IEnumerable<MegaType> MegaTypes => Enum.GetValues<MegaType>();
    public IEnumerable<UnoMegaType> UnoMegaTypes => Enum.GetValues<UnoMegaType>();
    public IEnumerable<AvrType> AvrTypes => Enum.GetValues<AvrType>();

    [Reactive] private AvrType _avrType;

    [Reactive] private UnoMegaType _unoMegaType;

    [Reactive] private MegaType _megaType;

    [Reactive] private DeviceInputType _deviceInputType;

    [Reactive] private Arduino32U4Type _arduino32U4Type;
    [Reactive] private bool _showError;


    public List<int> AvailableSdaPins => GetSdaPins();
    public List<int> AvailableSclPins => GetSclPins();

    private int _sda = 18;
    private int _scl = 19;

    public int PeripheralSda
    {
        get => _sda;
        set
        {
            this.RaiseAndSetIfChanged(ref _sda, value);
            PeripheralErrorText = Pico.TwiIndexByPin[value] != Pico.TwiIndexByPin[_scl]
                ? Resources.DifferentI2CGroup
                : null;
        }
    }

    public int PeripheralScl
    {
        get => _scl;
        set
        {
            this.RaiseAndSetIfChanged(ref _scl, value);
            PeripheralErrorText = Pico.TwiIndexByPin[value] != Pico.TwiIndexByPin[_sda]
                ? Resources.DifferentI2CGroup
                : null;
        }
    }

    [Reactive] private Bitmap _logo;
    [Reactive] private string? _peripheralErrorText;

    private List<int> GetSdaPins()
    {
        return Pico.TwiTypeByPin
            .Where(s => s.Value is TwiPinType.Sda)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetSclPins()
    {
        return Pico.TwiTypeByPin
            .Where(s => s.Value is TwiPinType.Scl)
            .Select(s => s.Key).ToList();
    }

    private IConfigurableDevice? _selectedDevice;

    public IConfigurableDevice? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (value is Arduino arduino)
                UnoMegaType = arduino.Board.ArdwiinoName switch
                {
                    "uno" => UnoMegaType.Uno,
                    "mega2560" => UnoMegaType.Mega,
                    "megaadk" => UnoMegaType.MegaAdk,
                    _ => UnoMegaType.Uno
                };

            this.RaiseAndSetIfChanged(ref _selectedDevice, value);
        }
    }

    [Reactive] private bool _lookingForDfu = false;


    [Reactive] private bool _installed;
    [Reactive] private bool _hasChanges;
    [Reactive] private bool _working;
    [Reactive] private bool _deviceNotProgrammed;
    [Reactive] private bool _bluetoothTx;
    [Reactive] private bool _fortnite;
    [Reactive] private bool _advancedBranded;

    [Reactive] private string _progressbarColor;

    [Reactive] private double _progress;

    [Reactive] private string _message;

    [Reactive] private string _updateMessage = Resources.UpToDate;

    [Reactive] private bool _hasUpdate = false;

    public PlatformIo Pio { get; } = new();


    // The Router associated with this Screen.
    // Required by the IScreen interface.
    public RoutingState Router { get; } = new();


    public virtual IObservable<PlatformIo.PlatformIoState> SaveUf2(ConfigViewModel model)
    {
        return Observable.Return(new PlatformIo.PlatformIoState(100, Resources.DoneMessage, false, ""));
    }

    public virtual IObservable<PlatformIo.PlatformIoState> Write(ConfigViewModel config, bool write, string extra = "",
        int startingPercentage = 0, int endingPercentage = 100)
    {
        StartWorking();
        var configFile = Path.Combine(Pio.FirmwareDir, "include", "config_data.h");
        File.WriteAllText(configFile, extra + config.Generate(extra.Length != 0 ? new MemoryStream() : null));
        var environment = config.Microcontroller.Board.Environment;
        if (config.IsBluetooth) environment = "picow";
        if (DeviceInputType is DeviceInputType.Peripheral)
        {
            environment += "_slave";
        }

        if (config.Device is not (Ardwiino or Santroller))
        {
            environment = environment.Replace("_8", "");
            environment = environment.Replace("_16", "");
        }

        // Ardwiino + uno -> santroller has to do things in a different order
        if (config.Microcontroller.Board.HasUsbmcu && config.Device is Ardwiino)
        {
            environment += "_usb";
        }

        if (config.Microcontroller.Board.HasUsbmcu && config.Device is Dfu)
        {
            environment += "_usb_serial";
        }

        var state = Observable.Return(new PlatformIo.PlatformIoState(startingPercentage, "", false, null));

        // When programming, the last 10 percentage is waiting for the device to show up
        if (write)
        {
            endingPercentage -= 10;
        }

        var env = environment;
        Programming = true;
        BehaviorSubject<PlatformIo.PlatformIoState> command;
        if (!write || extra.Length != 0)
        {
            command = Pio.RunPlatformIo(env, ["run"],
                Resources.BuildingVariantMessage + config.Variant,
                startingPercentage, endingPercentage, null);
        }
        else
        {
            command = Pio.RunPlatformIo(env, ["run", "--target", "upload"],
                Resources.WritingMessage,
                startingPercentage, endingPercentage, config.Device);
        }

        state = state.Concat(command);


        var output = new StringBuilder();
        var behaviorSubject =
            new BehaviorSubject<PlatformIo.PlatformIoState>(
                new PlatformIo.PlatformIoState(startingPercentage, "", false, null));

        state.ObserveOn(RxApp.MainThreadScheduler).Subscribe(s =>
            {
                behaviorSubject.OnNext(s);
                UpdateProgress(s);
                if (s.Log != null) output.Append(s.Log + "\n");
            }, s =>
            {
                var text = output.ToString();
                if (text.Trim().Length == 0)
                {
                    text = s.ToString();
                }

                ProgressbarColor = ProgressBarError;
                ShowIssueDialog.Handle((text, config)).Subscribe(_ => Programming = false);
            },
            () =>
            {
                Programming = false;
                if (DeviceInputType is DeviceInputType.Peripheral)
                {
                    Complete(100);
                }
            });

        return state.OnErrorResumeNext(Observable.Return(behaviorSubject.Value));
    }

    public void NavigateToUrl(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo(url) {UseShellExecute = true});
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
    }

    [RelayCommand]
    public async Task UpdateApp()
    {
        if (_mgr == null) return;
        Working = true;
        Message = Resources.DownloadingUpdate;
        PlatformIo.Exit();
        await _mgr.DownloadUpdatesAsync(_updateInfo!, i => Progress = i);
        _mgr.ApplyUpdatesAndRestart(null, null);
    }

    [RelayCommand]
    public void OpenCommercialUsePage()
    {
        NavigateToUrl("https://santroller.tangentmc.net/tool/commercial_use.html");
    }

    [RelayCommand]
    public void OpenDiscord()
    {
        NavigateToUrl("https://discord.gg/CmdYpXKcEU");
    }

    public void Complete(int total)
    {
        Working = false;
        Message = Resources.DoneMessage;
        Progress = total;
        ProgressbarColor = ProgressBarPrimary;
    }

    public void StartWorking()
    {
        Working = true;
        ProgressbarColor = ProgressBarPrimary;
    }

    protected void UpdateProgress(PlatformIo.PlatformIoState state)
    {
        if (!Working) return;
        LookingForDfu = state.Dfu;
        ProgressbarColor =
            state.Warning
                ? ProgressBarWarning
                : ProgressBarPrimary;
        Progress = state.Percentage;
        Message = state.Message;
    }

    private async void DevicePoller_Tick(object? sender, ElapsedEventArgs e)
    {
        var drives = DriveInfo.GetDrives();
        _currentDrivesTemp.UnionWith(_currentDrives);
        foreach (var drive in drives)
        {
            if (_currentDrivesTemp.Remove(drive.RootDirectory.FullName)) continue;

            try
            {
                var uf2 = Path.Combine(drive.RootDirectory.FullName, "INFO_UF2.txt");
                if (drive.IsReady)
                    if (File.Exists(uf2))
                    {
                        var text = await File.ReadAllTextAsync(uf2);
                        if (text.Contains("RP2350"))
                        {
                            AvailableDevices.Add(new PicoDevice(drive.RootDirectory.FullName, "pico2"));
                        }

                        if (text.Contains("RPI-RP2"))
                        {
                            AvailableDevices.Add(new PicoDevice(drive.RootDirectory.FullName, "pico"));
                        }
                    }
            }
            catch (IOException)
            {
                // Expected if the pico is unplugged   
            }

            _currentDrives.Add(drive.RootDirectory.FullName);
        }

        // We removed all valid devices above, so anything left in currentDrivesSet is no longer valid
        AvailableDevices.RemoveMany(AvailableDevices.Items.Where(x =>
            x is PicoDevice pico && _currentDrivesTemp.Contains(pico.GetPath())));
        _currentDrives.ExceptWith(_currentDrivesTemp);
        if (!_picoOnly)
        {
            var existingPorts = _currentPorts.ToHashSet();
            var ports = await Pio.GetPortsAsync();
            if (ports != null)
            {
                foreach (var port in ports)
                {
                    if (!port.Hwid.StartsWith("USB")) continue;
                    if (existingPorts.Contains(port.Port)) continue;
                    _currentPorts.Add(port.Port);
                    var arduino = new Arduino(port);
                    await Task.Delay(1000);
                    AvailableDevices.Add(arduino);
                }

                var currentSerialPorts = ports.Select(port => port.Port).ToHashSet();
                _currentPorts.RemoveWhere(port => !currentSerialPorts.Contains(port));
                AvailableDevices.RemoveMany(AvailableDevices.Items.Where(device =>
                    device is Arduino arduino && !currentSerialPorts.Contains(arduino.GetSerialPort())));
            }
        }

        _timer.Start();
    }


    private async Task<bool> CheckDependencies()
    {
        return await _manager?.CheckDrivers()!;
    }

    [RelayCommand]
    private async Task RescanAsync()
    {
        await _manager?.InvokeRescan()!;

        await ShowInformationDialog.Handle(Resources.UnplugReplugMessage);
    }

    private async Task InstallDependenciesAsync()
    {
        if (await CheckDependencies()) return;
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                 var yesNo = await ShowYesNoDialog.Handle(("Install", "Skip",
                    Resources.DriversMissingMessage)).ToTask();
                if (!yesNo.Response) return;
                await _manager?.InvokeDriverInstall()!;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var yesNo = await ShowYesNoDialog.Handle(("Install", "Skip",
                    Resources.RootRequiredMessage)).ToTask();
                if (!yesNo.Response) return;

                await _manager?.InvokeDriverInstall()!;
            }

            if (!await CheckDependencies())
            {
                // Pop open a dialog that it failed and to try again
            }
        } catch (Exception ex) {
            Trace.WriteLine(ex);
            Console.WriteLine(ex);
        }
    }
        


    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _manager?.Dispose();
    }

    public virtual void SetDifference(bool difference)
    {
        HasChanges = difference;
        if (Working || DeviceNotProgrammed || Router.NavigationStack.Last() is not ConfigViewModel) return;
        if (ShowError)
        {
            Message = Resources.ConfigurationErrorLabel;
            ProgressbarColor = ProgressBarError;
        }
        else
        {
            if (!difference)
            {
                Message = Resources.DoneMessage;
                ProgressbarColor = ProgressBarPrimary;
                Complete(100);
            }
            else
            {
                Message = Resources.DoneMessage;
                ProgressbarColor = ProgressBarWarning;
                Message = Resources.SaveChangesWarning;
            }
        }
    }

    public virtual void SaveConfiguration()
    {
    }
}