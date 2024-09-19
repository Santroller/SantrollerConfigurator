using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class LedIndex : ReactiveObject
{
    public LedIndex(Output output, bool peripheral, bool mpr121, byte i)
    {
        Output = output;
        Index = i;
        Peripheral = peripheral;
        Mpr121 = mpr121;
        _selected = Collection.Contains(Index);
    }

    private bool _selected;

    public Output Output { get; }
    public byte Index { get; }

    private bool Peripheral { get; }
    private bool Mpr121 { get; }

    private ObservableCollection<byte> Collection => Peripheral ? Output.LedIndicesPeripheral :
        Mpr121 ? Output.LedIndicesMpr121 : Output.LedIndices;

    public bool Selected
    {
        get => _selected;
        set
        {
            if (value)
            {
                Collection.Add(Index);
            }
            else
            {
                Collection.Remove(Index);
            }

            _selected = value;
            this.RaisePropertyChanged();
            Output.Model.UpdateErrors();
        }
    }
}

public abstract partial class Output : ReactiveObject
{
    private readonly Guid _id = new();
    public ConfigViewModel Model { get; }

    private readonly bool _configured;
    private Color _ledOff;

    private Color _ledOn;
    private bool _outputEnabled;

    private int _outputPin;

    public ReactiveCommand<Unit, Unit> MoveUp { get; }
    public ReactiveCommand<Unit, Unit> MoveDown { get; }


