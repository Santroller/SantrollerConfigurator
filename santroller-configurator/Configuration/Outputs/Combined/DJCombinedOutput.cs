using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public class DjCombinedOutput : CombinedTwiOutput
{
    public DjCombinedOutput(ConfigViewModel model, bool peripheral, int sda = -1, int scl = -1) :
        base(model, DjInput.DjTwiType, peripheral, DjInput.DjTwiFreq, "DJ", sda, scl)
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
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
        this.WhenAnyValue(x => x.Model.DjPollRate).Subscribe(_ => this.RaisePropertyChanged(nameof(PollRate)));
    }
    public int PollRate
    {
        get => Model.DjPollRate;
        set => Model.DjPollRate = value;
    }
    [Reactive] public bool DetectedLeft { get; set; }

    [Reactive] public bool DetectedRight { get; set; }

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
        return Resources.DjCombinedTitle;
    }

    public override Enum GetOutputType()
    {
        return SimpleType.DjTurntableSimple;
    }

    public void CreateDefaults()
    {
        Outputs.Clear();

        Outputs.AddRange(DjInputTypes.Where(s => s is not (DjInputType.LeftTurntable or DjInputType.RightTurntable))
            .Select(button => new DjButton(Model,
                new DjInput(button, Model, Peripheral, true, combined: true),
                Colors.Black, Colors.Black, Array.Empty<byte>(), 5, button, true)));
        Outputs.Add(new DjAxis(Model, new DjInput(DjInputType.LeftTurntable, Model, Peripheral, true, combined: true),
            Colors.Black,
            Colors.Black, Array.Empty<byte>(), 1, DjAxisType.LeftTableVelocity, true));
        Outputs.Add(new DjAxis(Model, new DjInput(DjInputType.RightTurntable, Model, Peripheral, true, combined: true),
            Colors.Black,
            Colors.Black, Array.Empty<byte>(), 1, DjAxisType.RightTableVelocity, true));
    }

    public override void UpdateBindings()
    {
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedDjCombinedOutput(Peripheral, Sda, Scl, Outputs.Items.ToList());
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> bluetoothRaw, ReadOnlySpan<byte> usbHostInputsRaw,
        ReadOnlySpan<byte> peripheralWtRaw, Dictionary<int, bool> digitalPeripheral,
        ReadOnlySpan<byte> cloneRaw)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType,
            wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw, digitalPeripheral, cloneRaw);
        DetectedLeft = !djLeftRaw.IsEmpty;
        DetectedRight = !djRightRaw.IsEmpty;
    }
}