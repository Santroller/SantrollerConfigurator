using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
        _lastInputs.dpadLeftRight = 0x80;
        _lastInputs.dpadUpDown = 0x80;
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

    private CrkdNeckInputs _lastInputs = new();
    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw,
        ReadOnlySpan<byte> mpr121Raw, ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw,
        bool peripheralConnected, byte[] crkdRaw)
    {
        if (crkdRaw.Length != 0)
        {
            _lastInputs = StructTools.RawDeserialize<CrkdNeckInputs>(crkdRaw, 0);
        }

        switch (Input)
        {
            case CrkdNeckInputType.Green:
                RawValue = (_lastInputs.buttons & 1 << 0) != 0 ? 1 : 0;
                break;
            case CrkdNeckInputType.Red:
                RawValue = (_lastInputs.buttons & 1 << 1) != 0 ? 1 : 0;
                break;
            case CrkdNeckInputType.Yellow:
                RawValue = (_lastInputs.buttons & 1 << 2) != 0 ? 1 : 0;
                break;
            case CrkdNeckInputType.Blue:
                RawValue = (_lastInputs.buttons & 1 << 3) != 0 ? 1 : 0;
                break;
            case CrkdNeckInputType.Orange:
                RawValue = (_lastInputs.buttons & 1 << 4) != 0 ? 1 : 0;
                break;
            case CrkdNeckInputType.DpadLeft:
                RawValue = _lastInputs.dpadLeftRight == 0xFF ? 1 : 0;
                break;
            case CrkdNeckInputType.DpadRight:
                RawValue = _lastInputs.dpadLeftRight == 0x00 ? 1 : 0;
                break;
            case CrkdNeckInputType.DpadUp:
                RawValue = _lastInputs.dpadUpDown == 0x00 ? 1 : 0;
                break;
            case CrkdNeckInputType.DpadDown:
                RawValue = _lastInputs.dpadUpDown == 0xFF ? 1 : 0;
                break;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CrkdNeckInputs
    {
        public readonly byte header; // {0xA5, 0x01};, though we are eating the first header byte
        public readonly byte len; // 0x0C;
        public readonly ushort padding; //{0x00, 0x00};
        public readonly byte buttons;
        public byte dpadUpDown; // none: 0x80, up: 0x00, down: 0xFF
        public byte dpadLeftRight; // none: 0x80, right: 0x00, left: 0xFF
        public readonly byte footer1; // 0x00
        public readonly byte footer2; // 0x01
        public readonly byte footer3; // 0x15
        public readonly byte crc; // CRC-8/MAXIM-DOW
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