    protected Output(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte[] ledIndicesPeripheral, byte[] ledIndicesMpr121, bool outputEnabled, bool outputInverted,
        bool peripheralOutput, int outputPin,
        bool childOfCombined)
    {
        Model = model;
        OutputPin = outputPin;
        OutputEnabled = outputEnabled;
        OutputInverted = outputInverted;
        PeripheralOutput = peripheralOutput;
        ChildOfCombined = childOfCombined;
        ButtonText = Resources.Assign;
        ButtonTextMidiNote = Resources.Assign;
        ButtonTextUsbHostKey = Resources.Assign;
        ButtonTextUsbHostMouseAxis = Resources.Assign;
        ButtonTextUsbHostMouseButton = Resources.Assign;
        Input = input;
        LedIndices = new ObservableCollection<byte>(ledIndices);
        LedIndicesMpr121 = new ObservableCollection<byte>(ledIndicesMpr121);
        LedIndicesPeripheral = new ObservableCollection<byte>(ledIndicesPeripheral);
        LedOn = ledOn;
        LedOff = ledOff;
        MoveUp = ReactiveCommand.Create(() => Model.MoveUp(this),
            Model.Bindings.Connect().Select(_ => Model.Bindings.Items.IndexOf(this) != 0));
        MoveDown = ReactiveCommand.Create(() => Model.MoveDown(this),
            Model.Bindings.Connect().Select(_ => Model.Bindings.Items.IndexOf(this) != Model.Bindings.Count - 1));
        _availableIndicesHelper = this.WhenAnyValue(x => x.Model.LedCount)
            .Select(x => Enumerable.Range(1, x).Select(s => new LedIndex(this, false, false, (byte) s)).ToArray())
            .ToProperty(this, x => x.AvailableIndices);
        _availableIndicesMpr121Helper = this.WhenAnyValue(x => x.Model.Mpr121CapacitiveCount)
            .Select(x => x > 4 ? x : 4)
            .Select(x => Enumerable.Range(x, 12 - x).Select(s => new LedIndex(this, false, true, (byte) s)).ToArray())
            .ToProperty(this, x => x.AvailableIndicesMpr121);
        _availableIndicesPeripheralHelper = this.WhenAnyValue(x => x.Model.LedCountPeripheral)
            .Select(x => Enumerable.Range(1, x).Select(s => new LedIndex(this, true, false, (byte) s)).ToArray())
            .ToProperty(this, x => x.AvailableIndicesPeripheral);
        _isDjHelper = this.WhenAnyValue(x => x.Input).Select(x => x.InnermostInputs().First() is DjInput)
            .ToProperty(this, x => x.IsDj);
        _isUsbHelper = this.WhenAnyValue(x => x.Input).Select(x => x.InnermostInputs().First() is UsbHostInput)
            .ToProperty(this, x => x.IsUsb);
        _isWiiHelper = this.WhenAnyValue(x => x.Input).Select(x => x.InnermostInputs().First() is WiiInput)
            .ToProperty(this, x => x.IsWii);
        _isAccelHelper = this.WhenAnyValue(x => x.Input).Select(x => x.InnermostInputs().First() is AccelInput)
            .ToProperty(this, x => x.IsAccel);
        _isMpr121Helper = this.WhenAnyValue(x => x.Input).Select(x => x.InnermostInputs().First() is Mpr121Input)
            .ToProperty(this, x => x.IsMpr121);
        _isGh5OrCloneHelper = this.WhenAnyValue(x => x.Input)
            .Select(x =>
                x.InnermostInputs().First() is Gh5NeckInput or CloneNeckInput &&
                this is not GuitarAxis {Type: GuitarAxisType.Slider})
            .ToProperty(this, x => x.IsGh5OrClone);
        _isUsbHostKeyboardHelper = this.WhenAnyValue(x => x.Input)
            .Select(x =>
                x.InnermostInputs().First() is UsbHostInput {Input: UsbHostInputType.KeyboardInput})
            .ToProperty(this, x => x.IsUsbHostKeyboard);
        _isMidiHelper = this.WhenAnyValue(x => x.Input)
            .Select(x =>
                x.InnermostInputs().First() is MidiInput)
            .ToProperty(this, x => x.IsMidi);
        _isMidiNoteHelper = this.WhenAnyValue(x => x.Input)
            .Select(x =>
                x.InnermostInputs().First() is MidiInput {Input: MidiType.Note})
            .ToProperty(this, x => x.IsMidiNote);
        _isUsbHostMouseAxisHelper = this.WhenAnyValue(x => x.Input)
            .Select(x =>
                x.InnermostInputs().First() is UsbHostInput {Input: UsbHostInputType.MouseAxis})
            .ToProperty(this, x => x.IsUsbHostMouseAxis);
        _isUsbHostMouseButtonHelper = this.WhenAnyValue(x => x.Input)
            .Select(x =>
                x.InnermostInputs().First() is UsbHostInput {Input: UsbHostInputType.MouseButton})
            .ToProperty(this, x => x.IsUsbHostMouseButton);
        _isPs2Helper = this.WhenAnyValue(x => x.Input).Select(x => x.InnermostInputs().First() is Ps2Input)
            .ToProperty(this, x => x.IsPs2);
        _isWtHelper = this.WhenAnyValue(x => x.Input)
            .Select(x =>
                x.InnermostInputs().First() is GhWtTapInput && this is not GuitarAxis {Type: GuitarAxisType.Slider})
            .ToProperty(this, x => x.IsWt);
        _titleHelper = this.WhenAnyValue(x => x.Input.Title, x => x.Model.DeviceControllerType, x => x.ShouldUpdateDetails,
                x => x.Model.LegendType, x => x.Model.SwapSwitchFaceButtons)
            .Select(x => $"{x.Item1} ({GetName(x.Item2, x.Item4, x.Item5)})")
            .ToProperty(this, x => x.Title);
        _areLedsEnabledHelper = this.WhenAnyValue(x => x.Model.LedType).Select(x => x is not LedType.None)
            .ToProperty(this, x => x.AreLedsEnabled);
        _areLedsSetHelper = LedIndices.ToObservableChangeSet(x => x).ToCollection().Select(s => s.Count != 0)
            .ToProperty(this, x => x.AreLedsSet);
        _areLedsSetPeripheralHelper = LedIndicesPeripheral.ToObservableChangeSet(x => x).ToCollection().Select(s => s.Count != 0)
            .ToProperty(this, x => x.AreLedsSetPeripheral);
        _areLedsEnabledPeripheralHelper = this.WhenAnyValue(x => x.Model.LedTypePeripheral).Select(x => x is not LedType.None)
            .ToProperty(this, x => x.AreLedsEnabledPeripheral);
        _isApa102Helper = this.WhenAnyValue(x => x.Model.LedType)
            .Select(x => x is not (LedType.None or LedType.Stp16Cpc26 or LedType.Ws2812 or LedType.Ws2812W))
            .ToProperty(this, x => x.IsApa102);
        _isApa102PeripheralHelper = this.WhenAnyValue(x => x.Model.LedTypePeripheral)
            .Select(x => x is not (LedType.None or LedType.Stp16Cpc26 or LedType.Ws2812 or LedType.Ws2812W))
            .ToProperty(this, x => x.IsApa102Peripheral);
        _ledsUseColoursHelper = this.WhenAnyValue(x => x.Model.LedType).Select(x => x is not (LedType.None or LedType.Stp16Cpc26))
            .ToProperty(this, x => x.LedsUseColours);
        _ledsUseColoursPeripheralHelper = this.WhenAnyValue(x => x.Model.LedTypePeripheral).Select(x => x is not (LedType.None or LedType.Stp16Cpc26))
            .ToProperty(this, x => x.LedsUseColoursPeripheral);
        _isWs2812Helper = this.WhenAnyValue(x => x.Model.LedType).Select(x => x is LedType.Ws2812 or LedType.Ws2812W)
            .ToProperty(this, x => x.IsWs2812);
        _isWs2812PeripheralHelper = this.WhenAnyValue(x => x.Model.LedTypePeripheral).Select(x => x is LedType.Ws2812 or LedType.Ws2812W)
            .ToProperty(this, x => x.IsWs2812Peripheral);
        _isStpHelper = this.WhenAnyValue(x => x.Model.LedType).Select(x => x is LedType.Stp16Cpc26)
            .ToProperty(this, x => x.IsStp);
        _isStpPeripheralHelper = this.WhenAnyValue(x => x.Model.LedTypePeripheral).Select(x => x is LedType.Stp16Cpc26)
            .ToProperty(this, x => x.IsStpPeripheral);
        _localisedNameHelper = this.WhenAnyValue(x => x.Model.DeviceControllerType, x => x.ShouldUpdateDetails, x => x.Model.LegendType,
                x => x.Model.SwapSwitchFaceButtons)
            .Select(x => GetName(x.Item1, x.Item3, x.Item4))
            .ToProperty(this, x => x.LocalisedName);
        _valueRawHelper = this.WhenAnyValue(x => x.Input.RawValue, x => x.Enabled).Select(x => x.Item2 ? x.Item1 : 0)
            .ToProperty(this, x => x.ValueRaw);
        _imageOpacityHelper = this.WhenAnyValue(x => x.ValueRaw, x => x.Input, x => x.IsCombined)
            .Select(GetOpacity)
            .ToProperty(this, s => s.ImageOpacity);
        _combinedOpacityHelper = this.WhenAnyValue(x => x.Enabled)
            .Select(s => s ? 1 : 0.5)
            .ToProperty(this, s => s.CombinedOpacity);
        _outputTypeHelper = this.WhenAnyValue(x => x.Model.DeviceControllerType, x => x.ShouldUpdateDetails,
                x => x.ChildOfCombined)
            .Select(x => x.Item3 ? GetChildOutputType() : GetOutputType())
            .ToProperty(this, x => x.OutputType);
        _combinedBackgroundHelper = this.WhenAnyValue(x => x.Enabled)
            .Select(enabled => enabled ? Brush.Parse("#99000000") : Brush.Parse("#33000000"))
            .ToProperty(this, s => s.CombinedBackground);
        this.WhenAnyValue(x => x.Model.HasPeripheral).Subscribe(s => this.RaisePropertyChanged(nameof(InputTypes)));
        this.WhenAnyValue(x => x.Model.HasAccel).Subscribe(s => this.RaisePropertyChanged(nameof(InputTypes)));
        this.WhenAnyValue(x => x.UsesPwm).Subscribe(s =>
        {
            if (s && !AvailablePwmPins.Contains(_outputPin))
            {
                _outputPin = -1;
            }
        });
        Outputs = new SourceList<Output>();
        Outputs.Add(this);
        AnalogOutputs = ReadOnlyObservableCollection<Output>.Empty;
        DigitalOutputs = ReadOnlyObservableCollection<Output>.Empty;
        AllDigitalOutputs = ReadOnlyObservableCollection<Output>.Empty;
        if (this is OutputButton or {Input.IsAnalog: false})
        {
            AllDigitalOutputs = new ReadOnlyObservableCollection<Output>(new ObservableCollection<Output>([this]));
        }

        Outputs.Connect().Bind(out var allOutputs).Subscribe();
        AllOutputs = allOutputs;
        _configured = true;
        IsVisible = !Model.Branded || LedIndices.Any() || this is Led || this is BluetoothOutput ||
                    this is CombinedOutput || this is OutputAxis {Input: not DigitalToAnalog} ||
                    this is {Input: AnalogToDigital} || this is JoystickToDpad;
    }

    private bool _outputPeripheral;

    public bool PeripheralOutput
    {
        get => _outputPeripheral;
        set
        {
            this.RaiseAndSetIfChanged(ref _outputPeripheral, value);
            if (OutputEnabled)
            {
                OutputPinConfig = new DirectPinConfig(Model, "led_output", OutputPin, Model.HasPeripheral && value,
                    DevicePinMode.Output);
            }
        }
    }

    private bool _outputInverted;

