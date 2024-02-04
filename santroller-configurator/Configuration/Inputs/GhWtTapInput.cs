using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class GhWtTapInput : Input
{
    public static string GhWtAnalogPinType = "ghwt";
    public static string GhWtS0PinType = "ghwts0";
    public static string GhWtS1PinType = "ghwts1";
    public static string GhWtS2PinType = "ghwts2";

    private readonly Dictionary<BarButton, int> _channels = new()
    {
        {BarButton.Green, 1},
        {BarButton.Red, 0},
        {BarButton.Yellow, 2},
        {BarButton.Blue, 3},
        {BarButton.Orange, 4}
    };

    public static readonly Dictionary<GhWtInputType, int> ChannelsFromInput = new()
    {
        {GhWtInputType.TapGreen, 1},
        {GhWtInputType.TapRed, 0},
        {GhWtInputType.TapYellow, 2},
        {GhWtInputType.TapBlue, 3},
        {GhWtInputType.TapOrange, 4}
    };

    public GhWtTapInput(GhWtInputType input, ConfigViewModel model, bool peripheral, int pinInput, int pinS0, int pinS1,
        int pinS2,
        bool combined = false) : base(model)
    {
        ResetMin();
        Combined = combined;
        Input = input;
        IsAnalog = input is GhWtInputType.TapBar;
        PinConfigAnalog = Model.GetPinForType(GhWtAnalogPinType, peripheral, pinInput, DevicePinMode.PullUp);
        PinConfigS0 = Model.GetPinForType(GhWtS0PinType, peripheral, pinS0, DevicePinMode.Output);
        PinConfigS1 = Model.GetPinForType(GhWtS1PinType, peripheral, pinS1, DevicePinMode.Output);
        PinConfigS2 = Model.GetPinForType(GhWtS2PinType, peripheral, pinS2, DevicePinMode.Output);
        this.WhenAnyValue(x => x.PinConfigAnalog.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Pin)));
        this.WhenAnyValue(x => x.PinConfigS0.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(PinS0)));
        this.WhenAnyValue(x => x.PinConfigS1.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(PinS1)));
        this.WhenAnyValue(x => x.PinConfigS2.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(PinS2)));
        this.WhenAnyValue(x => x.Model.WtSensitivity).Subscribe(_ => this.RaisePropertyChanged(nameof(Sensitivity)));
    }

    public void ResetMin()
    {
        _maximumCount = 0;
        Array.Clear(_maximums);
    }

    private readonly int[] _maximums = new int[5];
    private int _maximumCount;

    public DirectPinConfig PinConfigAnalog { get; }
    private DirectPinConfig PinConfigS0 { get; }
    private DirectPinConfig PinConfigS1 { get; }
    private DirectPinConfig PinConfigS2 { get; }

    public override bool Peripheral => PinConfigAnalog.Peripheral;

    public int Pin
    {
        get => PinConfigAnalog.Pin;
        set
        {
            PinConfigAnalog.Pin = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(PinConfigs));
        }
    }

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
    [Reactive]
    public int RawTap { get; set; }

    public override IList<PinConfig> PinConfigs => new List<PinConfig>
        {PinConfigAnalog, PinConfigS0, PinConfigS1, PinConfigS2};


    public ReadOnlyObservableCollection<int> AvailablePins => Model.AvailablePinsAnalog;
    public ReadOnlyObservableCollection<int> AvailablePinsDigital => Model.AvailablePinsDigital;

    public GhWtInputType Input { get; set; }
    public bool Combined { get; }

    public int Sensitivity
    {
        get => Model.WtSensitivity;
        set => Model.WtSensitivity = value;
    }

    public override string Title => EnumToStringConverter.Convert(Input);

    public override InputType? InputType =>
        Peripheral ? Types.InputType.WtNeckPeripheralInput : Types.InputType.WtNeckInput;

    public override bool IsUint => true;

    public override IList<DevicePin> Pins => new List<DevicePin>
    {
        new(PinConfigAnalog.Pin, DevicePinMode.Floating)
    };

    public override string Generate()
    {
        var var = "rawWt";
        if (Peripheral)
        {
            var = "rawWtPeripheral";
        }

        return Input == GhWtInputType.TapBar ? $"gh5_mapping[{var}]" : $"({var} & {1 << (byte) Input})";
    }

    public override SerializedInput Serialise()
    {
        if (Combined) return new SerializedGhWtInputCombined(Input, Peripheral);
        return new SerializedGhWtInput(Peripheral, PinConfigAnalog.Pin, PinS0, PinS1, PinS2, Input);
    }


    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw)
    {
        var raw = Peripheral ? peripheralWtRaw : ghWtRaw;
        if (raw.IsEmpty) return;
        var inputs = new int[5];
        for (var i = 0; i < inputs.Length; i++)
        {
            inputs[i] = BitConverter.ToInt32(raw[(i * 4)..((i + 1) * 4)]);
        }

        if (_maximumCount < 10)
        {
            _maximumCount++;
            for (var i = 0; i < inputs.Length; i++)
            {
                _maximums[i] = Math.Max(_maximums[i], inputs[i]);
            }
        }
        
        switch (Input)
        {
            case <= GhWtInputType.TapOrange:
                var input = ChannelsFromInput[Input];
                RawValue = inputs[input] > _maximums[input] + Model.WtSensitivity ? 1 : 0;
                RawTap = inputs[input];
                break;
            case GhWtInputType.TapBar:
            case GhWtInputType.TapAll:
                var b =
                    _channels
                        .Where(s => inputs[s.Value] > _maximums[s.Value] + Model.WtSensitivity)
                        .Select(s => s.Key)
                        .Aggregate<BarButton, BarButton>(0, (current, barButton) => current | barButton);
                RawValue = Gh5NeckInput.Gh5MappingsReversed.GetValueOrDefault(b, 0);
                break;
        }
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        return string.Join("\n", bindings.Select(binding => binding.Item2));
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[]
        {
            Peripheral ? "INPUT_WT_SLAVE_NECK" : "INPUT_WT_NECK", $"WT_PIN_INPUT {Pin}", $"WT_PIN_S0 {PinS0}",
            $"WT_PIN_S1 {PinS1}", $"WT_PIN_S2 {PinS2}"
        };
    }
}