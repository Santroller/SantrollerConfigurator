using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public partial class DjInput : TwiInput
{
    public static readonly string DjTwiType = "dj";
    public static readonly int DjTwiFreq = 150000;

    public DjInput(DjInputType input, ConfigViewModel model, bool peripheral, int sda = -1,
        int scl = -1, bool combined = false) : base(
        DjTwiType, DjTwiFreq, peripheral, sda, scl, model)
    {
        Smoothing = model.DjSmoothing;
        Combined = combined;
        BindableTwi = !combined && Model.Microcontroller.TwiAssignable && !model.Branded;
        Input = input;
        IsAnalog = Input <= DjInputType.RightTurntable;
        this.WhenAnyValue(x => x.Model.DjPollRate).Subscribe(_ => this.RaisePropertyChanged(nameof(PollRate)));
        this.WhenAnyValue(x => x.Model.DjSmoothing).Subscribe(_ => this.RaisePropertyChanged(nameof(Smoothing)));
    }

    public int PollRate
    {
        get => Model.DjPollRate;
        set => Model.DjPollRate = value;
    }

    public bool Combined { get; }

    public bool Smoothing
    {
        get => Model.DjSmoothing;
        set => Model.DjSmoothing = value;
    }

    public bool BindableTwi { get; }

    public DjInputType Input { get; set; }
    public override InputType? InputType => Types.InputType.TurntableInput;

    public override IList<DevicePin> Pins => Array.Empty<DevicePin>();
    public override bool IsUint => false;
    public override string Title => EnumToStringConverter.Convert(Input);

    public override string Generate()
    {
        switch (Input)
        {
            case DjInputType.LeftTurntable:
                return "(dj_turntable_left << 9)";
            case DjInputType.RightTurntable:
                return "(dj_turntable_right << 9)";
            case DjInputType.LeftBlue:
            case DjInputType.LeftGreen:
            case DjInputType.LeftRed:
                return $"(dj_left[0] & {1 << ((byte) Input - (byte) DjInputType.LeftGreen + 4)})";
            case DjInputType.RightGreen:
            case DjInputType.RightRed:
            case DjInputType.RightBlue:
                return $"(dj_right[0] & {1 << ((byte) Input - (byte) DjInputType.RightGreen + 4)})";
        }

        throw new InvalidOperationException("Shouldn't get here!");
    }

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
        switch (Input)
        {
            case DjInputType.LeftTurntable when !djLeftRaw.IsEmpty:
                RawValue = (sbyte) djLeftRaw[2] << 9;
                break;
            case DjInputType.RightTurntable when !djRightRaw.IsEmpty:
                RawValue = (sbyte) djRightRaw[2] << 9;
                break;
            case DjInputType.LeftBlue when !djLeftRaw.IsEmpty:
            case DjInputType.LeftGreen when !djLeftRaw.IsEmpty:
            case DjInputType.LeftRed when !djLeftRaw.IsEmpty:
                RawValue = (djLeftRaw[0] & (1 << ((byte) Input - (byte) DjInputType.LeftGreen + 4))) != 0 ? 1 : 0;
                break;
            case DjInputType.RightGreen when !djRightRaw.IsEmpty:
            case DjInputType.RightRed when !djRightRaw.IsEmpty:
            case DjInputType.RightBlue when !djRightRaw.IsEmpty:
                RawValue = (djRightRaw[0] & (1 << ((byte) Input - (byte) DjInputType.RightGreen + 4))) != 0 ? 1 : 0;
                break;
        }
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        if (mode is not (ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Shared or ConfigField.XboxOne
            or ConfigField.Xbox360 or ConfigField.Xbox
            or ConfigField.Ps4 or ConfigField.Universal or ConfigField.Mouse or ConfigField.Keyboard
            or ConfigField.Consumer))
            return "";
        var left = string.Join(";",
            bindings.Where(binding => binding.Item1 is DjInput
                {
                    Input: DjInputType.LeftTurntable or DjInputType.LeftGreen or DjInputType.LeftRed
                    or DjInputType.LeftBlue
                })
                .Select(binding => binding.Item2));
        var right = string.Join(";",
            bindings.Where(binding => binding.Item1 is DjInput
                {
                    Input: DjInputType.RightTurntable or DjInputType.RightGreen or DjInputType.RightRed
                    or DjInputType.RightBlue
                })
                .Select(binding => binding.Item2));

        return $$"""
                 if (djLeftValid) {
                     {{left}}
                 }
                 if (djRightValid) {
                     {{right}}
                 }
                 """;
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return new List<string>(base.RequiredDefines()) {"INPUT_DJ_TURNTABLE"};
    }

    public override SerializedInput Serialise()
    {
        if (Combined) return new SerializedDjInputCombined(Input, Peripheral);

        return new SerializedDjInput(Peripheral, Sda, Scl, Input);
    }
}