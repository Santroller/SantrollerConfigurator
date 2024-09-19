using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using static GuitarConfigurator.NetCore.Configuration.Outputs.Combined.WiiCombinedOutput;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class AccelInput : Input
{

    public AccelInput(AccelInputType input, ConfigViewModel model, bool combined = false) : base(model)
    {
        Input = input;
        Combined = combined;
        BindableTwi = !combined && Model.Microcontroller.TwiAssignable && !model.Branded;
        IsAnalog = true;
    }
    public AccelInputType Input { get; }

    public bool Combined { get; }

    public bool BindableTwi { get; }

    public override IList<PinConfig> PinConfigs => [];
    public override InputType? InputType => Types.InputType.AccelInput;
    public override bool Peripheral => false;

    public override bool IsUint => false;
    public override IList<DevicePin> Pins => Array.Empty<DevicePin>();

    public override string Title => EnumToStringConverter.Convert(Input);

    public override IReadOnlyList<string> RequiredDefines()
    {
        return [];
    }

    public override string Generate(BinaryWriter? writer)
    {
        return Input switch
        {
            AccelInputType.AccelX => "filtered[0]",
            AccelInputType.AccelY => "filtered[1]",
            AccelInputType.AccelZ => "filtered[2]",
            _ => "filtered[0]"
        };
    }

    public override SerializedInput Serialise()
    {
        return new SerializedAccelInput(Input);
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiData, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw,
        ReadOnlySpan<byte> mpr121Raw, ReadOnlySpan<byte> midiRaw)
    {
        if (adxlRaw.IsEmpty || adxlRaw.Length < 6) return;
        RawValue = Input switch
        {
            AccelInputType.AccelX => BitConverter.ToInt16(adxlRaw.ToArray(), 0),
            AccelInputType.AccelY => BitConverter.ToInt16(adxlRaw.ToArray(), 2),
            AccelInputType.AccelZ => BitConverter.ToInt16(adxlRaw.ToArray(), 4),
            _ => RawValue
        };
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        return string.Join("\n", bindings.Select(binding => binding.Item2));
    }
}