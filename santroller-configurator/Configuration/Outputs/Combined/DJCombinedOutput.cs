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
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public partial class DjCombinedOutput : CombinedTwiOutput
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
        Outputs.Connect().Filter(x => x is OutputButton or {Input.IsAnalog:false})
            .Bind(out var allDigitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
        AllDigitalOutputs = allDigitalOutputs;
        this.WhenAnyValue(x => x.Model.DjPollRate).Subscribe(_ => this.RaisePropertyChanged(nameof(PollRate)));
    }
    public int PollRate
    {
        get => Model.DjPollRate;
        set => Model.DjPollRate = value;
    }

    [Reactive] private bool _detectedLeft;

    [Reactive] private bool _detectedRight;

    public override void SetOutputsOrDefaults(IEnumerable<Output> outputs)
    {
        Outputs.Clear();
        Outputs.AddRange(outputs);
        if (Outputs.Count == 0)
        {
            CreateDefaults();
        }
    }

    public override HostInput MakeInput(UsbHostInputType type)
    {
        return new DjInput(type, Model, Peripheral, Sda, Scl, IsCombined);
    }


    public override HostInput? MakeInput(ProKeyType type)
    {
        return null;
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

    public override void CreateDefaults()
    {
        Outputs.Clear();

        Outputs.AddRange(Enum.GetValues<DjInputType>().Where(s => s is not (DjInputType.LeftTurntable or DjInputType.RightTurntable))
            .Select(button => new DjButton(Model,true,
                new DjInput(Mappings[button], Model, Peripheral, combined: true),
                Colors.Black, Colors.Black, [], [], [], 5, button, false, false ,false, -1, true)));
        Outputs.Add(new DjAxis(Model,true, new DjInput(UsbHostInputType.LeftTableVelocity, Model, Peripheral, combined: true),
            Colors.Black,
            Colors.Black, [], [], [], 1, 1,DjAxisType.LeftTableVelocity, false, false ,false, -1, true));
        Outputs.Add(new DjAxis(Model, true,new DjInput(UsbHostInputType.RightTableVelocity, Model, Peripheral, combined: true),
            Colors.Black,
            Colors.Black, [], [], [], 1, 1,DjAxisType.RightTableVelocity, false, false ,false, -1, true));
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
        ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw, ReadOnlySpan<byte> mpr121Raw,
        ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType,
            wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw, digitalPeripheral, cloneRaw, adxlRaw, mpr121Raw, midiRaw, bluetoothInputsRaw);
        DetectedLeft = !djLeftRaw.IsEmpty;
        DetectedRight = !djRightRaw.IsEmpty;
    }
}