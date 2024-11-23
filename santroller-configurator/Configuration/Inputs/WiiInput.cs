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

public class WiiInput : TwiInput
{
    public static readonly string WiiTwiType = "wii";
    public static readonly int WiiTwiFreq = 400000;

    public WiiInput(UsbHostInputType input, ConfigViewModel model, bool peripheral, int sda = -1,
        int scl = -1,
        bool combined = false) : base(input, WiiTwiType, WiiTwiFreq, peripheral, sda, scl, model, combined)
    {

    }

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
        return base.RequiredDefines().Concat(["INPUT_WII"]).ToList();
    }

}