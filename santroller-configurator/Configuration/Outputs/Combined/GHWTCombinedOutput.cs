using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public partial class GhwtCombinedOutput : CombinedOutput
{
    private static readonly Dictionary<GhWtInputType, StandardButtonType> Taps = new()
    {
        {GhWtInputType.TapGreen, StandardButtonType.A},
        {GhWtInputType.TapRed, StandardButtonType.B},
        {GhWtInputType.TapYellow, StandardButtonType.Y},
        {GhWtInputType.TapBlue, StandardButtonType.X},
        {GhWtInputType.TapOrange, StandardButtonType.LeftShoulder}
    };

    private static readonly Dictionary<GhWtInputType, InstrumentButtonType> TapRb = new()
    {
        {GhWtInputType.TapGreen, InstrumentButtonType.SoloGreen},
        {GhWtInputType.TapRed, InstrumentButtonType.SoloRed},
        {GhWtInputType.TapYellow, InstrumentButtonType.SoloYellow},
        {GhWtInputType.TapBlue, InstrumentButtonType.SoloBlue},
        {GhWtInputType.TapOrange, InstrumentButtonType.SoloOrange}
    };


    private readonly DirectPinConfig _pin;
    private readonly DirectPinConfig _pinConfigS0;
    private readonly DirectPinConfig _pinConfigS1;
    private readonly DirectPinConfig _pinConfigS2;

    public GhwtCombinedOutput(ConfigViewModel model, bool peripheral, int pin = -1, int pinS0 = -1, int pinS1 = -1,
        int pinS2 = -1) : base(model)
    {
        Peripheral = peripheral;
        UpdateDetails();
        Outputs.Clear();
        _pin = Model.GetPinForType(GhWtTapInput.GhWtAnalogPinType, peripheral, pin, DevicePinMode.PullUp);
        _pinConfigS0 = Model.GetPinForType(GhWtTapInput.GhWtS0PinType, peripheral, pinS0, DevicePinMode.Output);
        _pinConfigS1 = Model.GetPinForType(GhWtTapInput.GhWtS1PinType, peripheral, pinS1, DevicePinMode.Output);
        _pinConfigS2 = Model.GetPinForType(GhWtTapInput.GhWtS2PinType, peripheral, pinS2, DevicePinMode.Output);
        this.WhenAnyValue(x => x._pin.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Pin)));
        this.WhenAnyValue(x => x._pinConfigS0.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(PinS0)));
        this.WhenAnyValue(x => x._pinConfigS1.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(PinS1)));
        this.WhenAnyValue(x => x._pinConfigS2.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(PinS2)));
        this.WhenAnyValue(x => x.Model.WtSensitivity).Subscribe(_ => this.RaisePropertyChanged(nameof(Sensitivity)));
        Outputs.Connect().Filter(x => x is OutputAxis)
            .Filter(s => s.IsVisible)
            .Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton)
            .Filter(s => s.IsVisible)
            .Bind(out var digitalOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton or {Input.IsAnalog: false})
            .Bind(out var allDigitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
        AllDigitalOutputs = allDigitalOutputs;
    }

    public bool Peripheral { get; }

    public int Pin
    {
        get => _pin.Pin;
        set => _pin.Pin = value;
    }

    public int PinS0
    {
        get => _pinConfigS0.Pin;
        set => _pinConfigS0.Pin = value;
    }

    public int Sensitivity
    {
        get => Model.WtSensitivity;
        set => Model.WtSensitivity = value;
    }

    public int PinS1
    {
        get => _pinConfigS1.Pin;
        set => _pinConfigS1.Pin = value;
    }

    public int PinS2
    {
        get => _pinConfigS2.Pin;
        set => _pinConfigS2.Pin = value;
    }

    [Reactive] private string _rawTaps = string.Empty;

    public ReadOnlyObservableCollection<int> AvailablePinsDigital => Model.AvailablePinsDigital;

    public override void SetOutputsOrDefaults(IEnumerable<Output> outputs)
    {
        Outputs.Clear();
        Outputs.AddRange(outputs);
        if (Outputs.Count == 0)
        {
            CreateDefaults();
        }
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return Peripheral ? Resources.GhwtCombinedPeripheralTitle : Resources.GhwtCombinedTitle;
    }

    public override Enum GetOutputType()
    {
        return SimpleType.WtNeckSimple;
    }

    public void CreateDefaults()
    {
        Outputs.Add(new GuitarAxis(Model, true,
            new GhWtTapInput(GhWtInputType.TapBar, Model, Peripheral, Pin, PinS0, PinS1, PinS2,
                true),
            Colors.Black,
            Colors.Black, [], [], [], short.MinValue, short.MaxValue,
            0,
            false, GuitarAxisType.Slider, false, false, false, -1, true));
        UpdateBindings();
    }

    public override IEnumerable<Output> ValidOutputs()
    {
        var tapAnalog =
            Outputs.Items.FirstOrDefault(s => s is {Input: GhWtTapInput {Input: GhWtInputType.TapBar}});
        var tapFrets =
            Outputs.Items.FirstOrDefault(s => s is {Input: GhWtTapInput {Input: GhWtInputType.TapAll}});
        if (tapAnalog == null && tapFrets == null) return Outputs.Items;
        var outputs = new List<Output>(Outputs.Items);
        // Map Tap bar to Upper frets on RB guitars
        if (tapAnalog != null && Model.DeviceControllerType is DeviceControllerType.RockBandGuitar)
        {
            outputs.AddRange(TapRb.Select(pair => new GuitarButton(Model, tapAnalog.Enabled,
                new GhWtTapInput(pair.Key, Model, Peripheral, Pin, PinS0, PinS1, PinS2, true), Colors.Black,
                Colors.Black,
                [], [], [], 5, pair.Value, false, false, false, -1,
                true)));

            outputs.Remove(tapAnalog);
        }

        if (tapFrets == null) return outputs;
        outputs.AddRange(Taps.Select(pair => new ControllerButton(Model, tapFrets.Enabled,
            new GhWtTapInput(pair.Key, Model, Peripheral, Pin, PinS0, PinS1, PinS2, true), Colors.Black, Colors.Black,
            [], [], [], 5, pair.Value, false, false, false, -1,
            true)).Cast<Output>());

        outputs.Remove(tapFrets);

        return outputs;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedGhwtCombinedOutput(Peripheral, Pin, PinS0, PinS1, PinS2, Outputs.Items.ToList());
    }

    public override void Update(Dictionary<int, int> analogRaw, Dictionary<int, bool> digitalRaw,
        ReadOnlySpan<byte> ps2Raw, ReadOnlySpan<byte> wiiRaw,
        ReadOnlySpan<byte> djLeftRaw, ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw,
        ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType, ReadOnlySpan<byte> usbHostRaw,
        ReadOnlySpan<byte> bluetoothRaw,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw,
        ReadOnlySpan<byte> mpr121Raw, ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw,
        bool peripheralConnected, byte[] crkdRaw)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw, ps2ControllerType,
            wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw, digitalPeripheral, cloneRaw,
            adxlRaw, mpr121Raw, midiRaw, bluetoothInputsRaw, peripheralConnected, crkdRaw);
        var raw = Peripheral ? peripheralWtRaw : ghWtRaw;
        if (raw.IsEmpty) return;
        var inputs = new int[5];
        for (var i = 0; i < inputs.Length; i++)
        {
            inputs[i] = BitConverter.ToInt32(raw[(i * 4)..((i + 1) * 4)]);
        }

        var ret = "";
        foreach (var (button, value) in GhWtTapInput.ChannelsFromInput)
        {
            ret += $"{inputs[value]} ";
        }

        RawTaps = ret;
    }

    public override void UpdateBindings()
    {
        var axisController = Outputs.Items.FirstOrDefault(s => s is ControllerAxis);
        var axisGuitar = Outputs.Items.FirstOrDefault(s => s is GuitarAxis);
        var tapAll = Outputs.Items.FirstOrDefault(s => s is OutputButton);
        if (Model.DeviceControllerType.Is5FretGuitar())
        {
            if (tapAll == null)
            {
                var button = new GuitarButton(Model, true,
                    new GhWtTapInput(GhWtInputType.TapAll, Model, Peripheral, Pin, PinS0, PinS1, PinS2,
                        true), Colors.Black,
                    Colors.Black, [], [], [], 5,
                    InstrumentButtonType.SliderToFrets, false, false, false, -1, true);
                button.Enabled = false;
                Outputs.Add(button);
            }

            if (axisController == null) return;
            Outputs.Remove(axisController);
            Outputs.Add(new GuitarAxis(Model, true,
                new GhWtTapInput(GhWtInputType.TapBar, Model, Peripheral, Pin, PinS0, PinS1, PinS2,
                    true),
                Colors.Black,
                Colors.Black, [], [], [], short.MinValue,
                short.MaxValue, 0,
                false, GuitarAxisType.Slider, false, false, false, -1, true));
        }
        else if (Model.DeviceControllerType == DeviceControllerType.Gamepad)
        {
            if (tapAll != null) Outputs.Remove(tapAll);
            if (axisGuitar != null)
            {
                Outputs.Remove(axisGuitar);
            }

            if (axisController != null) return;
            Outputs.Add(new ControllerAxis(Model, true,
                new GhWtTapInput(GhWtInputType.TapBar, Model, Peripheral, Pin, PinS0, PinS1, PinS2,
                    true),
                Colors.Black,
                Colors.Black, [], [], [], short.MinValue,
                short.MaxValue, 0, 0,
                ushort.MaxValue, StandardAxisType.LeftStickX, false, false, false, -1, true));
        }
        else
        {
            if (tapAll != null) Outputs.Remove(tapAll);
            if (axisGuitar != null)
            {
                Outputs.Remove(axisGuitar);
            }

            if (axisController != null)
            {
                Outputs.Remove(axisController);
            }
        }
    }

    protected override IEnumerable<PinConfig> GetOwnPinConfigs()
    {
        return [_pin, _pinConfigS0, _pinConfigS1, _pinConfigS2];
    }
}