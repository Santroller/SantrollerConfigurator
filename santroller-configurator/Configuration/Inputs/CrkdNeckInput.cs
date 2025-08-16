using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class CrkdNeckInput : UartInput
{
    public static readonly string CrkdUartType = "crkd";
    public static readonly uint CrkdUartFreq = 460800;

    public CrkdNeckInput(CrkdNeckInputType input, ConfigViewModel model, bool peripheral, int tx = -1,
        int rx = -1, bool combined = false) : base(
        CrkdUartType, peripheral, tx, rx, CrkdUartFreq, model)
    {
        Combined = combined;
        BindableUart = !combined && Model.Microcontroller.UartAssignable && !model.Branded;
        Input = input;
        IsAnalog = false;
    }

    public override string Title => EnumToStringConverter.Convert(Input);
    public bool Combined { get; }
    public bool ShouldShowPins => !Combined && !Model.Branded;
    public bool BindableUart { get; }

    public override InputType? InputType => Types.InputType.CrkdNeckInput;
    public CrkdNeckInputType Input { get; set; }

    public override IList<DevicePin> Pins => Array.Empty<DevicePin>();
    public override bool IsUint => true;

    public override string Generate()
    {
        if (Input is CrkdNeckInputType.DpadDown)
        {
            return "lastCrkd.dpadUpDown == 0x00";
        }
        if (Input is CrkdNeckInputType.DpadUp)
        {
            return "lastCrkd.dpadUpDown == 0xFF";
        }
        if (Input is CrkdNeckInputType.DpadLeft)
        {
            return "lastCrkd.dpadLeftRight == 0xFF";
        }
        if (Input is CrkdNeckInputType.DpadRight)
        {
            return "lastCrkd.dpadLeftRight == 0x00";
        }
        return Output.GetReportField(Input, "lastCrkd", false);
    }

    public override SerializedInput Serialise()
    {
        if (Combined) return new SerializedCrkdNeckInputCombined(Input, Peripheral);
        return new SerializedCrkdNeckInput(Peripheral, Tx, Rx, Input);
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw,
        ReadOnlySpan<byte> mpr121Raw, ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw,
        bool peripheralConnected)
    {
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        return string.Join("\n", bindings.Select(binding => binding.Item2));
    }


    public override IReadOnlyList<string> RequiredDefines()
    {
        return base.RequiredDefines().Concat(["INPUT_CRKD_NECK"]).ToList();
    }
}