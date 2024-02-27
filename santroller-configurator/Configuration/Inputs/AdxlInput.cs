using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using static GuitarConfigurator.NetCore.Configuration.Outputs.Combined.WiiCombinedOutput;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class AdxlInput : TwiInput
{
    public static readonly string AdxlTwiType = "adxl";
    public static readonly int AdxlTwiFreq = 400000;

    public AdxlInput(AdxlInputType input, ConfigViewModel model, bool peripheral, int sda = -1,
        int scl = -1,
        bool combined = false) : base(
        AdxlTwiType, AdxlTwiFreq, peripheral, sda, scl, model)
    {
        Input = input;
        Combined = combined;
        BindableTwi = !combined && Model.Microcontroller.TwiAssignable && !model.Branded;
        IsAnalog = true;
    }

    public AdxlInputType Input { get; }

    public bool Combined { get; }

    public bool BindableTwi { get; }

    public override InputType? InputType => Types.InputType.AdxlInput;

    public override bool IsUint => false;
    public override IList<DevicePin> Pins => Array.Empty<DevicePin>();

    public override string Title => EnumToStringConverter.Convert(Input);

    public override string Generate()
    {
        return Input switch
        {
            AdxlInputType.AccelX => "filtered[0]",
            AdxlInputType.AccelY => "filtered[1]",
            AdxlInputType.AccelZ => "filtered[2]",
            _ => "filtered[0]"
        };
    }

    public override SerializedInput Serialise()
    {
        return new SerializedAdxlInput(Sda, Scl, Input, Peripheral);
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiData, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw)
    {
        if (adxlRaw.IsEmpty || adxlRaw.Length < 6) return;
        RawValue = Input switch
        {
            AdxlInputType.AccelX => BitConverter.ToInt16(adxlRaw.ToArray(), 0),
            AdxlInputType.AccelY => BitConverter.ToInt16(adxlRaw.ToArray(), 2),
            AdxlInputType.AccelZ => BitConverter.ToInt16(adxlRaw.ToArray(), 4),
            _ => RawValue
        };
    }
    
    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        return string.Join("\n", bindings.Select(binding => binding.Item2));
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return base.RequiredDefines().Concat(new[] {"INPUT_ADXL"}).ToList();
    }
}