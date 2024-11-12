using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using static GuitarConfigurator.NetCore.Configuration.Outputs.Combined.WiiCombinedOutput;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class WiiInput : HostInput, ITwi
{
    public static readonly string WiiTwiType = "wii";
    public static readonly int WiiTwiFreq = 400000;
    private readonly TwiConfig _twiConfig;

    public WiiInput(UsbHostInputType input, ConfigViewModel model, bool peripheral, int sda = -1,
        int scl = -1,
        bool combined = false) : base(input, model, combined)
    {
        var config = Model.GetTwiForType(WiiTwiType, peripheral);
        _twiConfig = config ?? Model.Microcontroller.AssignTwiPins(model, WiiTwiType, peripheral, sda, scl, WiiTwiFreq, false);


        this.WhenAnyValue(x => x._twiConfig.Scl).Subscribe(_ => this.RaisePropertyChanged(nameof(Scl)));
        this.WhenAnyValue(x => x._twiConfig.Sda).Subscribe(_ => this.RaisePropertyChanged(nameof(Sda)));
        BindableTwi = !combined && Model.Microcontroller.TwiAssignable && !model.Branded;
    }
    
    
    public int Sda
    {
        get => _twiConfig.Sda;
        set => _twiConfig.Sda = value;
    }

    public int Scl
    {
        get => _twiConfig.Scl;
        set => _twiConfig.Scl = value;
    }

    public override bool Peripheral => _twiConfig.Peripheral;


    public List<int> AvailableSdaPins => GetSdaPins();
    public List<int> AvailableSclPins => GetSclPins();
    public override IList<PinConfig> PinConfigs => new List<PinConfig> {_twiConfig};

    public List<int> TwiPins()
    {
        return [Sda, Scl];
    }

    private List<int> GetSdaPins()
    {
        return Model.Microcontroller.TwiPins(false)
            .Where(s => s.Value is TwiPinType.Sda)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetSclPins()
    {
        return Model.Microcontroller.TwiPins(false)
            .Where(s => s.Value is TwiPinType.Scl)
            .Select(s => s.Key).ToList();
    }
    public bool BindableTwi { get; }

    public override InputType? InputType => Types.InputType.WiiInput;
    public override IList<DevicePin> Pins => Array.Empty<DevicePin>();

    public override string Title => EnumToStringConverter.Convert(Input);
    public override string Field => "lastSuccessfulWiiPacket";

    public override SerializedInput Serialise()
    {
        if (Combined) return new SerializedWiiInputCombined(Input, Peripheral);

        return new SerializedWiiInput(Sda, Scl, Input, Peripheral);
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiData, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw,
        ReadOnlySpan<byte> mpr121Raw, ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw)
    {
        if (wiiControllerType.IsEmpty) return;
        Update(wiiData);
    }
    
    public override IReadOnlyList<string> RequiredDefines()
    {
        return [$"{WiiTwiType}_TWI_PORT {_twiConfig.Definition}", "INPUT_WII"];
    }

    private enum DrumType
    {
        DrumGreen,
        DrumRed,
        DrumYellow,
        DrumBlue,
        DrumOrange,
        DrumKick,
        DrumHihat
    }
}