using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class BhDrumInput : TwiInput
{
    public static readonly string BhDrumTwiType = "bh_drum";
    public static readonly int BhDrumTwiFreq = 100000;

    public BhDrumInput(DrumBrainInputType input, ConfigViewModel model, int sda = -1, int scl = -1) : base(
        BhDrumTwiType, BhDrumTwiFreq, false, sda, scl, model)
    {
        Input = input;
        IsAnalog = true;
    }

    public DrumBrainInputType Input { get; }

    public override InputType? InputType => Types.InputType.BhDrumInput;
    public override IList<DevicePin> Pins => Array.Empty<DevicePin>();
    public override bool IsUint => true;
    public override string Title => EnumToStringConverter.Convert(Input);

    public override string Generate(BinaryWriter? writer)
    {
        return DrumBrain.Generate(Input);
    }

    public override SerializedInput Serialise()
    {
        return new SerializedBhDrumInput(Sda, Scl, Input);
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
