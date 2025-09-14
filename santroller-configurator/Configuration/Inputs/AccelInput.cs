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

    public override bool IsUint => Input is AccelInputType.Adc0 or AccelInputType.Adc1 or AccelInputType.Adc2;
    public override IList<DevicePin> Pins => Array.Empty<DevicePin>();

    public override string Title => EnumToStringConverter.Convert(Input);

    public override IReadOnlyList<string> RequiredDefines()
    {
        return [];
    }

    public override string Generate()
    {
        return Input switch
        {
            AccelInputType.AccelX => "filtered[0]",
            AccelInputType.AccelY => "filtered[1]",
            AccelInputType.AccelZ => "filtered[2]",
            AccelInputType.Adc0 => "accel_adc[0]",
            AccelInputType.Adc1 => "accel_adc[1]",
            AccelInputType.Adc2 => "accel_adc[2]",
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
        ReadOnlySpan<byte> mpr121Raw, ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw,
        bool peripheralConnected, byte[] crkdRaw)
    {
        if (adxlRaw.IsEmpty || adxlRaw.Length < 6) return;
        var data = adxlRaw.ToArray();
        RawValue = Input switch
        {
            AccelInputType.AccelX => BitConverter.ToInt16(data, 0),
            AccelInputType.AccelY => BitConverter.ToInt16(data, 2),
            AccelInputType.AccelZ => BitConverter.ToInt16(data, 4),
            _ => RawValue
        };
        if (adxlRaw.Length > 6)
        {
            RawValue = Input switch
            {
                AccelInputType.Adc0 => BitConverter.ToUInt16(data, 6),
                AccelInputType.Adc1 => BitConverter.ToUInt16(data, 8),
                AccelInputType.Adc2 => BitConverter.ToUInt16(data, 10),
                _ => RawValue
            };
        }
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        return string.Join("\n", bindings.Select(binding => binding.Item2));
    }
}