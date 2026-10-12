using System;
using System.Collections.Generic;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

// Shared logic for the Guitar Hero World Tour and Band Hero drum brains, which both report pad velocities
public static class DrumBrain
{
    public static readonly Dictionary<DrumBrainInputType, DrumAxisType> MappingsGh = new()
    {
        {DrumBrainInputType.Kick, DrumAxisType.Kick},
        {DrumBrainInputType.Red, DrumAxisType.Red},
        {DrumBrainInputType.Yellow, DrumAxisType.Yellow},
        {DrumBrainInputType.Blue, DrumAxisType.Blue},
        {DrumBrainInputType.Orange, DrumAxisType.Orange},
        {DrumBrainInputType.Green, DrumAxisType.Green}
    };

    // The GH yellow and orange pads are cymbals, so map them to the closest RB cymbals
    public static readonly Dictionary<DrumBrainInputType, DrumAxisType> MappingsRb = new()
    {
        {DrumBrainInputType.Kick, DrumAxisType.Kick},
        {DrumBrainInputType.Red, DrumAxisType.Red},
        {DrumBrainInputType.Yellow, DrumAxisType.YellowCymbal},
        {DrumBrainInputType.Blue, DrumAxisType.Blue},
        {DrumBrainInputType.Orange, DrumAxisType.GreenCymbal},
        {DrumBrainInputType.Green, DrumAxisType.Green}
    };

    public static Dictionary<DrumBrainInputType, DrumAxisType> MappingsFor(DeviceControllerType type)
    {
        return type is DeviceControllerType.RockBandDrums ? MappingsRb : MappingsGh;
    }

    public static string Generate(DrumBrainInputType input)
    {
        return $"(drum_brain_velocities[{(int) input}] << 8)";
    }

    public static int? Read(ConfigViewModel model, DrumBrainInputType input)
    {
        if (model.Device is not Santroller {DrumBrainRaw.Length: >= 1 + (int) DrumBrainInputType.Green + 1} santroller) return null;
        return santroller.DrumBrainRaw[1 + (int) input] << 8;
    }

    public static bool Detected(ConfigViewModel model)
    {
        return model.Device is Santroller {DrumBrainRaw.Length: > 0} santroller && santroller.DrumBrainRaw[0] != 0;
    }
}
