using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Other;

public enum StageKitCommand
{
    Fog,
    Strobe,
    LedGreen,
    LedRed,
    LedYellow,
    LedBlue
}

public enum StageKitStrobeSpeed
{
    Slow,
    Medium,
    Fast,
    Fastest
}

public enum FiveFretGuitar
{
    Open,
    Green,
    Red,
    Yellow,
    Blue,
    Orange
}

public enum SixFretGuitar
{
    Open,
    Black1,
    Black2,
    Black3,
    White1,
    White2,
    White3
}

public enum RockBandDrum
{
    KickPedal,
    RedPad,
    YellowPad,
    BluePad,
    GreenPad,
    YellowCymbal,
    BlueCymbal,
    GreenCymbal
}

public enum GuitarHeroDrum
{
    KickPedal,
    RedPad,
    YellowCymbal,
    BluePad,
    OrangeCymbal,
    GreenPad
}

public enum Turntable
{
    ScratchLeft,
    GreenNoteLeft,
    RedNoteLeft,
    BlueNoteLeft,
    ScratchRight,
    GreenNoteRight,
    RedNoteRight,
    BlueNoteRight
}

public enum LedCommandType
{
    KeyboardNumLock,
    KeyboardCapsLock,
    KeyboardScrollLock,
    Auth,
    Player,
    Combo,
    NoteHit,
    NoteMiss,
    StarPowerInactive,
    StarPowerActive,
    DjEuphoria,
    StageKitLed,
    Ps4LightBar,
    BluetoothConnected,
    Mode,
    AlwaysOn
}

public enum RumbleCommand
{
    StageKitFogOn = 1,
    StageKitFogOff,
    StageKitStrobeLightSlow,
    StageKitStrobeLightMedium,
    StageKitStrobeLightFast,
    StageKitStrobeLightFastest,
    StageKitStrobeLightOff,
    SantrollerStarPowerGauge,
    SantrollerStarPowerActive,
    SantrollerMultiplier,
    SantrollerSolo,
    SantrollerNoteMiss,
    StageKitStrobeLightBlue = 0x20,
    StageKitStrobeLightGreen = 0x40,
    StageKitStrobeLightYellow = 0x60,
    StageKitStrobeLightRed = 0x80,
    SantrollerInputSpecific = 0x90,
    SantrollerEuphoriaLed = 0xA0,
    StageKitReset = 0xFF
}

public partial class Led : Output
{
    private readonly SourceList<LedCommandType> _rumbleCommands = new();

    private readonly ObservableAsPropertyHelper<bool> _outputHasColours;

    private readonly ObservableAsPropertyHelper<bool> _usesPwm;

