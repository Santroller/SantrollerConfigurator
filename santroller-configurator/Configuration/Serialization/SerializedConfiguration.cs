using System.Collections.Generic;
using System.Linq;
using DynamicData;
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
        RolloverMode = model.RolloverMode;
        Bindings = bindings.Select(s => s.Serialize()).ToList();
        DeviceType = model.DeviceControllerType;
        EmulationType = model.EmulationType;
        XInputOnWindows = model.XInputOnWindows;
        XInputAuth = model.XInputAuth;
        LedType = model.LedType;
        LedMosi = model.LedMosi;
        LedSck = model.LedSck;
        LedCount = model.LedCount;
        LedBrightness = model.LedBrightness;
        MouseMovementType = model.MouseMovementType;
        WtSensitivity = model.WtSensitivity;
        UsbHostDp = model.UsbHostDp;
        Mode = model.Mode;
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
    [ProtoMember(23)] public ModeType Mode { get; private set; }
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
    [ProtoMember(41)] public int PeripheralScl{ get; private set; }
    [ProtoMember(42)] public int Stp16Oe { get; private set; }
    [ProtoMember(43)] public int Stp16Le { get; private set; }
    [ProtoMember(44)] public int Stp16OePeripheral { get; private set; }
    [ProtoMember(45)] public int Stp16LePeripheral { get; private set; }
    [ProtoMember(46)] public RolloverMode RolloverMode { get; private set; }
    [ProtoMember(47)] public bool XInputAuth { get; private set; }
    [ProtoMember(48)] public int LedBrightness { get; private set; }
    [ProtoMember(49)] public bool Apa102IsFullSize { get; private set; }

    [ProtoMember(50)] public bool Ps3OnRpcs3 { get; private set; } = true;

    public void LoadConfiguration(ConfigViewModel model)
    {
        model.SetDeviceTypeAndRhythmTypeWithoutUpdating(DeviceType, EmulationType);
        model.XInputOnWindows = XInputOnWindows;
        model.Ps3OnRpcs3 = Ps3OnRpcs3;
        model.XInputAuth = XInputAuth;
        model.Bindings.Clear();
        model.Mode = Mode;
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
        model.RolloverMode = RolloverMode;
        model.LedBrightness = LedBrightness == 0 ? 32 : LedBrightness;
        model.Apa102IsFullSize = Apa102IsFullSize;
        if (HasPeripheral)
        {
            model.PeripheralScl = PeripheralScl;
            model.PeripheralSda = PeripheralSda;
        }
        if (DjPollRate == 0)
        {
            model.DjPollRate = 1;
        }

        var generated = Bindings.Select(s => s.Generate(model)).ToList();
        if (generated.Any())
        {
            model.Bindings.Clear();
            model.Bindings.AddRange(generated);
            model.UpdateErrors();
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
        if (model.IsApa102Peripheral || model.IsStp16Peripheral)
        {
            model.LedMosiPeripheral = LedMosiPeripheral;
            model.LedSckPeripheral = LedSckPeripheral;
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
        model.XInputOnWindows = XInputOnWindows;
        model.XInputAuth = XInputAuth;
        model.PollRate = PollRate;
        model.Debounce = Debounce;
        model.StrumDebounce = StrumDebounce;
        model.Deque = QueueBasedInputs;
        model.DjPollRate = DjPollRate;
        model.DjSmoothing = DjSmooth;
        model.SwapSwitchFaceButtons = SwapSwitchFaceButtons;
        model.WtSensitivity = WtSensitivity;
        model.CombinedStrumDebounce = CombinedStrumDebounce;
        model.BtRxAddr = BtRxMacAddress;
        model.LedBrightness = LedBrightness;
        var clone = new List<Output>(model.Bindings.Items);
        var generated = Bindings.Select(s => s.Generate(model)).SelectMany(s => s.Outputs.Items)
            .GroupBy(s => s.GetOutputType()).ToDictionary(s => s.Key, s => s);
        model.Bindings.Clear();
        model.Bindings.AddRange(clone);
        if (generated.Any())
        {
            var current = model.Bindings.Items.SelectMany(s => s.Outputs.Items).GroupBy(s => s.GetOutputType())
                .ToDictionary(s => s.Key, s => s);
            foreach (var (key, currentOutputs) in current)
            {
                if (generated.TryGetValue(key, out var outputs))
                {
                    // Group up outputs by type, and then copy them across in order. This does mean that if someone uses multiple of the same output type, and changes the order around, calibration and LEDs may swap places, but that is fine.
                    foreach (var (output, outputToMerge) in currentOutputs.Zip(outputs))
                    {
                        output.LedOn = outputToMerge.LedOn;
                        output.LedOff = outputToMerge.LedOff;
                        output.LedIndices = outputToMerge.LedIndices;
                        output.LedIndicesPeripheral = outputToMerge.LedIndicesPeripheral;
                        if (outputToMerge is DjAxis djToMerge && output is DjAxis outputDj)
                        {
                            outputDj.Multiplier = djToMerge.Multiplier;
                            outputDj.Invert = djToMerge.Invert;
                        }

                        if (outputToMerge is DrumAxis drumToMerge && output is DrumAxis outputDrum)
                        {
                            outputDrum.Debounce = drumToMerge.Debounce;
                        }

                        if (outputToMerge is OutputAxis axisToMerge && output is OutputAxis outputAxis)
                        {
                            outputAxis.Min = axisToMerge.Min;
                            outputAxis.Max = axisToMerge.Max;
                            outputAxis.DeadZone = axisToMerge.DeadZone;
                        }

                        if (outputToMerge is OutputButton buttonToMerge && output is OutputButton outputButton)
                        {
                            outputButton.Debounce = buttonToMerge.Debounce;
                        }
                    }
                }
            }
        }
    }
}