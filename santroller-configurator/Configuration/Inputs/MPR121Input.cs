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

public partial class Mpr121Input : Input
{
    public Mpr121Input(int input, ConfigViewModel model, bool peripheral) : base(model)
    {
        Input = input;
        IsAnalog = false;
        Peripheral = peripheral;
        _availableInputsHelper = this.WhenAnyValue(x => x.Model.Mpr121CapacitiveCount)
            .Select(x => Enumerable.Range(4, 8).Concat(Enumerable.Range(0, x)))
            .ToProperty(this, x => x.AvailableInputs);

        this.WhenAnyValue(x => x.Model.Mpr121CapacitiveCount).Subscribe(x =>
        {
            if (x >= 4) return;
            if (Input >= x && Input < 4)
            {
                Input = 4;
            }
        });
    }

    public override bool Peripheral { get; }
    public override string Title => $"MPR121 Touch Sensor {Input}";

    private int _input;

    public int Input
    {
        get => _input;
        set
        {
            this.RaiseAndSetIfChanged(ref _input, value);
            Model.UpdateErrors();
        }
    }

    [ObservableAsProperty] private IEnumerable<int> _availableInputs  = null!;

    public override IList<PinConfig> PinConfigs => Array.Empty<PinConfig>();
    public override InputType? InputType => Types.InputType.Mpr121Input;

    public override IList<DevicePin> Pins => Array.Empty<DevicePin>();
    public override bool IsUint => true;

    public override string Generate()
    {
        return Input < Model.Mpr121CapacitiveCount ? $"(mpr121_raw & {1 << Input})" : $"((mpr121_raw & {1 << Input}) == 0)";
    }

    public override SerializedInput Serialise()
    {
        return new SerializedMpr121Input(Peripheral, Input);
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
        if (mpr121Raw.IsEmpty) return;
        var raw = BitConverter.ToUInt16(mpr121Raw);
        if (Input < Model.Mpr121CapacitiveCount)
        {
            RawValue = (raw & (1 << Input)) != 0 ? 1 : 0;
        }
        else
        {
            // Pull-up, so its inverted
            RawValue = (raw & (1 << Input)) != 0 ? 0 : 1;
        }
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