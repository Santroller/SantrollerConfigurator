using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class Ps2Input : HostInput, ISpi
{
    public static readonly string Ps2SpiType = "ps2";
    public static readonly uint Ps2SpiFreq = 500000;
    public static readonly bool Ps2SpiCpol = true;
    public static readonly bool Ps2SpiCpha = true;
    public static readonly bool Ps2SpiMsbFirst = false;
    public static readonly string Ps2AckType = "ps2_ack";
    public static readonly string Ps2AttType = "ps2_att";
    private readonly SpiConfig _spiConfig;

    private readonly DirectPinConfig _ackConfig;
    private readonly DirectPinConfig _attConfig;

    public Ps2Input(UsbHostInputType input, ConfigViewModel model, bool peripheral, int miso = -1,
        int mosi = -1,
        int sck = -1, int att = -1, int ack = -1, bool combined = false) : base(input, model, combined)
    {
        BindableSpi = !Combined && !model.Branded && Model.Microcontroller.SpiAssignable;
        BindableAtt = !Combined && !model.Branded && Model.Microcontroller is not (Uno or Mega);
        var config = Model.GetSpiForType(Ps2SpiType, peripheral);
        _spiConfig = config ?? Model.Microcontroller.AssignSpiPins(model, Ps2SpiType, peripheral, true, true, mosi,
            miso, sck, Ps2SpiCpol, Ps2SpiCpha, Ps2SpiMsbFirst, Ps2SpiFreq, false);

        this.WhenAnyValue(x => x._spiConfig.Miso).Subscribe(_ => this.RaisePropertyChanged(nameof(Miso)));
        this.WhenAnyValue(x => x._spiConfig.Mosi).Subscribe(_ => this.RaisePropertyChanged(nameof(Mosi)));
        this.WhenAnyValue(x => x._spiConfig.Sck).Subscribe(_ => this.RaisePropertyChanged(nameof(Sck)));
        _ackConfig = Model.GetPinForType(Ps2AckType, peripheral, ack, DevicePinMode.Floating);
        _attConfig = Model.GetPinForType(Ps2AttType, peripheral, att, DevicePinMode.Floating);
        this.WhenAnyValue(x => x._attConfig.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Att)));
        this.WhenAnyValue(x => x._ackConfig.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Ack)));
        if (Model.Microcontroller is (Uno or Mega))
        {
            Att = 10;
        }
    }

    public int Ack
    {
        get => _ackConfig.Pin;
        set => _ackConfig.Pin = value;
    }

    public int Att
    {
        get => _attConfig.Pin;
        set => _attConfig.Pin = value;
    }
    
    
    public int Mosi
    {
        get => _spiConfig.Mosi;
        set => _spiConfig.Mosi = value;
    }

    public int Miso
    {
        get => _spiConfig.Miso;
        set => _spiConfig.Miso = value;
    }

    public override bool Peripheral => _spiConfig.Peripheral;

    public int Sck
    {
        get => _spiConfig.Sck;
        set => _spiConfig.Sck = value;
    }
    public List<int> SpiPins()
    {
        return [Mosi, Miso, Sck];
    }
    public List<int> AvailableMosiPins => this.GetMosiPins();
    public List<int> AvailableMisoPins => this.GetMisoPins();
    public List<int> AvailableSckPins => this.GetSckPins();
    public bool BindableSpi { get; }
    public bool BindableAtt { get; }

    public override InputType? InputType => Types.InputType.Ps2Input;
    public ReadOnlyObservableCollection<int> AvailablePins => Model.AvailablePinsDigital;
    public ReadOnlyObservableCollection<int> AvailablePinsInterrupt => Model.AvailablePinsInterrupt;

    public override IList<DevicePin> Pins => new List<DevicePin>
    {
        new(Att, DevicePinMode.Output),
        new(Ack, DevicePinMode.Floating)
    };

    public override IList<PinConfig> PinConfigs =>
        new List<PinConfig> {_spiConfig, _ackConfig, _attConfig};

    public override string Title => EnumToStringConverter.Convert(Input);
    public override string Field => "lastSuccessfulPS2Packet";

    public override SerializedInput Serialise()
    {
        if (Combined)
            return new SerializedPs2InputCombined(Input, Peripheral);
        return new SerializedPs2Input(Peripheral, Miso, Mosi, Sck, Att, Ack, Input);
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Data,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw,
        ReadOnlySpan<byte> mpr121Raw, ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw)
    {
        if (ps2ControllerType.IsEmpty || ps2Data.IsEmpty) return;
        Update(ps2Data);
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        var defines = new List<string>
        {
            $"{Ps2SpiType.ToUpper()}_SPI_PORT {_spiConfig.Definition}",
            "INPUT_PS2",
            $"PS2_ACK {Ack}",
            $"INPUT_PS2_ATT_SET() {Model.Microcontroller.GenerateDigitalWrite(Att, true, Peripheral)}",
            $"INPUT_PS2_ATT_CLEAR() {Model.Microcontroller.GenerateDigitalWrite(Att, false, Peripheral)}"
        };
        defines.AddRange(Model.Microcontroller.GeneratePs2Defines(Ack, "INTERRUPT_PS2_ACK"));

        return defines;
    }

    
}