    public bool OutputInverted
    {
        get => _outputInverted;
        set
        {
            this.RaiseAndSetIfChanged(ref _outputInverted, value);
            if (Model.Device is not Santroller santroller || OutputPin == -1) return;
            if (UsesPwm)
            {
                santroller.AnalogWrite(OutputPin, value ? 255 - Test : Test);
            }
            else
            {
                santroller.DigitalWrite(OutputPin, value ? Test == 0 : Test != 0);
            }
        }
    }


    private int _test;

    public int Test
    {
        get => _test;
        set
        {
            if (Model.Device is not Santroller santroller || !UsesPwm) return;
            santroller.AnalogWrite(OutputPin, OutputInverted ? 255 - value : value);

            this.RaiseAndSetIfChanged(ref _test, value);
        }
    }

    public bool TestDigital
    {
        get => _test != 0;
        set
        {
            if (Model.Device is not Santroller santroller || UsesPwm) return;
            santroller.DigitalWrite(OutputPin, OutputInverted ? !value : value);

            this.RaiseAndSetIfChanged(ref _test, value ? 255 : 0);
        }
    }

    public virtual bool UsesPwm => false;

    public bool OutputEnabled
    {
        get => _outputEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _outputEnabled, value);
            if (value)
            {
                OutputPinConfig ??= new DirectPinConfig(Model, "led_output", OutputPin,
                    Model.HasPeripheral && PeripheralOutput, DevicePinMode.Output);
            }
            else
            {
                OutputPinConfig = null;
            }

