using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public partial class Mpr121SliderInput : Input
{
    public Mpr121SliderInput(ConfigViewModel model, bool peripheral, int inputGreen, int inputRed, int inputYellow,
        int inputBlue, int inputOrange) : base(model)
    {
        IsAnalog = true;
        Peripheral = peripheral;
        InputGreen = inputGreen;
        InputRed = inputRed;
        InputYellow = inputYellow;
        InputBlue = inputBlue;
        InputOrange = inputOrange;
        // First four sensors are cap only, so hide them if they aren't enabled
        _availableInputsHelper = this.WhenAnyValue(x => x.Model.Mpr121CapacitiveCount)
            .Select(x => Enumerable.Range(4, 8).Concat(Enumerable.Range(0, x)))
            .ToProperty(this, x => x.AvailableInputs);

        this.WhenAnyValue(x => x.Model.Mpr121CapacitiveCount).Subscribe(x =>
        {
            if (x >= 4) return;
            if (InputGreen >= x && InputGreen < 4)
            {
                InputGreen = 4;
            }

            if (InputRed >= x && InputRed < 4)
            {
                InputRed = 4;
            }

            if (InputYellow >= x && InputYellow < 4)
            {
                InputYellow = 4;
            }

            if (InputBlue >= x && InputBlue < 4)
            {
                InputBlue = 4;
            }

            if (InputOrange >= x && InputOrange < 4)
            {
                InputOrange = 4;
            }
        });
    }

    public override bool Peripheral { get; }
    public override string Title => $"MPR121 Touch Sensor {InputGreen}";

    private int _inputGreen;

    public int InputGreen
    {
        get => _inputGreen;
        set
        {
            this.RaiseAndSetIfChanged(ref _inputGreen, value);
            Model.UpdateErrors();
        }
    }

    private int _inputRed;

    public int InputRed
    {
        get => _inputRed;
        set
        {
            this.RaiseAndSetIfChanged(ref _inputRed, value);
            Model.UpdateErrors();
        }
    }

    private int _inputYellow;

    public int InputYellow
    {
        get => _inputYellow;
        set
        {
            this.RaiseAndSetIfChanged(ref _inputYellow, value);
            Model.UpdateErrors();
        }
    }

    private int _inputBlue;

    public int InputBlue
    {
        get => _inputBlue;
        set
        {
            this.RaiseAndSetIfChanged(ref _inputBlue, value);
            Model.UpdateErrors();
        }
    }

    private int _inputOrange;

    public int InputOrange
    {
        get => _inputOrange;
        set
        {
            this.RaiseAndSetIfChanged(ref _inputOrange, value);
            Model.UpdateErrors();
        }
    }

    [ObservableAsProperty] private IEnumerable<int> _availableInputs = null!;

    public IEnumerable<int> MappedInputs => [InputGreen, InputRed, InputYellow, InputBlue, InputOrange];

    public override IList<PinConfig> PinConfigs => Array.Empty<PinConfig>();
    public override InputType? InputType => Types.InputType.Mpr121Input;

    public override IList<DevicePin> Pins => Array.Empty<DevicePin>();
    public override bool IsUint => true;

    public override string Generate(BinaryWriter? writer)
    {
        var inputs = new[] {InputGreen, InputRed, InputYellow, InputBlue, InputOrange};
        var mapping = string.Join(" | ", inputs.Select((t, i) => t > i
            ? $"((mpr121_raw & {1 << t}) >> {t - i})"
            : $"((mpr121_raw & {1 << t}) << {i - t})"));
        
        return $"gh5_mapping[{mapping}]";
    }

    public override SerializedInput Serialise()
    {
        return new SerializedMpr121SliderInput(Peripheral, InputGreen, InputRed, InputYellow, InputBlue, InputOrange);
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw,
        ReadOnlySpan<byte> mpr121Raw, ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw,
        bool peripheralConnected)
    {
        if (mpr121Raw.IsEmpty) return;
        var inputs = new[]
        {
            (BarButton.Green, InputGreen), (BarButton.Red, InputRed), (BarButton.Yellow, InputYellow),
            (BarButton.Blue, InputBlue), (BarButton.Orange, InputOrange)
        };
        var raw = BitConverter.ToUInt16(mpr121Raw);
        var touched =
            inputs
                .Where(s => (raw & (1 << s.Item2)) != 0)
                .Select(s => s.Item1)
                .Aggregate<BarButton, BarButton>(0, (current, barButton) => current | barButton);
        RawValue = Gh5NeckInput.Gh5MappingsReversed.GetValueOrDefault(touched, 0);
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        return string.Join("\n", bindings.Select(binding => binding.Item2));
    }


    public override IReadOnlyList<string> RequiredDefines()
    {
        return ["INPUT_MPR121"];
    }
}