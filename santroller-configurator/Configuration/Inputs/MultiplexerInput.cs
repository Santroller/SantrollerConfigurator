using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class MultiplexerInput : DirectInput
{
    private MultiplexerType _multiplexerType;

    public MultiplexerInput(int pin, bool peripheral, int channel, int s0, int s1, int s2, int s3, MultiplexerType multiplexerType,
        ConfigViewModel model) : base(
        pin, false, peripheral, DevicePinMode.Analog, model)
    {
        Channel = channel;
        MultiplexerType = multiplexerType;
        PinConfigS0 = new DirectPinConfig(model, Guid.NewGuid().ToString(), s0, peripheral, DevicePinMode.Output);
        PinConfigS1 = new DirectPinConfig(model, Guid.NewGuid().ToString(), s1, peripheral, DevicePinMode.Output);
        PinConfigS2 = new DirectPinConfig(model, Guid.NewGuid().ToString(), s2, peripheral, DevicePinMode.Output);
        // Only actually fully init the 4th pin for 16 channel multiplexers
        PinConfigS3 = new DirectPinConfig(model, Guid.NewGuid().ToString(), s3, peripheral, DevicePinMode.Output);
        this.WhenAnyValue(x => x.MultiplexerType).Select(s => s is MultiplexerType.SixteenChannel)
            .ToPropertyEx(this, x => x.IsSixteenChannel);
    }

    public DirectPinConfig PinConfigS0 { get; }
    public DirectPinConfig PinConfigS1 { get; }
    public DirectPinConfig PinConfigS2 { get; }
    public DirectPinConfig PinConfigS3 { get; }

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] public bool IsSixteenChannel { get; }

    [Reactive] public int Channel { get; set; }

    public MultiplexerType MultiplexerType
    {
        get => _multiplexerType;
        set
        {
            if (value == MultiplexerType.EightChannel && _multiplexerType == MultiplexerType.SixteenChannel)
                if (Channel > 7)
                    Channel = 7;

            this.RaiseAndSetIfChanged(ref _multiplexerType, value);
        }
    }


    public ReadOnlyObservableCollection<int> AvailableDigitalPins => Model.AvailablePinsDigital;
    public MultiplexerType[] MultiplexerTypes => Enum.GetValues<MultiplexerType>();

    public int PinS0
    {
        get => PinConfigS0.Pin;
        set
        {
            PinConfigS0.Pin = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(PinConfigs));
        }
    }

    public int PinS1
    {
        get => PinConfigS1.Pin;
        set
        {
            PinConfigS1.Pin = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(PinConfigs));
        }
    }

    public int PinS2
    {
        get => PinConfigS2.Pin;
        set
        {
            PinConfigS2.Pin = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(PinConfigs));
        }
    }

    public int PinS3
    {
        get => PinConfigS3.Pin;
        set
        {
            PinConfigS3.Pin = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(PinConfigs));
        }
    }

    public override InputType? InputType => Peripheral ? Types.InputType.MultiplexerPeripheralInput : Types.InputType.MultiplexerInput;

    public override IList<PinConfig> PinConfigs => MultiplexerType == MultiplexerType.SixteenChannel
        ? new List<PinConfig> {PinConfig, PinConfigS0, PinConfigS1, PinConfigS2, PinConfigS3}
        : new List<PinConfig> {PinConfig, PinConfigS0, PinConfigS1, PinConfigS2};

    public override string Generate()
    {
        // We put all bits at once, so generate a mask for the bits that are being modified
        // Then, get the bits representing a channel and if the bit is set, then set that pin in bits, so that it actually 
        // gets driven high.
        var mask = (1 << PinS0) | (1 << PinS1) | (1 << PinS2);
        var bits = 0;
        if ((Channel & (1 << 0)) != 0) bits |= 1 << PinS0;
        if ((Channel & (1 << 1)) != 0) bits |= 1 << PinS1;
        if ((Channel & (1 << 2)) != 0) bits |= 1 << PinS2;
        if (IsSixteenChannel)
        {
            mask |= 1 << PinS3;
            if ((Channel & (1 << 3)) != 0) bits |= 1 << PinS3;
        }

        if (Peripheral)
        {
            if (IsSixteenChannel)
            {
                return $"slaveReadMultiplexer({Model.Microcontroller.GetChannel(Pin, false)}, {Channel}, {PinS0}, {PinS1}, {PinS2}, {PinS3});";
            }
            return $"slaveReadMultiplexer({Model.Microcontroller.GetChannel(Pin, false)}, {Channel}, {PinS0}, {PinS1}, {PinS2});";
        }
        return $"multiplexer_read({Model.Microcontroller.GetChannel(Pin, false)}, {mask}, {bits});";
    }

    public override SerializedInput Serialise()
    {
        return new SerializedMultiplexerInput(Pin, PinConfigS0.Peripheral, PinS0, PinS1, PinS2, PinS3, MultiplexerType, Channel);
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        return string.Join("\n", bindings.Select(binding => binding.Item2));
    }

    //TODO: not really sure how best to do this?
    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw, ReadOnlySpan<byte> djRightRaw,
        ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw, ReadOnlySpan<byte> ps2ControllerType,
        ReadOnlySpan<byte> wiiControllerType, ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw,
        ReadOnlySpan<byte> peripheralWtRaw, Dictionary<int, bool> digitalPeripheral, Dictionary<int, int> analogPeripheral)
    {
        RawValue = analogRaw.GetValueOrDefault(Pin, 0);
    }
}