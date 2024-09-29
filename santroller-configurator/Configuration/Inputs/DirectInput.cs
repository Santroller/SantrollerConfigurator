using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public partial class DirectInput : InputWithPin
{
    public DirectInput(int pin, bool invert, bool peripheral, DevicePinMode pinMode, ConfigViewModel model) : base(
        model, new DirectPinConfig(model, Guid.NewGuid().ToString(), pin, peripheral, pinMode))
    {
        Inverted = invert;
        IsAnalog = PinConfig.PinMode == DevicePinMode.Analog;
    }


    public IEnumerable<DevicePinMode> DevicePinModes => GetPinModes();

    public override bool IsUint => true;

    [Reactive] private bool _inverted;

    public override InputType? InputType => IsAnalog ? Types.InputType.AnalogPinInput : Peripheral ? Types.InputType.DigitalPeripheralInput : Types.InputType.DigitalPinInput;

    protected override string DetectionText => IsAnalog ? Resources.DetectAxis : Resources.DetectButton;

    public override IList<DevicePin> Pins => new List<DevicePin>
    {
        new(Pin, PinMode)
    };

    public override string Title => "Direct";

    private IEnumerable<DevicePinMode> GetPinModes()
    {
        var modes = Enum.GetValues<DevicePinMode>()
            .Where(mode => mode is not (DevicePinMode.Output or DevicePinMode.Analog));
        return Model.Microcontroller.Board.IsAvr
            ? modes.Where(mode => mode is not (DevicePinMode.BusKeep or DevicePinMode.PullDown))
            : modes;
    }

    public override SerializedInput Serialise()
    {
        return new SerializedDirectInput(PinConfig.Pin, PinConfig.Peripheral, Inverted, PinConfig.PinMode);
    }

    public override string Generate(BinaryWriter? writer)
    {
        var invert = PinMode == DevicePinMode.PullUp;
        if (Inverted) invert = !invert;
        return IsAnalog
            ? Model.Microcontroller.GenerateAnalogRead(PinConfig.Pin, Model, Peripheral)
            : Model.Microcontroller.GenerateDigitalRead(PinConfig.Pin, invert, Peripheral);
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
        ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw)
    {
        var dRaw = digitalRaw;
        if (Peripheral)
        {
            dRaw = digitalPeripheral;
        }
        if (IsAnalog)
        {
            RawValue = analogRaw.GetValueOrDefault(Pin, 0);
        }
        else
        {
            // Pullups mean low is a logical high, which is inherently an invert
            var invert = PinMode == DevicePinMode.PullUp;
            if (Inverted) invert = !invert;
            RawValue = dRaw.GetValueOrDefault(Pin, invert) switch
            {
                true when invert => 0,
                false when invert => 1,
                true => 1,
                false => 0
            };
        }
    }
}