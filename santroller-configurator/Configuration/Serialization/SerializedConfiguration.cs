using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedConfiguration
{
    public SerializedConfiguration()
    {
    }

    public SerializedConfiguration(ConfigViewModel model)
    {
        Update(model, model.Bindings.Items);
    }

    public void Update(ConfigViewModel model, IEnumerable<Output> bindings, bool allowErrors = true)
    {
        if (!allowErrors && model.HasError) return;
        Rb3CymbalGlitchFix = model.CymbalGlitchFix;
        MidiSerialEnabled = model.MidiSerialEnabled;
        MidiSerialPin = model.MidiSerialPin;
        SleepEnabled = model.SleepEnabled;
        SleepPin = model.SleepWakeUpPin;
        SleepTimer = model.DeviceSleep;
        LedTimer = model.LedSleep;
        RolloverMode = model.RolloverMode;
        // do NOT serialise empty outputs!
        Bindings = bindings.Where(s => s is not EmptyOutput).Select(s => s.Serialize()).ToList();
        DeviceType = model.DeviceControllerType;
        XInputOnWindows = model.XInputOnWindows;
        LedType = model.LedType;
        LedMosi = model.LedMosi;
        if (LedType.IsWs2812())
        {
            LedMosi = model.Ws2812Data;
        }
        LedSck = model.LedSck;
        LedCount = model.LedCount;
        LedBrightnessOn = model.LedBrightnessOn;
        LedBrightnessOff = model.LedBrightnessOff;
        MouseMovementType = model.MouseMovementType;
        WtSensitivity = model.WtSensitivity;
        UsbHostDp = model.UsbHostDp;
        LocalDebounceMode = model.LocalDebounceMode;
        Debounce = model.Debounce;
        StrumDebounce = model.StrumDebounce;
        PollRate = model.PollRate;
        CombinedStrumDebounce = model.CombinedStrumDebounce;
        QueueBasedInputs = model.Deque;
        DjPollRate = model.DjPollRate;
        DjSmooth = model.DjSmoothing;
        SwapSwitchFaceButtons = model.SwapSwitchFaceButtons;
        Variant = model.Variant;
        BtRxMacAddress = model.BtRxAddr;
        LedTypePeripheral = model.LedTypePeripheral;
        LedMosiPeripheral = model.LedMosiPeripheral;
        if (LedTypePeripheral.IsWs2812() || LedTypePeripheral.IsWs2812W())
        {
            LedMosiPeripheral = model.Ws2812DataPeripheral;
        }
        LedSckPeripheral = model.LedSckPeripheral;
        LedCountPeripheral = model.LedCountPeripheral;
        HasPeripheral = model.HasPeripheral;
        PeripheralSda = model.PeripheralSda;
        PeripheralScl = model.PeripheralScl;
        Stp16Oe = model.Stp16Oe;
        Stp16Le = model.Stp16Le;
        Stp16OePeripheral = model.Stp16OePeripheral;
        Stp16LePeripheral = model.Stp16LePeripheral;
        Apa102IsFullSize = model.Apa102IsFullSize;
        Ps3OnRpcs3 = model.Ps3OnRpcs3;
        Mpr121CapacitiveCount = model.Mpr121CapacitiveCount;
        Mpr121Scl = model.Mpr121Scl;
        Mpr121Sda = model.Mpr121Sda;
        HasMpr121 = model.HasMpr121;
        HasMax1704X = model.HasMax1704X;
        Max1704XSda = model.Max1704XSda;
        Max1704XScl = model.Max1704XScl;
        HideControllerView = model.HideControllerView;
        Ps4Instruments = model.Ps4Instruments;
        AdxlFilter = model.AccelFilter;
        HasWiiOutput = model.HasWiiOutput;
        WiiOutputScl = model.WiiOutputScl;
        WiiOutputSda = model.WiiOutputSda;
        HasPs2Output = model.HasPs2Output;
        
        Ps2OutputMiso = model.Ps2OutputMiso;
        Ps2OutputMosi = model.Ps2OutputMosi;
        Ps2OutputSck = model.Ps2OutputSck;
        Ps2OutputAck = model.Ps2OutputAck;
        Ps2OutputAtt = model.Ps2OutputAtt;
        HasWtDrumInput = model.HasWtDrumInput;
        HasBhDrumInput = model.HasBhDrumInput;
        WtDrumMiso = model.WtDrumMiso;
        WtDrumMosi = model.WtDrumMosi;
        WtDrumSck = model.WtDrumSck;
        WtDrumCs = model.WtDrumCs;
        HasMustangNeckInput = model.HasMustangNeckInput;
        MustangNeckMiso = model.MustangNeckMiso;
        MustangNeckMosi = model.MustangNeckMosi;
        MustangNeckSck = model.MustangNeckSck;
        MustangNeckCs = model.MustangNeckCs;
        BhDrumScl = model.BhDrumScl;
        BhDrumSda = model.BhDrumSda;
        DjFullRange = model.DjFullRange;
        DjNavButtons = model.DjNavButtons;
        SelectDpadLeftXb1 = model.SelectDpadLeftXb1;
        AdafruitHost = model.AdafruitHost;
        AccelScl = model.AccelScl;
        AccelSda = model.AccelSda;
        HasAccel = model.HasAccel;
        ClassicMode = model.Classic;
        IsBluetoothTx = model.IsBluetoothTx;
        InvertHidYAxis = model.InvertHidYAxis;
    }

    [ProtoMember(1)] public LedType LedType { get; private set; }
    [ProtoMember(2)] public bool XInputOnWindows { get; private set; }
    [ProtoMember(4)] public DeviceControllerType DeviceType { get; private set; }
    [ProtoMember(5)] public EmulationType EmulationType { get; private set; }
    [ProtoMember(7)] public List<SerializedOutput> Bindings { get; private set; } = new();
    [ProtoMember(8)] public int LedMosi { get; private set; }
    [ProtoMember(9)] public int LedSck { get; private set; }
    [ProtoMember(10)] public byte LedCount { get; private set; }
    [ProtoMember(11)] public MouseMovementType MouseMovementType { get; private set; }
    [ProtoMember(12)] public int WtSensitivity { get; private set; }
    [ProtoMember(14)] public int UsbHostDp { get; private set; }
    [ProtoMember(24)] public int Debounce { get; private set; }
    [ProtoMember(25)] public int StrumDebounce { get; private set; }
    [ProtoMember(26)] public int PollRate { get; private set; }
    [ProtoMember(27)] public bool CombinedStrumDebounce { get; private set; }
    [ProtoMember(28)] public bool QueueBasedInputs { get; private set; }
    [ProtoMember(29)] public int DjPollRate { get; private set; }
    [ProtoMember(30)] public bool DjDual { get; private set; }
    [ProtoMember(31)] public bool SwapSwitchFaceButtons { get; private set; }
    [ProtoMember(32)] public bool DjSmooth { get; private set; }
    [ProtoMember(33)] public string Variant { get; private set; } = "";
    [ProtoMember(34)] public string BtRxMacAddress { get; private set; } = "";
    [ProtoMember(35)] public LedType LedTypePeripheral { get; private set; }
    [ProtoMember(36)] public int LedMosiPeripheral { get; private set; }
    [ProtoMember(37)] public int LedSckPeripheral { get; private set; }
    [ProtoMember(38)] public byte LedCountPeripheral { get; private set; }
    [ProtoMember(39)] public bool HasPeripheral { get; private set; }
    [ProtoMember(40)] public int PeripheralSda { get; private set; }
    [ProtoMember(41)] public int PeripheralScl { get; private set; }
    [ProtoMember(42)] public int Stp16Oe { get; private set; }
    [ProtoMember(43)] public int Stp16Le { get; private set; }
    [ProtoMember(44)] public int Stp16OePeripheral { get; private set; }
    [ProtoMember(45)] public int Stp16LePeripheral { get; private set; }
    [ProtoMember(46)] public RolloverMode RolloverMode { get; private set; }
    [ProtoMember(48)] public int LedBrightnessOn { get; private set; }
    [ProtoMember(49)] public bool Apa102IsFullSize { get; private set; }

    [ProtoMember(50)] [DefaultValue(true)] public bool Ps3OnRpcs3 { get; private set; } = true;
    [ProtoMember(51)] public int Mpr121CapacitiveCount { get; private set; }
    [ProtoMember(52)] public bool HasMpr121 { get; private set; }
    [ProtoMember(53)] public int Mpr121Sda { get; private set; }
    [ProtoMember(54)] public int Mpr121Scl { get; private set; }
    [ProtoMember(56)] public int LedBrightnessOff { get; private set; }
    [ProtoMember(57)] public bool HasMax1704X { get; private set; }
    [ProtoMember(58)] public int Max1704XSda { get; private set; }
    [ProtoMember(59)] public int Max1704XScl { get; private set; }
    [ProtoMember(61)] public bool HideControllerView { get; private set; }
    [ProtoMember(62)] public bool Ps4Instruments { get; private set; }

    [ProtoMember(63)] public double AdxlFilter { get; private set; } = 0.05;
    [ProtoMember(65)] public bool HasWiiOutput { get; private set; }
    [ProtoMember(66)] public int WiiOutputSda { get; private set; }
    [ProtoMember(67)] public int WiiOutputScl { get; private set; }
    [ProtoMember(68)] public bool HasPs2Output { get; private set; }
    [ProtoMember(69)] public int Ps2OutputMosi { get; private set; }
    [ProtoMember(70)] public int Ps2OutputMiso { get; private set; }
    [ProtoMember(71)] public int Ps2OutputSck { get; private set; }
    [ProtoMember(72)] public int Ps2OutputAtt { get; private set; }
    [ProtoMember(73)] public int Ps2OutputAck { get; private set; }
    [ProtoMember(74)] public bool DjNavButtons { get; private set; } = false;
    [ProtoMember(75)] [DefaultValue(true)] public bool DjFullRange { get; private set; } = true;
    [ProtoMember(76)] public bool SelectDpadLeftXb1 { get; private set; } = false;
    [ProtoMember(77)] public bool AdafruitHost { get; private set; } = false;
    [ProtoMember(80)] public bool HasAccel { get; private set; }
    [ProtoMember(81)] public int AccelSda { get; private set; }
    [ProtoMember(82)] public int AccelScl { get; private set; }
    [ProtoMember(84)] public bool ClassicMode { get; private set; }
    [ProtoMember(85)] public bool IsBluetoothTx { get; private set; }
    [ProtoMember(86)] public bool LocalDebounceMode { get; private set; }
    [ProtoMember(87)] public bool SleepEnabled { get; private set; } = false;
    [ProtoMember(88)] public int SleepPin { get; private set; } = -1;
    [ProtoMember(89)] public int SleepTimer { get; private set; } = 5;
    [ProtoMember(90)] public int LedTimer { get; private set; } = 0;
    [ProtoMember(92)] public bool MidiSerialEnabled { get; private set; } = false;
    [ProtoMember(93)] public int MidiSerialPin { get; private set; } = 1;
    [ProtoMember(94)] public bool HasWtDrumInput { get; private set; }
    [ProtoMember(95)] public int WtDrumMosi { get; private set; }
    [ProtoMember(96)] public int WtDrumMiso { get; private set; }
    [ProtoMember(97)] public int WtDrumSck { get; private set; }
    [ProtoMember(98)] public int WtDrumCs { get; private set; }
    [ProtoMember(99)] public bool Rb3CymbalGlitchFix { get; private set; }
    [ProtoMember(100)] public bool HasBhDrumInput { get; private set; }
    [ProtoMember(101)] public int BhDrumSda { get; private set; }
    [ProtoMember(102)] public int BhDrumScl { get; private set; }
    [ProtoMember(103)] public bool InvertHidYAxis { get; private set; }
    [ProtoMember(104)] public bool HasMustangNeckInput { get; private set; }
    [ProtoMember(105)] public int MustangNeckMosi { get; private set; }
    [ProtoMember(106)] public int MustangNeckMiso { get; private set; }
    [ProtoMember(107)] public int MustangNeckSck { get; private set; }
    [ProtoMember(108)] public int MustangNeckCs { get; private set; }

    public void LoadConfiguration(ConfigViewModel model)
    {
        model.CymbalGlitchFix = Rb3CymbalGlitchFix;
        model.MidiSerialEnabled = MidiSerialEnabled;
        model.MidiSerialPin = MidiSerialPin;
        model.SleepEnabled = SleepEnabled;
        model.SleepWakeUpPin = SleepPin;
        model.DeviceSleep = SleepTimer;
        model.LedSleep = LedTimer;
        model.Classic = ClassicMode;
        model.SelectDpadLeftXb1 = SelectDpadLeftXb1;
        model.DjNavButtons = DjNavButtons;
        model.DjFullRange = DjFullRange;
        model.AccelFilter = AdxlFilter;
        model.Ps4Instruments = Ps4Instruments;
        model.HideControllerView = HideControllerView;
        model.Mpr121CapacitiveCount = Mpr121CapacitiveCount;
        model.SetDeviceTypeWithoutUpdating(DeviceType);
        model.XInputOnWindows = XInputOnWindows;
        model.Ps3OnRpcs3 = Ps3OnRpcs3;
        model.InvertHidYAxis = InvertHidYAxis;
        model.Bindings.Clear();
        model.LocalDebounceMode = LocalDebounceMode;
        model.PollRate = PollRate;
        model.Debounce = Debounce;
        model.StrumDebounce = StrumDebounce;
        model.Deque = QueueBasedInputs;
        model.DjPollRate = DjPollRate;
        model.DjSmoothing = DjSmooth;
        model.SwapSwitchFaceButtons = SwapSwitchFaceButtons;
        model.Variant = Variant;
        model.BtRxAddr = BtRxMacAddress;
        model.HasPeripheral = HasPeripheral;
        model.HasWiiOutput = HasWiiOutput;
        model.HasPs2Output = HasPs2Output;
        model.HasWtDrumInput = HasWtDrumInput;
        model.HasMustangNeckInput = HasMustangNeckInput;
        model.HasBhDrumInput = HasBhDrumInput;
        model.HasMpr121 = HasMpr121;
        model.HasMax1704X = HasMax1704X;
        model.RolloverMode = RolloverMode;
        model.LedBrightnessOn = (byte) (LedBrightnessOn == 0 ? 32 : LedBrightnessOn);
        model.LedBrightnessOff = (byte) (LedBrightnessOff == 0 ? 32 : LedBrightnessOff);
        model.Apa102IsFullSize = Apa102IsFullSize;
        model.Mpr121CapacitiveCount = Mpr121CapacitiveCount;
        model.AdafruitHost = AdafruitHost;
        model.HasAccel = HasAccel;
        model.IsBluetoothTx = IsBluetoothTx ||
                              EmulationType is EmulationType.Bluetooth or EmulationType.BluetoothKeyboardMouse;
        if (HasAccel)
        {
            model.AccelScl = AccelScl;
            model.AccelSda = AccelSda;
        }

        if (HasPeripheral)
        {
            model.PeripheralScl = PeripheralScl;
            model.PeripheralSda = PeripheralSda;
        }

        if (HasPs2Output)
        {
            model.Ps2OutputMiso = Ps2OutputMiso;
            model.Ps2OutputMosi = Ps2OutputMosi;
            model.Ps2OutputSck = Ps2OutputSck;
            model.Ps2OutputAck = Ps2OutputAck;
            model.Ps2OutputAtt = Ps2OutputAtt;
        }
        if (HasWtDrumInput)
        {
            model.WtDrumMiso = WtDrumMiso;
            model.WtDrumMosi = WtDrumMosi;
            model.WtDrumSck =  WtDrumSck;
            model.WtDrumCs =  WtDrumCs;
        }
        if (HasMustangNeckInput)
        {
            model.MustangNeckMiso = MustangNeckMiso;
            model.MustangNeckMosi = MustangNeckMosi;
            model.MustangNeckSck =  MustangNeckSck;
            model.MustangNeckCs =  MustangNeckCs;
        }


        if (HasBhDrumInput)
        {
            model.BhDrumScl = BhDrumScl;
            model.BhDrumSda = BhDrumSda;
        }

        if (HasWiiOutput)
        {
            model.WiiOutputScl = WiiOutputScl;
            model.WiiOutputSda = WiiOutputSda;
        }

        if (HasMpr121)
        {
            model.Mpr121Scl = Mpr121Scl;
            model.Mpr121Sda = Mpr121Sda;
        }

        if (HasMax1704X)
        {
            model.Max1704XScl = Max1704XScl;
            model.Max1704XSda = Max1704XSda;
        }

        if (DjPollRate == 0)
        {
            model.DjPollRate = 1;
        }

        var generated = Bindings.Select(s => s.Generate(model)).ToList();
        if (generated.Count != 0)
        {
            model.Bindings.Clear();
            model.Bindings.AddRange(generated);
            model.UpdateErrors();
        }

        if (model.Bindings.Items.Any(s => s.Input.InnermostInputs().Any(s2 => s2 is AccelInput)))
        {
            model.HasAccel = true;
        }

        if (model.UsbHostEnabled) model.UsbHostDp = UsbHostDp;

        model.LedType = LedType;
        model.LedCount = LedCount < 1 ? (byte) 1 : LedCount;
        model.LedTypePeripheral = LedTypePeripheral;
        model.LedCountPeripheral = LedCountPeripheral < 1 ? (byte) 1 : LedCountPeripheral;
        model.WtSensitivity = WtSensitivity;
        model.MouseMovementType = MouseMovementType;
        model.CombinedStrumDebounce = CombinedStrumDebounce;
        if (model.IsApa102 || model.IsStp16)
        {
            model.LedMosi = LedMosi;
            model.LedSck = LedSck;
        }
        if (model.IsWs2812)
        {
            model.Ws2812Data = LedMosi;
        }
        

        if (model.IsApa102Peripheral || model.IsStp16Peripheral)
        {
            model.LedMosiPeripheral = LedMosiPeripheral;
            model.LedSckPeripheral = LedSckPeripheral;
        }
        if (model.IsWs2812Peripheral)
        {
            model.Ws2812DataPeripheral = LedMosiPeripheral;
        }

        if (model.IsStp16)
        {
            model.Stp16Le = Stp16Le;
            model.Stp16Oe = Stp16Oe;
        }

        if (model.IsStp16Peripheral)
        {
            model.Stp16OePeripheral = Stp16OePeripheral;
            model.Stp16LePeripheral = Stp16LePeripheral;
        }

        model.SetUpDiff();
    }

    public void Merge(ConfigViewModel model)
    {
        model.SleepEnabled = SleepEnabled;
        model.SleepWakeUpPin = SleepPin;
        model.DeviceSleep = SleepTimer;
        model.LedSleep = LedTimer;
        model.XInputOnWindows = XInputOnWindows;
        model.PollRate = PollRate;
        model.Debounce = Debounce;
        model.InvertHidYAxis = InvertHidYAxis;
        model.StrumDebounce = StrumDebounce;
        model.Deque = QueueBasedInputs;
        model.DjPollRate = DjPollRate;
        model.DjSmoothing = DjSmooth;
        model.SwapSwitchFaceButtons = SwapSwitchFaceButtons;
        model.WtSensitivity = WtSensitivity;
        model.CombinedStrumDebounce = CombinedStrumDebounce;
        model.BtRxAddr = BtRxMacAddress;
        model.LedBrightnessOn = (byte) LedBrightnessOn;
        model.LedBrightnessOff = (byte) LedBrightnessOff;
        model.DjFullRange = DjFullRange;
        model.DjNavButtons = DjNavButtons;
        model.SelectDpadLeftXb1 = SelectDpadLeftXb1;
        model.AccelFilter = AdxlFilter;
        var clone = new List<Output>(model.Bindings.Items);
        var generated = Bindings.Select(s => s.Generate(model)).SelectMany(s => s.Outputs.Items)
            .GroupBy(s => s.GetOutputType()).ToDictionary(s => s.Key, s => s);
        model.Bindings.Clear();
        model.Bindings.AddRange(clone);
        if (generated.Count == 0) return;
        var current = model.Bindings.Items.SelectMany(s => s.Outputs.Items).GroupBy(s => s.GetOutputType())
            .ToDictionary(s => s.Key, s => s);
        foreach (var (key, currentOutputs) in current)
        {
            if (!generated.TryGetValue(key, out var outputs)) continue;
            // Group up outputs by type, and then copy them across in order. This does mean that if someone uses multiple of the same output type, and changes the order around, calibration and LEDs may swap places, but that is fine.
            foreach (var (output, outputToMerge) in currentOutputs.Zip(outputs))
            {
                output.LedOn = outputToMerge.LedOn;
                output.LedOff = outputToMerge.LedOff;
                output.LedIndices = outputToMerge.LedIndices;
                output.LedIndicesPeripheral = outputToMerge.LedIndicesPeripheral;
                output.Enabled = outputToMerge.Enabled;
                switch (outputToMerge)
                {
                    case DjAxis djToMerge when output is DjAxis outputDj:
                        outputDj.Multiplier = djToMerge.Multiplier;
                        break;
                    case DrumAxis drumToMerge when output is DrumAxis outputDrum:
                        outputDrum.Debounce = drumToMerge.Debounce;
                        break;
                    case JoystickToDpad dpadToMerge when output is JoystickToDpad dpad:
                        dpadToMerge.Threshold = dpad.Threshold;
                        break;
                }

                switch (outputToMerge)
                {
                    case OutputAxis axisToMerge when output is OutputAxis outputAxis:
                        outputAxis.Min = axisToMerge.Min;
                        outputAxis.Max = axisToMerge.Max;
                        outputAxis.DeadZone = axisToMerge.DeadZone;
                        break;
                    case OutputButton buttonToMerge when output is OutputButton outputButton:
                        outputButton.Debounce = buttonToMerge.Debounce;
                        break;
                }

                if (outputToMerge.Input is AnalogToDigital adcToMerge && output.Input is AnalogToDigital adc)
                {
                    adc.Threshold = adcToMerge.Threshold;
                }
            }
        }
    }
}