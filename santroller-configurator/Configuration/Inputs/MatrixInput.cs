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
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class MatrixInput : InputWithPin
{
    // Matrix rows are idle high and pulled low while being scanned, so the init code needs to be able to find them
    public const string MatrixOutputType = "matrix-output";

    public MatrixInput(int pin, int outPin, bool invert, ConfigViewModel model) : base(
        model, new DirectPinConfig(model, Guid.NewGuid().ToString(), pin, false, DevicePinMode.PullUp))
    {
        Inverted = invert;
        IsAnalog = false;
        OutPinConfig = new DirectPinConfig(model, $"{MatrixOutputType}-{Guid.NewGuid()}", outPin, false,
            DevicePinMode.Output);
    }

    public DirectPinConfig OutPinConfig { get; }

    public override IList<PinConfig> PinConfigs => new List<PinConfig> {PinConfig, OutPinConfig};

    public override bool IsUint => true;

    [Reactive] public bool Inverted { get; set; }

    public override InputType? InputType => Types.InputType.MatrixInput;

    protected override string DetectionText => Resources.DetectButton;

    public override IList<DevicePin> Pins => new List<DevicePin>
    {
        new(Pin, PinMode),
        new(OutPin, OutPinConfig.PinMode)
    };

    public int OutPin
    {
        get => OutPinConfig.Pin;
        set
        {
            OutPinConfig.Pin = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(PinConfigs));
        }
    }

    public override string Title => "Matrix";

    public override SerializedInput Serialise()
    {
        return new SerializedMatrixInput(PinConfig.Pin, OutPinConfig.Pin, Inverted);
    }

    public override string Generate(BinaryWriter? writer)
    {
        return Inverted ? $"matrix_read({Pin}, {OutPin})" : $"!matrix_read({Pin}, {OutPin})";
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        return string.Join("\n", bindings.Select(binding => binding.Item2));
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[] {"INPUT_DIRECT"};
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw, ReadOnlySpan<byte> djRightRaw,
        ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw, ReadOnlySpan<byte> ps2ControllerType,
        ReadOnlySpan<byte> wiiControllerType, ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw,
        ReadOnlySpan<byte> peripheralWtRaw, Dictionary<int, bool> digitalPeripheral,
        ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw, ReadOnlySpan<byte> mpr121Raw,
        ReadOnlySpan<byte> midiRaw)
    {
        if (Model.Device is not Santroller santroller || Pin == -1 || OutPin == -1) return;
        // Columns are pulled up, so a pressed button reads low
        var pressed = !santroller.MatrixRead(Pin, OutPin);
        if (Inverted) pressed = !pressed;
        RawValue = pressed ? 1 : 0;
    }
}
