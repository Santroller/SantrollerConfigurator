using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class DirectInput : InputWithPin
{
    public DirectInput(int pin, bool invert, bool peripheral, DevicePinMode pinMode, ConfigViewModel model) : base(
        model, new DirectPinConfig(model, Guid.NewGuid().ToString(), pin, peripheral, pinMode))
    {
        Inverted = invert;
        IsAnalog = PinConfig.PinMode == DevicePinMode.Analog;
    }


    public IEnumerable<DevicePinMode> DevicePinModes => GetPinModes();

    public override bool IsUint => true;

    [Reactive] public bool Inverted { get; set; }

    public override InputType? InputType => Peripheral ? IsAnalog ? Types.InputType.AnalogPeripheralInput : Types.InputType.DigitalPeripheralInput : IsAnalog ? Types.InputType.AnalogPinInput : Types.InputType.DigitalPinInput;

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
        return Model.Microcontroller.Board.IsAvr()
            ? modes.Where(mode => mode is not (DevicePinMode.BusKeep or DevicePinMode.PullDown))
            : modes;
    }

    public override SerializedInput Serialise()
    {
        return new SerializedDirectInput(PinConfig.Pin, PinConfig.Peripheral, Inverted, PinConfig.PinMode);
    }

    public override string Generate()
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
        ReadOnlySpan<byte> peripheralWtRaw, Dictionary<int, bool> digitalPeripheral, Dictionary<int, int> analogPeripheral)
    {
        if (IsAnalog)
        {
            RawValue = analogRaw.GetValueOrDefault(Pin, 0);
        }
        else
        {
            // Pullups mean low is a logical high, which is inherently an invert
            var invert = PinMode == DevicePinMode.PullUp;
            if (Inverted) invert = !invert;
            RawValue = digitalRaw.GetValueOrDefault(Pin, invert) switch
            {
                true when invert => 0,
                false when invert => 1,
                true => 1,
                false => 0
            };
        }
    }
}