using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public partial class CrkdCombinedOutput : CombinedUartOutput
{
    private static readonly Dictionary<CrkdNeckInputType, InstrumentButtonType> Standard = new()
    {
        { CrkdNeckInputType.Green, InstrumentButtonType.Green },
        { CrkdNeckInputType.Red, InstrumentButtonType.Red },
        { CrkdNeckInputType.Yellow, InstrumentButtonType.Yellow },
        { CrkdNeckInputType.Blue, InstrumentButtonType.Blue },
        { CrkdNeckInputType.Orange, InstrumentButtonType.Orange },
    };

    private static readonly Dictionary<CrkdNeckInputType, StandardButtonType> Dpad = new()
    {
        { CrkdNeckInputType.DpadUp, StandardButtonType.DpadUp },
        { CrkdNeckInputType.DpadDown, StandardButtonType.DpadDown },
        { CrkdNeckInputType.DpadLeft, StandardButtonType.DpadLeft },
        { CrkdNeckInputType.DpadRight, StandardButtonType.DpadRight },
    };

    public CrkdCombinedOutput(ConfigViewModel model, bool peripheral, int tx = -1, int rx = -1) : base(model,
        CrkdNeckInput.CrkdUartType, peripheral, CrkdNeckInput.CrkdUartFreq, tx, rx)
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
        Outputs.Connect().Filter(x => x is OutputButton or { Input.IsAnalog: false })
            .Bind(out var allDigitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
        AllDigitalOutputs = allDigitalOutputs;
    }

    [Reactive] private bool _detected;

    public override void SetOutputsOrDefaults(IEnumerable<Output> outputs)
    {
        Outputs.Clear();
        Outputs.AddRange(outputs);
        if (Outputs.Count == 0)
        {
            CreateDefaults();
        }
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return Resources.CrkdCombinedTitle;
    }

    public override Enum GetOutputType()
    {
        return SimpleType.CrkdNeckSimple;
    }

    public void CreateDefaults()
    {
        Outputs.Clear();
        foreach (var (key, value) in Dpad)
        {
            var button = new ControllerButton(Model, true,
                new CrkdNeckInput(key, Model, Peripheral, combined: true), Colors.Black,
                Colors.Black, [], [], [], 5, value, false, false, false, -1, true)
            {
                Enabled = true
            };
            Outputs.Add(button);
        }
        UpdateBindings();
    }

    public override IEnumerable<Output> ValidOutputs()
    {
        return Outputs.Items;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedCrkdCombinedOutput(Peripheral, Tx, Rx, Outputs.Items.ToList());
    }

    public override void UpdateBindings()
    {
        if (Model.DeviceControllerType.Is5FretGuitar())
        {
            foreach (var (key, value) in Standard)
            {
                var item = Outputs.Items.FirstOrDefault(s => s.Input is CrkdNeckInput gh5 && gh5.Input == key);
                if (item != null) continue;
                var button = new GuitarButton(Model, true,
                    new CrkdNeckInput(key, Model, Peripheral, combined: true), Colors.Black,
                    Colors.Black, [], [], [], 5, value, false, false, false, -1, true)
                {
                    Enabled = true
                };
                Outputs.Add(button);
            }
        }
        else if (Model.DeviceControllerType == DeviceControllerType.Gamepad)
        {
            var toRemove = new List<Output>();
            foreach (var key in Standard.Keys)
            {
                var item = Outputs.Items.FirstOrDefault(s => s.Input is CrkdNeckInput gh5 && gh5.Input == key);
                if (item != null)
                {
                    toRemove.Add(item);
                }
            }

            Outputs.RemoveMany(toRemove);
        }
        else
        {
            var toRemove = new List<Output>();
            foreach (var key in Standard.Keys)
            {
                var item = Outputs.Items.FirstOrDefault(s => s.Input is CrkdNeckInput gh5 && gh5.Input == key);
                if (item != null)
                {
                    toRemove.Add(item);
                }
            }

            Outputs.RemoveMany(toRemove);
        }
    }

    private int _invalidCount = 0;
    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> bluetoothRaw, ReadOnlySpan<byte> usbHostInputsRaw,
        ReadOnlySpan<byte> peripheralWtRaw, Dictionary<int, bool> digitalPeripheral,
        ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw, ReadOnlySpan<byte> mpr121Raw,
        ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw, bool peripheralConnected, byte[] crkdRaw)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType,
            wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw, digitalPeripheral, cloneRaw, adxlRaw, mpr121Raw, midiRaw, bluetoothInputsRaw, peripheralConnected, crkdRaw);
        if (crkdRaw.Length == 0)
        {
            if (_invalidCount < 1000)
            {
                _invalidCount++;
            }
        }
        else
        {
            _invalidCount = 0;
        }
        Detected = _invalidCount < 500;
    }
}