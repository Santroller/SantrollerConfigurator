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

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public partial class MatrixInput : InputWithPin
{
    public MatrixInput(int pin, int outPin, bool invert, ConfigViewModel model) : base(
        model, new DirectPinConfig(model, Guid.NewGuid().ToString(), pin, false, DevicePinMode.PullUp))
    {
        Inverted = invert;
        IsAnalog = false;
        OutPinConfig = new DirectPinConfig(model, Guid.NewGuid().ToString(), outPin, false, DevicePinMode.Output);
    }
    public DirectPinConfig OutPinConfig { get; }

    public IEnumerable<DevicePinMode> DevicePinModes => GetPinModes();
    
    
    public override IList<PinConfig> PinConfigs => new List<PinConfig> {PinConfig, OutPinConfig};

    public override bool IsUint => true;

    [Reactive] private bool _inverted;

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

    private IEnumerable<DevicePinMode> GetPinModes()
    {
        var modes = Enum.GetValues<DevicePinMode>()
            .Where(mode => mode is not (DevicePinMode.Output or DevicePinMode.Analog or DevicePinMode.Skip));
        return Model.Microcontroller.Board.IsAvr
            ? modes.Where(mode => mode is not (DevicePinMode.BusKeep or DevicePinMode.PullDown))
            : modes;
    }

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
        return ["INPUT_DIRECT"];
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw, ReadOnlySpan<byte> djRightRaw,
        ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw, ReadOnlySpan<byte> ps2ControllerType,
        ReadOnlySpan<byte> wiiControllerType, ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw,
        ReadOnlySpan<byte> peripheralWtRaw, Dictionary<int, bool> digitalPeripheral,
        ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw, ReadOnlySpan<byte> mpr121Raw,
        ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw, bool peripheralConnected)
    {
        if (Model.Device is not Santroller santroller)
        {
            return;
        }
        // Pullups mean low is a logical high, which is inherently an invert
        var invert = PinMode == DevicePinMode.PullUp;
        if (Inverted) invert = !invert;
        RawValue = santroller.MatrixRead(Pin, OutPin) switch
        {
            true when invert => 0,
            false when invert => 1,
            true => 1,
            false => 0
        };
    }
}