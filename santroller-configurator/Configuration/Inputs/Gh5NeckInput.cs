using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        {0x19, BarButton.Green | BarButton.Yellow},
        {0x1A, BarButton.Yellow},
        {0x2C, BarButton.Green | BarButton.Red | BarButton.Yellow | BarButton.Blue},
        {0x2D, BarButton.Green | BarButton.Yellow | BarButton.Blue},
        {0x2E, BarButton.Red | BarButton.Yellow | BarButton.Blue},
        {0x2F, BarButton.Yellow | BarButton.Blue},
        {0x46, BarButton.Green | BarButton.Red | BarButton.Blue},
        {0x47, BarButton.Green | BarButton.Blue},
        {0x48, BarButton.Red | BarButton.Blue},
        {0x49, BarButton.Blue},
        {0x5F, BarButton.Green | BarButton.Red | BarButton.Yellow | BarButton.Blue | BarButton.Orange},
        {0x60, BarButton.Green | BarButton.Red | BarButton.Blue | BarButton.Orange},
        {0x61, BarButton.Green | BarButton.Yellow | BarButton.Blue | BarButton.Orange},
        {0x62, BarButton.Green | BarButton.Blue | BarButton.Orange},
        {0x63, BarButton.Red | BarButton.Yellow | BarButton.Blue | BarButton.Orange},
        {0x64, BarButton.Red | BarButton.Blue | BarButton.Orange},
        {0x65, BarButton.Yellow | BarButton.Blue | BarButton.Orange},
        {0x66, BarButton.Blue | BarButton.Orange},
        {0x78, BarButton.Green | BarButton.Red | BarButton.Yellow | BarButton.Orange},
        {0x79, BarButton.Green | BarButton.Red | BarButton.Orange},
        {0x7A, BarButton.Green | BarButton.Yellow | BarButton.Orange},
        {0x7B, BarButton.Green | BarButton.Orange},
        {0x7C, BarButton.Red | BarButton.Yellow | BarButton.Orange},
        {0x7D, BarButton.Red | BarButton.Orange},
        {0x7E, BarButton.Yellow | BarButton.Orange},
        {0x7F, BarButton.Orange},
        {0x95, BarButton.Green},
        {0xB0, BarButton.Green | BarButton.Red},
        {0xCD, BarButton.Red},
        {0xE5, BarButton.Green | BarButton.Red | BarButton.Yellow},
        {0xE6, BarButton.Red | BarButton.Yellow}
    };
    
    public static readonly Dictionary<BarButton, int> Gh5MappingsReversed = Gh5Mappings.ToDictionary(x => x.Value, x => x.Key);

    private static readonly List<Gh5NeckInputType> Tap = new()
    {
        Gh5NeckInputType.TapGreen,
        Gh5NeckInputType.TapRed,
        Gh5NeckInputType.TapYellow,
        Gh5NeckInputType.TapBlue,
        Gh5NeckInputType.TapOrange
    };

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

    public override string Generate()
    {
        if (Input <= Gh5NeckInputType.Orange)
            return $"(fivetar_buttons[0] & {1 << ((byte) Input - (byte) Gh5NeckInputType.Green)})";

        if (Input == Gh5NeckInputType.TapBar) return "fivetar_buttons[1]";

        var mappings = MappingByInput[Input];
        return "(gh5Valid && (" +
               string.Join(" || ", mappings.Select(mapping => $"(fivetar_buttons[1] == {mapping})")) + "))";
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
        Dictionary<int, bool> digitalPeripheral, Dictionary<int, int> analogPeripheral)
    {
        if (gh5Raw.IsEmpty) return;
        switch (Input)
        {
            case <= Gh5NeckInputType.Orange:
                RawValue = (gh5Raw[0] & (1 << ((byte) Input - (byte) Gh5NeckInputType.Green))) != 0 ? 1 : 0;
                break;
            case Gh5NeckInputType.TapBar:
                RawValue = gh5Raw[1];
                break;
            case Gh5NeckInputType.TapAll:
                RawValue = gh5Raw[1];
                break;
            default:
            {
                var mappings = MappingByInput[Input];
                RawValue = mappings.Contains(gh5Raw[1]) ? 1 : 0;
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
        if (Input <= Gh5NeckInputType.Orange) return base.RequiredDefines().Concat(new[] {"INPUT_GH5_NECK"}).ToList();

        return base.RequiredDefines().Concat(new[] {"INPUT_GH5_NECK", "INPUT_GH5_NECK_TAP_BAR"}).ToList();
    }
}