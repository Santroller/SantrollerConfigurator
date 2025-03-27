using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public partial class Ps2CombinedOutput : CombinedSpiOutput
{
    public static readonly Dictionary<Ps2InputType, StandardButtonType> Buttons = new()
    {
        {Ps2InputType.MouseLeft, StandardButtonType.A},
        {Ps2InputType.MouseRight, StandardButtonType.B},
        {Ps2InputType.Cross, StandardButtonType.A},
        {Ps2InputType.Circle, StandardButtonType.B},
        {Ps2InputType.Square, StandardButtonType.X},
        {Ps2InputType.Triangle, StandardButtonType.Y},
        {Ps2InputType.L1, StandardButtonType.LeftShoulder},
        {Ps2InputType.R1, StandardButtonType.RightShoulder},
        {Ps2InputType.L3, StandardButtonType.LeftThumbClick},
        {Ps2InputType.R3, StandardButtonType.RightThumbClick},
        {Ps2InputType.Select, StandardButtonType.Back},
        {Ps2InputType.Start, StandardButtonType.Start},
        {Ps2InputType.DpadDown, StandardButtonType.DpadDown},
        {Ps2InputType.DpadUp, StandardButtonType.DpadUp},
        {Ps2InputType.DpadLeft, StandardButtonType.DpadLeft},
        {Ps2InputType.DpadRight, StandardButtonType.DpadRight},
        {Ps2InputType.GuitarGreen, StandardButtonType.A},
        {Ps2InputType.GuitarRed, StandardButtonType.B},
        {Ps2InputType.GuitarYellow, StandardButtonType.Y},
        {Ps2InputType.GuitarBlue, StandardButtonType.X},
        {Ps2InputType.GuitarOrange, StandardButtonType.LeftShoulder},
        {Ps2InputType.GuitarStrumDown, StandardButtonType.DpadDown},
        {Ps2InputType.GuitarStrumUp, StandardButtonType.DpadUp},
        {Ps2InputType.GuitarSelect, StandardButtonType.Back},
        {Ps2InputType.GuitarStart, StandardButtonType.Start},
        {Ps2InputType.NegConR, StandardButtonType.RightShoulder},
        {Ps2InputType.NegConA, StandardButtonType.B},
        {Ps2InputType.NegConB, StandardButtonType.Y},
        {Ps2InputType.NegConStart, StandardButtonType.Start}
    };


    private static readonly Dictionary<Ps2InputType, StandardButtonType> Tap = new()
    {
        {Ps2InputType.GuitarTapGreen, StandardButtonType.A},
        {Ps2InputType.GuitarTapRed, StandardButtonType.B},
        {Ps2InputType.GuitarTapYellow, StandardButtonType.Y},
        {Ps2InputType.GuitarTapBlue, StandardButtonType.X},
        {Ps2InputType.GuitarTapOrange, StandardButtonType.LeftShoulder}
    };

    private static readonly Dictionary<Ps2InputType, InstrumentButtonType> TapRb = new()
    {
        {Ps2InputType.GuitarTapGreen, InstrumentButtonType.SoloGreen},
        {Ps2InputType.GuitarTapRed, InstrumentButtonType.SoloRed},
        {Ps2InputType.GuitarTapYellow, InstrumentButtonType.SoloYellow},
        {Ps2InputType.GuitarTapBlue, InstrumentButtonType.SoloBlue},
        {Ps2InputType.GuitarTapOrange, InstrumentButtonType.SoloOrange}
    };


    public static readonly Dictionary<Ps2InputType, StandardAxisType> Axis = new()
    {
        {Ps2InputType.LeftStickX, StandardAxisType.LeftStickX},
        {Ps2InputType.LeftStickY, StandardAxisType.LeftStickY},
        {Ps2InputType.RightStickX, StandardAxisType.RightStickX},
        {Ps2InputType.RightStickY, StandardAxisType.RightStickY},
        {Ps2InputType.Dualshock2L2, StandardAxisType.LeftTrigger},
        {Ps2InputType.Dualshock2R2, StandardAxisType.RightTrigger},
        {Ps2InputType.GuitarWhammy, StandardAxisType.RightStickX},
        {Ps2InputType.NegConTwist, StandardAxisType.LeftStickX},
        {Ps2InputType.JogConWheel, StandardAxisType.LeftStickX},
        {Ps2InputType.MouseX, StandardAxisType.LeftStickX},
        {Ps2InputType.MouseY, StandardAxisType.LeftStickY},
        {Ps2InputType.GunConHSync, StandardAxisType.LeftStickX},
        {Ps2InputType.GunConVSync, StandardAxisType.LeftStickY},
        {Ps2InputType.NegConL, StandardAxisType.LeftTrigger}
    };

    public static readonly Dictionary<Ps2InputType, Ps3AxisType> Ps3Axis = new()
    {
        {Ps2InputType.Dualshock2UpButton, Ps3AxisType.PressureDpadUp},
        {Ps2InputType.Dualshock2RightButton, Ps3AxisType.PressureDpadRight},
        {Ps2InputType.Dualshock2LeftButton, Ps3AxisType.PressureDpadLeft},
        {Ps2InputType.Dualshock2DownButton, Ps3AxisType.PressureDpadDown},
        {Ps2InputType.Dualshock2L1, Ps3AxisType.PressureL1},
        {Ps2InputType.Dualshock2R1, Ps3AxisType.PressureR1},
        {Ps2InputType.Dualshock2Triangle, Ps3AxisType.PressureTriangle},
        {Ps2InputType.Dualshock2Circle, Ps3AxisType.PressureCircle},
        {Ps2InputType.Dualshock2Cross, Ps3AxisType.PressureCross},
        {Ps2InputType.Dualshock2Square, Ps3AxisType.PressureSquare}
    };

    private readonly DirectPinConfig _ackConfig;
    private readonly DirectPinConfig _attConfig;

    public Ps2CombinedOutput(ConfigViewModel model, bool peripheral, int miso = -1, int mosi = -1,
        int sck = -1, int att = -1, int ack = -1) : base(model, Ps2Input.Ps2SpiType,
        peripheral, Ps2Input.Ps2SpiFreq, Ps2Input.Ps2SpiCpol, Ps2Input.Ps2SpiCpha, Ps2Input.Ps2SpiMsbFirst, "PS2", miso,
        mosi, sck)
    {
        Outputs.Clear();
        _ackConfig = Model.GetPinForType(Ps2Input.Ps2AckType, peripheral, ack, DevicePinMode.Floating);
        _attConfig = Model.GetPinForType(Ps2Input.Ps2AttType, peripheral, att, DevicePinMode.Output);
        this.WhenAnyValue(x => x._attConfig.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Att)));
        this.WhenAnyValue(x => x._ackConfig.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Ack)));

        Outputs.Connect().Filter(x => x is OutputAxis)
            .Filter(s => s.IsVisible)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Length != 0)
            .Filter(this.WhenAnyValue(x => x.ControllerFound, x => x.DetectedType, x => x.SelectedType)
                .Select(CreateFilter))
            .Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton or JoystickToDpad or StartSelectHome)
            .Filter(s => s.IsVisible)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Length != 0)
            .Filter(this.WhenAnyValue(x => x.ControllerFound, x => x.DetectedType, x => x.SelectedType)
                .Select(CreateFilter))
            .Bind(out var digitalOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton or JoystickToDpad or StartSelectHome or {Input.IsAnalog: false})
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Length != 0)
            .Filter(this.WhenAnyValue(x => x.ControllerFound, x => x.DetectedType, x => x.SelectedType)
                .Select(CreateFilter))
            .Bind(out var allDigitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
        AllDigitalOutputs = allDigitalOutputs;
    }

    public int Ack
    {
        get => _ackConfig.Pin;
        set => _ackConfig.Pin = value;
    }

    public int Att
    {
        get => _attConfig.Pin;
        set => _attConfig.Pin = value;
    }

    public ReadOnlyObservableCollection<int> AvailablePinsInterrupt => Model.AvailablePinsInterrupt;

    [Reactive] private Ps2ControllerType _detectedType;
    [Reactive] private Ps2ControllerType _selectedType  = Ps2ControllerType.Selected;
    public IEnumerable<Ps2ControllerType> Ps2ControllerTypes => Enum.GetValues<Ps2ControllerType>();

    [Reactive] private bool _controllerFound;

    public override IEnumerable<Output> ValidOutputs()
    {
        var outputs = base.ValidOutputs().ToList();
        var joyToDpad = outputs.FirstOrDefault(s => s is JoystickToDpad);
        if (joyToDpad != null)
        {
            outputs.Remove(joyToDpad);
            outputs.Add(joyToDpad.ValidOutputs());
        }

        var startSelectHome = outputs.FirstOrDefault(s => s is StartSelectHome);
        if (startSelectHome != null)
        {
            outputs.Remove(startSelectHome);
            outputs.Add(startSelectHome.ValidOutputs());
        }

        var tapAnalog =
            Outputs.Items.FirstOrDefault(s => s is {Input: Ps2Input {Input: Ps2InputType.GuitarTapBar}});
        var tapFrets =
            Outputs.Items.FirstOrDefault(s => s is {Input: Ps2Input {Input: Ps2InputType.GuitarTapAll}});
        if (tapAnalog == null && tapFrets == null) return outputs;
        // Map Tap bar to Upper frets on RB guitars
        if (tapAnalog != null && Model.DeviceControllerType is DeviceControllerType.RockBandGuitar)
        {
            outputs.AddRange(TapRb.Select(pair => new GuitarButton(Model,tapAnalog.Enabled,
                new Ps2Input(pair.Key, Model, Peripheral, Miso, Mosi, Sck, Att, Ack, true),
                Colors.Black, Colors.Black, [], [], [], 5,
                pair.Value, false, false,
                false, -1, true)));

            outputs.Remove(tapAnalog);
        }

        if (tapFrets == null) return outputs;
        if (Model.DeviceControllerType.Is5FretGuitar())
        {
            outputs.AddRange(Tap.Select(pair => new ControllerButton(Model,tapFrets.Enabled,
                new Ps2Input(pair.Key, Model, Peripheral, Miso, Mosi, Sck, Att, Ack, true),
                Colors.Black, Colors.Black, [], [], [], 5,
                pair.Value, false, false,
                false, -1, true)));
        }

        outputs.Remove(tapFrets);

        return outputs;
    }

    public override void SetOutputsOrDefaults(IEnumerable<Output> outputs)
    {
        Outputs.Clear();
        Outputs.AddRange(outputs);
        if (Outputs.Count == 0)
        {
            CreateDefaults();
        }
    }

    private static Func<Output, bool> CreateFilter(
        (bool controllerFound, Ps2ControllerType detectedType, Ps2ControllerType selectedType) tuple)
    {
        if (tuple.selectedType == Ps2ControllerType.All)
        {
            return _ => true;
        }

        var controllerType = tuple.selectedType;
        if (controllerType == Ps2ControllerType.Selected)
        {
            controllerType = tuple.detectedType;
            if (!tuple.controllerFound)
            {
                return _ => true;
            }
        }

        return output => output is JoystickToDpad ||
                         (output.Input.InnermostInputs().First() is Ps2Input ps2Input &&
                          ps2Input.SupportsType(controllerType));
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return Resources.Ps2CombinedTitle;
    }

    public override Enum GetOutputType()
    {
        return SimpleType.Ps2InputSimple;
    }

    public void CreateDefaults()
    {
        Outputs.Clear();
        foreach (var pair in Buttons)
            Outputs.Add(new ControllerButton(Model,true,
                new Ps2Input(pair.Key, Model, Peripheral, Miso, Mosi, Sck, Att, Ack, true),
                Colors.Black,
                Colors.Black, [], [], [],
                10,
                pair.Value, false, false, false, -1, true));

        Outputs.Add(new ControllerButton(Model,true,
            new AnalogToDigital(
                new Ps2Input(Ps2InputType.NegConI, Model, Peripheral, Miso, Mosi, Sck, Att, Ack, true),
                AnalogToDigitalType.Trigger, 128, Model),
            Colors.Black, Colors.Black, [], [], [], 10,
            StandardButtonType.A, false,
            false, false, -1, true));
        Outputs.Add(new ControllerButton(Model,true,
            new AnalogToDigital(
                new Ps2Input(Ps2InputType.NegConIi, Model, Peripheral, Miso, Mosi, Sck, Att, Ack, true),
                AnalogToDigitalType.Trigger, 128, Model),
            Colors.Black, Colors.Black, [], [], [], 10,
            StandardButtonType.X, false,
            false, false, -1, true));
        Outputs.Add(new ControllerButton(Model,true,
            new AnalogToDigital(
                new Ps2Input(Ps2InputType.NegConL, Model, Peripheral, Miso, Mosi, Sck, Att, Ack, true),
                AnalogToDigitalType.Trigger, 240, Model),
            Colors.Black, Colors.Black, [], [], [], 10,
            StandardButtonType.LeftShoulder,
            false, false, false, -1, true));

        Outputs.Add(new ControllerAxis(Model,true,
            new DigitalToAnalog(new Ps2Input(Ps2InputType.L2, Model, Peripheral, Miso, Mosi, Sck, Att, Ack, true),
                ushort.MaxValue,
                Model, DigitalToAnalogType.Trigger),
            Colors.Black,
            Colors.Black, [], [], [], ushort.MinValue,
            ushort.MaxValue, ushort.MaxValue/2,0,
            ushort.MaxValue,
            StandardAxisType.LeftTrigger, false, false, false, -1,
            true));
        Outputs.Add(new ControllerAxis(Model,true,
            new DigitalToAnalog(new Ps2Input(Ps2InputType.R2, Model, Peripheral, Miso, Mosi, Sck, Att, Ack, true),
                ushort.MaxValue,
                Model, DigitalToAnalogType.Trigger),
            Colors.Black,
            Colors.Black, [], [], [], ushort.MinValue,
            ushort.MaxValue, ushort.MaxValue/2,0,
            ushort.MaxValue,
            StandardAxisType.RightTrigger, false, false, false, -1,
            true));
        foreach (var pair in Axis)
            if (pair.Value is StandardAxisType.LeftTrigger or StandardAxisType.RightTrigger)
                Outputs.Add(new ControllerAxis(Model,true,
                    new Ps2Input(pair.Key, Model, Peripheral, Miso, Mosi, Sck, Att, Ack, true),
                    Colors.Black,
                    Colors.Black, [], [], [], ushort.MinValue,
                    ushort.MaxValue, ushort.MaxValue/2,0,
                    50000, pair.Value, false, false, false, -1,
                    true));
            else if (pair.Key is Ps2InputType.GuitarWhammy)
                Outputs.Add(new ControllerAxis(Model,true,
                    new Ps2Input(pair.Key, Model, Peripheral, Miso, Mosi, Sck, Att, Ack, true),
                    Colors.Black,
                    Colors.Black, [], [], [], ushort.MinValue,
                    ushort.MaxValue, ushort.MaxValue/2,0,
                    ushort.MaxValue, pair.Value, false, false, false, -1,
                    true));
            else
                Outputs.Add(new ControllerAxis(Model,true,
                    new Ps2Input(pair.Key, Model, Peripheral, Miso, Mosi, Sck, Att, Ack, true),
                    Colors.Black,
                    Colors.Black, [], [], [], short.MinValue,
                    short.MaxValue, 0,0,
                    ushort.MaxValue, pair.Value, false, false, false, -1,
                    true));

        Outputs.Add(new JoystickToDpad(Model, true,Peripheral, short.MaxValue / 2, false) {Enabled = false});
        Outputs.Add(new StartSelectHome(Model, true,Peripheral, false) {Enabled = false});
        UpdateBindings();
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedPs2CombinedOutput(Peripheral, Miso, Mosi, Sck, Att, Ack, Outputs.Items.ToList());
    }


    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> bluetoothRaw, ReadOnlySpan<byte> usbHostInputsRaw,
        ReadOnlySpan<byte> peripheralWtRaw, Dictionary<int, bool> digitalPeripheral,
        ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw, ReadOnlySpan<byte> mpr121Raw,
        ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw, bool peripheralConnected)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType,
            wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw, digitalPeripheral, cloneRaw,
            adxlRaw, mpr121Raw, midiRaw, bluetoothInputsRaw, peripheralConnected);
        if (ps2ControllerType.IsEmpty)
        {
            ControllerFound = false;
            return;
        }

        var type = ps2ControllerType[0];
        if (!Enum.IsDefined(typeof(Ps2ControllerType), type) || (Ps2ControllerType) type == Ps2ControllerType.None)
        {
            ControllerFound = false;
            return;
        }

        ControllerFound = true;
        var newType = (Ps2ControllerType) type;
        DetectedType = newType;
    }

    public override void UpdateBindings()
    {
        if (Model.DeviceControllerType is not DeviceControllerType.Gamepad)
        {
            Outputs.RemoveMany(Outputs.Items.Where(s => s is OutputAxis));
        }
        else
        {
            if (!Outputs.Items.Any(s => s is ControllerAxis))
            {
                foreach (var pair in Axis)
                    if (pair.Value is StandardAxisType.LeftTrigger or StandardAxisType.RightTrigger ||
                        pair.Key is Ps2InputType.GuitarWhammy)
                        Outputs.Add(new ControllerAxis(Model,true,
                            new Ps2Input(pair.Key, Model, Peripheral, Miso, Mosi, Sck, Att, Ack, true),
                            Colors.Black,
                            Colors.Black, [], [], [],
                            ushort.MinValue, ushort.MaxValue, ushort.MaxValue/2,0,
                            ushort.MaxValue,
                            pair.Value, false, false, false, -1, true));
                    else
                        Outputs.Add(new ControllerAxis(Model,true,
                            new Ps2Input(pair.Key, Model, Peripheral, Miso, Mosi, Sck, Att, Ack, true),
                            Colors.Black,
                            Colors.Black, [], [], [], short.MinValue,
                            short.MaxValue, 0,0,
                            ushort.MaxValue,
                            pair.Value, false, false, false, -1, true));
            }
        }

        if (Model.DeviceControllerType.IsGuitar())
        {
            if (!Outputs.Items.Any(s => s is GuitarAxis {Type: GuitarAxisType.Whammy}))
            {
                Outputs.Add(new GuitarAxis(Model,true,
                    new Ps2Input(Ps2InputType.GuitarWhammy, Model, Peripheral, Miso, Mosi, Sck, Att, Ack, true),
                    Colors.Black,
                    Colors.Black, [], [], [], 0, ushort.MaxValue,
                    8000, false,
                    GuitarAxisType.Whammy, false, false, false, -1, true));
            }

            if (!Outputs.Items.Any(s => s is GuitarAxis {Type: GuitarAxisType.Slider}))
            {
                Outputs.Add(new GuitarAxis(Model,true,
                    new Ps2Input(Ps2InputType.GuitarTapBar, Model, Peripheral, Miso, Mosi, Sck, Att, Ack, true),
                    Colors.Black,
                    Colors.Black, [], [], [], 0, ushort.MaxValue, 0,
                    false,
                    GuitarAxisType.Slider, false, false, false, -1, true));
            }

            if (!Outputs.Items.Any(s => s.Input.InnermostInputs().First() is Ps2Input {Input: Ps2InputType.GuitarTilt}))
            {
                Outputs.Add(new GuitarAxis(Model,true,
                    new DigitalToAnalog(
                        new Ps2Input(Ps2InputType.GuitarTilt, Model, Peripheral, Miso, Mosi, Sck, Att, Ack,
                            true), 32767,
                        Model, DigitalToAnalogType.Tilt), Colors.Black,
                    Colors.Black, [], [], [], ushort.MinValue,
                    ushort.MaxValue,
                    0, false, GuitarAxisType.Tilt, false, false, false, -1, true));
            }
        }
        else
        {
            Outputs.RemoveMany(Outputs.Items.Where(s => s is GuitarAxis
            {
                Type: GuitarAxisType.Whammy or GuitarAxisType.Slider
            }));
            Outputs.RemoveMany(Outputs.Items.Where(s => s.Input.InnermostInputs().First() is Ps2Input
            {
                Input: Ps2InputType.GuitarTilt
            }));
        }

        var tapFrets =
            Outputs.Items.FirstOrDefault(s => s is {Input: Ps2Input {Input: Ps2InputType.GuitarTapAll}});
        if (Model.DeviceControllerType.Is5FretGuitar())
        {
            if (tapFrets == null)
            {
                Outputs.Add(new GuitarButton(Model,true,
                    new Ps2Input(Ps2InputType.GuitarTapAll, Model, Peripheral, Miso, Mosi, Sck, Att, Ack, true),
                    Colors.Black,
                    Colors.Black, [], [], [], 10,
                    InstrumentButtonType.SliderToFrets, false, false, false, -1, true)
                {
                    Enabled = false
                });
            }
        }
        else
        {
            if (tapFrets != null)
            {
                Outputs.Remove(tapFrets);
            }
        }

        InstrumentButtonTypeExtensions.ConvertBindings(Outputs, Model, true);
        if (Model.DeviceControllerType.Is5FretGuitar())
        {
            foreach (var output in Outputs.Items)
            {
                if (output is GuitarButton guitarButton)
                {
                    if (!InstrumentButtonTypeExtensions.LiveToGuitar.ContainsKey(guitarButton.Type)) continue;
                    Outputs.Remove(output);
                    Outputs.Add(new GuitarButton(Model, true,output.Input, output.LedOn, output.LedOff,
                        output.LedIndices.ToArray(), output.LedIndicesPeripheral.ToArray(),
                        output.LedIndicesMpr121.ToArray(), guitarButton.Debounce,
                        InstrumentButtonTypeExtensions.LiveToGuitar[guitarButton.Type], false, false, false, -1,
                        true));
                }

                if (output is not ControllerButton button) continue;
                if (!InstrumentButtonTypeExtensions.GuitarMappings.ContainsKey(button.Type)) continue;
                Outputs.Remove(output);
                Outputs.Add(new GuitarButton(Model, true,output.Input, output.LedOn, output.LedOff,
                    output.LedIndices.ToArray(), output.LedIndicesPeripheral.ToArray(),
                    output.LedIndicesMpr121.ToArray(), button.Debounce,
                    InstrumentButtonTypeExtensions.GuitarMappings[button.Type], false, false, false, -1, true));
            }
        }
        else if (Model.DeviceControllerType is DeviceControllerType.LiveGuitar)
        {
            foreach (var output in Outputs.Items)
            {
                if (output is GuitarButton guitarButton)
                {
                    if (!InstrumentButtonTypeExtensions.GuitarToLive.ContainsKey(guitarButton.Type)) continue;
                    Outputs.Remove(output);
                    Outputs.Add(new GuitarButton(Model, true,output.Input, output.LedOn, output.LedOff,
                        output.LedIndices.ToArray(), output.LedIndicesPeripheral.ToArray(),
                        output.LedIndicesMpr121.ToArray(), guitarButton.Debounce,
                        InstrumentButtonTypeExtensions.GuitarToLive[guitarButton.Type], false, false, false, -1,
                        true));
                }

                if (output is not ControllerButton button) continue;
                if (!InstrumentButtonTypeExtensions.LiveGuitarMappings.ContainsKey(button.Type)) continue;
                Outputs.Remove(output);
                Outputs.Add(new GuitarButton(Model, true,output.Input, output.LedOn, output.LedOff,
                    output.LedIndices.ToArray(), output.LedIndicesPeripheral.ToArray(),
                    output.LedIndicesMpr121.ToArray(), button.Debounce,
                    InstrumentButtonTypeExtensions.LiveGuitarMappings[button.Type], false, false, false, -1, true));
            }
        }
        else
        {
            foreach (var output in Outputs.Items)
            {
                if (output is not GuitarButton guitarButton) continue;
                Outputs.Remove(output);
                Outputs.Add(new ControllerButton(Model, true,output.Input, output.LedOn, output.LedOff,
                    output.LedIndices.ToArray(), output.LedIndicesPeripheral.ToArray(),
                    output.LedIndicesMpr121.ToArray(), guitarButton.Debounce,
                    InstrumentButtonTypeExtensions.GuitarToStandard[guitarButton.Type], false, false, false, -1,
                    true));
            }
        }

        if (Model.DeviceControllerType == DeviceControllerType.Gamepad)
        {
            if (!Outputs.Items.Any(
                    s => s is ControllerAxis {Type: StandardAxisType.LeftTrigger, Input: DigitalToAnalog}))
            {
                Outputs.Add(new ControllerAxis(Model,true,
                    new DigitalToAnalog(
                        new Ps2Input(Ps2InputType.L2, Model, Peripheral, Miso, Mosi, Sck, Att, Ack, true),
                        ushort.MaxValue,
                        Model, DigitalToAnalogType.Trigger),
                    Colors.Black,
                    Colors.Black, [], [], [], ushort.MinValue,
                    ushort.MaxValue, ushort.MaxValue/2,0,
                    ushort.MaxValue,
                    StandardAxisType.LeftTrigger, false, false, false, -1,
                    true));
            }

            if (!Outputs.Items.Any(
                    s => s is ControllerAxis {Type: StandardAxisType.RightTrigger, Input: DigitalToAnalog}))
            {
                Outputs.Add(new ControllerAxis(Model,true,
                    new DigitalToAnalog(
                        new Ps2Input(Ps2InputType.R2, Model, Peripheral, Miso, Mosi, Sck, Att, Ack, true),
                        ushort.MaxValue,
                        Model, DigitalToAnalogType.Trigger),
                    Colors.Black,
                    Colors.Black, [], [], [], ushort.MinValue,
                    ushort.MaxValue, ushort.MaxValue/2,0,
                    ushort.MaxValue,
                    StandardAxisType.RightTrigger, false, false, false, -1,
                    true));
            }

            if (Outputs.Items.Any(s => s is Ps3Axis)) return;
            foreach (var pair in Ps3Axis)
                Outputs.Add(new Ps3Axis(Model,true,
                    new Ps2Input(pair.Key, Model, Peripheral, Miso, Mosi, Sck, Att, Ack, true),
                    Colors.Black,
                    Colors.Black, [], [], [], short.MinValue,
                    short.MaxValue, 0,
                    pair.Value, false, false, false, -1, true));

            return;
        }

        Outputs.RemoveMany(Outputs.Items.Where(s => s is Ps3Axis));
    }

    protected override IEnumerable<PinConfig> GetOwnPinConfigs()
    {
        return [SpiConfig, _attConfig, _ackConfig];
    }
}