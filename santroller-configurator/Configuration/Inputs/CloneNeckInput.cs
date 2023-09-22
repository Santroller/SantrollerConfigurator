using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class CloneNeckInput : TwiInput
{
    public static readonly string CloneTwiType = "clone";
    public static readonly int CloneTwiFreq = 100000;

    private static readonly Dictionary<Gh5NeckInputType, int> MappingsIdx = new()
    {
        {Gh5NeckInputType.TapGreen, 1},
        {Gh5NeckInputType.TapRed, 1},
        {Gh5NeckInputType.TapYellow,1 },
        {Gh5NeckInputType.TapBlue, 1},
        {Gh5NeckInputType.TapOrange, 2},
        {Gh5NeckInputType.Green, 2},
        {Gh5NeckInputType.Red, 2},
        {Gh5NeckInputType.Yellow, 2},
        {Gh5NeckInputType.Blue, 2},
        {Gh5NeckInputType.Orange, 2},
    };

    private static readonly Dictionary<Gh5NeckInputType, int> MappingsBit = new()
    {
        {Gh5NeckInputType.TapGreen, 0x08},
        {Gh5NeckInputType.TapRed, 0x04},
        {Gh5NeckInputType.TapYellow, 0x02},
        {Gh5NeckInputType.TapBlue, 0x01},
        {Gh5NeckInputType.TapOrange, 0x80},
        {Gh5NeckInputType.Green, 0x40},
        {Gh5NeckInputType.Red, 0x01},
        {Gh5NeckInputType.Yellow, 0x02},
        {Gh5NeckInputType.Blue, 0x10},
        {Gh5NeckInputType.Orange, 0x20},
    };

    private static readonly Dictionary<Gh5NeckInputType, BarButton> InputToButton = new()
    {
        {Gh5NeckInputType.TapGreen, BarButton.Green},
        {Gh5NeckInputType.TapRed, BarButton.Red},
        {Gh5NeckInputType.TapYellow, BarButton.Yellow},
        {Gh5NeckInputType.TapBlue, BarButton.Blue},
        {Gh5NeckInputType.TapOrange, BarButton.Orange}
    };
    private static readonly List<Gh5NeckInputType> Taps = new()
    {
        Gh5NeckInputType.TapGreen,
        Gh5NeckInputType.TapRed,
        Gh5NeckInputType.TapYellow,
        Gh5NeckInputType.TapBlue,
        Gh5NeckInputType.TapOrange
    };

    public CloneNeckInput(Gh5NeckInputType input, ConfigViewModel model, bool peripheral, int sda = -1,
        int scl = -1, bool combined = false) : base(
        CloneTwiType, CloneTwiFreq, peripheral, sda, scl, model)
    {
        Combined = combined;
        BindableTwi = !combined && Model.Microcontroller.TwiAssignable && !model.Branded;
        Input = input;
        IsAnalog = Input == Gh5NeckInputType.TapBar;
    }

    public override string Title => EnumToStringConverter.Convert(Input);
    public bool Combined { get; }
    public bool ShouldShowPins => !Combined && !Model.Branded; 
    public bool BindableTwi { get; }

    public override InputType? InputType => Types.InputType.CloneNeckInput;
    public Gh5NeckInputType Input { get; set; }

    public override IList<DevicePin> Pins => Array.Empty<DevicePin>();
    public override bool IsUint => true;

    private string GetMapping(Gh5NeckInputType inputType)
    {
        return $"clone_buttons[{MappingsIdx[inputType]}] & {MappingsBit[inputType]}";
    }
    public override string Generate()
    {
        return Input <= Gh5NeckInputType.TapOrange ? GetMapping(Input) : string.Join(" | ", Taps.Select((type, i) => $"(({GetMapping(type)}) != 0) << {i}"));
    }

    public override SerializedInput Serialise()
    {
        if (Combined) return new SerializedCloneNeckInputCombined(Input, Peripheral);
        return new SerializedCloneNeckInput(Peripheral, Sda, Scl, Input);
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw)
    {
        if (cloneRaw.IsEmpty) return;
        switch (Input)
        {
            case <= Gh5NeckInputType.TapOrange:
                RawValue = (cloneRaw[MappingsIdx[Input]] & MappingsBit[Input]) != 0 ? 1 : 0;
                break;
            case Gh5NeckInputType.TapBar:
            case Gh5NeckInputType.TapAll:
                BarButton output = 0;
                foreach (var type in Taps)
                {
                    if ((cloneRaw[MappingsIdx[type]] & MappingsBit[type]) != 0)
                    {
                        output |= InputToButton[type];
                    }
                }

                RawValue = Gh5NeckInput.Gh5MappingsReversed.TryGetValue(output, out var value) ? value : 0;
                break;
        }
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        return string.Join("\n", bindings.Select(binding => binding.Item2));
    }


    public override IReadOnlyList<string> RequiredDefines()
    {
        return base.RequiredDefines().Concat(new[] {"INPUT_CLONE_NECK"}).ToList();
    }
}