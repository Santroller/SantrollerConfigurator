using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public class Gh5CombinedOutput : CombinedTwiOutput
{
    private static readonly Dictionary<Gh5NeckInputType, InstrumentButtonType> Taps = new()
    {
        {Gh5NeckInputType.TapGreen, InstrumentButtonType.Green},
        {Gh5NeckInputType.TapRed, InstrumentButtonType.Red},
        {Gh5NeckInputType.TapYellow, InstrumentButtonType.Yellow},
        {Gh5NeckInputType.TapBlue, InstrumentButtonType.Blue},
        {Gh5NeckInputType.TapOrange, InstrumentButtonType.Orange}
    };

    private static readonly Dictionary<Gh5NeckInputType, InstrumentButtonType> Standard = new()
    {
        {Gh5NeckInputType.Green, InstrumentButtonType.Green},
        {Gh5NeckInputType.Red, InstrumentButtonType.Red},
        {Gh5NeckInputType.Yellow, InstrumentButtonType.Yellow},
        {Gh5NeckInputType.Blue, InstrumentButtonType.Blue},
        {Gh5NeckInputType.Orange, InstrumentButtonType.Orange}
    };

    private static readonly Dictionary<Gh5NeckInputType, InstrumentButtonType> TapsRb = new()
    {
        {Gh5NeckInputType.TapGreen, InstrumentButtonType.SoloGreen},
        {Gh5NeckInputType.TapRed, InstrumentButtonType.SoloRed},
        {Gh5NeckInputType.TapYellow, InstrumentButtonType.SoloYellow},
        {Gh5NeckInputType.TapBlue, InstrumentButtonType.SoloBlue},
        {Gh5NeckInputType.TapOrange, InstrumentButtonType.SoloOrange}
    };


    public Gh5CombinedOutput(ConfigViewModel model, bool peripheral, int sda = -1, int scl = -1) : base(model,
        Gh5NeckInput.Gh5TwiType, peripheral, Gh5NeckInput.Gh5TwiFreq, "GH5", sda, scl)
    {
        Outputs.Clear();
        Outputs.Connect().Filter(x => x is OutputAxis)
            .Filter(s => s.IsVisible)
            .Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton)
            .Filter(s => s.IsVisible)
            .Bind(out var digitalOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton)
            .Bind(out var allDigitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
        AllDigitalOutputs = allDigitalOutputs;
    }

    [Reactive] public bool Detected { get; set; }

    public override void SetOutputsOrDefaults(IReadOnlyCollection<Output> outputs)
    {
        Outputs.Clear();
        if (outputs.Any())
            Outputs.AddRange(outputs);
        else
            CreateDefaults();
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return Resources.Gh5CombinedTitle;
    }

    public override Enum GetOutputType()
    {
        return SimpleType.Gh5NeckSimple;
    }

    public void CreateDefaults()
    {
        Outputs.Clear();
        Outputs.Add(new ControllerAxis(Model,
            new Gh5NeckInput(Gh5NeckInputType.TapBar, Model, Peripheral, combined: true),
            Colors.Black,
            Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(),Array.Empty<byte>(), short.MinValue, short.MaxValue, 0, ushort.MaxValue,
            StandardAxisType.RightStickY, false, false ,false, -1, true));
        UpdateBindings();
    }

    public override IEnumerable<Output> ValidOutputs()
    {
        var tapAnalog =
            Outputs.Items.FirstOrDefault(
                s => s is {Enabled: true, Input: Gh5NeckInput {Input: Gh5NeckInputType.TapBar}});
        var tapFrets =
            Outputs.Items.FirstOrDefault(
                s => s is {Enabled: true, Input: Gh5NeckInput {Input: Gh5NeckInputType.TapAll}});
        if (tapAnalog == null && tapFrets == null) return Outputs.Items.Where(s => s.Enabled);
        var outputs = new List<Output>(Outputs.Items.Where(s => s.Enabled));

        // Map Tap bar to Upper frets on RB guitars
        if (tapAnalog != null && Model.DeviceControllerType is DeviceControllerType.RockBandGuitar)
        {
            outputs.AddRange(TapsRb.Select(pair => new GuitarButton(Model,
                new Gh5NeckInput(pair.Key, Model, Peripheral, Sda, Scl, true), Colors.Black, Colors.Black,
                Array.Empty<byte>(),Array.Empty<byte>(), Array.Empty<byte>(), 5,
                pair.Value, false, false ,false, -1, true)));

            outputs.Remove(tapAnalog);
        }

        if (tapFrets == null) return outputs;

        outputs.AddRange(Taps.Select(pair => new GuitarButton(Model,
            new Gh5NeckInput(pair.Key, Model, Peripheral, Sda, Scl, true),
            Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(),Array.Empty<byte>(), 5, pair.Value, false, false ,false, -1, true)));

        outputs.Remove(tapFrets);

        return outputs;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedGh5CombinedOutput(Peripheral, Sda, Scl, Outputs.Items.ToList());
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
            wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw, digitalPeripheral, cloneRaw, adxlRaw, mpr121Raw);
        Detected = !gh5Raw.IsEmpty;
    }

    public override void UpdateBindings()
    {
        var axisController = Outputs.Items.FirstOrDefault(s => s is ControllerAxis);
        var axisGuitar = Outputs.Items.FirstOrDefault(s => s is GuitarAxis);
        var tapAll = Outputs.Items.FirstOrDefault(s => s is OutputButton);
        if (Model.DeviceControllerType.Is5FretGuitar())
        {
            if (tapAll == null)
            {
                var button = new GuitarButton(Model,
                    new Gh5NeckInput(Gh5NeckInputType.TapAll, Model, Peripheral, combined: true), Colors.Black,
                    Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(),Array.Empty<byte>(), 5, InstrumentButtonType.Slider, false, false ,false, -1, true)
                {
                    Enabled = false
                };
                Outputs.Add(button);
            }

            foreach (var (key, value) in Standard)
            {
                var item = Outputs.Items.FirstOrDefault(s => s.Input is Gh5NeckInput gh5 && gh5.Input == key);
                if (item != null) continue;
                var button = new GuitarButton(Model,
                    new Gh5NeckInput(key, Model, Peripheral, combined: true), Colors.Black,
                    Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(),Array.Empty<byte>(), 5, value, false, false ,false, -1, true)
                {
                    Enabled = false
                };
                Outputs.Add(button);
            }

            if (axisController == null) return;
            Outputs.Remove(axisController);
            Outputs.Add(new GuitarAxis(Model,
                new Gh5NeckInput(Gh5NeckInputType.TapBar, Model, Peripheral, Sda, Scl,
                    true),
                Colors.Black,
                Colors.Black, Array.Empty<byte>(),Array.Empty<byte>(), Array.Empty<byte>(), short.MinValue, short.MaxValue, 0,
                false, GuitarAxisType.Slider, false, false ,false, -1, true));
        }
        else if (Model.DeviceControllerType == DeviceControllerType.Gamepad)
        {
            var toRemove = new List<Output>();
            foreach (var key in Standard.Keys)
            {
                var item = Outputs.Items.FirstOrDefault(s => s.Input is Gh5NeckInput gh5 && gh5.Input == key);
                if (item != null)
                {
                    toRemove.Add(item);
                }
            }

            Outputs.RemoveMany(toRemove);
            if (tapAll != null) Outputs.Remove(tapAll);
            if (axisGuitar != null)
            {
                Outputs.Remove(axisGuitar);
            }

            if (axisController != null) return;

            Outputs.Add(new ControllerAxis(Model,
                new Gh5NeckInput(Gh5NeckInputType.TapBar, Model, Peripheral, Sda, Scl,
                    true),
                Colors.Black,
                Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(),Array.Empty<byte>(), short.MinValue, short.MaxValue, 0,
                ushort.MaxValue, StandardAxisType.LeftStickX, false, false ,false, -1, true));
        }
        else
        {
            var toRemove = new List<Output>();
            foreach (var key in Standard.Keys)
            {
                var item = Outputs.Items.FirstOrDefault(s => s.Input is Gh5NeckInput gh5 && gh5.Input == key);
                if (item != null)
                {
                    toRemove.Add(item);
                }
            }

            Outputs.RemoveMany(toRemove);
            if (tapAll != null) Outputs.Remove(tapAll);

            if (axisGuitar != null)
            {
                Outputs.Remove(axisGuitar);
            }

            if (axisController != null)
            {
                Outputs.Remove(axisController);
            }
        }
    }
}