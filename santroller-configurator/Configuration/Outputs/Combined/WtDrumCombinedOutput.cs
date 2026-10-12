using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public class WtDrumCombinedOutput : CombinedSpiOutput
{
    private readonly DirectPinConfig _csConfig;

    public WtDrumCombinedOutput(ConfigViewModel model, int miso = -1, int mosi = -1, int sck = -1, int cs = -1) :
        base(model, WtDrumInput.WtDrumSpiType, false, WtDrumInput.WtDrumSpiFreq, WtDrumInput.WtDrumSpiCpol,
            WtDrumInput.WtDrumSpiCpha, WtDrumInput.WtDrumSpiMsbFirst, "WT Drum", miso, mosi, sck)
    {
        Outputs.Clear();
        _csConfig = Model.GetPinForType(WtDrumInput.WtDrumCsType, false, cs, DevicePinMode.Output);
        this.WhenAnyValue(x => x._csConfig.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Cs)));
        Outputs.Connect().Filter(x => x is OutputAxis)
            .Filter(s => s.IsVisible)
            .Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton)
            .Filter(s => s.IsVisible)
            .Bind(out var digitalOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton or {Input.IsAnalog: false})
            .Bind(out var allDigitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
        AllDigitalOutputs = allDigitalOutputs;
    }

    public int Cs
    {
        get => _csConfig.Pin;
        set => _csConfig.Pin = value;
    }

    [Reactive] public bool Detected { get; set; }

    public override void SetOutputsOrDefaults(IReadOnlyCollection<Output> outputs)
    {
        Outputs.Clear();
        if (outputs.Count != 0)
            Outputs.AddRange(outputs);
        else
            CreateDefaults();
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return Resources.WtDrumCombinedTitle;
    }

    public override Enum GetOutputType()
    {
        return SimpleType.WtDrumSimple;
    }

    private void CreateDefaults()
    {
        Outputs.Clear();
        foreach (var (input, axis) in DrumBrain.MappingsFor(Model.DeviceControllerType))
        {
            Outputs.Add(new DrumAxis(Model, new WtDrumInput(input, Model, Miso, Mosi, Sck, Cs), Colors.Black,
                Colors.Black, [], [], [], ushort.MaxValue / 2, ushort.MaxValue, 0, 0,
                axis, false, false, false, -1, true));
        }
    }

    public override void UpdateBindings()
    {
        CreateDefaults();
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedWtDrumCombinedOutput(Miso, Mosi, Sck, Cs, Outputs.Items.ToList());
    }

    protected override IEnumerable<PinConfig> GetOwnPinConfigs()
    {
        return base.GetOwnPinConfigs().Append(_csConfig);
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> bluetoothRaw, ReadOnlySpan<byte> usbHostInputsRaw,
        ReadOnlySpan<byte> peripheralWtRaw, Dictionary<int, bool> digitalPeripheral,
        ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw, ReadOnlySpan<byte> mpr121Raw,
        ReadOnlySpan<byte> midiRaw)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw,
            digitalPeripheral, cloneRaw, adxlRaw, mpr121Raw, midiRaw);
        Detected = DrumBrain.Detected(Model);
    }
}