            Model.UpdateErrors();
        }
    }

    public ReadOnlyObservableCollection<int> AvailablePins => Model.AvailablePinsDigital;
    public List<int> AvailablePwmPins => Model.Microcontroller.PwmPins;

    public DirectPinConfig? OutputPinConfig { get; private set; }

    public int OutputPin
    {
        get => _outputPin;
        set
        {
            this.RaiseAndSetIfChanged(ref _outputPin, value);
            if (OutputPinConfig == null) return;
            OutputPinConfig.Pin = value;
        }
    }

    private double GetOpacity((int, Input, bool) s)
    {
        var check = s.Item1 != 0 || s.Item3 || s.Item2.IsAnalog;

        if (this is PianoKey)
        {
            check = s.Item1 != 0;
        }

        if (this is DrumAxis axis)
        {
            check = s.Item1 > axis.Min;
            if (axis.Min > axis.Max)
            {
                check = (s.Item1 - axis.Min) < axis.DeadZone;
            }
        }

        if (check)
        {
            return 1;
        }

        return 0.65;
    }


    public virtual bool LedsHaveColours => true;

    private bool ShouldUpdateDetails { get; set; }

    [Reactive] private Input _input;

    [Reactive] private bool _enabled = true;

    [Reactive] private bool _expanded;

    public Color LedOn
    {
        get => _ledOn;
        set
        {
            this.RaiseAndSetIfChanged(ref _ledOn, value);
            if (!_configured || Model.Device is not Santroller santroller) return;
            if (Model.LedType != LedType.None)
            {
                foreach (var ledIndex in LedIndices)
                {
                    santroller.SetLed((byte) (ledIndex - 1), value, Model.LedBrightnessOn);
                }
            }

            if (Model.LedTypePeripheral != LedType.None)
            {
                foreach (var ledIndex in LedIndicesPeripheral)
                {
                    santroller.SetLedPeripheral((byte) (ledIndex - 1), value, Model.LedBrightnessOn);
                }
            }

            if (Model.HasMpr121)
            {
                foreach (var ledIndex in LedIndicesMpr121)
                {
                    santroller.SetLedMpr121((byte) (ledIndex - 1), true);
                }
            }
        }
    }

    public Color LedOff
    {
        get => _ledOff;
        set
        {
            this.RaiseAndSetIfChanged(ref _ledOff, value);
            if (!_configured || Model.Device is not Santroller santroller) return;
            if (Model.LedType != LedType.None)
            {
                foreach (var ledIndex in LedIndices)
                {
                    santroller.SetLed((byte) (ledIndex - 1), value, Model.LedBrightnessOff);
                }
            }

            if (Model.LedTypePeripheral != LedType.None)
            {
                foreach (var ledIndex in LedIndicesPeripheral)
                {
                    santroller.SetLedPeripheral((byte) (ledIndex - 1), value, Model.LedBrightnessOff);
                }
            }

            if (Model.HasMpr121)
            {
                foreach (var ledIndex in LedIndicesMpr121)
                {
                    santroller.SetLedMpr121((byte) (ledIndex - 1), false);
                }
            }
        }
    }


    public InputType? SelectedInputType
    {
        get => Input.InputType;
        set => SetInput(value, null, null, null, null, null, null, null, null);
    }

    public Key UsbHostKey
    {
        get => GetUsbHostKey() ?? Key.A;
        set => SetUsbHostKey(value);
    }

    public int MidiNote
    {
        get => GetMidiNote() ?? 1;
        set => SetMidiNote(value);
    }

    public IEnumerable<int> MidiNotes => Enumerable.Range(0, 129);

    // Available notes to start pro keys at need to leave space for 25 keys
    public IEnumerable<int> MidiNotesFirst => Enumerable.Range(0, 129 - 25);

    public MouseAxisType UsbHostKeyMouseAxisType
    {
        get => GetUsbHostMouseAxis() ?? MouseAxisType.X;
        set => SetUsbHostMouseAxis(value);
    }

    public MouseButtonType UsbHostKeyMouseButtonType
    {
        get => GetUsbHostMouseButton() ?? MouseButtonType.Left;
        set => SetUsbHostMouseButton(value);
    }

    public WiiInputType WiiInputType
    {
        get => (Input.InnermostInputs().First() as WiiInput)?.Input ?? WiiInputType.ClassicA;
        set => SetInput(SelectedInputType, value, null, null, null, null, null, null, null);
    }

    public Ps2InputType Ps2InputType
    {
        get => (Input.InnermostInputs().First() as Ps2Input)?.Input ?? Ps2InputType.Cross;
        set => SetInput(SelectedInputType, null, value, null, null, null, null, null, null);
    }

    public object KeyOrMouse
    {
        get => GetKey() ?? Key.Space;
        set => SetKey(value);
    }

    public DjInputType DjInputType
    {
        get => (Input.InnermostInputs().First() as DjInput)?.Input ?? DjInputType.LeftGreen;
        set => SetInput(SelectedInputType, null, null, null, null, value, null, null, null);
    }

    public UsbHostInputType UsbInputType
    {
        get => (Input.InnermostInputs().First() as UsbHostInput)?.Input ?? UsbHostInputType.A;
        set => SetInput(SelectedInputType, null, null, null, null, null, value, null, null);
    }

    public Gh5NeckInputType Gh5NeckInputType
    {
        get => (Input.InnermostInputs().First() as Gh5NeckInput)?.Input ??
               (Input.InnermostInputs().First() as CloneNeckInput)?.Input ?? Gh5NeckInputType.Green;
        set => SetInput(SelectedInputType, null, null, null, value, null, null, null, null);
    }

    public GhWtInputType GhWtInputType
    {
        get => (Input.InnermostInputs().First() as GhWtTapInput)?.Input ?? GhWtInputType.TapGreen;
        set => SetInput(SelectedInputType, null, null, value, null, null, null, null, null);
    }

    public AccelInputType AccelInputType
    {
        get => (Input.InnermostInputs().First() as AccelInput)?.Input ?? AccelInputType.AccelX;
        set => SetInput(SelectedInputType, null, null, null, null, null, null, value, null);
    }

    public MidiType MidiType
    {
        get => (Input.InnermostInputs().First() as MidiInput)?.Input ?? MidiType.Note;
        set => SetInput(SelectedInputType, null, null, null, null, null, null, null, value);
    }

    public IEnumerable<MidiType> MidiTypes => Enum.GetValues<MidiType>();

    public IEnumerable<GhWtInputType> GhWtInputTypes =>
        Enum.GetValues<GhWtInputType>().Where(s => s is not GhWtInputType.TapAll);

    public IEnumerable<Gh5NeckInputType> Gh5NeckInputTypes =>
        Enum.GetValues<Gh5NeckInputType>().Where(s => s is not Gh5NeckInputType.TapAll);

    public IEnumerable<UsbHostInputType> UsbInputTypes => Enum.GetValues<UsbHostInputType>();

    public IEnumerable<object> KeyOrMouseInputs => Enum.GetValues<MouseButtonType>().Cast<object>()
        .Concat(Enum.GetValues<MouseAxisType>().Cast<object>()).Concat(KeyboardButton.Keys.Cast<object>());

    public IEnumerable<Key> KeyboardButtons => KeyboardButton.Keys;
    public IEnumerable<MouseButtonType> MouseButtonTypes => Enum.GetValues<MouseButtonType>();
    public IEnumerable<MouseAxisType> MouseAxisTypes => Enum.GetValues<MouseAxisType>();


    public IEnumerable<Ps2InputType> Ps2InputTypes => Enum.GetValues<Ps2InputType>();

    public IEnumerable<WiiInputType> WiiInputTypes =>
        Enum.GetValues<WiiInputType>().OrderBy(s => EnumToStringConverter.Convert(s));

    public IEnumerable<DjInputType> DjInputTypes => Enum.GetValues<DjInputType>();
    public IEnumerable<AccelInputType> AdxlInputTypes => Enum.GetValues<AccelInputType>();

    public IEnumerable<InputType> InputTypes =>
        Enum.GetValues<InputType>().Where(s =>
            (this is not GuitarAxis {Type: GuitarAxisType.Slider} ||
             s is InputType.Gh5NeckInput or InputType.WtNeckInput or InputType.ConstantInput
                 or InputType.WtNeckPeripheralInput or InputType.CloneNeckInput ||
             (s is InputType.Mpr121Input && Model.HasMpr121)) &&
            (s is not InputType.WtNeckPeripheralInput || Model.HasPeripheral) &&
            (s is not InputType.MultiplexerInput || Model.IsPico) &&
            (s is not InputType.DigitalPeripheralInput || Model.HasPeripheral) &&
            (s is not InputType.Mpr121Input || Model.HasMpr121) &&
            s is not InputType.BluetoothInput &&
            (s is not InputType.UsbHostInput || Model.IsPico) &&
            (s is not InputType.AccelInput || Model.HasAccel));

    private Enum GetChildOutputType()
    {
        if (Input.InnermostInputs().First() is WiiInput wii) return wii.Input;

        if (Input.InnermostInputs().First() is Ps2Input ps2) return ps2.Input;

        if (Input.InnermostInputs().First() is AccelInput adxl) return adxl.Input;

        if (Input.InnermostInputs().First() is DjInput dj) return dj.Input;

        if (Input.InnermostInputs().First() is Gh5NeckInput gh5) return gh5.Input;

        if (Input.InnermostInputs().First() is CloneNeckInput c) return c.Input;

        if (Input.InnermostInputs().First() is GhWtTapInput wt) return wt.Input;

        if (Input.InnermostInputs().First() is UsbHostInput usb) return usb.Input;

        return GetOutputType();
    }

    protected void UpdateDetails()
    {
        ShouldUpdateDetails = true;
        this.RaisePropertyChanged(nameof(ShouldUpdateDetails));
        ShouldUpdateDetails = false;
        this.RaisePropertyChanged(nameof(ShouldUpdateDetails));
    }

    [RelayCommand]
    public void TestLEDs()
    {
        if (!_configured || Model.Device is not Santroller santroller) return;
        if (Model.LedType == LedType.Stp16Cpc26)
        {
            foreach (var ledIndex in LedIndices)
            {
                santroller.SetLedStp((byte) (ledIndex - 1), true);
            }
        }
        else if (Model.LedType != LedType.None)
        {
            foreach (var ledIndex in LedIndices)
            {
                santroller.SetLed((byte) (ledIndex - 1), LedOn, Model.LedBrightnessOn);
            }
        }

        if (Model.LedTypePeripheral == LedType.Stp16Cpc26)
        {
            foreach (var ledIndex in LedIndicesPeripheral)
            {
                santroller.SetLedStpPeripheral((byte) (ledIndex - 1), true);
            }
        }
        else if (Model.LedTypePeripheral != LedType.None)
        {
            foreach (var ledIndex in LedIndicesPeripheral)
            {
                santroller.SetLedPeripheral((byte) (ledIndex - 1), LedOn, Model.LedBrightnessOn);
            }
        }

        if (Model.HasMpr121)
        {
            foreach (var ledIndex in LedIndicesMpr121)
            {
                santroller.SetLedMpr121((byte) (ledIndex - 1), true);
            }
        }
    }

    [ObservableAsProperty] private Enum _outputType = null!;
    [ObservableAsProperty] private string _localisedName = "";
    [ObservableAsProperty] private bool _isDj;
    [ObservableAsProperty] private bool _isWii;
    [ObservableAsProperty] private bool _isAccel;
    [ObservableAsProperty] private bool _isMpr121;
    [ObservableAsProperty] private bool _isUsb;
    [ObservableAsProperty] private bool _isPs2;
    [ObservableAsProperty] private bool _isGh5OrClone;

    [ObservableAsProperty] private bool _isUsbHostKeyboard;

    [ObservableAsProperty] private bool _isMidi;

    [ObservableAsProperty] private bool _isMidiNote;
    [ObservableAsProperty] private bool _isUsbHostMouseAxis;
    [ObservableAsProperty] private bool _isUsbHostMouseButton;
    [ObservableAsProperty] private bool _isWt;
    [ObservableAsProperty] private bool _areLedsEnabled;
    [ObservableAsProperty] private bool _areLedsSet;
    [ObservableAsProperty] private bool _areLedsSetPeripheral;
    [ObservableAsProperty] private bool _areLedsEnabledPeripheral;
    [ObservableAsProperty] private bool _isApa102;
    [ObservableAsProperty] private bool _isApa102Peripheral;
    [ObservableAsProperty] private bool _ledsUseColours;
    [ObservableAsProperty] private bool _ledsUseColoursPeripheral;
    [ObservableAsProperty] private bool _isStp;
    [ObservableAsProperty] private bool _isStpPeripheral;
    [ObservableAsProperty] private bool _isWs2812;
    [ObservableAsProperty] private bool _isWs2812Peripheral;
    [ObservableAsProperty] private LedIndex[] _availableIndices = [];
    [ObservableAsProperty] private LedIndex[] _availableIndicesPeripheral = [];
    [ObservableAsProperty] private LedIndex[] _availableIndicesMpr121 = [];

    [ObservableAsProperty] private double _combinedOpacity;
    [ObservableAsProperty] private IBrush _combinedBackground = Brush.Parse("#99000000");

    [ObservableAsProperty] private double _imageOpacity;

    [ObservableAsProperty] private int _valueRaw;
    [ObservableAsProperty] private string _title = "";

    public abstract bool IsCombined { get; }
    public ObservableCollection<byte> LedIndices { get; set; }
    public ObservableCollection<byte> LedIndicesPeripheral { get; set; }
    public ObservableCollection<byte> LedIndicesMpr121 { get; set; }
    public string Id => _id.ToString();


    public abstract bool IsStrum { get; }

    public SourceList<Output> Outputs { get; }

    public ReadOnlyObservableCollection<Output> AnalogOutputs { get; protected set; }
    public ReadOnlyObservableCollection<Output> DigitalOutputs { get; protected set; }
    public ReadOnlyObservableCollection<Output> AllDigitalOutputs { get; protected set; }
    private ReadOnlyObservableCollection<Output> AllOutputs { get; set; }

    public abstract bool IsKeyboard { get; }
    public bool IsLed => this is Led;

    public bool ChildOfCombined { get; }

    public bool ShouldShowEnabled => ChildOfCombined && !Model.Branded;
    public bool IsEmpty => this is EmptyOutput;

    [Reactive] private string _buttonText;
    [Reactive] private string _buttonTextMidiNote;
    [Reactive] private string _buttonTextUsbHostKey;
    [Reactive] private string _buttonTextUsbHostMouseAxis;
    [Reactive] private string _buttonTextUsbHostMouseButton;

    public virtual string ErrorText
    {
        get
        {
            var strings = GetPinConfigs().Select(s => s.ErrorText).Distinct().Where(s => !string.IsNullOrEmpty(s))
                .ToList();
            if (Model.HasMpr121)
            {
                foreach (var channel in LedIndicesMpr121)
                {
                    if (Model.Outputs.Any(s =>
                            s.Input.InnermostInputs().Any(s2 =>
                                s2 is Mpr121Input s3 && s3.Input == channel ||
                                s2 is Mpr121SliderInput s5 && s5.MappedInputs.Contains(channel))))
                    {
                        strings.Add($"Mpr121 Channel Conflict: {channel} used as both LED and input!");
                    }
                }

                foreach (var s in Input.InnermostInputs())
                {
                    switch (s)
                    {
                        case Mpr121Input s2 when
                            Model.Outputs.Any(s3 => s3 != this && s3.LedIndicesMpr121.Contains((byte) s2.Input)):
                            strings.Add($"Mpr121 Channel Conflict: {s2.Input} used as both LED and input!");
                            break;
                        case Mpr121SliderInput s4:
                            strings.AddRange(s4.MappedInputs
                                .Where(mapped =>
                                    Model.Outputs.Any(s3 => s3 != this && s3.LedIndicesMpr121.Contains((byte) mapped)))
                                .Select(mapped => $"Mpr121 Channel Conflict: {mapped} used as both LED and input!"));
                            break;
                    }
                }
            }

            var text = string.Join(", ", strings);
            if (text.Contains("missing")) return Resources.ErrorPinConfigurationMissing;
            return string.IsNullOrEmpty(text) ? "" : text;
        }
    }


    public abstract string LedOnLabel { get; }

    public abstract string LedOffLabel { get; }

    public virtual bool SupportsLedOff => true;
    public bool ConfigurableInput => Input is not (FixedInput or MacroInput);
    public bool IsVisible { get; }

    private object? GetKey()
    {
        return this switch
        {
            KeyboardButton button => button.Key,
            MouseAxis axis => axis.Type,
            MouseButton button => button.Type,
            _ => null
        };
    }

    private void SetMidiNote(int value)
    {
        var current = GetMidiNote();
        if (current == null) return;
        if (current == value) return;
        if (this is MidiCombinedOutput mco)
        {
            mco.FirstNote = value;
            return;
        }

        if (Input is AnalogToDigital atd)
        {
            Input = new AnalogToDigital(new MidiInput(MidiType.Note, value, Model), atd.AnalogToDigitalType,
                atd.Threshold, Model);
            return;
        }

        Input = new MidiInput(MidiType.Note, value, Model);
    }

    private int? GetMidiNote()
    {
        if (this is MidiCombinedOutput mco)
        {
            return mco.FirstNote;
        }

        return (Input.InnermostInputs().First() as MidiInput)?.Key;
    }

    private Key? GetUsbHostKey()
    {
        return (Input.InnermostInputs().First() as UsbHostInput)?.Key;
    }

    private MouseAxisType? GetUsbHostMouseAxis()
    {
        return (Input.InnermostInputs().First() as UsbHostInput)?.MouseAxisType;
    }

    private MouseButtonType? GetUsbHostMouseButton()
    {
        return (Input.InnermostInputs().First() as UsbHostInput)?.MouseButtonType;
    }

    private void SetUsbHostKey(Key value)
    {
        var current = GetUsbHostKey();
        if (current == null) return;
        if (current == value) return;
        if (Input is DigitalToAnalog dta)
        {
            Input = new DigitalToAnalog(new UsbHostInput(value, Model), dta.On, Model, dta.Type);
            return;
        }

        Input = new UsbHostInput(value, Model);
    }

    private void SetUsbHostMouseAxis(MouseAxisType value)
    {
        var current = GetUsbHostMouseAxis();
        if (current == null) return;
        if (current == value) return;
        if (Input is AnalogToDigital atd)
        {
            Input = new AnalogToDigital(new UsbHostInput(value, Model), atd.AnalogToDigitalType, atd.Threshold, Model);
            return;
        }

        Input = new UsbHostInput(value, Model);
    }

    private void SetUsbHostMouseButton(MouseButtonType value)
    {
        var current = GetUsbHostMouseButton();
        if (current == null) return;
        if (current == value) return;
        if (Input is DigitalToAnalog dta)
        {
            Input = new DigitalToAnalog(new UsbHostInput(value, Model), dta.On, Model, dta.Type);
            return;
        }

        Input = new UsbHostInput(value, Model);
    }

    private void SetKey(object value)
    {
        var current = GetKey();
        if (current == null) return;
        if (current.GetType() == value.GetType() && (int) current == (int) value) return;

        int debounce = 1;
        int min = short.MinValue;
        int max = short.MaxValue;
        var deadzone = 0;
        switch (this)
        {
            case OutputAxis axis:
                min = axis.Min;
                max = axis.Max;
                deadzone = axis.DeadZone;
                break;
            case OutputButton button:
                debounce = button.Debounce;
                break;
        }

        Output? newOutput = value switch
        {
            Key key => new KeyboardButton(Model, Input, LedOn, LedOff, LedIndices.ToArray(),
                LedIndicesPeripheral.ToArray(), LedIndicesMpr121.ToArray(), debounce, key, false, false, false, -1),
            MouseButtonType mouseButtonType => new MouseButton(Model, Input, LedOn, LedOff, LedIndices.ToArray(),
                LedIndicesPeripheral.ToArray(), LedIndicesMpr121.ToArray(),
                debounce, mouseButtonType, false, false, false, -1),
            MouseAxisType axisType => new MouseAxis(Model, Input, LedOn, LedOff, LedIndices.ToArray(),
                LedIndicesPeripheral.ToArray(), LedIndicesMpr121.ToArray(), min, max,
                deadzone, axisType, false, false, false, -1),
            _ => null
        };

        if (newOutput == null) return;
        newOutput.Expanded = Expanded;
        Model.Bindings.Insert(Model.Bindings.Items.IndexOf(this), newOutput);
        Model.RemoveOutput(this);
    }

    public abstract string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons);

    public abstract Enum GetOutputType();

    public static string GetReportField(object type, string field = "report")
    {
        var typeName = type.ToString()!;
        return $"{field}->{char.ToLower(typeName[0])}{typeName[1..]}";
    }

    [RelayCommand]
    public void CopyOn()
    {
        LedOff = LedOn;
    }

    [RelayCommand]
    public void CopyOff()
    {
        LedOn = LedOff;
    }

    private byte[] _midiNotes = new byte[127];

    [RelayCommand]
    private async Task FindAndAssignMidiNoteAsync()
    {
        ButtonTextMidiNote = Resources.AssignMidiNote;
        var current = _midiNotes.ToArray();
        var found = false;
        var noteCount = 128;
        if (this is MidiCombinedOutput)
        {
            noteCount -= 25;
        }

        while (!found)
        {
            await Task.Delay(100);
            for (var i = 0; i < noteCount && i < _midiNotes.Length && i < current.Length; i++)
            {
                if (_midiNotes[i] == current[i]) continue;
                MidiNote = i;

                found = true;
                break;
            }
        }

        ButtonTextMidiNote = Resources.Assign;
        this.RaisePropertyChanged(nameof(MidiNote));
    }

    [RelayCommand]
    private async Task FindAndAssignUsbHostMouseAxisAsync()
    {
        ButtonTextUsbHostMouseAxis = Resources.AssignMouseAxis;
        var lastPoint = await Model.KeyOrPointerEvent.OfType<Point>().Take(1).ToTask();
        await Task.Delay(100);
        var point = await Model.KeyOrPointerEvent.OfType<Point>().Take(1).ToTask();
        var diff = lastPoint - point;
        UsbHostKeyMouseAxisType = Math.Abs(diff.X) > Math.Abs(diff.Y) ? MouseAxisType.X : MouseAxisType.Y;

        ButtonTextUsbHostMouseAxis = Resources.Assign;
        this.RaisePropertyChanged(nameof(UsbHostKeyMouseAxisType));
    }

    [RelayCommand]
    private async Task FindAndAssignUsbHostMouseButtonAsync()
    {
        ButtonTextUsbHostMouseButton = Resources.AssignMouseButton;
        var pointerUpdateKind = await Model.KeyOrPointerEvent.OfType<PointerUpdateKind>().Take(1).ToTask();
        UsbHostKeyMouseButtonType = pointerUpdateKind switch
        {
            PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.LeftButtonReleased => MouseButtonType.Left,
            PointerUpdateKind.MiddleButtonPressed or PointerUpdateKind.MiddleButtonReleased => MouseButtonType.Middle,
            PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased => MouseButtonType.Right,
            _ => UsbHostKeyMouseButtonType
        };
        ButtonTextUsbHostMouseButton = Resources.Assign;
        this.RaisePropertyChanged(nameof(UsbHostKeyMouseButtonType));
    }

    [RelayCommand]
    private async Task FindAndAssignUsbHostKeyAsync()
    {
        ButtonTextUsbHostKey = Resources.AssignKeyboard;
        var keyEventArgs = await Model.KeyOrPointerEvent.OfType<KeyEventArgs>().Take(1).ToTask();
        UsbHostKey = keyEventArgs.Key;
        ButtonTextUsbHostKey = Resources.Assign;
        this.RaisePropertyChanged(nameof(UsbHostKey));
    }

    [RelayCommand]
    private async Task FindAndAssignAsync()
    {
        ButtonText = Resources.AssignMouseKeyboard;
        var lastEvent = await Model.KeyOrPointerEvent.Take(1).ToTask();
        switch (lastEvent)
        {
            case KeyEventArgs keyEventArgs:
                KeyOrMouse = keyEventArgs.Key;
                break;
            case PointerUpdateKind pointerUpdateKind:
                switch (pointerUpdateKind)
                {
                    case PointerUpdateKind.LeftButtonPressed:
                    case PointerUpdateKind.LeftButtonReleased:
                        KeyOrMouse = MouseButtonType.Left;
                        break;
                    case PointerUpdateKind.MiddleButtonPressed:
                    case PointerUpdateKind.MiddleButtonReleased:
                        KeyOrMouse = MouseButtonType.Middle;
                        break;
                    case PointerUpdateKind.RightButtonPressed:
                    case PointerUpdateKind.RightButtonReleased:
                        KeyOrMouse = MouseButtonType.Right;
                        break;
                }

                break;
            case PointerWheelEventArgs wheelEventArgs:
                KeyOrMouse = Math.Abs(wheelEventArgs.Delta.X) > Math.Abs(wheelEventArgs.Delta.Y)
                    ? MouseAxisType.ScrollX
                    : MouseAxisType.ScrollY;
                break;
            case Point point:
                await Task.Delay(100);
                while (true)
                {
                    var last = await Model.KeyOrPointerEvent.Take(1).ToTask();
                    if (last is not Point lastPoint) continue;
                    var diff = lastPoint - point;
                    KeyOrMouse = Math.Abs(diff.X) > Math.Abs(diff.Y) ? MouseAxisType.X : MouseAxisType.Y;
                    break;
                }

                break;
        }

        ButtonText = Resources.Assign;
    }

    private void SetInput(InputType? inputType, WiiInputType? wiiInput, Ps2InputType? ps2InputType,
        GhWtInputType? ghWtInputType, Gh5NeckInputType? gh5NeckInputType, DjInputType? djInputType,
        UsbHostInputType? usbInputType, AccelInputType? accelInputType, MidiType? midiType)
    {
        Input input;
        switch (inputType)
        {
            case InputType.ConstantInput
                when Input.InnermostInputs().First() is not FixedInput && this is OutputAxis axis:
                input = new ConstantInput(Model, 0, true, axis.Min, axis.Max,
                    axis is GuitarAxis {Type: GuitarAxisType.Slider}, axis is GuitarAxis {Type: GuitarAxisType.Pickup});
                break;
            case InputType.ConstantInput when Input.InnermostInputs().First() is not FixedInput:
                input = new ConstantInput(Model, 0, false, 0, 1, false, false);
                break;
            case InputType.ConstantInput:
                input = Input.InnermostInputs().First();
                break;
            case InputType.MidiInput when Input.InnermostInputs().First() is not MidiInput:
                midiType ??= MidiType.Note;
                input = new MidiInput(midiType.Value, 0, Model);
                break;
            case InputType.MidiInput when Input.InnermostInputs().First() is MidiInput midiInput:
                midiType ??= midiInput.Input;
                input = new MidiInput(midiType.Value, midiInput.Key, Model);
                break;
            case InputType.UsbHostInput when Input.InnermostInputs().First() is not UsbHostInput:
                usbInputType ??= UsbHostInputType.A;
                input = new UsbHostInput(usbInputType.Value, Model);
                break;
            case InputType.UsbHostInput when Input.InnermostInputs().First() is UsbHostInput usbHost:
                usbInputType ??= usbHost.Input;
                input = new UsbHostInput(usbInputType.Value, Model);
                break;
            case InputType.AnalogPinInput:
                input = new DirectInput(-1, false, false, DevicePinMode.Analog, Model);
                break;
            case InputType.MultiplexerInput:
                input = new MultiplexerInput(-1, false, 0, -1, -1, -1, -1, MultiplexerType.EightChannel, Model);
                break;
            case InputType.DigitalPinInput:
                input = new DirectInput(-1, false, false, DevicePinMode.PullUp, Model);
                break;
            case InputType.Mpr121Input when this is GuitarAxis {Type: GuitarAxisType.Slider}:
                input = new Mpr121SliderInput(Model, false, 0, 0, 0, 0, 0);
                break;
            case InputType.Mpr121Input:
                input = new Mpr121Input(0, Model, false);
                break;
            case InputType.MacroInput:
                input = new MacroInput(new DirectInput(-1, false, false, DevicePinMode.PullUp, Model),
                    new DirectInput(-1, false, false, DevicePinMode.PullUp, Model), Model);
                break;
            case InputType.DigitalPeripheralInput:
                input = new DirectInput(-1, false, true, DevicePinMode.PullUp, Model);
                break;
            case InputType.AccelInput when Input.InnermostInputs().First() is not AccelInput:
                accelInputType ??= AccelInputType.AccelX;
                input = new AccelInput(accelInputType.Value, Model, false);
                break;
            case InputType.AccelInput when Input.InnermostInputs().First() is AccelInput accel:
                accelInputType ??= AccelInputType.AccelX;
                input = new AccelInput(accelInputType.Value, Model, accel.Peripheral);
                break;
            case InputType.TurntableInput when Input.InnermostInputs().First() is not DjInput:
                djInputType ??= DjInputType.LeftGreen;
                input = new DjInput(djInputType.Value, Model, false);
                break;
            case InputType.TurntableInput when Input.InnermostInputs().First() is DjInput dj:
                djInputType ??= dj.Input;
                input = new DjInput(djInputType.Value, Model, dj.Peripheral, dj.Sda, dj.Scl);
                break;
            case InputType.Gh5NeckInput when Input.InnermostInputs().First() is not Gh5NeckInput:
                gh5NeckInputType ??= Gh5NeckInputType.Green;
                if (this is OutputAxis) gh5NeckInputType = Gh5NeckInputType.TapBar;
                input = new Gh5NeckInput(gh5NeckInputType.Value, Model, false);
                break;
            case InputType.Gh5NeckInput when Input.InnermostInputs().First() is Gh5NeckInput gh5:
                gh5NeckInputType ??= gh5.Input;
                if (this is OutputAxis) gh5NeckInputType = Gh5NeckInputType.TapBar;
                input = new Gh5NeckInput(gh5NeckInputType.Value, Model, gh5.Peripheral, gh5.Sda, gh5.Scl);
                break;
            case InputType.CloneNeckInput when Input.InnermostInputs().First() is not CloneNeckInput:
                gh5NeckInputType ??= Gh5NeckInputType.Green;
                if (this is OutputAxis) gh5NeckInputType = Gh5NeckInputType.TapBar;
                input = new CloneNeckInput(gh5NeckInputType.Value, Model, false);
                break;
            case InputType.CloneNeckInput when Input.InnermostInputs().First() is CloneNeckInput gh5:
                gh5NeckInputType ??= gh5.Input;
                if (this is OutputAxis) gh5NeckInputType = Gh5NeckInputType.TapBar;
                input = new CloneNeckInput(gh5NeckInputType.Value, Model, gh5.Peripheral, gh5.Sda, gh5.Scl);
                break;
            case InputType.WtNeckInput when Input.InnermostInputs().First() is not GhWtTapInput:
                ghWtInputType ??= GhWtInputType.TapGreen;
                if (this is OutputAxis) ghWtInputType = GhWtInputType.TapBar;
                input = new GhWtTapInput(ghWtInputType.Value, Model, false, -1, -1, -1, -1);
                break;
            case InputType.WtNeckInput when Input.InnermostInputs().First() is GhWtTapInput wt:
                ghWtInputType ??= wt.Input;
                if (this is OutputAxis) ghWtInputType = GhWtInputType.TapBar;
                input = new GhWtTapInput(ghWtInputType.Value, Model, false, wt.Pin, wt.PinS0, wt.PinS1,
                    wt.PinS2);
                break;
            case InputType.WtNeckPeripheralInput when Input.InnermostInputs().First() is not GhWtTapInput:
                ghWtInputType ??= GhWtInputType.TapGreen;
                if (this is OutputAxis) ghWtInputType = GhWtInputType.TapBar;
                input = new GhWtTapInput(ghWtInputType.Value, Model, true, -1, -1, -1, -1);
                break;
            case InputType.WtNeckPeripheralInput when Input.InnermostInputs().First() is GhWtTapInput wt:
                ghWtInputType ??= wt.Input;
                if (this is OutputAxis) ghWtInputType = GhWtInputType.TapBar;
                input = new GhWtTapInput(ghWtInputType.Value, Model, true, wt.Pin, wt.PinS0, wt.PinS1,
                    wt.PinS2);
                break;
            case InputType.WiiInput when Input.InnermostInputs().First() is not WiiInput:
                wiiInput ??= WiiInputType.ClassicA;
                input = new WiiInput(wiiInput.Value, Model, false);
                break;
            case InputType.WiiInput when Input.InnermostInputs().First() is WiiInput wii:
                wiiInput ??= wii.Input;
                input = new WiiInput(wiiInput.Value, Model, wii.Peripheral, wii.Sda, wii.Scl);
                break;
            case InputType.Ps2Input when Input.InnermostInputs().First() is not Ps2Input:
                ps2InputType ??= Ps2InputType.Cross;
                input = new Ps2Input(ps2InputType.Value, Model, false);
                break;
            case InputType.Ps2Input when Input.InnermostInputs().First() is Ps2Input ps2:
                ps2InputType ??= ps2.Input;
                input = new Ps2Input(ps2InputType.Value, Model, ps2.Peripheral, ps2.Miso, ps2.Mosi, ps2.Sck,
                    ps2.Att,
                    ps2.Ack);
                break;
            default:
                return;
        }

        switch (input.IsAnalog)
        {
            case true when this is OutputAxis:
            case false when this is OutputButton:
                Input = input;
                break;
            case true when this is OutputButton:
                var oldType = input.IsUint ? AnalogToDigitalType.Trigger : AnalogToDigitalType.JoyHigh;
                var oldThreshold = input.IsUint ? ushort.MaxValue / 2 : short.MaxValue / 2;
                if (Input is AnalogToDigital atd)
                {
                    oldThreshold = atd.Threshold;
                    oldType = atd.AnalogToDigitalType;
                }

                Input = new AnalogToDigital(input, oldType, oldThreshold, Model);
                break;
            case false when this is GuitarAxis {Type: GuitarAxisType.Tilt}:
                Input = new DigitalToAnalog(input, 32767, Model, DigitalToAnalogType.Tilt);
                break;
            case false when this is OutputAxis axis:
                int oldOn = axis.Trigger ? ushort.MaxValue : short.MaxValue;
                if (Input is DigitalToAnalog dta)
                {
                    oldOn = dta.On;
                }


                Input = axis switch
                {
                    GuitarAxis {Type: GuitarAxisType.Pickup} => new DigitalToAnalog(input, oldOn, Model,
                        DigitalToAnalogType.Pickup),
                    GuitarAxis {Type: GuitarAxisType.Slider} => new DigitalToAnalog(input, oldOn, Model,
                        DigitalToAnalogType.TapBar),
                    {Trigger: true} => new DigitalToAnalog(input, oldOn, Model, DigitalToAnalogType.Trigger),
                    _ => new DigitalToAnalog(input, oldOn, Model, DigitalToAnalogType.Normal)
                };

                break;
        }

        if (this is EmulationMode) Input = input;

        if (input.InnermostInputs().First() is not DirectInput && this is OutputAxis axis2)
        {
            // Reset min and max to be safe
            if (input.IsUint)
            {
                axis2.Min = ushort.MinValue;
                axis2.Max = ushort.MaxValue;
            }
            else
            {
                axis2.Min = short.MinValue;
                axis2.Max = short.MaxValue;
            }
        }

        this.RaisePropertyChanged(nameof(WiiInputType));
        this.RaisePropertyChanged(nameof(Ps2InputType));
        this.RaisePropertyChanged(nameof(GhWtInputType));
        this.RaisePropertyChanged(nameof(Gh5NeckInputType));
        this.RaisePropertyChanged(nameof(DjInputType));
        this.RaisePropertyChanged(nameof(MidiType));
        Model.UpdateErrors();
    }


    public abstract SerializedOutput Serialize();

    public abstract string Generate(ConfigField mode, int debounceIndex, int ledIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer);

    public abstract string GenerateOutput(ConfigField mode);

    public virtual IEnumerable<Output> ValidOutputs()
    {
        var (extra, _) = ControllerEnumConverter.FilterValidOutputs(Model.DeviceControllerType, Outputs.Items);
        return Outputs.Items.Except(extra).Where(output => output.Enabled);
    }

    [RelayCommand]
    private void Remove()
    {
        Model.RemoveOutput(this);
    }

    protected virtual IEnumerable<PinConfig> GetOwnPinConfigs()
    {
        return OutputPinConfig != null ? new[] {OutputPinConfig} : Enumerable.Empty<PinConfig>();
    }

    protected virtual IEnumerable<DevicePin> GetOwnPins()
    {
        return new List<DevicePin>
        {
            new(OutputPin, DevicePinMode.Output)
        };
    }

    public IEnumerable<PinConfig> GetPinConfigs()
    {
        return Outputs
            .Items.SelectMany(s => s.Outputs.Items).SelectMany(s => s.Input.Inputs()).SelectMany(s => s.PinConfigs)
            .Concat(GetOwnPinConfigs()).Distinct();
    }

    public virtual void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw, ReadOnlySpan<byte> djRightRaw,
        ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw, ReadOnlySpan<byte> ps2ControllerType,
        ReadOnlySpan<byte> wiiControllerType, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> bluetoothRaw,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral,
        ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw, ReadOnlySpan<byte> mpr121Raw,
        ReadOnlySpan<byte> midiRaw)
    {
        _midiNotes = midiRaw.ToArray();
        if (Enabled)
            Input.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw,
                ghWtRaw,
                ps2ControllerType, wiiControllerType, usbHostInputsRaw, usbHostRaw, peripheralWtRaw, digitalPeripheral,
                cloneRaw, adxlRaw, mpr121Raw, midiRaw);

        foreach (var output in AllOutputs)
            if (output != this)
                output.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw,
                    ghWtRaw,
                    ps2ControllerType, wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw,
                    digitalPeripheral, cloneRaw, adxlRaw, mpr121Raw, midiRaw);
    }

    public void UpdateErrors()
    {
        this.RaisePropertyChanged(nameof(ErrorText));
    }

    public abstract void UpdateBindings();
}