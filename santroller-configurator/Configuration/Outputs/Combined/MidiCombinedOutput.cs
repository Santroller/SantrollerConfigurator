using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Conversions;
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

    public static readonly Dictionary<int, DrumAxisType> MappingsDrumRb = new()
    {
        {38, DrumAxisType.Red},
        {31, DrumAxisType.Red},
        {34, DrumAxisType.Red},
        {37, DrumAxisType.Red},
        {39, DrumAxisType.Red},
        {40, DrumAxisType.Red},
        {48, DrumAxisType.Yellow},
        {50, DrumAxisType.Yellow},
        {45, DrumAxisType.Blue},
        {47, DrumAxisType.Blue},
        {41, DrumAxisType.Green},
        {43, DrumAxisType.Green},
        {22, DrumAxisType.YellowCymbal},
        {26, DrumAxisType.YellowCymbal},
        {42, DrumAxisType.YellowCymbal},
        {46, DrumAxisType.YellowCymbal},
        {54, DrumAxisType.YellowCymbal},
        {51, DrumAxisType.BlueCymbal},
        {53, DrumAxisType.BlueCymbal},
        {56, DrumAxisType.BlueCymbal},
        {59, DrumAxisType.BlueCymbal},
        {49, DrumAxisType.GreenCymbal},
        {52, DrumAxisType.GreenCymbal},
        {55, DrumAxisType.GreenCymbal},
        {57, DrumAxisType.GreenCymbal},
        {33, DrumAxisType.Kick},
        {35, DrumAxisType.Kick},
        {36, DrumAxisType.Kick},
        {44, DrumAxisType.Kick2}, // Hi-Hat Pedal
        {100, DrumAxisType.Kick2}, // Hi-Hat Pedal
    };
    
    public static readonly Dictionary<int, DrumAxisType> MappingsDrumGh = MappingsDrumRb.Select(RbToGh).ToDictionary();

    private static KeyValuePair<int, DrumAxisType> RbToGh(KeyValuePair<int, DrumAxisType> mapping)
    {
        return mapping.Value switch
        {
            DrumAxisType.Green or DrumAxisType.Red or DrumAxisType.Blue or DrumAxisType.Kick
                or DrumAxisType.Kick2 => mapping,
            DrumAxisType.Yellow or DrumAxisType.YellowCymbal => new KeyValuePair<int, DrumAxisType>(mapping.Key,
                DrumAxisType.Yellow),
            DrumAxisType.GreenCymbal => new KeyValuePair<int, DrumAxisType>(mapping.Key, DrumAxisType.Orange),
            _ => mapping
        };
    }
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

    public override void SetOutputsOrDefaults(IEnumerable<Output> outputs)
    {
        Outputs.Clear();
        Outputs.AddRange(outputs);
        if (Outputs.Count == 0)
        {
            CreateDefaults();
        }
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
        var outputs = Outputs.Items.OfType<PianoKey>().Where(s => s.Key is not (ProKeyType.PedalAnalog or ProKeyType.PedalDigital or ProKeyType.TouchPad or ProKeyType.Overdrive)).Select(s => new PianoKey(Model, true,
            new MidiInput(MidiType.Note, FirstNote + (int) s.Key, Model), s.LedOn,
            s.LedOff, s.LedIndices.ToArray(), s.LedIndicesPeripheral.ToArray(), s.LedIndicesMpr121.ToArray(), s.Key,
            s.OutputEnabled, false,
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
                    Outputs.Add(new DrumAxis(Model, true,new MidiInput(MidiType.Note, key, Model), Colors.Black,
                        Colors.Black, [], [], [], ushort.MaxValue / 2, ushort.MaxValue, 0, 0,
                        drumAxisType, false, false, false, -1, true));
                }

                break;
            case DeviceControllerType.RockBandDrums:
                foreach (var (key, drumAxisType) in MappingsDrumRb)
                {
                    Outputs.Add(new DrumAxis(Model, true, new MidiInput(MidiType.Note, key, Model), Colors.Black,
                        Colors.Black, [], [], [], ushort.MaxValue / 2, ushort.MaxValue, 0, 0,
                        drumAxisType, false, false, false, -1, true));
                }

                break;
            case DeviceControllerType.ProKeys:
                foreach (var key in Enum.GetValues<ProKeyType>())
                {
                    switch (key)
                    {
                        case ProKeyType.PedalAnalog:
                            Outputs.Add(new PianoKey(Model, true,new MidiInput(MidiType.SustainPedal, 0, Model), Colors.Black,
                                Colors.Black, [], [], [], key, false, false, 
                                false, -1, true));
                            break;
                        case ProKeyType.TouchPad:
                            Outputs.Add(new PianoKey(Model, true,new MidiInput(MidiType.PitchWheel, 0, Model), Colors.Black,
                                Colors.Black, [], [], [], key, false, false, 
                                false, -1, true));
                            break;
                        case ProKeyType.Overdrive:
                            Outputs.Add(new PianoKeyButton(Model, true,new AnalogToDigital(new MidiInput(MidiType.ModWheel, 0, Model), AnalogToDigitalType.Trigger, ushort.MaxValue/2, Model), Colors.Black,
                                Colors.Black, [], [], [], key, false, false, 
                                false, -1, true));
                            break;
                        case ProKeyType.PedalDigital:
                            Outputs.Add(new PianoKeyButton(Model, true,new AnalogToDigital(new MidiInput(MidiType.SustainPedal, 0, Model), AnalogToDigitalType.Trigger, ushort.MaxValue/2, Model), Colors.Black,
                                Colors.Black, [], [], [], key, false, false, 
                                false, -1, true));
                            break;
                        default:
                            Outputs.Add(new PianoKey(Model, true,new MidiInput(MidiType.Note, FirstNote+(int)key, Model), Colors.Black,
                                Colors.Black, [], [], [], key, false, false, 
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