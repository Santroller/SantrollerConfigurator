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
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public class WiiCombinedOutput : CombinedTwiOutput
{
    private static readonly Dictionary<WiiInputType, StandardButtonType> Buttons = new()
    {
        {WiiInputType.ClassicA, StandardButtonType.A},
        {WiiInputType.ClassicB, StandardButtonType.B},
        {WiiInputType.ClassicX, StandardButtonType.X},
        {WiiInputType.ClassicY, StandardButtonType.Y},
        {WiiInputType.ClassicZl, StandardButtonType.LeftShoulder},
        {WiiInputType.ClassicZr, StandardButtonType.RightShoulder},
        {WiiInputType.ClassicMinus, StandardButtonType.Back},
        {WiiInputType.ClassicPlus, StandardButtonType.Start},
        {WiiInputType.ClassicHome, StandardButtonType.Guide},
        {WiiInputType.ClassicDPadDown, StandardButtonType.DpadDown},
        {WiiInputType.ClassicDPadUp, StandardButtonType.DpadUp},
        {WiiInputType.ClassicDPadLeft, StandardButtonType.DpadLeft},
        {WiiInputType.ClassicDPadRight, StandardButtonType.DpadRight},
        {WiiInputType.DjHeroLeftGreen, StandardButtonType.A},
        {WiiInputType.DjHeroLeftRed, StandardButtonType.B},
        {WiiInputType.DjHeroLeftBlue, StandardButtonType.X},
        {WiiInputType.DjHeroRightGreen, StandardButtonType.A},
        {WiiInputType.DjHeroRightRed, StandardButtonType.B},
        {WiiInputType.DjHeroRightBlue, StandardButtonType.X},
        {WiiInputType.DjHeroEuphoria, StandardButtonType.Y},
        {WiiInputType.DjHeroPlus, StandardButtonType.Start},
        {WiiInputType.DjHeroMinus, StandardButtonType.Back},
        {WiiInputType.NunchukC, StandardButtonType.A},
        {WiiInputType.NunchukZ, StandardButtonType.B},
        {WiiInputType.GuitarGreen, StandardButtonType.A},
        {WiiInputType.GuitarRed, StandardButtonType.B},
        {WiiInputType.GuitarYellow, StandardButtonType.Y},
        {WiiInputType.GuitarBlue, StandardButtonType.X},
        {WiiInputType.GuitarOrange, StandardButtonType.LeftShoulder},
        {WiiInputType.GuitarStrumDown, StandardButtonType.DpadDown},
        {WiiInputType.GuitarStrumUp, StandardButtonType.DpadUp},
        {WiiInputType.GuitarMinus, StandardButtonType.Back},
        {WiiInputType.GuitarPlus, StandardButtonType.Start},
        {WiiInputType.UDrawPenClick, StandardButtonType.A},
        {WiiInputType.UDrawPenButton1, StandardButtonType.X},
        {WiiInputType.UDrawPenButton2, StandardButtonType.Y},
        {WiiInputType.TaTaConLeftDrumCenter, StandardButtonType.A},
        {WiiInputType.TaTaConLeftDrumRim, StandardButtonType.B},
        {WiiInputType.TaTaConRightDrumCenter, StandardButtonType.X},
        {WiiInputType.TaTaConRightDrumRim, StandardButtonType.Y},
        {WiiInputType.DrumGreen, StandardButtonType.A},
        {WiiInputType.DrumRed, StandardButtonType.B},
        {WiiInputType.DrumYellow, StandardButtonType.Y},
        {WiiInputType.DrumBlue, StandardButtonType.X},
        {WiiInputType.DrumOrange, StandardButtonType.LeftShoulder},
        {WiiInputType.DrumKickPedal, StandardButtonType.RightShoulder},
        {WiiInputType.DrumMinus, StandardButtonType.Back},
        {WiiInputType.DrumPlus, StandardButtonType.Start},
    };


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

    private static readonly Dictionary<WiiInputType, StandardButtonType> Tap = new()
    {
        {WiiInputType.GuitarTapGreen, StandardButtonType.A},
        {WiiInputType.GuitarTapRed, StandardButtonType.B},
        {WiiInputType.GuitarTapYellow, StandardButtonType.Y},
        {WiiInputType.GuitarTapBlue, StandardButtonType.X},
        {WiiInputType.GuitarTapOrange, StandardButtonType.LeftShoulder}
    };

    private static readonly Dictionary<WiiInputType, InstrumentButtonType> TapRb = new()
    {
        {WiiInputType.GuitarTapGreen, InstrumentButtonType.SoloGreen},
        {WiiInputType.GuitarTapRed, InstrumentButtonType.SoloRed},
        {WiiInputType.GuitarTapYellow, InstrumentButtonType.SoloYellow},
        {WiiInputType.GuitarTapBlue, InstrumentButtonType.SoloBlue},
        {WiiInputType.GuitarTapOrange, InstrumentButtonType.SoloOrange}
    };

    private static readonly Dictionary<WiiInputType, StandardAxisType> Axis = new()
    {
        {WiiInputType.ClassicLeftStickX, StandardAxisType.LeftStickX},
        {WiiInputType.ClassicLeftStickY, StandardAxisType.LeftStickY},
        {WiiInputType.ClassicRightStickX, StandardAxisType.RightStickX},
        {WiiInputType.ClassicRightStickY, StandardAxisType.RightStickY},
        {WiiInputType.ClassicLeftTrigger, StandardAxisType.LeftTrigger},
        {WiiInputType.ClassicRightTrigger, StandardAxisType.RightTrigger},
        {WiiInputType.DjCrossfadeSlider, StandardAxisType.RightStickY},
        {WiiInputType.DjEffectDial, StandardAxisType.RightStickX},
        {WiiInputType.DjTurntableLeft, StandardAxisType.LeftStickX},
        {WiiInputType.DjTurntableRight, StandardAxisType.LeftStickY},
        {WiiInputType.UDrawPenX, StandardAxisType.LeftStickX},
        {WiiInputType.UDrawPenY, StandardAxisType.LeftStickY},
        {WiiInputType.UDrawPenPressure, StandardAxisType.LeftTrigger},
        {WiiInputType.DrawsomePenX, StandardAxisType.LeftStickX},
        {WiiInputType.DrawsomePenY, StandardAxisType.LeftStickY},
        {WiiInputType.DrawsomePenPressure, StandardAxisType.LeftTrigger},
        {WiiInputType.NunchukStickX, StandardAxisType.LeftStickX},
        {WiiInputType.NunchukStickY, StandardAxisType.LeftStickY},
        {WiiInputType.GuitarJoystickX, StandardAxisType.LeftStickX},
        {WiiInputType.GuitarJoystickY, StandardAxisType.LeftStickY},
        {WiiInputType.DrumJoystickX, StandardAxisType.LeftStickX},
        {WiiInputType.DrumJoystickY, StandardAxisType.LeftStickY},
        {WiiInputType.GuitarWhammy, StandardAxisType.RightStickX}
    };

    public static readonly List<WiiInputType> UIntInputs = new()
    {
        WiiInputType.ClassicLeftTrigger,
        WiiInputType.ClassicRightTrigger,
        WiiInputType.DrumGreenPressure,
        WiiInputType.DrumRedPressure,
        WiiInputType.DrumYellowPressure,
        WiiInputType.DrumBluePressure,
        WiiInputType.DrumOrangePressure,
        WiiInputType.DrumKickPedalPressure,
        WiiInputType.GuitarWhammy,
        WiiInputType.GuitarTapBar,
        WiiInputType.UDrawPenPressure,
        WiiInputType.DrawsomePenPressure,
        WiiInputType.DjCrossfadeSlider,
        WiiInputType.DjEffectDial
    };

    private static readonly Dictionary<DjAxisType, StandardAxisType> DjToStandard = new()
    {
        {DjAxisType.Crossfader, StandardAxisType.RightStickY},
        {DjAxisType.EffectsKnob, StandardAxisType.RightStickX},
        {DjAxisType.LeftTableVelocity, StandardAxisType.LeftStickX},
        {DjAxisType.RightTableVelocity, StandardAxisType.LeftStickY}
    };

    private static readonly Dictionary<DjInputType, WiiInputType> DjToWiiButton = new()
    {
        {DjInputType.LeftGreen, WiiInputType.DjHeroLeftGreen},
        {DjInputType.LeftRed, WiiInputType.DjHeroLeftRed},
        {DjInputType.LeftBlue, WiiInputType.DjHeroLeftBlue},
        {DjInputType.RightGreen, WiiInputType.DjHeroRightGreen},
        {DjInputType.RightRed, WiiInputType.DjHeroRightRed},
        {DjInputType.RightBlue, WiiInputType.DjHeroRightBlue}
    };

    private static readonly Dictionary<WiiInputType, DrumAxisType> DrumAxisGh = new()
    {
        {WiiInputType.DrumGreen, DrumAxisType.Green},
        {WiiInputType.DrumRed, DrumAxisType.Red},
        {WiiInputType.DrumYellow, DrumAxisType.Yellow},
        {WiiInputType.DrumBlue, DrumAxisType.Blue},
        {WiiInputType.DrumOrange, DrumAxisType.Orange},
        {WiiInputType.DrumKickPedal, DrumAxisType.Kick}
        // {WiiInputType.DrumHiHatPedal, DrumAxisType.Kick2},
    };

    private static readonly Dictionary<WiiInputType, DrumAxisType> DrumAxisRb = new()
    {
        {WiiInputType.DrumGreen, DrumAxisType.Green},
        {WiiInputType.DrumRed, DrumAxisType.Red},
        {WiiInputType.DrumYellow, DrumAxisType.Yellow},
        {WiiInputType.DrumBlue, DrumAxisType.Blue},
        {WiiInputType.DrumOrange, DrumAxisType.Green},
        {WiiInputType.DrumKickPedal, DrumAxisType.Kick}
        // {WiiInputType.DrumHiHatPedal, DrumAxisType.Kick2},
    };

    public static readonly Dictionary<WiiInputType, StandardAxisType> AxisAcceleration = new()
    {
        {WiiInputType.NunchukRotationRoll, StandardAxisType.RightStickX},
        {WiiInputType.NunchukRotationPitch, StandardAxisType.RightStickY}
    };

    public WiiCombinedOutput(ConfigViewModel model, bool peripheral, int sda = -1, int scl = -1) : base(model,
        WiiInput.WiiTwiType,
        peripheral, WiiInput.WiiTwiFreq, "Wii", sda, scl)
    {
        Outputs.Clear();
        this.WhenAnyValue(x => x.DetectedType).Select(s => s is WiiControllerType.Guitar)
            .ToPropertyEx(this, x => x.IsGuitar);
        this.WhenAnyValue(x => x.DetectedType).Select(s => s is WiiControllerType.Dj)
            .ToPropertyEx(this, x => x.IsTurntable);
        Outputs.Connect().Filter(x => x is OutputAxis)
            .Filter(s => s.IsVisible)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Any())
            .Filter(this.WhenAnyValue(x => x.ControllerFound, x => x.DetectedType, x => x.SelectedType)
                .Select(CreateFilter))
            .Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton or JoystickToDpad or StartSelectHome)
            .Filter(s => s.IsVisible)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Any())
            .Filter(this.WhenAnyValue(x => x.ControllerFound, x => x.DetectedType, x => x.SelectedType)
                .Select(CreateFilter))
            .Bind(out var digitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
    }

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] public bool IsGuitar { get; }
    [ObservableAsProperty] public bool IsTurntable { get; }

    [Reactive] public WiiControllerType DetectedType { get; set; }
    [Reactive] public WiiControllerType SelectedType { get; set; } = WiiControllerType.Selected;

    public IEnumerable<WiiControllerType> WiiControllerTypes => Enum.GetValues<WiiControllerType>()
        .Where(s => s is not (WiiControllerType.ClassicControllerPro or WiiControllerType.MotionPlus));

    [Reactive] public bool ControllerFound { get; set; }

    public override void SetOutputsOrDefaults(IReadOnlyCollection<Output> outputs)
    {
        Outputs.Clear();
        if (outputs.Any())
            Outputs.AddRange(outputs);
        else
            CreateDefaults();
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

        return output => output is JoystickToDpad or StartSelectHome || output.Input is WiiInput wiiInput &&
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
            var output = new ControllerButton(Model, new WiiInput(pair.Key, Model, Peripheral, Sda, Scl, true),
                Colors.Black,
                Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), 10,
                pair.Value, false, false, false, -1, true);
            Outputs.Add(output);
        }

        foreach (var pair in Axis)
        {
            if (UIntInputs.Contains(pair.Key))
            {
                Outputs.Add(new ControllerAxis(Model, new WiiInput(pair.Key, Model, Peripheral, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), 0, ushort.MaxValue,
                    8000, ushort.MaxValue,
                    pair.Value, false, false, false, -1, true));
            }
            else
            {
                Outputs.Add(new ControllerAxis(Model, new WiiInput(pair.Key, Model, Peripheral, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), -30000, 30000, 4000,
                    ushort.MaxValue,
                    pair.Value, false, false, false, -1, true));
            }
        }

        Outputs.Add(new ControllerAxis(Model,
            new WiiInput(WiiInputType.GuitarTapBar, Model, Peripheral, Sda, Scl, true),
            Colors.Black,
            Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), short.MinValue, short.MaxValue,
            0,
            ushort.MaxValue, StandardAxisType.RightStickY, false, false, false, -1, true));
        foreach (var pair in AxisAcceleration)
            Outputs.Add(new ControllerAxis(Model, new WiiInput(pair.Key, Model, Peripheral, Sda, Scl, true),
                Colors.Black,
                Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), short.MinValue,
                short.MaxValue, 0,
                ushort.MaxValue, pair.Value, false, false, false, -1,
                true));
        var dpad = new JoystickToDpad(Model, Peripheral, short.MaxValue / 2, true)
        {
            Enabled = false
        };
        Outputs.Add(dpad);
        Outputs.Add(new StartSelectHome(Model, Peripheral, true) {Enabled = false});
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
            Outputs.Items.FirstOrDefault(s => s is {Enabled: true, Input: WiiInput {Input: WiiInputType.GuitarTapBar}});
        var tapFrets =
            Outputs.Items.FirstOrDefault(s => s is {Enabled: true, Input: WiiInput {Input: WiiInputType.GuitarTapAll}});
        if (tapAnalog == null && tapFrets == null) return outputs;
        // Map Tap bar to Upper frets on RB guitars
        if (tapAnalog != null && Model.DeviceControllerType is DeviceControllerType.RockBandGuitar)
        {
            outputs.AddRange(TapRb.Select(pair => new GuitarButton(Model,
                new WiiInput(pair.Key, Model, Peripheral, Sda, Scl, true),
                Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), 5,
                pair.Value, false, false, false, -1, true)));

            outputs.Remove(tapAnalog);
        }

        if (tapFrets == null) return outputs;
        if (Model.DeviceControllerType.Is5FretGuitar())
        {
            outputs.AddRange(Tap.Select(pair => new ControllerButton(Model,
                new WiiInput(pair.Key, Model, Peripheral, Sda, Scl, true),
                Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), 5,
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
        ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw, ReadOnlySpan<byte> mpr121Raw)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType,
            wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw, digitalPeripheral,
            cloneRaw, adxlRaw, mpr121Raw);
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
                        Outputs.Add(new ControllerAxis(Model, new WiiInput(pair.Key, Model, Peripheral, Sda, Scl, true),
                            Colors.Black,
                            Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), 0,
                            ushort.MaxValue, 8000,
                            ushort.MaxValue, pair.Value, false, false, false, -1, true));
                    }
                    else
                    {
                        Outputs.Add(new ControllerAxis(Model, new WiiInput(pair.Key, Model, Peripheral, Sda, Scl, true),
                            Colors.Black,
                            Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), -30000, 30000,
                            4000,
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
                    Outputs.Add(new DrumAxis(Model, new WiiInput(pair.Key, Model, Peripheral, Sda, Scl, true),
                        Colors.Black,
                        Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), -30000, 30000, 10,
                        10, pair.Value, false, false, false, -1,
                        true));
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
                    Outputs.Add(new DrumAxis(Model,
                        new WiiInput(WiiInputType.DrumOrange, Model, Peripheral, Sda, Scl, true),
                        first.LedOn, first.LedOff, first.LedIndices.ToArray(), first.LedIndicesPeripheral.ToArray(),
                        first.LedIndicesMpr121.ToArray(),
                        first.Min, first.Max, first.DeadZone,
                        10,
                        DrumAxisType.Orange, false, false, false, -1, true));
                else
                    Outputs.Add(new DrumAxis(Model,
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
                Outputs.Add(new GuitarButton(Model,
                    new WiiInput(WiiInputType.GuitarTapAll, Model, Peripheral, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), 10,
                    InstrumentButtonType.SliderToFrets, false, false, false, -1, true)
                {
                    Enabled = false
                });
            }

            if (pedal == null)
            {
                Outputs.Add(new GuitarAxis(Model,
                    new DigitalToAnalog(new WiiInput(WiiInputType.GuitarPedal, Model, Peripheral, Sda, Scl, true), short.MaxValue, Model, DigitalToAnalogType.Tilt),
                    Colors.Black,
                    Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), short.MinValue,
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

        if (Model.DeviceControllerType.IsGuitar() && !Model.DeviceControllerType.IsFortnite())
        {
            if (!Outputs.Items.Any(s => s is GuitarAxis {Type: GuitarAxisType.Whammy}))
            {
                Outputs.Add(new GuitarAxis(Model,
                    new WiiInput(WiiInputType.GuitarWhammy, Model, Peripheral, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), 0, ushort.MaxValue,
                    8000,
                    false, GuitarAxisType.Whammy, false, false, false, -1, true));
            }
        }
        else
        {
            Outputs.RemoveMany(Outputs.Items.Where(s => s is GuitarAxis {Type: GuitarAxisType.Whammy}));
        }

        // Map Slider on guitars to Slider, and to RightStickY on anything else
        if (Model.DeviceControllerType.Is5FretGuitar() && !Model.DeviceControllerType.IsFortnite())
        {
            if (!Outputs.Items.Any(s => s is GuitarAxis {Type: GuitarAxisType.Slider}))
            {
                Outputs.Add(new GuitarAxis(Model,
                    new WiiInput(WiiInputType.GuitarTapBar, Model, Peripheral, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), 0, ushort.MaxValue, 0,
                    false, GuitarAxisType.Slider, false, false, false, -1, true));
            }
        }
        else if (Model.DeviceControllerType == DeviceControllerType.Gamepad || Model.DeviceControllerType.IsFortnite())
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
                Outputs.AddRange(items.Select(item => new DjButton(Model, item.Input,
                    item.LedOn, item.LedOff, item.LedIndices.ToArray(), item.LedIndicesPeripheral.ToArray(),
                    item.LedIndicesMpr121.ToArray(),
                    item.Debounce, djInputType, false, false, false, -1, true)));
            }

            if (!Outputs.Items.Any(s => s is DjAxis {Type: DjAxisType.Crossfader}))
            {
                Outputs.Add(new DjAxis(Model,
                    new WiiInput(WiiInputType.DjCrossfadeSlider, Model, Peripheral, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), 0, ushort.MaxValue, 0,
                    DjAxisType.Crossfader, false, false, false, -1, true));
            }

            if (!Outputs.Items.Any(s => s is DjAxis {Type: DjAxisType.EffectsKnob}))
            {
                Outputs.Add(new DjAxis(Model,
                    new WiiInput(WiiInputType.DjEffectDial, Model, Peripheral, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), 0, ushort.MaxValue, 0,
                    DjAxisType.EffectsKnob, false, false, false, -1, true));
            }

            if (!Outputs.Items.Any(s => s is DjAxis {Type: DjAxisType.LeftTableVelocity}))
            {
                Outputs.Add(new DjAxis(Model,
                    new WiiInput(WiiInputType.DjTurntableLeft, Model, Peripheral, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), short.MinValue,
                    short.MaxValue, 0,
                    DjAxisType.LeftTableVelocity, false, false, false, -1, true));
            }

            if (!Outputs.Items.Any(s => s is DjAxis {Type: DjAxisType.RightTableVelocity}))
            {
                Outputs.Add(new DjAxis(Model,
                    new WiiInput(WiiInputType.DjTurntableRight, Model, Peripheral, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), short.MinValue,
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
                Outputs.Add(new ControllerButton(Model, djButton.Input,
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