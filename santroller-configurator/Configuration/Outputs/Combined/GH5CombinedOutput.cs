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

public partial class Gh5CombinedOutput : CombinedTwiOutput
{
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
        Outputs.Connect().Filter(x => x is OutputButton or {Input.IsAnalog:false})
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
        return Resources.Gh5CombinedTitle;
    }

    public override Enum GetOutputType()
    {
        return SimpleType.Gh5NeckSimple;
    }
    
    public override HostInput MakeInput(UsbHostInputType type)
    {
        return new Gh5NeckInput(type, Model, Peripheral, Sda, Scl, IsCombined);
    }


    public override HostInput? MakeInput(ProKeyType type)
    {
        return null;
    }

    // public void CreateDefaults()
    // {
    //     Outputs.Clear();
    //     Outputs.Add(new ControllerAxis(Model, true,
    //         new Gh5NeckInput(Gh5NeckInputType.TapBar, Model, Peripheral, combined: true),
    //         Colors.Black,
    //         Colors.Black, [], [],[], short.MinValue, short.MaxValue, 0,0, ushort.MaxValue,
    //         StandardAxisType.RightStickY, false, false ,false, -1, true));
    //     UpdateBindings();
    // }
    //
    // public override IEnumerable<Output> ValidOutputs()
    // {
    //     var tapAnalog =
    //         Outputs.Items.FirstOrDefault(
    //             s => s is {Enabled: true, Input: Gh5NeckInput {Input: Gh5NeckInputType.TapBar}});
    //     var tapFrets =
    //         Outputs.Items.FirstOrDefault(
    //             s => s is {Enabled: true, Input: Gh5NeckInput {Input: Gh5NeckInputType.TapAll}});
    //     if (tapAnalog == null && tapFrets == null) return Outputs.Items;
    //     var outputs = new List<Output>(Outputs.Items);
    //
    //     // Map Tap bar to Upper frets on RB guitars
    //     if (tapAnalog != null && Model.DeviceControllerType is DeviceControllerType.RockBandGuitar)
    //     {
    //         outputs.AddRange(TapsRb.Select(pair => new GuitarButton(Model, tapAnalog.Enabled,
    //             new Gh5NeckInput(pair.Key, Model, Peripheral, Sda, Scl, true), Colors.Black, Colors.Black,
    //             [],[], [], 5,
    //             pair.Value, false, false ,false, -1, true)));
    //
    //         outputs.Remove(tapAnalog);
    //     }
    //
    //     if (tapFrets == null) return outputs;
    //
    //     outputs.AddRange(Taps.Select(pair => new GuitarButton(Model, tapFrets.Enabled,
    //         new Gh5NeckInput(pair.Key, Model, Peripheral, Sda, Scl, true),
    //         Colors.Black, Colors.Black, [], [],[], 5, pair.Value, false, false ,false, -1, true)));
    //
    //     outputs.Remove(tapFrets);
    //
    //     return outputs;
    // }

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
        ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw, ReadOnlySpan<byte> mpr121Raw,
        ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType,
            wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw, digitalPeripheral, cloneRaw, adxlRaw, mpr121Raw, midiRaw, bluetoothInputsRaw);
        Detected = !gh5Raw.IsEmpty;
    }

    // public override void UpdateBindings()
    // {
    //     var axisController = Outputs.Items.FirstOrDefault(s => s is ControllerAxis);
    //     var axisGuitar = Outputs.Items.FirstOrDefault(s => s is GuitarAxis);
    //     var tapAll = Outputs.Items.FirstOrDefault(s => s is OutputButton);
    //     if (Model.DeviceControllerType.Is5FretGuitar())
    //     {
    //         if (tapAll == null)
    //         {
    //             var button = new GuitarButton(Model, true,
    //                 new Gh5NeckInput(Gh5NeckInputType.TapAll, Model, Peripheral, combined: true), Colors.Black,
    //                 Colors.Black, [], [],[], 5, InstrumentButtonType.Slider, false, false ,false, -1, true)
    //             {
    //                 Enabled = false
    //             };
    //             Outputs.Add(button);
    //         }
    //
    //         foreach (var (key, value) in Standard)
    //         {
    //             var item = Outputs.Items.FirstOrDefault(s => s.Input is Gh5NeckInput gh5 && gh5.Input == key);
    //             if (item != null) continue;
    //             var button = new GuitarButton(Model, true,
    //                 new Gh5NeckInput(key, Model, Peripheral, combined: true), Colors.Black,
    //                 Colors.Black, [], [],[], 5, value, false, false ,false, -1, true)
    //             {
    //                 Enabled = false
    //             };
    //             Outputs.Add(button);
    //         }
    //
    //         if (axisController == null) return;
    //         Outputs.Remove(axisController);
    //         Outputs.Add(new GuitarAxis(Model, true,
    //             new Gh5NeckInput(Gh5NeckInputType.TapBar, Model, Peripheral, Sda, Scl,
    //                 true),
    //             Colors.Black,
    //             Colors.Black, [],[], [], short.MinValue, short.MaxValue, 0,
    //             false, GuitarAxisType.Slider, false, false ,false, -1, true));
    //     }
    //     else if (Model.DeviceControllerType == DeviceControllerType.Gamepad)
    //     {
    //         var toRemove = new List<Output>();
    //         foreach (var key in Standard.Keys)
    //         {
    //             var item = Outputs.Items.FirstOrDefault(s => s.Input is Gh5NeckInput gh5 && gh5.Input == key);
    //             if (item != null)
    //             {
    //                 toRemove.Add(item);
    //             }
    //         }
    //
    //         Outputs.RemoveMany(toRemove);
    //         if (tapAll != null) Outputs.Remove(tapAll);
    //         if (axisGuitar != null)
    //         {
    //             Outputs.Remove(axisGuitar);
    //         }
    //
    //         if (axisController != null) return;
    //
    //         Outputs.Add(new ControllerAxis(Model, true,
    //             new Gh5NeckInput(Gh5NeckInputType.TapBar, Model, Peripheral, Sda, Scl,
    //                 true),
    //             Colors.Black,
    //             Colors.Black, [], [],[], short.MinValue, short.MaxValue, 0,0,
    //             ushort.MaxValue, StandardAxisType.LeftStickX, false, false ,false, -1, true));
    //     }
    //     else
    //     {
    //         var toRemove = new List<Output>();
    //         foreach (var key in Standard.Keys)
    //         {
    //             var item = Outputs.Items.FirstOrDefault(s => s.Input is Gh5NeckInput gh5 && gh5.Input == key);
    //             if (item != null)
    //             {
    //                 toRemove.Add(item);
    //             }
    //         }
    //
    //         Outputs.RemoveMany(toRemove);
    //         if (tapAll != null) Outputs.Remove(tapAll);
    //
    //         if (axisGuitar != null)
    //         {
    //             Outputs.Remove(axisGuitar);
    //         }
    //
    //         if (axisController != null)
    //         {
    //             Outputs.Remove(axisController);
    //         }
    //     }
    // }
}