    public Led(ConfigViewModel model, bool enabled, bool outputEnabled, bool outputInverted, int outputPin,
        bool peripheral,
        Color ledOn,
        Color ledOff, byte[] ledIndices, byte[] ledIndicesPeripheral, byte[] ledIndicesMpr121, LedCommandType command,
        int param,
        int param2) : base(model, enabled,
        new FixedInput(model, 0, false),
        ledOn, ledOff,
        ledIndices, ledIndicesPeripheral, ledIndicesMpr121, outputEnabled, outputInverted, peripheral, outputPin, false)
    {
        Player = 1;
        Combo = 1;
        StageKitLed = 1;
        StageKitCommand = 0;
        FiveFretGuitar = 0;
        SixFretGuitar = 0;
        GuitarHeroDrum = 0;
        RockBandDrum = 0;
        Turntable = 0;
        switch (command)
        {
            case LedCommandType.Player:
                Player = param + 1;
                break;
            case LedCommandType.Combo:
                Combo = param + 1;
                break;
            case LedCommandType.NoteHit:
                switch (model.DeviceControllerType)
                {
                    case DeviceControllerType.GuitarHeroGuitar or DeviceControllerType.RockBandGuitar:
                        FiveFretGuitar = (FiveFretGuitar) param;
                        break;
                    case DeviceControllerType.LiveGuitar:
                        SixFretGuitar = (SixFretGuitar) param;
                        break;
                    case DeviceControllerType.GuitarHeroDrums:
                        GuitarHeroDrum = (GuitarHeroDrum) param;
                        break;
                    case DeviceControllerType.RockBandDrums:
                        RockBandDrum = (RockBandDrum) param;
                        break;
                    case DeviceControllerType.Turntable:
                        Turntable = (Turntable) param;
                        break;
                }

                break;
            case LedCommandType.Mode:
                EmulationModeType = (EmulationModeType) param;

                break;
            case LedCommandType.StageKitLed:
                StageKitCommand = (StageKitCommand) param;
                switch (StageKitCommand)
                {
                    case StageKitCommand.Strobe:
                        break;
                    default:
                        StageKitLed = param2 + 1;
                        break;
                }

                break;
        }

        Command = command;
        _usesPwm = this.WhenAnyValue(x => x.Command)
            .Select(commandType => commandType is LedCommandType.DjEuphoria
                or LedCommandType.StarPowerActive
                or LedCommandType.StarPowerInactive).ToProperty(this, x => x.UsesPwm);
        _rumbleCommands.AddRange(Enum.GetValues<LedCommandType>());
        _rumbleCommands.Connect()
            .Filter(this.WhenAnyValue(x => x.Model.DeviceControllerType,
                x => x.Model.IsApa102, x => x.Model.IsBluetooth).Select(FilterLeds))
            .Bind(out var rumbleCommands)
            .Subscribe();
        RumbleCommands = rumbleCommands;
        _outputHasColours = this.WhenAnyValue(x => x.Command).Select(s => s is not LedCommandType.Ps4LightBar)
            .ToProperty(this, x => x.LedsHaveColours);

        _playerModeHelper = this.WhenAnyValue(x => x.Command).Select(s => s is LedCommandType.Player)
            .ToProperty(this, x => x.PlayerMode);

        _modeHelper = this.WhenAnyValue(x => x.Command).Select(s => s is LedCommandType.Mode)
            .ToProperty(this, x => x.Mode);

        _comboModeHelper = this.WhenAnyValue(x => x.Command).Select(s => s is LedCommandType.Combo)
            .ToProperty(this, x => x.ComboMode);

        _stageKitStrobeSpeedModeHelper = this.WhenAnyValue(x => x.Command, x => x.StageKitCommand).Select(s =>
                s.Item1 is LedCommandType.StageKitLed && s.Item2 is StageKitCommand.Strobe)
            .ToProperty(this, x => x.StageKitStrobeSpeedMode);

        _stageKitLedModeHelper = this.WhenAnyValue(x => x.Command, x => x.StageKitCommand).Select(s =>
                s.Item1 is LedCommandType.StageKitLed && s.Item2 is StageKitCommand.LedBlue or StageKitCommand.LedGreen
                    or StageKitCommand.LedRed or StageKitCommand.LedYellow)
            .ToProperty(this, x => x.StageKitLedMode);

        _stageKitModeHelper = this.WhenAnyValue(x => x.Command).Select(s => s is LedCommandType.StageKitLed)
            .ToProperty(this, x => x.StageKitMode);
        _fiveFretModeHelper = this.WhenAnyValue(x => x.Command, x => x.Model.DeviceControllerType)
            .Select(s => s.Item1 is LedCommandType.NoteHit && s.Item2.Is5FretGuitar())
            .ToProperty(this, x => x.FiveFretMode);

        _sixFretModeHelper = this.WhenAnyValue(x => x.Command, x => x.Model.DeviceControllerType)
            .Select(s => s.Item1 is LedCommandType.NoteHit && s.Item2 is DeviceControllerType.LiveGuitar)
            .ToProperty(this, x => x.SixFretMode);

        _guitarHeroDrumsModeHelper = this.WhenAnyValue(x => x.Command, x => x.Model.DeviceControllerType)
            .Select(s =>
                s.Item1 is LedCommandType.NoteHit && s.Item2 is DeviceControllerType.GuitarHeroDrums)
            .ToProperty(this, x => x.GuitarHeroDrumsMode);

        _rockBandDrumsModeHelper = this.WhenAnyValue(x => x.Command, x => x.Model.DeviceControllerType)
            .Select(s =>
                s.Item1 is LedCommandType.NoteHit && s.Item2 is DeviceControllerType.RockBandDrums)
            .ToProperty(this, x => x.RockBandDrumsMode);

        _turntableModeHelper = this.WhenAnyValue(x => x.Command, x => x.Model.DeviceControllerType)
            .Select(s => s.Item1 is LedCommandType.NoteHit && s.Item2 is DeviceControllerType.Turntable)
            .ToProperty(this, x => x.TurntableMode);
        UpdateDetails();
    }

    public override bool UsesPwm => _usesPwm?.Value ?? false;

    public override bool LedsHaveColours =>
        _outputHasColours.Value;

    [ObservableAsProperty] private bool _fiveFretMode;
    [ObservableAsProperty] private bool _sixFretMode;
    [ObservableAsProperty] private bool _guitarHeroDrumsMode;
    [ObservableAsProperty] private bool _rockBandDrumsMode;
    [ObservableAsProperty] private bool _turntableMode;
    [ObservableAsProperty] private bool _playerMode;
    [ObservableAsProperty] private bool _comboMode;
    [ObservableAsProperty] private bool _mode;
    [ObservableAsProperty] private bool _stageKitStrobeSpeedMode;
    [ObservableAsProperty] private bool _stageKitLedMode;
    [ObservableAsProperty] private bool _stageKitMode;
    public StageKitCommand[] StageKitCommands { get; } = Enum.GetValues<StageKitCommand>();
    public StageKitStrobeSpeed[] StageKitStrobeSpeeds { get; } = Enum.GetValues<StageKitStrobeSpeed>();
    public FiveFretGuitar[] FiveFretGuitars { get; } = Enum.GetValues<FiveFretGuitar>();
    public SixFretGuitar[] SixFretGuitars { get; } = Enum.GetValues<SixFretGuitar>();
    public RockBandDrum[] RockBandDrums { get; } = Enum.GetValues<RockBandDrum>();
    public GuitarHeroDrum[] GuitarHeroDrums { get; } = Enum.GetValues<GuitarHeroDrum>();

    public EmulationModeType[] EmulationModeTypes { get; } = Enum.GetValues<EmulationModeType>();
    public Turntable[] Turntables { get; } = Enum.GetValues<Turntable>();

