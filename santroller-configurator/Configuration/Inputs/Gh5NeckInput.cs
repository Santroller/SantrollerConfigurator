using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class Gh5NeckInput : TwiInput
{
    public static readonly string Gh5TwiType = "gh5";
    public static readonly int Gh5TwiFreq = 100000;

    public static readonly Dictionary<int, BarButton> Gh5Mappings = new()
    {
        {0x15, BarButton.Green},
        {0x30, BarButton.Green | BarButton.Red},
        {0x4D, BarButton.Red},
        {0x65, BarButton.Green | BarButton.Red | BarButton.Yellow},
        {0x66, BarButton.Red | BarButton.Yellow},
        {0x80, 0x00},
        {0x99, BarButton.Green | BarButton.Yellow},
        {0x9A, BarButton.Yellow},
        {0xAC, BarButton.Green | BarButton.Red | BarButton.Yellow | BarButton.Blue},
        {0xAD, BarButton.Green | BarButton.Yellow | BarButton.Blue},
        {0xAE, BarButton.Red | BarButton.Yellow | BarButton.Blue},
        {0xAF, BarButton.Yellow | BarButton.Blue},
        {0xC6, BarButton.Green | BarButton.Red | BarButton.Blue},
        {0xC7, BarButton.Green | BarButton.Blue},
        {0xC8, BarButton.Red | BarButton.Blue},
        {0xC9, BarButton.Blue},
        {0xDF, BarButton.Green | BarButton.Red | BarButton.Yellow | BarButton.Blue | BarButton.Orange},
        {0xE0, BarButton.Green | BarButton.Red | BarButton.Blue | BarButton.Orange},
        {0xE1, BarButton.Green | BarButton.Yellow | BarButton.Blue | BarButton.Orange},
        {0xE2, BarButton.Green | BarButton.Blue | BarButton.Orange},
        {0xE3, BarButton.Red | BarButton.Yellow | BarButton.Blue | BarButton.Orange},
        {0xE4, BarButton.Red | BarButton.Blue | BarButton.Orange},
        {0xE5, BarButton.Yellow | BarButton.Blue | BarButton.Orange},
        {0xE6, BarButton.Blue | BarButton.Orange},
        {0xF8, BarButton.Green | BarButton.Red | BarButton.Yellow | BarButton.Orange},
        {0xF9, BarButton.Green | BarButton.Red | BarButton.Orange},
        {0xFA, BarButton.Green | BarButton.Yellow | BarButton.Orange},
        {0xFB, BarButton.Green | BarButton.Orange},
        {0xFC, BarButton.Red | BarButton.Yellow | BarButton.Orange},
        {0xFD, BarButton.Red | BarButton.Orange},
        {0xFE, BarButton.Yellow | BarButton.Orange},
        {0xFF, BarButton.Orange},
    };

    public static readonly Dictionary<BarButton, int> Gh5MappingsReversed =
        Gh5Mappings.ToDictionary(x => x.Value, x => x.Key);

    private static readonly Dictionary<Gh5NeckInputType, int> Fret = new()
    {
        {Gh5NeckInputType.Green, 4},
        {Gh5NeckInputType.Red, 5},
        {Gh5NeckInputType.Yellow, 7},
        {Gh5NeckInputType.Blue, 6},
        {Gh5NeckInputType.Orange, 0},
    };

    private static readonly List<Gh5NeckInputType> Tap =
    [
        Gh5NeckInputType.TapGreen,
        Gh5NeckInputType.TapRed,
        Gh5NeckInputType.TapYellow,
        Gh5NeckInputType.TapBlue,
        Gh5NeckInputType.TapOrange
    ];

    private static readonly Dictionary<Gh5NeckInputType, BarButton> InputToButton = new()
    {
        {Gh5NeckInputType.TapGreen, BarButton.Green},
        {Gh5NeckInputType.TapRed, BarButton.Red},
        {Gh5NeckInputType.TapYellow, BarButton.Yellow},
        {Gh5NeckInputType.TapBlue, BarButton.Blue},
        {Gh5NeckInputType.TapOrange, BarButton.Orange}
    };

    private static readonly Dictionary<Gh5NeckInputType, ReadOnlyCollection<int>> MappingByInput =
        Tap.ToDictionary(type => type,
            type => Gh5Mappings.Where(mapping => mapping.Value.HasFlag(InputToButton[type]))
                .Select(mapping => mapping.Key).ToList().AsReadOnly());

    public Gh5NeckInput(Gh5NeckInputType input, ConfigViewModel model, bool peripheral, int sda = -1,
        int scl = -1, bool combined = false) : base(
        Gh5TwiType, Gh5TwiFreq, peripheral, sda, scl, model)
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

    public override InputType? InputType => Types.InputType.Gh5NeckInput;
    public Gh5NeckInputType Input { get; set; }

    public override IList<DevicePin> Pins => Array.Empty<DevicePin>();
    public override bool IsUint => true;

    public override string Generate(BinaryWriter? writer)
    {
        if (Input <= Gh5NeckInputType.Orange)
            return $"(fivetar_buttons[0] & {1 << Fret[Input]})";

        if (Input is Gh5NeckInputType.TapBar or Gh5NeckInputType.TapAll) return "(fivetar_buttons[1] ^ 0x80)";

        var mappings = MappingByInput[Input];
        return "(gh5Valid && (" +
               string.Join(" || ", mappings.Select(mapping => $"(fivetar_buttons[1] == {mapping ^ 0x80})")) + "))";
    }

    public override SerializedInput Serialise()
    {
        if (Combined) return new SerializedGh5NeckInputCombined(Input, Peripheral);
        return new SerializedGh5NeckInput(Peripheral, Sda, Scl, Input);
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw,
        ReadOnlySpan<byte> mpr121Raw, ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw)
    {
        if (gh5Raw.IsEmpty) return;
        switch (Input)
        {
            case <= Gh5NeckInputType.Orange:
                RawValue = (gh5Raw[0] & (1 << Fret[Input])) != 0 ? 1 : 0;
                break;
            case Gh5NeckInputType.TapBar:
                RawValue = (gh5Raw[1] + 0x80) & 0xFF;
                break;
            case Gh5NeckInputType.TapAll:
                RawValue = (gh5Raw[1] + 0x80) & 0xFF;
                break;
            default:
            {
                var mappings = MappingByInput[Input];
                RawValue = mappings.Contains(gh5Raw[1] ^ 0x80) ? 1 : 0;
                break;
            }
        }
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        return string.Join("\n", bindings.Select(binding => binding.Item2));
    }


    public override IReadOnlyList<string> RequiredDefines()
    {
        return base.RequiredDefines().Concat(new[] {"INPUT_GH5_NECK"}).ToList();
    }
}