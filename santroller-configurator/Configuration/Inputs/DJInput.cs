using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public partial class DjInput : TwiInput
{
    public static readonly string DjTwiType = "dj";
    public static readonly int DjTwiFreq = 150000;

    public DjInput(DjInputType input, ConfigViewModel model, bool peripheral,  bool smoothing, int sda = -1,
        int scl = -1, bool combined = false) : base(
        DjTwiType, DjTwiFreq, peripheral, sda, scl, model)
    {
        Smoothing = smoothing;
        Combined = combined;
        BindableTwi = !combined && Model.Microcontroller.TwiAssignable && !model.Branded;
        Input = input;
        IsAnalog = Input <= DjInputType.RightTurntable;
        this.WhenAnyValue(x => x.Model.DjPollRate).Subscribe(_ => this.RaisePropertyChanged(nameof(PollRate)));
        this.WhenAnyValue(x => x.Model.DjDual).Subscribe(_ => this.RaisePropertyChanged(nameof(Dual)));
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

    public bool Dual
    {
        get => Model.DjDual;
        set => Model.DjDual = value;
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
                return "(dj_turntable_left)";
            case DjInputType.RightTurntable:
                return Dual ? "" : "(dj_turntable_right)";
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
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw)
    {
        switch (Input)
        {
            case DjInputType.LeftTurntable when !djLeftRaw.IsEmpty:
                RawValue = (sbyte) djLeftRaw[2];
                break;
            case DjInputType.RightTurntable when !djRightRaw.IsEmpty:
                RawValue = (sbyte) djRightRaw[2];
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
            or ConfigField.Xbox360
            or ConfigField.Ps4 or ConfigField.Universal))
            return "";
        var left = string.Join(";",
            bindings.Where(binding => (!Dual || (binding.Item1 as DjInput)!.Input != DjInputType.LeftTurntable) &&
                                      (binding.Item1 as DjInput)!.Input.ToString().Contains("Left"))
                .Select(binding => binding.Item2));
        var right = string.Join(";",
            bindings.Where(binding => (!Dual || (binding.Item1 as DjInput)!.Input != DjInputType.RightTurntable) &&
                                      (binding.Item1 as DjInput)!.Input.ToString().Contains("Right"))
                .Select(binding => binding.Item2));
        var dual = "";
        if (Dual)
        {
            dual = bindings.Where(binding => (binding.Item1 as DjInput)!.Input == DjInputType.LeftTurntable)
                .Select(binding => binding.Item2).FirstOrDefault("");
        }

        return $$"""
                 if (djLeftValid) {
                     {{left}}
                 }
                 if (djRightValid) {
                     {{right}}
                 }
                 {{dual}}
                 """;
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return new List<string>(base.RequiredDefines()) {"INPUT_DJ_TURNTABLE"};
    }

    public override SerializedInput Serialise()
    {
        if (Combined) return new SerializedDjInputCombined(Input, Peripheral);

        return new SerializedDjInput(Peripheral, Sda, Scl, Input, Smoothing);
    }
}