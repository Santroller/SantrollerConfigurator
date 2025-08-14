using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Exceptions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public partial class EmptyOutput : Output
{
    private readonly ObservableAsPropertyHelper<IEnumerable<object>> _combinedTypes;

    private readonly ObservableAsPropertyHelper<bool> _isKeyboard;

    private Key? _key;

    private MouseAxisType? _mouseAxisType;

    private MouseButtonType? _mouseButtonType;

    public EmptyOutput(ConfigViewModel model) : base(model, true, new FixedInput(model, 0, false), Colors.Black,
        Colors.Black,
        [], [], [], false, false, false, -1, false)
    {
        _isControllerHelper = this.WhenAnyValue(x => x.Model.DeviceControllerType,
                x => x is not DeviceControllerType.KeyboardMouse)
            .ToProperty(this, x => x.IsController);
        _isKeyboard = this.WhenAnyValue(x => x.Model.DeviceControllerType, x => x is DeviceControllerType.KeyboardMouse)
            .ToProperty(this, x => x.IsKeyboard);
        _combinedTypes = this.WhenAnyValue(vm => vm.Model.DeviceControllerType, vm => vm.Model.HasPeripheral,
                vm => vm.Model.IsBluetoothTx, vm => vm.Model.HasWiiCombinedOutput, vm => vm.Model.HasPs2CombinedOutput,
                vm => vm.Model.HasGhwtCombinedOutput, vm => vm.Model.HasCloneCombinedOutput,
                vm => vm.Model.HasDjCombinedOutput, vm => vm.Model.HasGh5CombinedOutput, 
                vm => vm.Model.HasUsbHostCombinedOutput, vm => vm.Model.HasBhDrumInput, vm => vm.Model.HasWtDrumInput,
                (type, hasPeripheral, isBluetooth, hasWii, hasPs2, hasGhwt, hasClone, hasDj, hasGh5, hasUsbHost, hasBhDrum, hasWtDrum) =>
                    ControllerEnumConverter.GetTypes(type).Where(s2 =>
                        (type.IsProGuitar() || s2 is not SimpleType.ProGuitar) &&
                        (type.IsDrum() || type.IsGuitar() ||
                         s2 is not (SimpleType.FestivalGamepad or SimpleType.FestivalIos or SimpleType.FestivalKeyboard
                             or SimpleType.FestivalLayer)) &&
                        (model.IsPico ||
                         s2 is not (SimpleType.WtNeckSimple or SimpleType.Bluetooth or SimpleType.BluetoothTx
                             or SimpleType.UsbHost
                             or SimpleType.Peripheral)) &&
                        (hasPeripheral || s2 is not SimpleType.WtNeckPeripheralSimple) &&
                        (!isBluetooth || s2 is not SimpleType.Bluetooth) &&
                        (isBluetooth || s2 is not SimpleType.Max1704X) &&
                        (!hasWii || s2 is not SimpleType.WiiInputSimple) &&
                        (!hasPs2 || s2 is not SimpleType.Ps2InputSimple) &&
                        (!hasGhwt || s2 is not SimpleType.WtNeckSimple) &&
                        (!hasClone || s2 is not SimpleType.CloneNeckSimple) &&
                        (!hasDj || s2 is not SimpleType.DjTurntableSimple) &&
                        (!hasGh5 || s2 is not SimpleType.Gh5NeckSimple) &&
                        (!hasUsbHost || s2 is not SimpleType.UsbHost) &&
                        (!hasBhDrum || s2 is not SimpleType.BhDrumInput) &&
                        (!hasWtDrum || s2 is not SimpleType.WtDrumInput)
                    ))
            .ToProperty(this, x => x.CombinedTypes);
    }

    [ObservableAsProperty] private bool _isController;
    public override bool IsKeyboard => _isKeyboard.Value;

    public IEnumerable<object> CombinedTypes => _combinedTypes.Value;

    public object? CombinedType
    {
        get => null;
        set => Generate(value);
    }

    public Key? Key
    {
        get => _key;
        set
        {
            this.RaiseAndSetIfChanged(ref _key, value);
            this.RaiseAndSetIfChanged(ref _mouseAxisType, null, nameof(MouseAxisType));
            this.RaiseAndSetIfChanged(ref _mouseButtonType, null, nameof(MouseButtonType));
        }
    }

    public IEnumerable<Key> Keys => Enum.GetValues<Key>();

    public MouseAxisType? MouseAxisType
    {
        get => _mouseAxisType;
        set
        {
            this.RaiseAndSetIfChanged(ref _mouseAxisType, value);
            this.RaiseAndSetIfChanged(ref _mouseButtonType, null, nameof(MouseButtonType));
            this.RaiseAndSetIfChanged(ref _key, null, nameof(Key));
        }
    }


    public MouseButtonType? MouseButtonType
    {
        get => _mouseButtonType;
        set
        {
            this.RaiseAndSetIfChanged(ref _mouseButtonType, value);
            this.RaiseAndSetIfChanged(ref _mouseAxisType, null, nameof(MouseAxisType));
            this.RaiseAndSetIfChanged(ref _key, null, nameof(Key));
        }
    }

    public override string ErrorText => Resources.ErrorInputUnbound;

    public override string LedOnLabel => "";
    public override string LedOffLabel => "";

    public override bool IsCombined => false;
    public override bool IsStrum => false;

    private void Generate(object? value)
    {
        switch (value)
        {
            case SimpleType.BluetoothTx:
                Model.IsBluetoothTx = true;
                Model.Bindings.Remove(this);
                Model.UpdateErrors();
                return;
            case SimpleType.Accel:
                Model.HasAccel = true;
                Model.Bindings.Remove(this);
                Model.UpdateErrors();
                return;
            case SimpleType.WiiOutputs:
                Model.HasWiiOutput = true;
                Model.Bindings.Remove(this);
                Model.UpdateErrors();
                return;
            case SimpleType.Ps2Outputs:
                Model.HasPs2Output = true;
                Model.Bindings.Remove(this);
                Model.UpdateErrors();
                return;
            case SimpleType.BhDrumInput:
                Model.HasBhDrumInput = true;
                Model.Bindings.Remove(this);
                Model.UpdateErrors();
                return;
            case SimpleType.WtDrumInput:
                Model.HasWtDrumInput = true;
                Model.Bindings.Remove(this);
                Model.UpdateErrors();
                return;
            case SimpleType.Peripheral:
                Model.HasPeripheral = true;
                Model.Bindings.Remove(this);
                Model.UpdateErrors();
                return;
            case SimpleType.Max1704X:
                Model.HasMax1704X = true;
                Model.Bindings.Remove(this);
                Model.UpdateErrors();
                return;
            case SimpleType.Mpr121:
                Model.HasMpr121 = true;
                Model.Bindings.Remove(this);
                Model.UpdateErrors();
                return;
            case SimpleType.FestivalKeyboard:
                Model.Bindings.Remove(this);
                Model.Bindings.Add(new EmulationMode(Model, true,
                    new DirectInput(-1, false, false, DevicePinMode.PullUp, Model), EmulationModeType.Fnf));
                Model.UpdateErrors();
                return;
            case SimpleType.FestivalGamepad:
                Model.Bindings.Remove(this);
                Model.Bindings.Add(new EmulationMode(Model, true,
                    new DirectInput(-1, false, false, DevicePinMode.PullUp, Model), EmulationModeType.FnfHid));
                Model.UpdateErrors();
                return;
            case SimpleType.FestivalLayer:
                Model.Bindings.Remove(this);
                Model.Bindings.Add(new EmulationMode(Model, true,
                    new DirectInput(-1, false, false, DevicePinMode.PullUp, Model), EmulationModeType.FnfLayer));
                Model.UpdateErrors();
                return;
            case SimpleType.FestivalIos:
                Model.Bindings.Remove(this);
                Model.Bindings.Add(new EmulationMode(Model, true,
                    new DirectInput(-1, false, false, DevicePinMode.PullUp, Model), EmulationModeType.FnfIos));
                Model.UpdateErrors();
                return;
        }

        Output? output = Model.DeviceControllerType switch
        {
            DeviceControllerType.KeyboardMouse => this switch
            {
                {MouseAxisType: not null} => new MouseAxis(Model, true, new FixedInput(Model, 0, false), Colors.Black,
                    Colors.Black,
                    [], [], [], short.MinValue, short.MaxValue, 0,
                    MouseAxisType.Value, false, false, false, -1),
                {MouseButtonType: not null} => new MouseButton(Model, true, new FixedInput(Model, 0, false),
                    Colors.Black,
                    Colors.Black,
                    [], [], [], 5,
                    MouseButtonType.Value, false, false, false, -1),
                {Key: not null} => new KeyboardButton(Model, true, new FixedInput(Model, 0, false), Colors.Black,
                    Colors.Black,
                    [], [], [], 5,
                    Key.Value, false, false, false, -1),
                _ => null
            },
            _ => value switch
            {
                SimpleType simpleType => simpleType switch
                {
                    SimpleType.WiiInputSimple => new WiiCombinedOutput(Model, false),
                    SimpleType.Gh5NeckSimple => new Gh5CombinedOutput(Model, false),
                    SimpleType.CrkdNeckSimple => new CrkdCombinedOutput(Model, false),
                    SimpleType.CloneNeckSimple => new CloneCombinedOutput(Model, false),
                    SimpleType.Ps2InputSimple => new Ps2CombinedOutput(Model, false),
                    SimpleType.WtNeckSimple => new GhwtCombinedOutput(Model, false),
                    SimpleType.WtNeckPeripheralSimple => new GhwtCombinedOutput(Model, true),
                    SimpleType.DjTurntableSimple => new DjCombinedOutput(Model, false),
                    SimpleType.Led => new Led(Model, true, !Model.IsApa102, false, -1,
                        false, Colors.Black, Colors.Black,
                        [], [], [],
                        Enum.GetValues<LedCommandType>().Where(Led.FilterLeds((Model.DeviceControllerType,
                            Model.IsApa102, Model.IsBluetooth))).First(), 0, 0),
                    SimpleType.Rumble => new Rumble(Model, true, -1,
                        false, RumbleMotorType.Left),
                    SimpleType.ConsoleMode => new EmulationMode(Model, true,
                        new DirectInput(-1, false, false, DevicePinMode.PullUp, Model),
                        EmulationModeType.XboxOne),
                    SimpleType.Reset => new Reset(Model, true,
                        new DirectInput(-1, false, false, DevicePinMode.PullUp, Model)),
                    SimpleType.Wakeup360 => new Wakeup360(Model, true,
                        new DirectInput(-1, false, false, DevicePinMode.PullUp, Model)),
                    SimpleType.UsbHost => new UsbHostCombinedOutput(Model),
                    SimpleType.Bluetooth => new Combined.BluetoothCombinedOutput(Model),
                    SimpleType.Midi => new MidiCombinedOutput(Model, 24),
                    SimpleType.ProGuitar => new ProGuitarCombinedOutput(Model),
                    _ => null
                },
                StandardAxisType.RightTrigger or StandardAxisType.LeftTrigger => new ControllerAxis(Model, true,
                    new DirectInput(-1, false, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, [], [], [],
                    ushort.MinValue, ushort.MaxValue, ushort.MaxValue / 2, 0,
                    0, (StandardAxisType) value, false, false, false, -1, false),
                StandardAxisType standardAxisType => new ControllerAxis(Model, true,
                    new DirectInput(-1, false, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, [], [], [],
                    ushort.MinValue, ushort.MaxValue, ushort.MaxValue / 2, 0,
                    ushort.MaxValue, standardAxisType, false, false, false, -1, false),
                StandardButtonType standardButtonType => new ControllerButton(Model, true,
                    new DirectInput(-1, false, false, DevicePinMode.PullUp, Model),
                    Colors.Black,
                    Colors.Black, [], [], [], 5,
                    standardButtonType, false, false, false, -1, false),
                InstrumentButtonType standardButtonType => new GuitarButton(Model, true,
                    new DirectInput(-1, false, false, DevicePinMode.PullUp, Model),
                    Colors.Black,
                    Colors.Black, [], [], [], 5,
                    standardButtonType, false, false, false, -1, false),
                DrumAxisType drumAxisType => new DrumAxis(Model, true,
                    new DirectInput(-1, false, false, DevicePinMode.Analog, Model),null,
                    Colors.Black, Colors.Black, [], [], [],
                    ushort.MaxValue / 2, ushort.MaxValue, 0, 10, drumAxisType, false, false, false, -1, false),
                Ps3AxisType ps3AxisType => new Ps3Axis(Model, true,
                    new DirectInput(-1, false, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, [], [], [],
                    ushort.MinValue, ushort.MaxValue, 0,
                    ps3AxisType, false, false, false, -1),
                GuitarAxisType.Slider => new GuitarAxis(Model, true,
                    new GhWtTapInput(GhWtInputType.TapBar, Model, false, -1,
                        -1, -1,
                        -1),
                    Colors.Black, Colors.Black, [], [], [],
                    ushort.MinValue, ushort.MaxValue, 0, false, GuitarAxisType.Slider, false, false, false, -1, false),
                GuitarAxisType.Pickup => new GuitarAxis(Model, true,
                    new DirectInput(-1, false, false, DevicePinMode.Analog, Model),
                    (ushort.MaxValue / 5) * 1, (ushort.MaxValue / 5) * 2, (ushort.MaxValue / 5) * 3,
                    (ushort.MaxValue / 5) * 4,
                    Colors.Black, Colors.Black, [], [], [],
                    ushort.MinValue, ushort.MaxValue, 0, false, GuitarAxisType.Pickup, false, false, false, -1, false),
                GuitarAxisType.Tilt => new GuitarAxis(Model, true,
                    new DirectInput(-1, false, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, [], [], [],
                    ushort.MinValue, ushort.MaxValue, 0, false, GuitarAxisType.Tilt, false, false, false, -1, false),
                GuitarAxisType guitarAxisType => new GuitarAxis(Model, true,
                    new DirectInput(-1, false, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, [], [], [],
                    ushort.MinValue, ushort.MaxValue, 0, false, guitarAxisType, false, false, false, -1, false),
                DjAxisType.LeftTableVelocity => new DjAxis(Model, true,
                    new DirectInput(-1, false, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, [], [], [],
                    1, 1, DjAxisType.LeftTableVelocity, false, false, false, -1, false),
                DjAxisType.RightTableVelocity => new DjAxis(Model, true,
                    new DirectInput(-1, false, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, [], [], [],
                    1, 1, DjAxisType.RightTableVelocity, false, false, false, -1, false),
                DjAxisType.EffectsKnob => new DjAxis(Model, true,
                    new DirectInput(-1, false, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, [], [], [],
                    1, 1, DjAxisType.EffectsKnob, false, false, false, -1, false),
                DjAxisType djAxisType => new DjAxis(Model, true,
                    new DirectInput(-1, false, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, [], [], [],
                    ushort.MinValue, ushort.MaxValue, 0, djAxisType, false, false, false, -1, false),
                DjInputType djInputType => new DjButton(Model, true,
                    new DirectInput(-1, false, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, [], [], [], 10,
                    djInputType, false, false, false, -1, false),
                ProKeyType proKeyType and (ProKeyType.Overdrive or ProKeyType.PedalDigital) => new PianoKeyButton(Model,
                    true,
                    new DirectInput(-1, false, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, [], [], [],
                    proKeyType, false, false, false, -1, false),
                ProKeyType proKeyType => new PianoKey(Model, true,
                    new DirectInput(-1, false, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, [], [], [],
                    proKeyType, false, false, false, -1, false),
                _ => null
            }
        };
        if (output != null)
        {
            output.Expanded = true;
            if (output is Combined.BluetoothCombinedOutput)
            {
                Model.ResetBluetoothRelated();
            }

            Model.Bindings.Add(output);

            if (output is CombinedOutput combinedOutput)
            {
                combinedOutput.SetOutputsOrDefaults(Array.Empty<Output>());
            }
        }

        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            Model.Bindings.Remove(this);
            Model.UpdateErrors();
        });
    }

    public override void UpdateBindings()
    {
    }

    public override SerializedOutput Serialize()
    {
        throw new IncompleteConfigurationException(ErrorText);
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return "Unset Setting";
    }

    public override Enum GetOutputType()
    {
        return EmptyType.Empty;
    }


    public override string Generate(ConfigField mode, int debounceIndex, int ledIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        throw new IncompleteConfigurationException(ErrorText);
    }

    public override string GenerateOutput(ConfigField mode)
    {
        return "";
    }
}