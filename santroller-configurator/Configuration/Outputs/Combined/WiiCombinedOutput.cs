using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public partial class WiiCombinedOutput : CombinedTwiOutput
{
    public static readonly Dictionary<int, WiiControllerType> ControllerTypeById = new()
    {
        {0x0000, WiiControllerType.Nunchuk},
        {0x0001, WiiControllerType.ClassicController},
        {0x0101, WiiControllerType.ClassicControllerPro},
        {0x0301, WiiControllerType.ClassicControllerPro},
        {0xFF12, WiiControllerType.UDraw},
        {0xFF13, WiiControllerType.Drawsome},
        {0x0003, WiiControllerType.Guitar},
        {0x0103, WiiControllerType.Drum},
        {0x0303, WiiControllerType.Dj},
        {0x0011, WiiControllerType.Taiko},
        {0x0005, WiiControllerType.MotionPlus}
    };

    public WiiCombinedOutput(ConfigViewModel model, bool peripheral, int sda = -1, int scl = -1) : base(model,
        WiiInput.WiiTwiType,
        peripheral, WiiInput.WiiTwiFreq, "Wii", sda, scl)
    {
        Outputs.Clear();
        _isGuitarHelper = this.WhenAnyValue(x => x.DetectedType).Select(s => s is WiiControllerType.Guitar)
            .ToProperty(this, x => x.IsGuitar);
        _isTurntableHelper = this.WhenAnyValue(x => x.DetectedType).Select(s => s is WiiControllerType.Dj)
            .ToProperty(this, x => x.IsTurntable);
        Outputs.Connect().Filter(x => x is OutputAxis or JoystickToDpad)
            .Filter(s => s.IsVisible)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Length != 0)
            .Filter(this.WhenAnyValue(x => x.ControllerFound, x => x.DetectedType, x => x.SelectedType)
                .Select(CreateFilter))
            .Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton or StartSelectHome)
            .Filter(s => s.IsVisible)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Length != 0)
            .Filter(this.WhenAnyValue(x => x.ControllerFound, x => x.DetectedType, x => x.SelectedType)
                .Select(CreateFilter))
            .Bind(out var digitalOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton or StartSelectHome or {Input.IsAnalog:false})
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

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] private bool _isGuitar;
    [ObservableAsProperty] private bool _isTurntable;

    [Reactive] private WiiControllerType _detectedType;
    [Reactive] private WiiControllerType _selectedType = WiiControllerType.Selected;

    public IEnumerable<WiiControllerType> WiiControllerTypes => Enum.GetValues<WiiControllerType>()
        .Where(s => s is not (WiiControllerType.ClassicControllerPro or WiiControllerType.MotionPlus));

    [Reactive] private bool _controllerFound;

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
        (bool controllerFound, WiiControllerType currentType, WiiControllerType selectedType) tuple)
    {
        if (tuple.selectedType == WiiControllerType.All)
        {
            return _ => true;
        }

        var controllerType = tuple.selectedType;
        if (controllerType == WiiControllerType.Selected)
        {
            controllerType = tuple.currentType;
            if (!tuple.controllerFound)
            {
                return _ => true;
            }

            if (controllerType is WiiControllerType.ClassicControllerPro)
            {
                controllerType = WiiControllerType.ClassicController;
            }
        }

        return output => output is JoystickToDpad or StartSelectHome || output.Input.InnermostInputs().First() is WiiInput wiiInput &&
            wiiInput.WiiControllerType == controllerType;
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return Resources.WiiCombinedTitle;
    }

    public void CreateDefaults()
    {
        Outputs.Clear();
        foreach (var pair in Buttons)
        {
            var output = new ControllerButton(Model, true, new WiiInput(pair.Key, Model, Peripheral, Sda, Scl, true),
                Colors.Black,
                Colors.Black, [], [], [], 10,
                pair.Value, false, false, false, -1, true);
            Outputs.Add(output);
        }

        foreach (var pair in Axis)
        {
            if (UIntInputs.Contains(pair.Key))
            {
                var threshold = ushort.MaxValue;
                if (pair.Key is WiiInputType.ClassicLeftTrigger or WiiInputType.ClassicRightTrigger)
                {
                    threshold = 50000;
                }
                Outputs.Add(new ControllerAxis(Model, true, new WiiInput(pair.Key, Model, Peripheral, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, [], [], [], 0, ushort.MaxValue,
                    ushort.MaxValue/2,8000, threshold,
                    pair.Value, false, false, false, -1, true));
            }
            else
            {
                Outputs.Add(new ControllerAxis(Model, true, new WiiInput(pair.Key, Model, Peripheral, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, [], [], [], -30000, 30000, 0,4000,
                    ushort.MaxValue,
                    pair.Value, false, false, false, -1, true));
            }
        }

        Outputs.Add(new ControllerAxis(Model,true,
            new WiiInput(WiiInputType.GuitarTapBar, Model, Peripheral, Sda, Scl, true),
            Colors.Black,
            Colors.Black, [], [], [], short.MinValue, short.MaxValue,
            0,0,
            ushort.MaxValue, StandardAxisType.RightStickY, false, false, false, -1, true));
        foreach (var pair in AxisAcceleration)
            Outputs.Add(new ControllerAxis(Model, true,new WiiInput(pair.Key, Model, Peripheral, Sda, Scl, true),
                Colors.Black,
                Colors.Black, [], [], [], short.MinValue,
                short.MaxValue, 0,0,
                ushort.MaxValue, pair.Value, false, false, false, -1,
                true));
        var dpad = new JoystickToDpad(Model, true,Peripheral, short.MaxValue / 2, true)
        {
            Enabled = false
        };
        Outputs.Add(dpad);
        Outputs.Add(new StartSelectHome(Model, true,Peripheral, true) {Enabled = false});
        UpdateBindings();
    }

    public override IEnumerable<Output> ValidOutputs()
    {
        var outputs = new List<Output>(base.ValidOutputs());

        var joyToDpad = outputs.FirstOrDefault(s => s is JoystickToDpad);
        if (joyToDpad?.Enabled == true)
        {
            outputs.Remove(joyToDpad);
            outputs.Add(joyToDpad.ValidOutputs());
        }

        var startSelectHome = outputs.FirstOrDefault(s => s is StartSelectHome);
        if (startSelectHome?.Enabled == true)
        {
            outputs.Remove(startSelectHome);
            outputs.Add(startSelectHome.ValidOutputs());
        }

        var tapAnalog =
            Outputs.Items.FirstOrDefault(s => s is {Input: WiiInput {Input: WiiInputType.GuitarTapBar}});
        var tapFrets =
            Outputs.Items.FirstOrDefault(s => s is {Input: WiiInput {Input: WiiInputType.GuitarTapAll}});
        if (tapAnalog == null && tapFrets == null) return outputs;
        // Map Tap bar to Upper frets on RB guitars
        if (tapAnalog != null && Model.DeviceControllerType is DeviceControllerType.RockBandGuitar)
        {
            outputs.AddRange(TapRb.Select(pair => new GuitarButton(Model,tapAnalog.Enabled,
                new WiiInput(pair.Key, Model, Peripheral, Sda, Scl, true),
                Colors.Black, Colors.Black, [], [], [], 5,
                pair.Value, false, false, false, -1, true)));

            outputs.Remove(tapAnalog);
        }

        if (tapFrets == null) return outputs;
        if (Model.DeviceControllerType.Is5FretGuitar())
        {
            outputs.AddRange(Tap.Select(pair => new GuitarButton(Model,tapFrets.Enabled,
                new WiiInput(pair.Key, Model, Peripheral, Sda, Scl, true),
                Colors.Black, Colors.Black, [], [], [], 5,
                pair.Value, false, false, false, -1, true)));
        }

        outputs.Remove(tapFrets);

        return outputs;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedWiiCombinedOutput(Peripheral, Sda, Scl, Outputs.Items.ToList());
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> bluetoothRaw, ReadOnlySpan<byte> usbHostInputsRaw,
        ReadOnlySpan<byte> peripheralWtRaw, Dictionary<int, bool> digitalPeripheral,
        ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw, ReadOnlySpan<byte> mpr121Raw,
        ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType,
            wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw, digitalPeripheral,
            cloneRaw, adxlRaw, mpr121Raw, midiRaw, bluetoothInputsRaw);
        if (wiiControllerType.IsEmpty)
        {
            ControllerFound = false;
            return;
        }

        ControllerFound = true;

        var type = BitConverter.ToUInt16(wiiControllerType);
        var newType = ControllerTypeById.GetValueOrDefault(type);

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
                {
                    if (UIntInputs.Contains(pair.Key))
                    {
                        Outputs.Add(new ControllerAxis(Model, true,new WiiInput(pair.Key, Model, Peripheral, Sda, Scl, true),
                            Colors.Black,
                            Colors.Black, [], [], [], 0,
                            ushort.MaxValue, 0,8000,
                            ushort.MaxValue, pair.Value, false, false, false, -1, true));
                    }
                    else
                    {
                        Outputs.Add(new ControllerAxis(Model,true, new WiiInput(pair.Key, Model, Peripheral, Sda, Scl, true),
                            Colors.Black,
                            Colors.Black, [], [], [], -30000, 30000,
                            0,4000,
                            ushort.MaxValue, pair.Value, false, false, false, -1, true));
                    }
                }
            }
        }

        // Drum Specific mappings
        if (Model.DeviceControllerType.IsDrum())
        {
            var isGh = Model.DeviceControllerType is DeviceControllerType.GuitarHeroDrums;
            // Drum Inputs to Drum Axis
            if (!Outputs.Items.Any(s => s is DrumAxis))
            {
                foreach (var pair in isGh ? DrumAxisGh : DrumAxisRb)
                    Outputs.Add(new DrumAxis(Model, true,new WiiInput(pair.Key, Model, Peripheral, Sda, Scl, true),
                        Colors.Black,
                        Colors.Black, [], [], [], -30000, 30000, 10,
                        10, pair.Value, false, false, false, -1,
                        true));
                Outputs.RemoveMany(Outputs.Items.Where(s => s is ControllerButton cb && cb.Input.InnermostInputs().Any(s2 => s2 is WiiInput w && (DrumAxisGh.ContainsKey(w.Input) || DrumAxisRb.ContainsKey(w.Input)))));
            }
            else
            {
                // We already have drum inputs mapped, but need to handle swapping between GH and RB 
                var first = Outputs.Items.OfType<DrumAxis>().First(s => s.Input is WiiInput
                {
                    Input: WiiInputType.DrumOrange
                });
                Outputs.Remove(first);
                // Rb maps orange to green, while gh maps orange to orange
                if (isGh)
                    Outputs.Add(new DrumAxis(Model,true,
                        new WiiInput(WiiInputType.DrumOrange, Model, Peripheral, Sda, Scl, true),
                        first.LedOn, first.LedOff, first.LedIndices.ToArray(), first.LedIndicesPeripheral.ToArray(),
                        first.LedIndicesMpr121.ToArray(),
                        first.Min, first.Max, first.DeadZone,
                        10,
                        DrumAxisType.Orange, false, false, false, -1, true));
                else
                    Outputs.Add(new DrumAxis(Model,true,
                        new WiiInput(WiiInputType.DrumOrange, Model, Peripheral, Sda, Scl, true),
                        first.LedOn, first.LedOff, first.LedIndices.ToArray(), first.LedIndicesPeripheral.ToArray(),
                        first.LedIndicesMpr121.ToArray(),
                        first.Min, first.Max, first.DeadZone,
                        10,
                        DrumAxisType.Green, false, false, false, -1, true));
            }
        }
        else
        {
            var currentDrums = Outputs.Items.OfType<DrumAxis>();
            foreach (var drumAxis in currentDrums)
            {
                Outputs.Add(new ControllerButton(Model, true,drumAxis.Input,
                    drumAxis.LedOn, drumAxis.LedOff, drumAxis.LedIndices.ToArray(),
                    drumAxis.LedIndicesPeripheral.ToArray(), drumAxis.LedIndicesMpr121.ToArray(), drumAxis.Debounce,
                    Buttons[DrumToWii[drumAxis.Type]], false, false, false, -1, true));
            }

            // Remove all drum inputs if we aren't in Drum emulation mode
            Outputs.RemoveMany(Outputs.Items.Where(s => s is DrumAxis));
        }

        var tapFrets =
            Outputs.Items.FirstOrDefault(s => s is {Input: WiiInput {Input: WiiInputType.GuitarTapAll}});
        var pedal =
            Outputs.Items.FirstOrDefault(s => s is {Input: WiiInput {Input: WiiInputType.GuitarPedal}});

        if (Model.DeviceControllerType.Is5FretGuitar())
        {
            if (tapFrets == null)
            {
                Outputs.Add(new GuitarButton(Model,true,
                    new WiiInput(WiiInputType.GuitarTapAll, Model, Peripheral, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, [], [], [], 10,
                    InstrumentButtonType.SliderToFrets, false, false, false, -1, true)
                {
                    Enabled = false
                });
            }

            if (pedal == null)
            {
                Outputs.Add(new GuitarAxis(Model,true,
                    new DigitalToAnalog(new WiiInput(WiiInputType.GuitarPedal, Model, Peripheral, Sda, Scl, true), short.MaxValue, Model, DigitalToAnalogType.Tilt),
                    Colors.Black,
                    Colors.Black, [], [], [], short.MinValue,
                    short.MaxValue, 0, false, GuitarAxisType.Tilt, false, false, false, -1, true));
            }
        }
        else
        {
            if (tapFrets != null)
            {
                Outputs.Remove(tapFrets);
            }
            if (pedal != null)
            {
                Outputs.Remove(pedal);
            }
        }

        if (Model.DeviceControllerType.IsGuitar())
        {
            if (!Outputs.Items.Any(s => s is GuitarAxis {Type: GuitarAxisType.Whammy}))
            {
                Outputs.Add(new GuitarAxis(Model,true,
                    new WiiInput(WiiInputType.GuitarWhammy, Model, Peripheral, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, [], [], [], 0, ushort.MaxValue,
                    8000,
                    false, GuitarAxisType.Whammy, false, false, false, -1, true));
            }
        }
        else
        {
            Outputs.RemoveMany(Outputs.Items.Where(s => s is GuitarAxis {Type: GuitarAxisType.Whammy}));
        }

        // Map Slider on guitars to Slider, and to RightStickY on anything else
        if (Model.DeviceControllerType.Is5FretGuitar())
        {
            if (!Outputs.Items.Any(s => s is GuitarAxis {Type: GuitarAxisType.Slider}))
            {
                Outputs.Add(new GuitarAxis(Model,true,
                    new WiiInput(WiiInputType.GuitarTapBar, Model, Peripheral, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, [], [], [], 0, ushort.MaxValue, 0,
                    false, GuitarAxisType.Slider, false, false, false, -1, true));
            }
        }
        else if (Model.DeviceControllerType == DeviceControllerType.Gamepad)
        {
            Outputs.RemoveMany(Outputs.Items.Where(s => s is GuitarAxis {Type: GuitarAxisType.Slider}));
        }

        InstrumentButtonTypeExtensions.ConvertBindings(Outputs, Model, true);

        // Map all DJ Hero axis and buttons
        if (Model.DeviceControllerType is DeviceControllerType.Turntable)
        {
            var currentAxisStandard = Outputs.Items.OfType<ControllerAxis>().ToList();
            var currentButtonStandard = Outputs.Items.OfType<ControllerButton>().ToList();
            foreach (var (djInputType, wiiInputType) in DjToWiiButton)
            {
                var items = currentButtonStandard.Where(s => s.Input is WiiInput wii && wii.Input == wiiInputType)
                    .ToList();
                Outputs.RemoveMany(items);
                Outputs.AddRange(items.Select(item => new DjButton(Model, true,item.Input,
                    item.LedOn, item.LedOff, item.LedIndices.ToArray(), item.LedIndicesPeripheral.ToArray(),
                    item.LedIndicesMpr121.ToArray(),
                    item.Debounce, djInputType, false, false, false, -1, true)));
            }

            if (!Outputs.Items.Any(s => s is DjAxis {Type: DjAxisType.Crossfader}))
            {
                Outputs.Add(new DjAxis(Model,true,
                    new WiiInput(WiiInputType.DjCrossfadeSlider, Model, Peripheral, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, [], [], [], 0, ushort.MaxValue, 0,
                    DjAxisType.Crossfader, false, false, false, -1, true));
            }

            if (!Outputs.Items.Any(s => s is DjAxis {Type: DjAxisType.EffectsKnob}))
            {
                Outputs.Add(new DjAxis(Model,true,
                    new WiiInput(WiiInputType.DjEffectDial, Model, Peripheral, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, [], [], [], 0, ushort.MaxValue, 0,
                    DjAxisType.EffectsKnob, false, false, false, -1, true));
            }

            if (!Outputs.Items.Any(s => s is DjAxis {Type: DjAxisType.LeftTableVelocity}))
            {
                Outputs.Add(new DjAxis(Model,true,
                    new WiiInput(WiiInputType.DjTurntableLeft, Model, Peripheral, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, [], [], [], short.MinValue,
                    short.MaxValue, 0,
                    DjAxisType.LeftTableVelocity, false, false, false, -1, true));
            }

            if (!Outputs.Items.Any(s => s is DjAxis {Type: DjAxisType.RightTableVelocity}))
            {
                Outputs.Add(new DjAxis(Model,true,
                    new WiiInput(WiiInputType.DjTurntableRight, Model, Peripheral, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, [], [], [], short.MinValue,
                    short.MaxValue, 0,
                    DjAxisType.RightTableVelocity, false, false, false, -1, true));
            }
        }
        else
        {
            var currentButtonDj = Outputs.Items.OfType<DjButton>();
            foreach (var djButton in currentButtonDj)
            {
                Outputs.Remove(djButton);
                Outputs.Add(new ControllerButton(Model, true,djButton.Input,
                    djButton.LedOn, djButton.LedOff, djButton.LedIndices.ToArray(),
                    djButton.LedIndicesPeripheral.ToArray(), djButton.LedIndicesMpr121.ToArray(), djButton.Debounce,
                    Buttons[DjToWiiButton[djButton.Type]], false, false, false, -1, true));
            }

            Outputs.RemoveMany(Outputs.Items.Where(s => s is DjAxis));
        }
    }

    public override Enum GetOutputType()
    {
        return SimpleType.WiiInputSimple;
    }
}