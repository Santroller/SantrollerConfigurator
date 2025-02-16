using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public partial class MidiInput : Input
{
    public static readonly string[] Notes = ["A","A#/Bb","B","C","C#/Db","D","D#/Eb","E","F","F#/Gb","G","G#/Ab"];
    public static readonly Dictionary<int, string> Drums = new Dictionary<int, string>()
    {
        {35, "Acoustic Bass Drum or Low Bass Drum"},
        {36, "Electric Bass Drum or High Bass Drum"},
        {37, "Side Stick"},
        {38, "Acoustic Snare"},
        {39, "Hand Clap"},
        {40, "Electric Snare or Rimshot"},
        {41, "Low Floor Tom"},
        {42, "Closed Hi-hat"},
        {43, "High Floor Tom"},
        {44, "Pedal Hi-hat"},
        {45, "Low Tom"},
        {46, "Open Hi-hat"},
        {47, "Low-Mid Tom"},
        {48, "High-Mid Tom"},
        {49, "Crash Cymbal 1"},
        {50, "High Tom"},
        {51, "Ride Cymbal 1"},
        {52, "Chinese Cymbal"},
        {53, "Ride Bell"},
        {54, "Tambourine"},
        {55, "Splash Cymbal"},
        {56, "Cowbell"},
        {57, "Crash Cymbal 2"},
        {58, "Vibraslap"},
        {59, "Ride Cymbal 2"},
        {60, "High Bongo"},
        {61, "Low Bongo"},
        {62, "Mute High Conga"},
        {63, "Open High Conga"},
        {64, "Low Conga"},
        {65, "High Timbale"},
        {66, "Low Timbale"},
        {67, "High Agogô"},
        {68, "Low Agogô"},
        {69, "Cabasa"},
        {70, "Maracas"},
        {71, "Short Whistle"},
        {72, "Long Whistle"},
        {73, "Short Guiro"},
        {74, "Long Guiro"},
        {75, "Claves"},
        {76, "High Woodblock"},
        {77, "Low Woodblock"},
        {78, "Mute Cuica"},
        {79, "Open Cuica"},
        {80, "Mute Triangle"},
        {81, "Open Triangle"}
    };
    public MidiInput(MidiType input, int key, ConfigViewModel model, bool combined = false) : base(model)
    {
        Key = key;
        Combined = combined;
        Input = input;
        IsAnalog = true;
    }

    public bool Combined { get; }
    public override bool Peripheral => false;

    public MidiType Input { get; }

    public int Key { get; }

    public static string GetNote(int key, DeviceControllerType deviceControllerType)
    {
        if (deviceControllerType is DeviceControllerType.ProKeys)
        {
            if (key < 21)
            {
                return key.ToString();
            }

            key -= 21;
            return $"{Notes[key % Notes.Length]}{(key / Notes.Length)} ({key + 21})";
        }

        if (deviceControllerType is DeviceControllerType.GuitarHeroDrums or DeviceControllerType.RockBandDrums)
        {
            if (Drums.TryGetValue(key, out var drum))
            {
                return $"{drum} ({key})";
            }
        }

        return $"Midi Note {key}";
    }

    public override bool IsUint => Input is not MidiType.PitchWheel;
    public override IList<DevicePin> Pins => [];
    public override IList<PinConfig> PinConfigs => [];

    public override InputType? InputType => Types.InputType.MidiInput;

    public override string Title =>
        Input switch
        {
            MidiType.Note => string.Format(Resources.MidiNoteLabel, GetNote(Key, Model.DeviceControllerType)),
            _ => EnumToStringConverter.Convert(Input)
        };


    [Reactive] private string _usbHostInfo = "";
    [Reactive] private int _connectedDevices;

    public override IReadOnlyList<string> RequiredDefines()
    {
        return ["INPUT_MIDI"];
    }

    public override string Generate(BinaryWriter? writer)
    {
        return Input switch
        {
            MidiType.Note => $"(midiData.midiVelocitiesTemp[{Key}] << 8)",
            MidiType.PitchWheel => "midiData.midiPitchWheel",
            MidiType.ModWheel => "(midiData.midiModWheel << 8)",
            MidiType.SustainPedal => "(midiData.midiSustainPedal << 8)",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override SerializedInput Serialise()
    {
        return new SerializedMidiInput(Input, Key);
    }

    public override void Update(Dictionary<int, int> analogRaw, Dictionary<int, bool> digitalRaw,
        ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw,
        ReadOnlySpan<byte> mpr121Raw, ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw,
        bool peripheralConnected)
    {
        if (Combined || midiRaw.IsEmpty) return;
        var inputs = StructTools.RawDeserialize<MidiInputs>(midiRaw, 0);
        RawValue = Input switch
        {
            MidiType.Note => inputs.velocities[Key] << 8,
            MidiType.PitchWheel => inputs.pitchWheel,
            MidiType.ModWheel => inputs.modWheel << 8,
            MidiType.SustainPedal => inputs.sustainPedal << 8,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MidiInputs
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=128)] public readonly byte[] velocities;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=128)] public readonly byte[] velocitiesTemp;
        public readonly short pitchWheel;
        public readonly byte modWheel;
        public readonly byte sustainPedal;

    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings, ConfigField mode)
    {
        return string.Join("\n", bindings.Select(binding => binding.Item2));
    }

}