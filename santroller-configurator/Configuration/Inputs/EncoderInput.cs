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

public partial class EncoderInput : InputWithPin
{
    public EncoderInput(int pin, bool peripheral, ConfigViewModel model) : base(
        model, new DirectPinConfig(model, Guid.NewGuid().ToString(), pin, peripheral, DevicePinMode.Floating))
    {
        IsAnalog = PinConfig.PinMode == DevicePinMode.Analog;
    }

    public override bool IsUint => true;

    [Reactive] private bool _inverted;

    public override InputType? InputType => Types.InputType.EncoderInput;

    protected override string DetectionText => Resources.DetectAxis;

    public override IList<DevicePin> Pins => new List<DevicePin>
    {
        new(Pin, PinMode), new(Pin+1, PinMode)
    };

    public override string Title => "Encoder";

    public override SerializedInput Serialise()
    {
        return new SerializedEncoderInput(PinConfig.Peripheral, PinConfig.Pin);
    }

    public override string Generate()
    {
        if (Peripheral)
        {
            return "slaveReadQuad()";
        }

        return "delta";
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        return string.Join("\n", bindings.Select(binding => binding.Item2));
    }


    public override IReadOnlyList<string> RequiredDefines()
    {
        if (Peripheral)
        {
            
            return ["INPUT_QUAD_SLAVE "+Pin];
        }
        return ["INPUT_QUAD "+Pin];
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw, ReadOnlySpan<byte> djRightRaw,
        ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw, ReadOnlySpan<byte> ps2ControllerType,
        ReadOnlySpan<byte> wiiControllerType, ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw,
        ReadOnlySpan<byte> peripheralWtRaw, Dictionary<int, bool> digitalPeripheral,
        ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw, ReadOnlySpan<byte> mpr121Raw,
        ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw, bool peripheralConnected, byte[] crkdRaw)
    {
        var dRaw = digitalRaw;
        if (Peripheral)
        {
            if (!peripheralConnected)
            {
                RawValue = 0;
                return;
            }
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