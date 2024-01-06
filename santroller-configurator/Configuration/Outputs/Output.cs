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
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class LedIndex : ReactiveObject
{
    public LedIndex(Output output, bool peripheral, byte i)
    {
        Output = output;
        Index = i;
        Peripheral = peripheral;
        _selected = Collection.Contains(Index);
    }

    private bool _selected;

    public Output Output { get; }
    public byte Index { get; }

    private bool Peripheral { get; }

    private ObservableCollection<byte> Collection => Peripheral ? Output.LedIndicesPeripheral : Output.LedIndices;

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

    public ReactiveCommand<Unit, Unit> MoveUp { get; }
    public ReactiveCommand<Unit, Unit> MoveDown { get; }


    protected Output(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte[] ledIndicesPeripheral,
        bool childOfCombined)
    {
        ChildOfCombined = childOfCombined;
        ButtonText = Resources.Assign;
        Model = model;
        Input = input;
        LedIndices = new ObservableCollection<byte>(ledIndices);
        LedIndicesPeripheral = new ObservableCollection<byte>(ledIndicesPeripheral);
        LedOn = ledOn;
        LedOff = ledOff;
        MoveUp = ReactiveCommand.Create(() => Model.MoveUp(this),
            Model.Bindings.Connect().Select(_ => Model.Bindings.Items.IndexOf(this) != 0));
        MoveDown = ReactiveCommand.Create(() => Model.MoveDown(this),
            Model.Bindings.Connect().Select(_ => Model.Bindings.Items.IndexOf(this) != Model.Bindings.Count - 1));
        AvailableIndices = Array.Empty<LedIndex>();
        AvailableIndicesPeripheral = Array.Empty<LedIndex>();
        this.WhenAnyValue(x => x.Model.LedCount)
            .Select(x => Enumerable.Range(1, x).Select(s => new LedIndex(this, false, (byte) s)).ToArray())
            .ToPropertyEx(this, x => x.AvailableIndices);
        this.WhenAnyValue(x => x.Model.LedCountPeripheral)
            .Select(x => Enumerable.Range(1, x).Select(s => new LedIndex(this, true, (byte) s)).ToArray())
            .ToPropertyEx(this, x => x.AvailableIndicesPeripheral);
        this.WhenAnyValue(x => x.Input).Select(x => x.InnermostInputs() is DjInput)
            .ToPropertyEx(this, x => x.IsDj);
        this.WhenAnyValue(x => x.Input).Select(x => x.InnermostInputs() is UsbHostInput)
            .ToPropertyEx(this, x => x.IsUsb);
        this.WhenAnyValue(x => x.Input).Select(x => x.InnermostInputs() is WiiInput)
            .ToPropertyEx(this, x => x.IsWii);
        this.WhenAnyValue(x => x.Input)
            .Select(x => x.InnermostInputs() is Gh5NeckInput or CloneNeckInput && this is not GuitarAxis)
            .ToPropertyEx(this, x => x.IsGh5OrClone);
        this.WhenAnyValue(x => x.Input).Select(x => x.InnermostInputs() is Ps2Input)
            .ToPropertyEx(this, x => x.IsPs2);
        this.WhenAnyValue(x => x.Input)
            .Select(x => x.InnermostInputs() is GhWtTapInput && this is not GuitarAxis)
            .ToPropertyEx(this, x => x.IsWt);
        this.WhenAnyValue(x => x.Input.Title, x => x.Model.DeviceControllerType, x => x.ShouldUpdateDetails,
                x => x.Model.LegendType, x => x.Model.SwapSwitchFaceButtons)
            .Select(x => $"{x.Item1} ({GetName(x.Item2, x.Item4, x.Item5)})")
            .ToPropertyEx(this, x => x.Title);
        this.WhenAnyValue(x => x.Model.LedType).Select(x => x is not LedType.None)
            .ToPropertyEx(this, x => x.AreLedsEnabled);
        this.WhenAnyValue(x => x.Model.LedTypePeripheral).Select(x => x is not LedType.None)
            .ToPropertyEx(this, x => x.AreLedsEnabledPeripheral);
        this.WhenAnyValue(x => x.Model.LedType).Select(x => x is not (LedType.None or LedType.Stp16Cpc26))
            .ToPropertyEx(this, x => x.LedsRequireColours);
        this.WhenAnyValue(x => x.Model.LedTypePeripheral).Select(x => x is not (LedType.None or LedType.Stp16Cpc26))
            .ToPropertyEx(this, x => x.LedsRequireColoursPeripheral);
        this.WhenAnyValue(x => x.Model.DeviceControllerType, x => x.ShouldUpdateDetails, x => x.Model.LegendType,
                x => x.Model.SwapSwitchFaceButtons)
            .Select(x => GetName(x.Item1, x.Item3, x.Item4))
            .ToPropertyEx(this, x => x.LocalisedName);
        this.WhenAnyValue(x => x.Input.RawValue, x => x.Enabled).Select(x => x.Item2 ? x.Item1 : 0)
            .ToPropertyEx(this, x => x.ValueRaw);
        this.WhenAnyValue(x => x.ValueRaw, x => x.Input, x => x.IsCombined)
            .Select(GetOpacity)
            .ToPropertyEx(this, s => s.ImageOpacity);
        this.WhenAnyValue(x => x.Enabled)
            .Select(s => s ? 1 : 0.5)
            .ToPropertyEx(this, s => s.CombinedOpacity);
        this.WhenAnyValue(x => x.Model.DeviceControllerType, x => x.ShouldUpdateDetails,
                x => x.ChildOfCombined)
            .Select(x => x.Item3 ? GetChildOutputType() : GetOutputType())
            .ToPropertyEx(this, x => x.OutputType);
        this.WhenAnyValue(x => x.Enabled)
            .Select(enabled => enabled ? Brush.Parse("#99000000") : Brush.Parse("#33000000"))
            .ToPropertyEx(this, s => s.CombinedBackground);
        this.WhenAnyValue(x => x.Model.HasPeripheral).Subscribe(s => this.RaisePropertyChanged(nameof(InputTypes)));
        Outputs = new SourceList<Output>();
        Outputs.Add(this);
        AnalogOutputs = new ReadOnlyObservableCollection<Output>(new ObservableCollection<Output>());
        DigitalOutputs = new ReadOnlyObservableCollection<Output>(new ObservableCollection<Output>());
        Outputs.Connect().Bind(out var allOutputs).Subscribe();
        AllOutputs = allOutputs;
        _configured = true;
        IsVisible = !Model.Branded || LedIndices.Any() || this is Led || this is BluetoothOutput ||
                    this is CombinedOutput || this is OutputAxis {Input: not DigitalToAnalog};
    }

    private double GetOpacity((int, Input, bool) s)
    {
        var check = s.Item1 != 0 || s.Item3 || s.Item2.IsAnalog;

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

    [Reactive] public Input Input { get; set; }


    [Reactive] public bool Enabled { get; set; } = true;

    [Reactive] public bool Expanded { get; set; }

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
                    santroller.SetLed((byte) (ledIndex - 1), Model.LedType.GetLedBytes(value));
                }
            }

            if (Model.LedTypePeripheral != LedType.None)
            {
                foreach (var ledIndex in LedIndicesPeripheral)
                {
                    santroller.SetLedPeripheral((byte) (ledIndex - 1), Model.LedTypePeripheral.GetLedBytes(value));
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
                    santroller.SetLed((byte) (ledIndex - 1), Model.LedType.GetLedBytes(value));
                }
            }

            if (Model.LedTypePeripheral != LedType.None)
            {
                foreach (var ledIndex in LedIndicesPeripheral)
                {
                    santroller.SetLedPeripheral((byte) (ledIndex - 1), Model.LedTypePeripheral.GetLedBytes(value));
                }
            }
        }
    }


    public InputType? SelectedInputType
    {
        get => Input.InputType;
        set => SetInput(value, null, null, null, null, null, null);
    }

    public WiiInputType WiiInputType
    {
        get => (Input.InnermostInputs() as WiiInput)?.Input ?? WiiInputType.ClassicA;
        set => SetInput(SelectedInputType, value, null, null, null, null, null);
    }

    public Ps2InputType Ps2InputType
    {
        get => (Input.InnermostInputs() as Ps2Input)?.Input ?? Ps2InputType.Cross;
        set => SetInput(SelectedInputType, null, value, null, null, null, null);
    }

    public object KeyOrMouse
    {
        get => GetKey() ?? Key.Space;
        set => SetKey(value);
    }

    public DjInputType DjInputType
    {
        get => (Input.InnermostInputs() as DjInput)?.Input ?? DjInputType.LeftGreen;
        set => SetInput(SelectedInputType, null, null, null, null, value, null);
    }

    public UsbHostInputType UsbInputType
    {
        get => (Input.InnermostInputs() as UsbHostInput)?.Input ?? UsbHostInputType.A;
        set => SetInput(SelectedInputType, null, null, null, null, null, value);
    }

    public Gh5NeckInputType Gh5NeckInputType
    {
        get => (Input.InnermostInputs() as Gh5NeckInput)?.Input ??
               (Input.InnermostInputs() as CloneNeckInput)?.Input ?? Gh5NeckInputType.Green;
        set => SetInput(SelectedInputType, null, null, null, value, null, null);
    }

    public GhWtInputType GhWtInputType
    {
        get => (Input.InnermostInputs() as GhWtTapInput)?.Input ?? GhWtInputType.TapGreen;
        set => SetInput(SelectedInputType, null, null, value, null, null, null);
    }

    public IEnumerable<GhWtInputType> GhWtInputTypes =>
        Enum.GetValues<GhWtInputType>().Where(s => s is not GhWtInputType.TapAll);

    public IEnumerable<Gh5NeckInputType> Gh5NeckInputTypes =>
        Enum.GetValues<Gh5NeckInputType>().Where(s => s is not Gh5NeckInputType.TapAll);

    public IEnumerable<UsbHostInputType> UsbInputTypes => Enum.GetValues<UsbHostInputType>();

    public IEnumerable<object> KeyOrMouseInputs => Enum.GetValues<MouseButtonType>().Cast<object>()
        .Concat(Enum.GetValues<MouseAxisType>().Cast<object>()).Concat(KeyboardButton.Keys.Cast<object>());

    public IEnumerable<Ps2InputType> Ps2InputTypes => Enum.GetValues<Ps2InputType>();

    public IEnumerable<WiiInputType> WiiInputTypes =>
        Enum.GetValues<WiiInputType>().OrderBy(s => EnumToStringConverter.Convert(s));

    public IEnumerable<DjInputType> DjInputTypes => Enum.GetValues<DjInputType>();

    public IEnumerable<InputType> InputTypes =>
        Enum.GetValues<InputType>().Where(s =>
            (this is not GuitarAxis {Type: GuitarAxisType.Slider} ||
             s is InputType.Gh5NeckInput or InputType.WtNeckInput or InputType.ConstantInput
                 or InputType.WtNeckPeripheralInput or InputType.CloneNeckInput) &&
            (s is not InputType.WtNeckPeripheralInput || Model.HasPeripheral) &&
            (s is not InputType.MultiplexerInput || Model.IsPico) &&
            (s is not InputType.DigitalPeripheralInput || Model.HasPeripheral) &&
            s is not InputType.BluetoothInput &&
            (s is not InputType.UsbHostInput || Model.IsPico));

    private Enum GetChildOutputType()
    {
        if (Input.InnermostInputs() is WiiInput wii) return wii.Input;

        if (Input.InnermostInputs() is Ps2Input ps2) return ps2.Input;

        if (Input.InnermostInputs() is DjInput dj) return dj.Input;

        if (Input.InnermostInputs() is Gh5NeckInput gh5) return gh5.Input;

        if (Input.InnermostInputs() is CloneNeckInput c) return c.Input;

        if (Input.InnermostInputs() is GhWtTapInput wt) return wt.Input;

        if (Input.InnermostInputs() is UsbHostInput usb) return usb.Input;

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

        if (Model.LedTypePeripheral == LedType.Stp16Cpc26)
        {
            foreach (var ledIndex in LedIndicesPeripheral)
            {
                santroller.SetLedStpPeripheral((byte) (ledIndex - 1), true);
            }
        }
    }

    // ReSharper disable UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] public Enum OutputType { get; } = null!;
    [ObservableAsProperty] public string LocalisedName { get; } = "";
    [ObservableAsProperty] public bool IsDj { get; }
    [ObservableAsProperty] public bool IsWii { get; }
    [ObservableAsProperty] public bool IsUsb { get; }
    [ObservableAsProperty] public bool IsPs2 { get; }
    [ObservableAsProperty] public bool IsGh5OrClone { get; }
    [ObservableAsProperty] public bool IsWt { get; }
    [ObservableAsProperty] public bool AreLedsEnabled { get; }
    [ObservableAsProperty] public bool AreLedsEnabledPeripheral { get; }
    [ObservableAsProperty] public bool LedsRequireColours { get; }
    [ObservableAsProperty] public bool LedsRequireColoursPeripheral { get; }
    [ObservableAsProperty] public LedIndex[] AvailableIndices { get; }
    [ObservableAsProperty] public LedIndex[] AvailableIndicesPeripheral { get; }

    [ObservableAsProperty] public double CombinedOpacity { get; }
    [ObservableAsProperty] public IBrush CombinedBackground { get; } = Brush.Parse("#99000000");

    [ObservableAsProperty] public double ImageOpacity { get; }

    [ObservableAsProperty] public int ValueRaw { get; }
    [ObservableAsProperty] public string Title { get; } = "";

    // ReSharper enable UnassignedGetOnlyAutoProperty

    public abstract bool IsCombined { get; }
    public ObservableCollection<byte> LedIndices { get; set; }
    public ObservableCollection<byte> LedIndicesPeripheral { get; set; }
    public string Id => _id.ToString();


    public abstract bool IsStrum { get; }

    public SourceList<Output> Outputs { get; }

    public ReadOnlyObservableCollection<Output> AnalogOutputs { get; protected set; }
    public ReadOnlyObservableCollection<Output> DigitalOutputs { get; protected set; }
    private ReadOnlyObservableCollection<Output> AllOutputs { get; set; }

    public abstract bool IsKeyboard { get; }
    public bool IsLed => this is Led;

    public bool ChildOfCombined { get; }

    public bool ShouldShowEnabled => ChildOfCombined && !Model.Branded;
    public bool IsEmpty => this is EmptyOutput;

    [Reactive] public string ButtonText { get; set; }

    public virtual string ErrorText
    {
        get
        {
            var text = string.Join(", ",
                GetPinConfigs().Select(s => s.ErrorText).Distinct().Where(s => !string.IsNullOrEmpty(s)));
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
        switch (this)
        {
            case KeyboardButton button:
                return button.Key;
            case MouseAxis axis:
                return axis.Type;
            case MouseButton button:
                return button.Type;
        }

        return null;
    }

    private void SetKey(object value)
    {
        var current = GetKey();
        if (current == null) return;
        if (current.GetType() == value.GetType() && (int) current == (int) value) return;

        byte debounce = 1;
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
                LedIndicesPeripheral.ToArray(), debounce, key),
            MouseButtonType mouseButtonType => new MouseButton(Model, Input, LedOn, LedOff, LedIndices.ToArray(),
                LedIndicesPeripheral.ToArray(),
                debounce, mouseButtonType),
            MouseAxisType axisType => new MouseAxis(Model, Input, LedOn, LedOff, LedIndices.ToArray(),
                LedIndicesPeripheral.ToArray(), min, max,
                deadzone, axisType),
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
        UsbHostInputType? usbInputType)
    {
        Input input;
        switch (inputType)
        {
            case InputType.ConstantInput when Input.InnermostInputs().First() is not FixedInput && this is OutputAxis axis:
                input = new ConstantInput(Model, 0, true, axis.Min, axis.Max,
                    axis is GuitarAxis {Type: GuitarAxisType.Slider}, axis is GuitarAxis {Type: GuitarAxisType.Pickup});
                break;
            case InputType.ConstantInput when Input.InnermostInputs().First() is not FixedInput:
                input = new ConstantInput(Model, 0, false, 0, 1, false, false);
                break;
            case InputType.ConstantInput:
                input = Input.InnermostInputs().First();
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
            case InputType.MacroInput:
                input = new MacroInput(new DirectInput(-1, false, false, DevicePinMode.PullUp, Model),
                    new DirectInput(-1, false, false, DevicePinMode.PullUp, Model), Model);
                break;
            case InputType.DigitalPeripheralInput:
                input = new DirectInput(-1, false, true, DevicePinMode.PullUp, Model);
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

                if (axis.Trigger)
                {
                    Input = new DigitalToAnalog(input, oldOn, Model, DigitalToAnalogType.Trigger);
                }
                else
                    Input = axis switch
                    {
                        GuitarAxis {Type: GuitarAxisType.Pickup} => new DigitalToAnalog(input, oldOn, Model,
                            DigitalToAnalogType.Pickup),
                        GuitarAxis {Type: GuitarAxisType.Slider} => new DigitalToAnalog(input, oldOn, Model,
                            DigitalToAnalogType.TapBar),
                        _ => new DigitalToAnalog(input, oldOn, Model, DigitalToAnalogType.Normal)
                    };

                break;
        }

        if (this is EmulationMode) Input = input;


        if (input.InnermostInputs() is not DirectInput && this is OutputAxis axis2)
        {
            // Reset min and max to be safe
            if (Input.IsUint)
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
        Model.UpdateErrors();
    }


    public abstract SerializedOutput Serialize();

    public abstract string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer);

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
        return Enumerable.Empty<PinConfig>();
    }

    protected virtual IEnumerable<DevicePin> GetOwnPins()
    {
        return Enumerable.Empty<DevicePin>();
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
        ReadOnlySpan<byte> cloneRaw)
    {
        if (Enabled)
            Input.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw,
                ghWtRaw,
                ps2ControllerType, wiiControllerType, usbHostInputsRaw, usbHostRaw, peripheralWtRaw, digitalPeripheral,
                cloneRaw);

        foreach (var output in AllOutputs)
            if (output != this)
                output.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw,
                    ghWtRaw,
                    ps2ControllerType, wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw,
                    digitalPeripheral, cloneRaw);
    }

    public void UpdateErrors()
    {
        this.RaisePropertyChanged(nameof(ErrorText));
    }

    public abstract void UpdateBindings();
}