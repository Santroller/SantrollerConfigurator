using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Devices;
using Nefarius.Utilities.DeviceManagement.Drivers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
#if Windows
using Microsoft.Win32;
#endif

namespace GuitarConfigurator.NetCore.ViewModels;

public partial class MainWindowViewModel : ReactiveObject, IScreen, IDisposable
{
    private const string UpToDate = "Up to date";
    private const string UdevFile = "99-santroller.rules";
    private const string UdevPath = $"/etc/udev/rules.d/{UdevFile}";
    private static readonly Regex VersionRegex = new("v\\d+\\.\\d+\\.\\d+$");
    private readonly HashSet<string> _currentDrives = new();
    private readonly HashSet<string> _currentDrivesTemp = new();
    private readonly HashSet<string> _currentPorts = new();
    public string ProgressBarError;
    public string ProgressBarPrimary;
    public string ProgressBarWarning;
    private readonly Timer _timer = new();
    private bool hasLibUsb = true;


    private readonly SourceList<DeviceInputType> _allDeviceInputTypes = new();
    private readonly ConfigurableUsbDeviceManager? _manager;
    private readonly bool _picoOnly;
    public bool LibUsbMissing = false;

    public MainWindowViewModel(bool picoOnly, string primary = "#FF0078D7", string warning = "#FFd7cb00",
        string error = "red")
    {
        Logo = new Bitmap(AssetLoader.Open(new Uri("avares://SantrollerConfigurator/Assets/Icons/logo.png")));
        ProgressBarError = error;
        ProgressBarPrimary = primary;
        ProgressBarWarning = warning;
        _picoOnly = picoOnly;
        UpdateMessage = CheckForUpdates();
        Message = "Connected";
        GoBack = ReactiveCommand.CreateFromObservable(() =>
            Router.NavigateAndReset.Execute(Router.NavigationStack.First()));
        ProgressbarColor = ProgressBarPrimary;
        Working = true;
        AssetUtils.InitNativeLibrary();
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
        catch (DllNotFoundException)
        {
            Message =
                "You are missing libusb-1.0.so. On ubuntu or other debian based distributions, you will need to install 'libusb-1.0-0-dev'";
            ProgressbarColor = ProgressBarError;
            Progress = 100;
            hasLibUsb = false;
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
        Router.CurrentViewModel
            .Select(s => s is ConfigViewModel)
            .ToPropertyEx(this, s => s.HasSidebar);
        this.WhenAnyValue(x => x.ProgressbarColor, x => x.Progress)
            .Select(x => ShouldUseDarkTextColorForBackground(x.Item1) && x.Item2 > 45)
            .ToPropertyEx(this, x => x.ProgressBarShouldUseDarkTextColor);
        this.WhenAnyValue(x => x.ProgressbarColor)
            .Select(ShouldUseDarkTextColorForBackground)
            .Select(x => x ? "#FF000000" : "#FFFFFFFF")
            .ToPropertyEx(this, x => x.AccentedButtonTextColor);
        this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s?.MigrationSupported != false)
            .ToPropertyEx(this, s => s.MigrationSupported);
        this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s != null)
            .ToPropertyEx(this, s => s.Connected);
        this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s?.IsPico() == true)
            .ToPropertyEx(this, s => s.IsPico);
        this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s is Arduino arduino && arduino.Is32U4())
            .ToPropertyEx(this, s => s.Is32U4);
        this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s is Dfu || (s is Arduino arduino && (arduino.IsUno() || arduino.IsMega())))
            .ToPropertyEx(this, s => s.IsUnoMega);
        this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s is Arduino arduino && arduino.IsUno())
            .ToPropertyEx(this, s => s.IsUno);
        this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s is Arduino arduino && arduino.IsMega())
            .ToPropertyEx(this, s => s.IsMega);
        this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s is Dfu)
            .ToPropertyEx(this, s => s.IsDfu);
        this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s?.IsGeneric() == true)
            .ToPropertyEx(this, s => s.IsGeneric);
        this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s is not (null or Ardwiino or Santroller))
            .ToPropertyEx(this, s => s.NewDevice);
        this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s is not Santroller)
            .ToPropertyEx(this, s => s.NewDeviceOrArdwiino);
        this.WhenAnyValue(x => x.DeviceInputType)
            .Select(s => s is DeviceInputType.Peripheral)
            .ToPropertyEx(this, s => s.IsPeripheral);
        this.WhenAnyValue(x => x.SelectedDevice, x => x.Installed, x => x.IsPeripheral, x => x.PeripheralErrorText)
            .Select(s =>
                s is {Item1: not null and not Santroller {IsSantroller: false}, Item2: true} &&
                (!s.Item3 || s.Item4 == null))
            .ToPropertyEx(this, s => s.ReadyToConfigure);
        this.WhenAnyValue(x => x.SelectedDevice)
            .Select(s => s is not Santroller {IsSantroller: false})
            .ToPropertyEx(this, s => s.IsSantroller);
        // Make sure that the selected device input type is reset so that we don't end up doing something invalid
        this.WhenAnyValue(s => s.SelectedDevice).Subscribe(_ =>
        {
            DeviceInputType = DeviceInputType.Direct;
            this.RaisePropertyChanged(nameof(DeviceInputType));
        });
    }

    public bool Programming { get; private set; }
    public ReadOnlyObservableCollection<DeviceInputType> DeviceInputTypes { get; }

    public Interaction<(string yesText, string noText, string text), AreYouSureWindowViewModel>
        ShowYesNoDialog { get; } = new();

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

    private static Func<DeviceInputType, bool> CreateFilter(IConfigurableDevice? s)
    {
        return type => type is not (DeviceInputType.Usb or DeviceInputType.Bluetooth or DeviceInputType.Peripheral) ||
                       s is PicoDevice;
    }

    public static bool ShouldUseDarkTextColorForBackground(string color)
    {
        var c = Color.Parse(color);
        return c.R * 0.299 + c.G * 0.587 + c.B * 0.114 > 140;
    }

    private string CheckForUpdates()
    {
        try
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://github.com");
            var response = client.GetAsync($"sanjay900/guitar-configurator/info/refs?service=git-upload-pack").Result;
            response.EnsureSuccessStatusCode();
            var res = response.Content.ReadAsStringAsync().Result.Split("\n")[2..^1];
            var latestTagVer = new Version(GitVersionInformation.SemVer);
            var isPreRelease = int.Parse(GitVersionInformation.CommitsSinceVersionSource) != 0;
            var preReleaseTag = "";
            bool preReleaseOutdated = false;
            foreach (var re in res)
            {
                var split = re.Split(" ");
                var tagHash = split[0][4..];
                var tag = split[1];
                if (!tag.StartsWith("refs/tags/"))
                {
                    continue;
                }

                tag = tag.Replace("refs/tags/", "");
                if (isPreRelease && tag == "preview" && !tagHash.StartsWith(GitVersionInformation.Sha))
                {
                    preReleaseTag = tagHash[..5];
                }

                if (!VersionRegex.Match(tag).Success)
                {
                    continue;
                }

                tag = tag.Replace("v", "");

                if (new Version(tag) > latestTagVer)
                {
                    latestTagVer = new Version(tag);
                }
            }

            if (isPreRelease)
            {
                if (preReleaseOutdated)
                {
                    return "New Prerelease: " + preReleaseTag;
                }
            }
            else if (latestTagVer != new Version(GitVersionInformation.SemVer))
            {
                return "New Version: " + latestTagVer;
            }
        }
        catch (Exception ex)
        {
            return "Unable to check for updates: " + ex.Message;
        }

        return UpToDate;
    }

    public void AddDevice(IConfigurableDevice device)
    {
        if (_picoOnly && !device.IsPico()) return;
        AvailableDevices.Add(device);
    }

    public void Begin(bool platformIo)
    {
        if (!hasLibUsb)
        {
            return;
        }
        _timer.Elapsed += DevicePoller_Tick;
        _timer.AutoReset = false;
        StartWorking();
#if Windows
        RegistryKey? key =
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          Registry.CurrentUser.OpenSubKey(@"System\CurrentControlSet\Control\MediaProperties\PrivateProperties\Joystick\OEM\VID_1209&PID_2882", true);
        if (key != null) {
            if (key.GetValue("OEMName") != null) {
                key.DeleteValue("OEMName");
            }
            key.Close();
        }
#endif
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
                Working = false;
                Installed = true;
                _manager?.Register();
                _timer.Start();
            });

            _ = InstallDependenciesAsync();
        }
        else
        {
            Complete(100);
            Working = false;
            Installed = true;
            _manager?.Register();
            _timer.Start();
        }
    } // ReSharper disable UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] public bool MigrationSupported { get; }
    [ObservableAsProperty] public bool IsPico { get; }
    [ObservableAsProperty] public bool Is32U4 { get; }
    [ObservableAsProperty] public bool IsUno { get; }
    [ObservableAsProperty] public bool IsUnoMega { get; }
    [ObservableAsProperty] public bool IsMega { get; }
    [ObservableAsProperty] public bool IsDfu { get; }
    [ObservableAsProperty] public bool IsGeneric { get; }
    [ObservableAsProperty] public bool NewDevice { get; }
    [ObservableAsProperty] public bool NewDeviceOrArdwiino { get; }
    [ObservableAsProperty] public bool IsPeripheral { get; }

    [ObservableAsProperty] public bool Connected { get; }

    [ObservableAsProperty] public bool HasSidebar { get; }

    [ObservableAsProperty] public bool ProgressBarShouldUseDarkTextColor { get; }
    [ObservableAsProperty] public string AccentedButtonTextColor { get; } = "";

    [ObservableAsProperty] public bool ReadyToConfigure { get; }
    [ObservableAsProperty] public bool IsSantroller { get; }
    // ReSharper enable UnassignedGetOnlyAutoProperty

    public IEnumerable<Arduino32U4Type> Arduino32U4Types => Enum.GetValues<Arduino32U4Type>();
    public IEnumerable<MegaType> MegaTypes => Enum.GetValues<MegaType>();
    public IEnumerable<UnoMegaType> UnoMegaTypes => Enum.GetValues<UnoMegaType>();
    public IEnumerable<AvrType> AvrTypes => Enum.GetValues<AvrType>();

    [Reactive] public AvrType AvrType { get; set; }

    [Reactive] public UnoMegaType UnoMegaType { get; set; }

    [Reactive] public MegaType MegaType { get; set; }

    [Reactive] public DeviceInputType DeviceInputType { get; set; }

    [Reactive] public Arduino32U4Type Arduino32U4Type { get; set; }
    [Reactive] public bool ShowError { get; set; }


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

    [Reactive] public Bitmap Logo { get; set; }
    [Reactive] public string? PeripheralErrorText { get; set; }

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

    [Reactive] public bool LookingForDfu { get; set; } = false;


    [Reactive] private bool Installed { get; set; }
    [Reactive] public bool HasChanges { get; set; }
    [Reactive] public bool Working { get; set; }

    [Reactive] public string ProgressbarColor { get; set; }

    [Reactive] public double Progress { get; set; }

    [Reactive] public string Message { get; set; }

    public string UpdateMessage { get; }

    public bool HasUpdate => UpdateMessage != UpToDate;

    public PlatformIo Pio { get; } = new();


    // The Router associated with this Screen.
    // Required by the IScreen interface.
    public RoutingState Router { get; } = new();


    public virtual IObservable<PlatformIo.PlatformIoState> SaveUf2(ConfigViewModel model)
    {
        return Observable.Return(new PlatformIo.PlatformIoState(100, "Done", ""));
    }

    public virtual IObservable<PlatformIo.PlatformIoState> Write(ConfigViewModel config, string extra = "",
        int startingPercentage = 0, int endingPercentage = 100)
    {
        StartWorking();
        var configFile = Path.Combine(Pio.FirmwareDir, "include", "config_data.h");
        File.WriteAllText(configFile, extra + config.Generate(extra.Any() ? new MemoryStream() : null));
        var environment = config.Microcontroller.Board.Environment;
        if (config.IsBluetooth) environment = "picow";
        if (DeviceInputType is DeviceInputType.Peripheral)
        {
            environment = "pico_slave";
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

        var state = Observable.Return(new PlatformIo.PlatformIoState(startingPercentage, "", null));

        // When programming, the last 10 percentage is waiting for the device to show up
        if (!extra.Any())
        {
            endingPercentage -= 10;
        }

        var env = environment;
        Programming = true;
        BehaviorSubject<PlatformIo.PlatformIoState> command;
        if (extra.Any())
        {
            command = Pio.RunPlatformIo(env, new[] {"run"},
                "Building " + config.Variant,
                startingPercentage, endingPercentage, null);
        }
        else
        {
            command = Pio.RunPlatformIo(env, new[] {"run", "--target", "upload"},
                "Writing",
                startingPercentage, endingPercentage, config.Device);
        }

        state = state.Concat(command);


        var output = new StringBuilder();
        var behaviorSubject =
            new BehaviorSubject<PlatformIo.PlatformIoState>(
                new PlatformIo.PlatformIoState(startingPercentage, "", null));

        state.ObserveOn(RxApp.MainThreadScheduler).Subscribe(s =>
            {
                behaviorSubject.OnNext(s);
                UpdateProgress(s);
                if (s.Log != null) output.Append(s.Log + "\n");
            }, _ =>
            {
                ProgressbarColor = ProgressBarError;
                ShowIssueDialog.Handle((output.ToString(), config)).Subscribe(_ => Programming = false);
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
    public void OpenReleasesPage()
    {
        NavigateToUrl("https://github.com/Santroller/Santroller/releases");
    }

    [RelayCommand]
    public void OpenCommercialUsePage()
    {
        NavigateToUrl("https://santroller.tangentmc.net/tool/commercial_use.html");
    }

    public void Complete(int total)
    {
        Working = false;
        Message = "Done";
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
        LookingForDfu = state.Message.Contains("Looking for device in DFU mode");
        ProgressbarColor =
            state.Message.Contains("Please Unplug") ||
            LookingForDfu
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
                    if (File.Exists(uf2) && (await File.ReadAllTextAsync(uf2)).Contains("RPI-RP2"))
                        AvailableDevices.Add(new PicoDevice(drive.RootDirectory.FullName));
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


    private static bool CheckDependencies()
    {
        // Call check dependencies on startup, and pop up a dialog saying drivers are missing would you like to install if they are missing
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return DriverStore.ExistingDrivers.Any(s => s.Contains("atmel_usb_dfu"));

        return !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || File.Exists(UdevPath);
    }

    private async Task InstallDependenciesAsync()
    {
        if (CheckDependencies()) return;
        var yesNo = await ShowYesNoDialog.Handle(("Install", "Skip",
            "There are some drivers missing, would you like to install them?")).ToTask();
        if (!yesNo.Response) return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var appdataFolder = AssetUtils.GetAppDataFolder();
            var driverFolder = Path.Combine(appdataFolder, "drivers");
            await AssetUtils.ExtractXzAsync("dfu.tar.xz", appdataFolder, _ => { });

            var info = new ProcessStartInfo(Path.Combine(windowsDir, "pnputil.exe"));
            info.ArgumentList.AddRange(new[] {"-i", "-a", Path.Combine(driverFolder, "atmel_usb_dfu.inf")});
            info.UseShellExecute = true;
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;
            info.Verb = "runas";
            Process.Start(info);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Just copy the file to install it, using pkexec for admin
            var appdataFolder = AssetUtils.GetAppDataFolder();
            var rules = Path.Combine(appdataFolder, UdevFile);
            await AssetUtils.ExtractFileAsync(UdevFile, rules);
            var info = new ProcessStartInfo("pkexec");
            info.ArgumentList.AddRange(new[] {"cp", rules, UdevPath});
            info.UseShellExecute = true;
            var process = Process.Start(info);
            if (process == null) return;
            await process.WaitForExitAsync();
            // And then reload rules and trigger
            info = new ProcessStartInfo("pkexec");
            info.ArgumentList.AddRange(new[] {"udevadm", "control", "--reload-rules"});
            info.UseShellExecute = true;
            process = Process.Start(info);
            if (process == null) return;
            await process.WaitForExitAsync();

            info = new ProcessStartInfo("pkexec");
            info.ArgumentList.AddRange(new[] {"udevadm", "trigger"});
            info.UseShellExecute = true;
            process = Process.Start(info);
            if (process == null) return;
            await process.WaitForExitAsync();
        }

        if (!CheckDependencies())
        {
            // Pop open a dialog that it failed and to try again
        }
    }


    public void Dispose()
    {
        _manager?.Dispose();
    }

    public virtual void SetDifference(bool difference)
    {
        HasChanges = difference;
        if (Working || Router.NavigationStack.Last() is not ConfigViewModel) return;
        if (ShowError)
        {
            Message = Resources.ConfigurationErrorLabel;
            ProgressbarColor = ProgressBarError;
        }
        else
        {
            if (!difference)
            {
                Message = "Done";
                ProgressbarColor = ProgressBarPrimary;
                Complete(100);
            }
            else
            {
                Message = "Done";
                ProgressbarColor = ProgressBarWarning;
                Message = Resources.SaveChangesWarning;
            }
        }
    }
}