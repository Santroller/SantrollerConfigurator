using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public partial class Ps2CombinedOutput : CombinedSpiOutput, ISpi
{
    private readonly DirectPinConfig _ackConfig;
    private readonly DirectPinConfig _attConfig;

    public Ps2CombinedOutput(ConfigViewModel model, bool peripheral, int miso = -1, int mosi = -1,
        int sck = -1, int att = -1, int ack = -1) : base(model, Ps2Input.Ps2SpiType,
        peripheral, Ps2Input.Ps2SpiFreq, Ps2Input.Ps2SpiCpol, Ps2Input.Ps2SpiCpha, Ps2Input.Ps2SpiMsbFirst, "PS2", miso,
        mosi, sck)
    {
        Outputs.Clear();
        _ackConfig = Model.GetPinForType(Ps2Input.Ps2AckType, peripheral, ack, DevicePinMode.Floating);
        _attConfig = Model.GetPinForType(Ps2Input.Ps2AttType, peripheral, att, DevicePinMode.Output);
        this.WhenAnyValue(x => x._attConfig.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Att)));
        this.WhenAnyValue(x => x._ackConfig.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Ack)));

        Outputs.Connect().Filter(x => x is OutputAxis)
            .Filter(s => s.IsVisible)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Length != 0)
            .Filter(this.WhenAnyValue(x => x.ControllerFound, x => x.DetectedType, x => x.SelectedType)
                .Select(CreateFilter))
            .Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton or JoystickToDpad or StartSelectHome)
            .Filter(s => s.IsVisible)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Length != 0)
            .Filter(this.WhenAnyValue(x => x.ControllerFound, x => x.DetectedType, x => x.SelectedType)
                .Select(CreateFilter))
            .Bind(out var digitalOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton or JoystickToDpad or StartSelectHome or {Input.IsAnalog: false})
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Length != 0)
            .Filter(this.WhenAnyValue(x => x.ControllerFound, x => x.DetectedType, x => x.SelectedType)
                .Select(CreateFilter))
            .Bind(out var allDigitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
        AllDigitalOutputs = allDigitalOutputs;
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

    public ReadOnlyObservableCollection<int> AvailablePinsInterrupt => Model.AvailablePinsInterrupt;

    [Reactive] private Ps2ControllerType _detectedType;
    [Reactive] private Ps2ControllerType _selectedType  = Ps2ControllerType.Selected;
    public IEnumerable<Ps2ControllerType> Ps2ControllerTypes => Enum.GetValues<Ps2ControllerType>();

    [Reactive] private bool _controllerFound;

    public override void SetOutputsOrDefaults(IEnumerable<Output> outputs)
    {
        Outputs.Clear();
        Outputs.AddRange(outputs);
        if (Outputs.Count == 0)
        {
            CreateDefaults();
        }
    }

    public override HostInput MakeInput(UsbHostInputType type)
    {
        return new Ps2Input(type, Model, false, Miso, Mosi, Sck, Att, Ack, IsCombined);
    }


    public override HostInput? MakeInput(ProKeyType type)
    {
        return null;
    }

    private static Func<Output, bool> CreateFilter(
        (bool controllerFound, Ps2ControllerType detectedType, Ps2ControllerType selectedType) tuple)
    {
        // TODO: this?
        // if (tuple.selectedType == Ps2ControllerType.All)
        // {
            return _ => true;
        // }
        //
        // var controllerType = tuple.selectedType;
        // if (controllerType == Ps2ControllerType.Selected)
        // {
        //     controllerType = tuple.detectedType;
        //     if (!tuple.controllerFound)
        //     {
        //         return _ => true;
        //     }
        // }
        //
        // return output => output is JoystickToDpad ||
        //                  (output.Input.InnermostInputs().First() is Ps2Input ps2Input &&
        //                   ps2Input.SupportsType(controllerType));
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return Resources.Ps2CombinedTitle;
    }

    public override Enum GetOutputType()
    {
        return SimpleType.Ps2InputSimple;
    }


    public override SerializedOutput Serialize()
    {
        return new SerializedPs2CombinedOutput(Peripheral, Miso, Mosi, Sck, Att, Ack, Outputs.Items.ToList());
    }


    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> bluetoothRaw, ReadOnlySpan<byte> usbHostInputsRaw,
        ReadOnlySpan<byte> peripheralWtRaw, Dictionary<int, bool> digitalPeripheral,
        ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw, ReadOnlySpan<byte> mpr121Raw,
        ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType,
            wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw, digitalPeripheral, cloneRaw,
            adxlRaw, mpr121Raw, midiRaw, bluetoothInputsRaw);
        if (ps2ControllerType.IsEmpty)
        {
            ControllerFound = false;
            return;
        }

        var type = ps2ControllerType[0];
        if (!Enum.IsDefined(typeof(Ps2ControllerType), type))
        {
            ControllerFound = false;
            return;
        }

        ControllerFound = true;
        var newType = (Ps2ControllerType) type;
        DetectedType = newType;
    }

    protected override IEnumerable<PinConfig> GetOwnPinConfigs()
    {
        return [SpiConfig, _attConfig, _ackConfig];
    }
}