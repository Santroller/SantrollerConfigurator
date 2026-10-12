using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class WtDrumInput : SpiInput
{
    public static readonly string WtDrumSpiType = "wt_drum";
    public static readonly string WtDrumCsType = "wt_drum_cs";
    public static readonly uint WtDrumSpiFreq = 500000;
    public static readonly bool WtDrumSpiCpol = false;
    public static readonly bool WtDrumSpiCpha = true;
    public static readonly bool WtDrumSpiMsbFirst = true;

    private readonly DirectPinConfig _csConfig;

    public WtDrumInput(DrumBrainInputType input, ConfigViewModel model, int miso = -1, int mosi = -1, int sck = -1,
        int cs = -1) : base(WtDrumSpiType, false, WtDrumSpiFreq, WtDrumSpiCpol, WtDrumSpiCpha, WtDrumSpiMsbFirst,
        model, miso, mosi, sck)
    {
        Input = input;
        IsAnalog = true;
        _csConfig = Model.GetPinForType(WtDrumCsType, false, cs, DevicePinMode.Output);
        this.WhenAnyValue(x => x._csConfig.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Cs)));
    }

    public int Cs
    {
        get => _csConfig.Pin;
        set => _csConfig.Pin = value;
    }

    public DrumBrainInputType Input { get; }

    public override InputType? InputType => Types.InputType.WtDrumInput;
    public override IList<DevicePin> Pins => new List<DevicePin> {new(Cs, DevicePinMode.Output)};
    public override IList<PinConfig> PinConfigs => base.PinConfigs.Concat(new List<PinConfig> {_csConfig}).ToList();
    public override bool IsUint => true;
    public override string Title => EnumToStringConverter.Convert(Input);

    public override string Generate(BinaryWriter? writer)
    {
        return DrumBrain.Generate(Input);
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        var defines = base.RequiredDefines().ToList();
        defines.Add($"WT_DRUM_CS_SET() {Model.Microcontroller.GenerateDigitalWrite(Cs, true, false)}");
        defines.Add($"WT_DRUM_CS_CLEAR() {Model.Microcontroller.GenerateDigitalWrite(Cs, false, false)}");
        return defines;
    }

    public override SerializedInput Serialise()
    {
        return new SerializedWtDrumInput(Miso, Mosi, Sck, Cs, Input);
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw,
        ReadOnlySpan<byte> mpr121Raw, ReadOnlySpan<byte> midiRaw)
    {
        RawValue = DrumBrain.Read(Model, Input) ?? RawValue;
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings, ConfigField mode)
    {
        return string.Join("\n", bindings.Select(binding => binding.Item2));
    }
}