    private LedCommandType _command;

    public LedCommandType Command
    {
        get => _command;
        set
        {
            this.RaiseAndSetIfChanged(ref _command, value);
            this.RaisePropertyChanged(nameof(SupportsLedOff));
            UpdateDetails();
        }
    }

    private int _player;
    private int _stageKitLed;
    private int _combo;
    private StageKitCommand _stageKitCommand;
    private GuitarHeroDrum _guitarHeroDrum;
    private RockBandDrum _rockBandDrum;
    private Turntable _turntable;
    private FiveFretGuitar _fiveFretGuitar;
    private SixFretGuitar _sixFretGuitar;
    private EmulationModeType _emulationModeType;

    public int Player
    {
        get => _player;
        set
        {
            this.RaiseAndSetIfChanged(ref _player, value);
            UpdateDetails();
        }
    }

    public int StageKitLed
    {
        get => _stageKitLed;
        set
        {
            this.RaiseAndSetIfChanged(ref _stageKitLed, value);
            UpdateDetails();
        }
    }

    public int Combo
    {
        get => _combo;
        set
        {
            this.RaiseAndSetIfChanged(ref _combo, value);
            UpdateDetails();
        }
    }

    public EmulationModeType EmulationModeType
    {
        get => _emulationModeType;
        set
        {
            this.RaiseAndSetIfChanged(ref _emulationModeType, value);
            UpdateDetails();
        }
    }

    public GuitarHeroDrum GuitarHeroDrum
    {
        get => _guitarHeroDrum;
        set
        {
            this.RaiseAndSetIfChanged(ref _guitarHeroDrum, value);
            UpdateDetails();
        }
    }

    public RockBandDrum RockBandDrum
    {
        get => _rockBandDrum;
        set
        {
            this.RaiseAndSetIfChanged(ref _rockBandDrum, value);
            UpdateDetails();
        }
    }

    public Turntable Turntable
    {
        get => _turntable;
        set
        {
            this.RaiseAndSetIfChanged(ref _turntable, value);
            UpdateDetails();
        }
    }

    public FiveFretGuitar FiveFretGuitar
    {
        get => _fiveFretGuitar;
        set
        {
            this.RaiseAndSetIfChanged(ref _fiveFretGuitar, value);
            UpdateDetails();
        }
    }

    public SixFretGuitar SixFretGuitar
    {
        get => _sixFretGuitar;
        set
        {
            this.RaiseAndSetIfChanged(ref _sixFretGuitar, value);
            UpdateDetails();
        }
    }

    public StageKitCommand StageKitCommand
    {
        get => _stageKitCommand;
        set
        {
            this.RaiseAndSetIfChanged(ref _stageKitCommand, value);
            UpdateDetails();
        }
    }

    public override bool IsCombined => false;
    public override bool IsStrum => false;

    public ReadOnlyObservableCollection<LedCommandType> RumbleCommands { get; }


    public override string LedOnLabel => Command switch
    {
        LedCommandType.StageKitLed when StageKitCommand is StageKitCommand.Fog => Resources.LEDColourActiveFog,
        LedCommandType.StarPowerActive or LedCommandType.StarPowerInactive => Resources.LedColourActiveStarPower,
        LedCommandType.DjEuphoria => Resources.LedColourActiveDjEuphoria,
        _ => Resources.LedColourActive
    };

    public override string LedOffLabel => Command switch
    {
        LedCommandType.StageKitLed when StageKitCommand is StageKitCommand.Fog => Resources.LedColourInactiveFog,
        LedCommandType.StarPowerActive or LedCommandType.StarPowerInactive => Resources.LedColourInactiveStarPower,
        LedCommandType.DjEuphoria => Resources.LedColourInactiveDjEuphoria,
        _ => Resources.LedColourInactive
    };

    public override bool SupportsLedOff =>
        Command is not (LedCommandType.Auth or LedCommandType.Mode or LedCommandType.AlwaysOn or LedCommandType.Player);

    public override bool IsKeyboard => false;
    public virtual bool IsController => false;

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        if (Command is LedCommandType.NoteHit)
        {
            switch (deviceControllerType)
            {
                case DeviceControllerType.Turntable:
                    return string.Format(Resources.LedCommandTitleStageKit1, EnumToStringConverter.Convert(Command),
                        EnumToStringConverter.Convert(Turntable));
                case DeviceControllerType.GuitarHeroDrums:
                    return string.Format(Resources.LedCommandTitleStageKit1, EnumToStringConverter.Convert(Command),
                        EnumToStringConverter.Convert(GuitarHeroDrum));
                case DeviceControllerType.RockBandDrums:
                    return string.Format(Resources.LedCommandTitleStageKit1, EnumToStringConverter.Convert(Command),
                        EnumToStringConverter.Convert(RockBandDrum));
                case DeviceControllerType.GuitarHeroGuitar:
                case DeviceControllerType.RockBandGuitar:
                    return string.Format(Resources.LedCommandTitleStageKit1, EnumToStringConverter.Convert(Command),
                        EnumToStringConverter.Convert(FiveFretGuitar));
                case DeviceControllerType.LiveGuitar:
                    return string.Format(Resources.LedCommandTitleStageKit1, EnumToStringConverter.Convert(Command),
                        EnumToStringConverter.Convert(SixFretGuitar));
            }
        }

