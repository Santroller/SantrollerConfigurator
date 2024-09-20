using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public class MidiCombinedOutput : CombinedOutput
{
    public MidiCombinedOutput(ConfigViewModel model, int firstNote) : base(
        model)
    {
        FirstNote = firstNote;
        Outputs.Clear();
        Outputs.Connect().Filter(x => x is OutputAxis)
            .Filter(s => s.IsVisible)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Length != 0)
            .Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton)
            .Filter(s => s.IsVisible)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Length != 0)
            .Bind(out var digitalOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton or {Input.IsAnalog:false})
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Length != 0)
            .Bind(out var allDigitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
        AllDigitalOutputs = allDigitalOutputs;
        UpdateDetails();
    }

    public static readonly Dictionary<int, DrumAxisType> MappingsDrumGh = new()
    {
        {36, DrumAxisType.Kick},
        {38, DrumAxisType.Red},
        {40, DrumAxisType.Red},
        {43, DrumAxisType.Green},
        {45, DrumAxisType.Blue},
        {48, DrumAxisType.Yellow},
        {22, DrumAxisType.Yellow},
        {42, DrumAxisType.Yellow},
        {49, DrumAxisType.Orange},
        {52, DrumAxisType.Orange},
        {55, DrumAxisType.Orange},
        {57, DrumAxisType.Orange},
    };

    public static readonly Dictionary<int, DrumAxisType> MappingsDrumRb = new()
    {
        {35, DrumAxisType.Kick2},
        {36, DrumAxisType.Kick},
        {38, DrumAxisType.Red},
        {40, DrumAxisType.Red},
        {43, DrumAxisType.Green},
        {45, DrumAxisType.Blue},
        {48, DrumAxisType.Yellow},
        {26, DrumAxisType.BlueCymbal},
        {46, DrumAxisType.BlueCymbal},
        {22, DrumAxisType.YellowCymbal},
        {42, DrumAxisType.YellowCymbal},
        {49, DrumAxisType.GreenCymbal},
        {52, DrumAxisType.GreenCymbal},
        {55, DrumAxisType.GreenCymbal},
        {57, DrumAxisType.GreenCymbal},
        {51, DrumAxisType.BlueCymbal},
        {53, DrumAxisType.BlueCymbal},
        {59, DrumAxisType.BlueCymbal},
    };

    public override SerializedOutput Serialize()
    {
        return new SerializedCombinedMidiOutput(Outputs.Items.ToList(), (byte) FirstNote);
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return Resources.MidiInputsTitle;
    }

    public override Enum GetOutputType()
    {
        return SimpleType.Midi;
    }

    public override void SetOutputsOrDefaults(IReadOnlyCollection<Output> outputs)
    {
        Outputs.Clear();
        if (outputs.Count != 0)
            Outputs.AddRange(outputs);
        else
            CreateDefaults();
    }

    private int _firstNote;

    public int FirstNote
    {
        get => _firstNote;
        set
        {
            if (Model.DeviceControllerType is not DeviceControllerType.ProKeys) return;
            this.RaiseAndSetIfChanged(ref _firstNote, value);
            UpdateDefaults();
        }
    }

    public void UpdateDefaults()
    {
        if (Model.DeviceControllerType is not DeviceControllerType.ProKeys) return;
        var outputs = Outputs.Items.OfType<PianoKey>().Where(s => s.Key is not (ProKeyType.Pedal or ProKeyType.TouchPad or ProKeyType.Overdrive)).Select(s => new PianoKey(Model,
            new MidiInput(MidiType.Note, FirstNote + (int) s.Key, Model), s.LedOn,
            s.LedOff, s.LedIndices.ToArray(), s.LedIndicesPeripheral.ToArray(), s.LedIndicesMpr121.ToArray(), s.Key,
            0,s.OutputEnabled, false,
            s.OutputInverted, s.OutputPin, true) {Expanded = s.Expanded}).ToList();
        Outputs.Clear();
        Outputs.AddRange(outputs);
    }

    public void CreateDefaults()
    {
        Outputs.Clear();
        switch (Model.DeviceControllerType)
        {
            case DeviceControllerType.GuitarHeroDrums:
                foreach (var (key, drumAxisType) in MappingsDrumGh)
                {
                    Outputs.Add(new DrumAxis(Model, new MidiInput(MidiType.Note, key, Model), Colors.Black,
                        Colors.Black, [], [], [], ushort.MaxValue / 2, ushort.MaxValue, 0, 0,
                        drumAxisType, false, false, false, -1, true));
                }

                break;
            case DeviceControllerType.RockBandDrums:
                foreach (var (key, drumAxisType) in MappingsDrumRb)
                {
                    Outputs.Add(new DrumAxis(Model, new MidiInput(MidiType.Note, key, Model), Colors.Black,
                        Colors.Black, [], [], [], ushort.MaxValue / 2, ushort.MaxValue, 0, 0,
                        drumAxisType, false, false, false, -1, true));
                }

                break;
            case DeviceControllerType.ProKeys:
                foreach (var key in Enum.GetValues<ProKeyType>())
                {
                    switch (key)
                    {
                        case ProKeyType.Overdrive:
                            Outputs.Add(new PianoKey(Model, new MidiInput(MidiType.ModWheel, 0, Model), Colors.Black,
                                Colors.Black, [], [], [], key, ushort.MaxValue/2,false, false, 
                                false, -1, true));
                            break;
                        case ProKeyType.TouchPad:
                            Outputs.Add(new PianoKey(Model, new MidiInput(MidiType.PitchWheel, 0, Model), Colors.Black,
                                Colors.Black, [], [], [], key, 0,false, false, 
                                false, -1, true));
                            break;
                        case ProKeyType.Pedal:
                            Outputs.Add(new PianoKey(Model, new MidiInput(MidiType.SustainPedal, 0, Model), Colors.Black,
                                Colors.Black, [], [], [], key, ushort.MaxValue/2,false, false, 
                                false, -1, true));
                            break;
                        default:
                            Outputs.Add(new PianoKey(Model, new MidiInput(MidiType.Note, FirstNote+(int)key, Model), Colors.Black,
                                Colors.Black, [], [], [], key, 0,false, false, 
                                false, -1, true));
                            break;
                    }
                }

                break;
        }
    }

    public override void UpdateBindings()
    {
        CreateDefaults();
    }
}