        if (Command != LedCommandType.StageKitLed)
            return string.Format(Resources.LedCommandTitle, EnumToStringConverter.Convert(Command));

        if (StageKitCommand is StageKitCommand.Fog or StageKitCommand.Strobe)
        {
            return string.Format(Resources.LedCommandTitleStageKit1, EnumToStringConverter.Convert(Command),
                EnumToStringConverter.Convert(StageKitCommand));
        }

        return string.Format(Resources.LedCommandTitleStageKit2, EnumToStringConverter.Convert(Command),
            EnumToStringConverter.Convert(StageKitCommand), StageKitLed);
    }

    public static Func<LedCommandType, bool> FilterLeds(
        (DeviceControllerType controllerType, bool isApa102, bool isBluetooth) type)
    {
        return command =>
        {
            if (command is LedCommandType.BluetoothConnected && type.isBluetooth)
            {
                return true;
            }

            return type.controllerType switch
            {
                DeviceControllerType.KeyboardMouse => command is
                    LedCommandType.KeyboardCapsLock or LedCommandType.KeyboardScrollLock
                    or LedCommandType.KeyboardNumLock,
                _ => command switch
                {
                    LedCommandType.Auth or LedCommandType.Player or LedCommandType.Mode
                        or LedCommandType.AlwaysOn => true,
                    LedCommandType.Combo or LedCommandType.StarPowerActive or LedCommandType.StarPowerInactive
                        or LedCommandType.StageKitLed when
                        type.controllerType is DeviceControllerType.RockBandDrums
                            or DeviceControllerType.GuitarHeroDrums or DeviceControllerType.RockBandGuitar
                            or DeviceControllerType.GuitarHeroGuitar
                            or DeviceControllerType.LiveGuitar or DeviceControllerType.Turntable
                            or DeviceControllerType.StageKit => true,
                    LedCommandType.NoteHit or LedCommandType.NoteMiss when
                        type.controllerType is DeviceControllerType.RockBandDrums
                            or DeviceControllerType.GuitarHeroDrums or DeviceControllerType.RockBandGuitar
                            or DeviceControllerType.GuitarHeroGuitar
                            or DeviceControllerType.LiveGuitar => true,
                    LedCommandType.DjEuphoria when type.controllerType is DeviceControllerType.Turntable => true,
                    LedCommandType.Ps4LightBar when type.controllerType is DeviceControllerType.Gamepad => true,
                    _ => false
                }
            };
        };
    }

    public override SerializedOutput Serialize()
    {
        var param1 = 0;
        var param2 = 0;
        switch (Command)
        {
            case LedCommandType.Player:
                param1 = Player - 1;
                break;
            case LedCommandType.Combo:
                param1 = Combo - 1;
                break;
            case LedCommandType.NoteHit:
                switch (Model.DeviceControllerType)
                {
                    case DeviceControllerType.GuitarHeroGuitar:
                    case DeviceControllerType.RockBandGuitar:
                        param1 = (int) FiveFretGuitar;
                        break;
                    case DeviceControllerType.LiveGuitar:
                        param1 = (int) SixFretGuitar;
                        break;
                    case DeviceControllerType.GuitarHeroDrums:
                        param1 = (int) GuitarHeroDrum;
                        break;
                    case DeviceControllerType.RockBandDrums:
                        param1 = (int) RockBandDrum;
                        break;
                    case DeviceControllerType.Turntable:
                        param1 = (int) Turntable;
                        break;
                }

                break;
            case LedCommandType.Mode:
                param1 = (int) EmulationModeType;
                break;
            case LedCommandType.StageKitLed:
                param1 = (int) StageKitCommand;
                param2 = StageKitCommand switch
                {
                    StageKitCommand.Strobe => 0,
                    _ => StageKitLed - 1
                };
                break;
        }

        return new SerializedLed(Enabled, LedOn, LedOff, LedIndices.ToArray(), LedIndicesPeripheral.ToArray(), Command,
            param1,
            param2, OutputEnabled,
            PeripheralOutput, OutputInverted,
            OutputPin, LedIndicesMpr121.ToArray());
    }

    public override Enum GetOutputType()
    {
        return Command;
    }

    public override string Generate(ConfigField mode, int debounceIndex, int ledIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        if (mode is not (ConfigField.StrobeLed or ConfigField.AuthLed or ConfigField.PlayerLed or ConfigField.RumbleLed
            or ConfigField.RumbleLedExpanded or ConfigField.DetectionFestival
            or ConfigField.KeyboardLed or ConfigField.LightBarLed or ConfigField.OffLed
            or ConfigField.InitLed or ConfigField.BluetoothLed)) return "";
        var on = "";
        var off = "";
        var between = "";
        var starPowerBetween = "";
        var ps4 = "";
        if (OutputPinConfig != null)
        {
            on =
                $"{Model.Microcontroller.GenerateDigitalWrite(OutputPinConfig.Pin, !OutputInverted, PeripheralOutput, Model.IsBluetooth)};";
            off =
                $"{Model.Microcontroller.GenerateDigitalWrite(OutputPinConfig.Pin, OutputInverted, PeripheralOutput, Model.IsBluetooth)};";
            between =
                $"{Model.Microcontroller.GenerateAnalogWrite(OutputPinConfig.Pin, $"{(OutputInverted ? "(255-" : "(")}rumble_left)", PeripheralOutput)};";
            starPowerBetween =
                $"{Model.Microcontroller.GenerateAnalogWrite(OutputPinConfig.Pin, (OutputInverted ? "(255-" : "(") + "last_star_power)", PeripheralOutput)};";
        }

        if (Model.IsApa102)
        {
            foreach (var index in LedIndices)
            {
                on += $"""

                       ledState[{index - 1}].select = 1;
                       {Model.LedType.GetLedAssignment(false, LedOn, index, Model.LedBrightnessOn, writer)}
                       """;


                if (SupportsLedOff)
                {
                    off += $"""

                            ledState[{index - 1}].select = 0;
                            {Model.LedType.GetLedAssignment(false, LedOff, index, Model.LedBrightnessOn, writer)}
                            """;
                }

                between +=
                    $"""

                     ledState[{index - 1}].select = 1;
                     {Model.LedType.GetLedAssignment(false, index, LedOn, LedOff, Model.LedBrightnessOn, Model.LedBrightnessOff, "rumble_left", writer)}
                     """;
                starPowerBetween +=
                    $"""

                     ledState[{index - 1}].select = 1;
                     {Model.LedType.GetLedAssignment(false, index, LedOn, LedOff, Model.LedBrightnessOn, Model.LedBrightnessOff, "last_star_power", writer)}
                     """;

                ps4 += $"""
                        ledState[{index - 1}].select = 1;
                        {Model.LedType.GetLedAssignment(false, "red", "green", "blue", Model.LedBrightnessOn.ToString(), index)};
                        """;
            }
        }

        if (Model.IsApa102Peripheral)
        {
            foreach (var index in LedIndicesPeripheral)
            {
                on += $"""

                       ledStatePeripheral[{index - 1}].select = 1;
                       {Model.LedTypePeripheral.GetLedAssignment(true, LedOn, index, Model.LedBrightnessOn, writer)}
                       """;
                if (SupportsLedOff)
                {
                    off += $"""

                            ledStatePeripheral[{index - 1}].select = 0;
                            {Model.LedTypePeripheral.GetLedAssignment(true, LedOff, index, Model.LedBrightnessOff, writer)}
                            """;
                }

                between +=
                    $"""

                     ledStatePeripheral[{index - 1}].select = 1;
                     {Model.LedTypePeripheral.GetLedAssignment(true, index, LedOn, LedOff, Model.LedBrightnessOn, Model.LedBrightnessOff, "rumble_left", writer)}
                     """;
                starPowerBetween +=
                    $"""

                     ledStatePeripheral[{index - 1}].select = 1;
                     {Model.LedTypePeripheral.GetLedAssignment(true, index, LedOn, LedOff, Model.LedBrightnessOn, Model.LedBrightnessOff, "last_star_power", writer)}
                     """;

                ps4 += $"""
                        ledStatePeripheral[{index - 1}].select = 1;
                        {Model.LedTypePeripheral.GetLedAssignment(false, "red", "green", "blue", Model.LedBrightnessOn.ToString(), index)};
                        """;
            }
        }

        if (Model.IsApa102 || Model.IsWs2812)
        {
            foreach (var index in LedIndices)
            {
                on += $"""

                       ledState[{index - 1}].select = 1;
                       {Model.LedType.GetLedAssignment(false, LedOn, index, Model.LedBrightnessOn, writer)}
                       """;

                if (SupportsLedOff)
                {
                    off += $"""

                            ledState[{index - 1}].select = 0;
                            {Model.LedType.GetLedAssignment(false, LedOff, index, Model.LedBrightnessOn, writer)}
                            """;
                }

                between +=
                    $"""

                     ledState[{index - 1}].select = 1;
                     {Model.LedType.GetLedAssignment(false, index, LedOn, LedOff, Model.LedBrightnessOn, Model.LedBrightnessOff, "rumble_left", writer)}
                     """;
                starPowerBetween +=
                    $"""

                     ledState[{index - 1}].select = 1;
                     {Model.LedType.GetLedAssignment(false, index, LedOn, LedOff, Model.LedBrightnessOn, Model.LedBrightnessOff, "last_star_power", writer)}
                     """;

                ps4 += $"""
                        ledState[{index - 1}].select = 1;
                        {Model.LedType.GetLedAssignment(false, "red", "green", "blue", Model.LedBrightnessOn.ToString(), index)};
                        """;
            }
        }

        if (Model.IsApa102Peripheral || Model.IsWs2812Peripheral)
        {
            foreach (var index in LedIndicesPeripheral)
            {
                on += $"""

                       ledStatePeripheral[{index - 1}].select = 1;
                       {Model.LedTypePeripheral.GetLedAssignment(true, LedOn, index, Model.LedBrightnessOn, writer)}
                       """;

                if (SupportsLedOff)
                {
                    off += $"""

                            ledStatePeripheral[{index - 1}].select = 0;
                            {Model.LedTypePeripheral.GetLedAssignment(true, LedOff, index, Model.LedBrightnessOff, writer)}
                            """;
                }

                between +=
                    $"""

                     ledStatePeripheral[{index - 1}].select = 1;
                     {Model.LedTypePeripheral.GetLedAssignment(true, index, LedOn, LedOff, Model.LedBrightnessOn, Model.LedBrightnessOff, "rumble_left", writer)}
                     """;
                starPowerBetween +=
                    $"""

                     ledStatePeripheral[{index - 1}].select = 1;
                     {Model.LedTypePeripheral.GetLedAssignment(true, index, LedOn, LedOff, Model.LedBrightnessOn, Model.LedBrightnessOff, "last_star_power", writer)}
                     """;

                ps4 += $"""
                        ledStatePeripheral[{index - 1}].select = 1;
                        {Model.LedTypePeripheral.GetLedAssignment(false, "red", "green", "blue", Model.LedBrightnessOn.ToString(), index)};
                        """;
            }
        }


        if (Model.HasMpr121)
        {
            foreach (var led in LedIndicesMpr121)
            {
                var index = led - 1;
                on += $"""

                       bit_clear(ledStateMpr121Select,{index % 8});
                       bit_set(ledStateMpr121,{index % 8});
                       """;
                off += $"""

                        bit_clear(ledStateMpr121Select,{index % 8});
                        bit_clear(ledStateMpr121,{index % 8});
                        """;
                between +=
                    $"""

                     bit_set(ledStateMpr121Select,{index % 8});
                     bit_write(rumble_left, ledStateMpr121,{index % 8});
                     """;
                starPowerBetween +=
                    $"""

                     bit_set(ledStateMpr121Select,{index % 8});
                     bit_write(last_star_power, ledStateMpr121,{index % 8});
                     """;

                ps4 += on;
            }
        }

        if (Model.IsStp16)
        {
            foreach (var led in LedIndices)
            {
                var index = led - 1;
                on += $"""

                       bit_clear(ledStateSelect[{index / 8}],{index % 8});
                       bit_set(ledState[{index / 8}],{index % 8});
                       """;
                off += $"""

                        bit_clear(ledStateSelect[{index / 8}],{index % 8});
                        bit_clear(ledState[{index / 8}],{index % 8});
                        """;
                between +=
                    $"""

                     bit_set(ledStateSelect[{index / 8}],{index % 8});
                     bit_write(rumble_left, ledState[{index / 8}],{index % 8});
                     """;
                starPowerBetween +=
                    $"""

                     bit_set(ledStateSelect[{index / 8}],{index % 8});
                     bit_write(last_star_power, ledState[{index / 8}],{index % 8});
                     """;

                ps4 += on;
            }
        }

        if (Model.IsStp16Peripheral)
        {
            foreach (var led in LedIndicesPeripheral)
            {
                var index = led - 1;
                on += $"""

                       bit_clear(ledStatePeripheralSelect[{index / 8}],{index % 8});
                       bit_set(ledStatePeripheral[{index / 8}],{index % 8});
                       """;
                off += $"""

                        bit_clear(ledStatePeripheralSelect[{index / 8}],{index % 8});
                        bit_clear(ledStatePeripheral[{index / 8}],{index % 8});
                        """;
                between +=
                    $"""

                     bit_set(ledStatePeripheralSelect[{index / 8}],{index % 8});
                     bit_write(rumble_left, ledStatePeripheral[{index / 8}],{index % 8});
                     """;
                starPowerBetween +=
                    $"""

                     bit_set(ledStatePeripheralSelect[{index / 8}],{index % 8});
                     bit_write(last_star_power, ledStatePeripheral[{index / 8}],{index % 8});
                     """;

                ps4 += on;
            }
        }

        switch (Command)
        {
            case LedCommandType.AlwaysOn when mode is ConfigField.InitLed:
                return on;
            case LedCommandType.Ps4LightBar when mode is ConfigField.LightBarLed:
                return $$"""
                         if (red || green || blue) {
                             {{ps4}}
                         } else {
                             {{off}}
                         }
                         """;
            case LedCommandType.Player when mode is ConfigField.PlayerLed:
                // If Player > 4, then light up LED 4 + LED - 4
                var condition = Player == 4 ? "player >= 4" : $"(player & 3) == {Player}";
                return $$"""
                         if ({{condition}}) {
                             {{on}}
                         } else {
                             {{off}}
                         }
                         """;
            // Auth commands are a set and forget type thing, they are never switched off after being turned on
            case LedCommandType.Auth when mode is ConfigField.AuthLed:
                return on;
            case LedCommandType.Mode
                when mode is ConfigField.DetectionFestival && EmulationModeType is EmulationModeType.FnfLayer:
                return $$"""
                         if (festival_gameplay_mode) {
                            {{on}}
                         }
                         """;
            case LedCommandType.Mode
                when mode is ConfigField.InitLed && EmulationModeType is not EmulationModeType.FnfLayer:
                return $$"""
                         if (consoleType == {{EmulationMode.GetDefinitionFor(EmulationModeType)}}) {
                            {{on}}
                         }
                         """;
            case LedCommandType.BluetoothConnected when mode is ConfigField.BluetoothLed:
                return $$"""
                         if (check_bluetooth_ready()) {
                             {{on}}
                         } else {
                             {{off}}
                         }
                         """;
        }

        switch (mode)
        {
            case ConfigField.InitLed:
                return off;
            case ConfigField.KeyboardLed when Command is LedCommandType.KeyboardCapsLock
                or LedCommandType.KeyboardNumLock
                or LedCommandType.KeyboardScrollLock:
                return
                    $$"""
                      if (leds & {{1 << (Command - LedCommandType.KeyboardCapsLock)}}) {
                          {{on}}
                      } else {
                          {{off}}
                      }
                      """;
        }

        switch (Command)
        {
            case LedCommandType.StageKitLed when StageKitCommand is StageKitCommand.Strobe &&
                                                 mode == ConfigField.StrobeLed:
                return $$"""
                         if (last_strobe && (millis() - last_strobe) > stage_kit_millis[strobe_delay]) {
                         last_strobe = millis();
                             {{on}}
                         } else if (last_strobe && (millis() - last_strobe) > 10) {
                             {{off}}
                         }
                         """;
            case LedCommandType.StarPowerInactive when mode == ConfigField.RumbleLedExpanded:
                return $$"""
                         star_power_active = report->starPowerActive;
                         if (!star_power_active) {
                            uint8_t rumble_left = report->starPowerState;
                            if (!rumble_left) {
                              {{starPowerBetween}}
                            } else {
                              {{off}}
                            }
                         }
                         """;
            case LedCommandType.StarPowerActive when mode == ConfigField.RumbleLedExpanded:
                return $$"""
                         star_power_active = report->starPowerActive;
                         if (star_power_active) {
                            uint8_t rumble_left = report->starPowerState;
                            if (!rumble_left) {
                              {{starPowerBetween}}
                            } else {
                              {{off}}
                            }
                         }
                         """;
            case LedCommandType.StarPowerInactive when mode == ConfigField.RumbleLed:
                return $$"""
                         if (rumble_right == {{(int) RumbleCommand.SantrollerStarPowerActive}}) {
                            star_power_active=rumble_left;
                            if (!rumble_left) {
                              {{starPowerBetween}}
                            } else {
                              {{off}}
                            }
                         }
                         if (!star_power_active && rumble_right == {{(int) RumbleCommand.SantrollerStarPowerGauge}}) {
                              last_star_power = rumble_left;
                              {{starPowerBetween}}
                         }
                         """;
            case LedCommandType.StarPowerActive when mode == ConfigField.RumbleLed:
                return $$"""
                         if (rumble_right == {{(int) RumbleCommand.SantrollerStarPowerActive}}) {
                            star_power_active=rumble_left;
                            if (rumble_left) {
                              {{starPowerBetween}}
                            } else {
                              {{off}}
                            }
                         }
                         if (star_power_active && rumble_right == {{(int) RumbleCommand.SantrollerStarPowerGauge}}) {
                              last_star_power = rumble_left;
                              {{starPowerBetween}}
                         }
                         """;
        }

        if (mode is ConfigField.OffLed && Command is LedCommandType.StageKitLed) return off;


        switch (Command)
        {
            case LedCommandType.DjEuphoria when mode is ConfigField.RumbleLed:
                return $$"""
                         if (rumble_right == {{(int) RumbleCommand.SantrollerEuphoriaLed}}) {
                             {{between}}
                         }
                         """;
            case LedCommandType.Combo when mode is ConfigField.RumbleLedExpanded:
                return $$"""
                         if (report->multiplier == {{Combo}}) {
                             {{on}}
                         } else if (report->multiplier == 0) {
                             {{off}}
                         }
                         """;
            case LedCommandType.Combo when mode is ConfigField.RumbleLed:
                return $$"""
                         if (rumble_right == {{(int) RumbleCommand.SantrollerMultiplier}}) {
                             if (rumble_left == {{Combo + 10}}) {
                                 {{on}}
                             } else if (rumble_left == 0) {
                                 {{off}}
                             }
                         }
                         """;

            case LedCommandType.NoteHit:
            {
                var santrollerCmd = Model.DeviceControllerType switch
                {
                    DeviceControllerType.GuitarHeroGuitar or DeviceControllerType.RockBandGuitar =>
                        (int) FiveFretGuitar,
                    DeviceControllerType.LiveGuitar => (int) SixFretGuitar,
                    DeviceControllerType.GuitarHeroDrums => (int) GuitarHeroDrum,
                    DeviceControllerType.RockBandDrums => (int) RockBandDrum,
                    DeviceControllerType.Turntable => (int) Turntable,
                    _ => 0
                };
                switch (mode)
                {
                    case ConfigField.RumbleLed:
                        return $$"""
                                 if (rumble_right == {{(int) RumbleCommand.SantrollerInputSpecific}}) {
                                     if ((rumble_left & ({{1 << santrollerCmd}}))) {
                                         {{on}}
                                     } else {
                                         {{off}}
                                     }
                                 }
                                 """;
                    case ConfigField.RumbleLedExpanded:
                        return $$"""
                                 if ((report->noteHitRaw & ({{1 << santrollerCmd}}))) {
                                     {{on}}
                                 } else {
                                     {{off}}
                                 }
                                 """;
                }

                break;
            }
        }


        if (Command is not LedCommandType.StageKitLed) return "";
        switch (StageKitCommand)
        {
            case StageKitCommand.Fog when mode is ConfigField.RumbleLedExpanded:
                return $$"""
                         if (report->stageKitFog) {
                             {{on}}
                         } else {
                             {{off}}
                         }
                         """;
            case StageKitCommand.Fog when mode is ConfigField.RumbleLed:
                return $$"""
                         if ((rumble_left == 0 && rumble_right == {{(int) RumbleCommand.StageKitFogOff}})) {
                             {{off}}
                         } else if (rumble_left == 0 && rumble_right == {{(int) RumbleCommand.StageKitFogOn}}) {
                             {{on}}
                         }
                         """;
            case StageKitCommand.Strobe when mode is ConfigField.RumbleLedExpanded:
                return
                    $$"""
                      strobe_delay = 5 - (report->stageKitStrobe);
                      if (strobe_delay == 0) {
                          last_strobe = 0;
                          {{off}}
                      } else {
                          last_strobe = millis();
                      }
                      """;
            case StageKitCommand.Strobe when mode is ConfigField.RumbleLed:
                return
                    $$"""
                      if (rumble_left == 0 && rumble_right >= {{(int) RumbleCommand.StageKitStrobeLightSlow}} && rumble_right <= {{(int) RumbleCommand.StageKitStrobeLightOff}}) {
                           strobe_delay = 5 - (rumble_right - {{(int) RumbleCommand.StageKitFogOff}});
                           last_strobe = millis();
                      }
                      if (strobe_delay == 0) {
                          last_strobe = 0;
                          {{off}}
                      }
                      """;
            case StageKitCommand.LedBlue when mode is ConfigField.RumbleLedExpanded:
            {
                var led = 1 << (StageKitLed - 1);
                return
                    $$"""
                      if ((report->stageKitBlue & {{led}}) == 0) {
                          {{off}}
                      } else {
                          {{on}}
                      }
                      """;
            }

            case StageKitCommand.LedGreen when mode is ConfigField.RumbleLedExpanded:
            {
                var led = 1 << (StageKitLed - 1);
                return
                    $$"""
                      if ((report->stageKitGreen & {{led}}) == 0) {
                          {{off}}
                      } else {
                          {{on}}
                      }
                      """;
            }
            case StageKitCommand.LedRed when mode is ConfigField.RumbleLedExpanded:
            {
                var led = 1 << (StageKitLed - 1);
                return
                    $$"""
                      if ((report->stageKitRed & {{led}}) == 0) {
                          {{off}}
                      } else {
                          {{on}}
                      }
                      """;
            }
            case StageKitCommand.LedYellow when mode is ConfigField.RumbleLedExpanded:
            {
                var led = 1 << (StageKitLed - 1);
                return
                    $$"""
                      if ((report->stageKitYellow & {{led}}) == 0) {
                          {{off}}
                      } else {
                          {{on}}
                      }
                      """;
            }
            case StageKitCommand.LedBlue when mode is ConfigField.RumbleLed:
            {
                var led = 1 << (StageKitLed - 1);
                return
                    $$"""
                      if (((rumble_left & {{led}}) == 0) && (rumble_right == {{(int) RumbleCommand.StageKitStrobeLightBlue}})) {
                          {{off}}
                      } else if ((rumble_left & {{led}}) && (rumble_right == {{(int) RumbleCommand.StageKitStrobeLightBlue}})) {
                          {{on}}
                      }
                      """;
            }

            case StageKitCommand.LedGreen when mode is ConfigField.RumbleLed:
            {
                var led = 1 << (StageKitLed - 1);
                return
                    $$"""
                      if (((rumble_left & {{led}}) == 0) && (rumble_right == {{(int) RumbleCommand.StageKitStrobeLightGreen}})) {
                          {{off}}
                      } else if ((rumble_left & {{led}}) && (rumble_right == {{(int) RumbleCommand.StageKitStrobeLightGreen}})) {
                          {{on}}
                      }
                      """;
            }
            case StageKitCommand.LedRed when mode is ConfigField.RumbleLed:
            {
                var led = 1 << (StageKitLed - 1);
                return
                    $$"""
                      if (((rumble_left & {{led}}) == 0) && (rumble_right == {{(int) RumbleCommand.StageKitStrobeLightRed}})) {
                          {{off}}
                      } else if ((rumble_left & {{led}}) && (rumble_right == {{(int) RumbleCommand.StageKitStrobeLightRed}})) {
                          {{on}}
                      }
                      """;
            }
            case StageKitCommand.LedYellow when mode is ConfigField.RumbleLed:
            {
                var led = 1 << (StageKitLed - 1);
                return
                    $$"""
                      if (((rumble_left & {{led}}) == 0) && (rumble_right == {{(int) RumbleCommand.StageKitStrobeLightYellow}})) {
                          {{off}}
                      } else if ((rumble_left & {{led}}) && (rumble_right == {{(int) RumbleCommand.StageKitStrobeLightYellow}})) {
                          {{on}}
                      }
                      """;
            }
            default:
                return "";
        }
    }

    public override string GenerateOutput(ConfigField mode)
    {
        return "";
    }

    public override void UpdateBindings()
    {
    }
}