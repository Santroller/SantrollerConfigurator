using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using static GuitarConfigurator.NetCore.ViewModels.ConfigViewModel;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public partial class DrumAxis : OutputAxis
{
    private const StandardButtonType BlueCymbalFlag = StandardButtonType.DpadDown;
    private const StandardButtonType YellowCymbalFlag = StandardButtonType.DpadUp;

    private static readonly Dictionary<DrumAxisType, StandardButtonType> ButtonsXbox360 = new()
    {
        { DrumAxisType.Green, StandardButtonType.A },
        { DrumAxisType.Red, StandardButtonType.B },
        { DrumAxisType.Blue, StandardButtonType.X },
        { DrumAxisType.Yellow, StandardButtonType.Y },
        { DrumAxisType.GreenCymbal, StandardButtonType.A },
        { DrumAxisType.BlueCymbal, StandardButtonType.X },
        { DrumAxisType.YellowCymbal, StandardButtonType.Y },
        { DrumAxisType.Orange, StandardButtonType.RightShoulder },
        { DrumAxisType.Kick, StandardButtonType.LeftShoulder },
        { DrumAxisType.Kick2, StandardButtonType.LeftThumbClick }
    };

    private static readonly Dictionary<DrumAxisType, Key> _fortniteKeysGh = new()
    {
        { DrumAxisType.Red, Key.F },
        { DrumAxisType.Yellow, Key.G },
        { DrumAxisType.Blue, Key.H },
        { DrumAxisType.Orange, Key.J },
        { DrumAxisType.Green, Key.K },
        { DrumAxisType.Kick, Key.Space },
    };

    private static readonly Dictionary<DrumAxisType, Key> _fortniteProKeysGh = new()
    {
        { DrumAxisType.Red, Key.F },
        { DrumAxisType.Yellow, Key.G },
        { DrumAxisType.Blue, Key.H },
        { DrumAxisType.Orange, Key.J },
        { DrumAxisType.Green, Key.K },
        { DrumAxisType.Kick, Key.Space },
    };

    private static readonly Dictionary<DrumAxisType, Key> _fortniteKeysRb = new()
    {
        { DrumAxisType.Red, Key.F },
        { DrumAxisType.Yellow, Key.G },
        { DrumAxisType.Blue, Key.H },
        { DrumAxisType.Green, Key.J },
        { DrumAxisType.Kick, Key.K },
    };

    private static readonly Dictionary<DrumAxisType, Key> _fortniteProKeysRb = new()
    {
        { DrumAxisType.Red, Key.F },
        { DrumAxisType.Yellow, Key.G },
        { DrumAxisType.Blue, Key.H },
        { DrumAxisType.Green, Key.J },
        { DrumAxisType.Kick, Key.K },
    };

    private static readonly Dictionary<DrumAxisType, StandardButtonType> ButtonsXboxOne = new()
    {
        { DrumAxisType.Green, StandardButtonType.A },
        { DrumAxisType.Red, StandardButtonType.B },
        { DrumAxisType.Kick, StandardButtonType.LeftShoulder },
        { DrumAxisType.Kick2, StandardButtonType.RightShoulder }
    };

    private static readonly Dictionary<DrumAxisType, StandardButtonType> ButtonsPs3 = new()
    {
        { DrumAxisType.Green, StandardButtonType.A },
        { DrumAxisType.Red, StandardButtonType.B },
        { DrumAxisType.Blue, StandardButtonType.X },
        { DrumAxisType.Yellow, StandardButtonType.Y },
        { DrumAxisType.GreenCymbal, StandardButtonType.A },
        { DrumAxisType.BlueCymbal, StandardButtonType.X },
        { DrumAxisType.YellowCymbal, StandardButtonType.Y },
        { DrumAxisType.Kick, StandardButtonType.LeftShoulder },
        { DrumAxisType.Orange, StandardButtonType.RightShoulder },
        { DrumAxisType.Kick2, StandardButtonType.RightShoulder }
    };


    private static readonly Dictionary<DrumAxisType, string> AxisMappings = new()
    {
        { DrumAxisType.Green, "report->greenVelocity" },
        { DrumAxisType.Red, "report->redVelocity" },
        { DrumAxisType.Yellow, "report->yellowVelocity" },
        { DrumAxisType.Blue, "report->blueVelocity" },
        { DrumAxisType.Orange, "report->orangeVelocity" },
        { DrumAxisType.GreenCymbal, "report->greenVelocity" },
        { DrumAxisType.YellowCymbal, "report->yellowVelocity" },
        { DrumAxisType.BlueCymbal, "report->blueVelocity" },
        { DrumAxisType.Kick, "report->kickVelocity" },
        { DrumAxisType.Kick2, "report->kickVelocity" }
    };

    private static readonly Dictionary<DrumAxisType, string> UniversalAxisMappings = new()
    {
        { DrumAxisType.Green, "report->greenVelocity" },
        { DrumAxisType.Red, "report->redVelocity" },
        { DrumAxisType.Yellow, "report->yellowVelocity" },
        { DrumAxisType.Blue, "report->blueVelocity" },
        { DrumAxisType.Orange, "report->orangeVelocity" },
        { DrumAxisType.GreenCymbal, "report->greenCymbalVelocity" },
        { DrumAxisType.YellowCymbal, "report->yellowCymbalVelocity" },
        { DrumAxisType.BlueCymbal, "report->blueCymbalVelocity" },
        { DrumAxisType.Kick, "report->kickVelocity" },
        { DrumAxisType.Kick2, "report->kickVelocity" }
    };

    private static readonly Dictionary<DrumAxisType, string> AxisMappingsXb1 = new()
    {
        { DrumAxisType.Green, "report->greenVelocity" },
        { DrumAxisType.Red, "report->redVelocity" },
        { DrumAxisType.Yellow, "report->yellowVelocity" },
        { DrumAxisType.Blue, "report->blueVelocity" },

        // Map orange to green for rb
        { DrumAxisType.Orange, "report->greenVelocity" },
        { DrumAxisType.GreenCymbal, "report->greenCymbalVelocity" },
        { DrumAxisType.YellowCymbal, "report->yellowCymbalVelocity" },
        { DrumAxisType.BlueCymbal, "report->blueCymbalVelocity" },
        { DrumAxisType.Kick, "digitalOnly" },
        { DrumAxisType.Kick2, "digitalOnly" }
    };

    public DrumAxis(ConfigViewModel model, bool enabled, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte[] ledIndicesPeripheral, byte[] ledIndicesMpr121, int min, int max,
        int deadZone, int debounce, DrumAxisType type, bool outputEnabled, bool outputPeripheral, bool outputInverted,
        int outputPin, bool childOfCombined) : base(model, enabled, input, ledOn,
        ledOff, ledIndices, ledIndicesPeripheral, ledIndicesMpr121,
        min, max, false, deadZone, true, outputEnabled, outputInverted, outputPeripheral, outputPin, childOfCombined)
    {
        Type = type;
        Debounce = debounce;
        UpdateDetails();
    }

    public DrumAxisType Type { get; }

    public override bool IsCombined => false;

    public override string LedOnLabel => "Drum Hit LED Colour";
    public override string LedOffLabel => "Drum not Hit LED Colour";

    public override bool IsKeyboard => false;

    [Reactive] private int _debounce;

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return EnumToStringConverter.Convert(Type);
    }

    public override Enum GetOutputType()
    {
        return Type;
    }


    private Thickness ComputeDrumMargin(int threshold)
    {
        var val = (float)threshold / ushort.MaxValue * ProgressWidth;
        return new Thickness(val - 5, 0, val - 5, 0);
    }


    public override string GenerateOutput(ConfigField mode)
    {
        return mode switch
        {
            ConfigField.Shared when Type is DrumAxisType.Kick2 => "kick2",
            ConfigField.Keyboard when Model is { IsRbDrumKit: true, IsFortniteFestivalPro: true } => _fortniteProKeysRb
                .TryGetValue(Type,
                    out var forniteKeyPro)
                ? GetReportField(forniteKeyPro)
                : "",
            ConfigField.Keyboard when Model.IsRbDrumKit => _fortniteKeysRb.TryGetValue(Type, out var forniteKey)
                ? GetReportField(forniteKey)
                : "",
            ConfigField.Keyboard when Model is { IsRbDrumKit: false, IsFortniteFestivalPro: true } => _fortniteProKeysGh
                .TryGetValue(Type,
                    out var forniteKeyPro)
                ? GetReportField(forniteKeyPro)
                : "",
            ConfigField.Keyboard when !Model.IsRbDrumKit => _fortniteKeysGh.TryGetValue(Type, out var forniteKey)
                ? GetReportField(forniteKey)
                : "",
            ConfigField.Universal or ConfigField.Shared => UniversalAxisMappings.GetValueOrDefault(Type, ""),
            // XB1 and PS4 use similar mappings
            ConfigField.XboxOne or ConfigField.Ps4 => AxisMappingsXb1.GetValueOrDefault(Type, ""),
            _ => AxisMappings.GetValueOrDefault(Type, "")
        };
    }

    public override bool ShouldFlip(ConfigField mode)
    {
        return false;
    }

    private AnalogToDigital? _drumInput;
    private ControllerButton? _button;
    private BinaryWriter? _lastWriter;

    public override string Generate(ConfigField mode, int debounceIndex, int ledIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        if (_drumInput == null || _drumInput.Min != Min)
        {
            _drumInput = new AnalogToDigital(Input, AnalogToDigitalType.Drum, Min, Model);
        }

        if (Model is { Branded: false, Builder: false } && !Enabled)
        {
            return "";
        }

        var input = Input;
        if (input.IsAnalog)
        {
            input = _drumInput;
        }

        if (writer == null || _lastWriter != writer || _button == null)
        {
            _lastWriter = writer;
            _button = new ControllerButton(Model, Enabled, input, LedOn, LedOff, LedIndices.ToArray(),
                LedIndicesPeripheral.ToArray(), LedIndicesMpr121.ToArray(),
                Debounce * 10, StandardButtonType.A,
                false, false, false, -1, false);
        }

        if (mode == ConfigField.Shared)
        {
            if (input.IsAnalog)
            {
                extra +=
                    $"lastDrum[{debounceIndex}] = {GenerateAssignment($"lastDrum[{debounceIndex}]", ConfigField.XboxOne, false, false, false, true, writer)};";
            }
            else
            {
                extra += $"lastDrum[{debounceIndex}] = 0xFFFF;";
            }

            var ret = _button.Generate(mode, debounceIndex, ledIndex, extra, combinedExtra, strumIndexes, combinedDebounce,
                    macros, writer);
            ret += $$"""
                     if (!debounce[{{debounceIndex}}]) {
                         lastDrum[{{debounceIndex}}] = 0;
                     }
                     """;
            return ret;
        }

        if (mode is not (ConfigField.Ps3 or ConfigField.Ps4
            or ConfigField.Ps3WithoutCapture or ConfigField.XboxOne or ConfigField.Xbox360
            or ConfigField.Universal or ConfigField.Xbox or ConfigField.Wii or ConfigField.Keyboard)) return "";
        if (string.IsNullOrEmpty(GenerateOutput(mode))) return "";

        var ifStatement = $"debounce[{debounceIndex}]";

        if (mode == ConfigField.Keyboard)
        {
            var outputVar = GenerateOutput(mode);
            var keyCode = KeyboardButton.KeyCodes.IndexOf(outputVar);
            if (keyCode == -1)
            {
                return "";
            }

            return Model.RolloverMode == RolloverMode.SixKro
                ? $$"""
                    if ({{ifStatement}}) {
                        setKey({{debounceIndex}},{{keyCode}},report);
                        {{extra}}
                    } 
                    """
                : $$"""
                    if ({{ifStatement}}) {
                        {{outputVar}} = true;
                        {{extra}}
                    }
                    """;
        }

        var outputButtons = "";
        switch (mode)
        {
            case ConfigField.Xbox360:
                if (ButtonsXbox360.TryGetValue(Type, out var value))
                    outputButtons += $"\n{GetReportField(value)} = true;";
                break;
            case ConfigField.Xbox:
                if (ButtonsXbox360.TryGetValue(Type, out var value5))
                    outputButtons += $"\n{GetReportField(value5)} = 0xff;";
                break;
            case ConfigField.XboxOne:
                if (ButtonsXboxOne.TryGetValue(Type, out var value1))
                    outputButtons += $"\n{GetReportField(value1)} = true;";
                break;
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4:
                if (ButtonsPs3.TryGetValue(Type, out var value2))
                    outputButtons += $"\n{GetReportField(value2)} = true;";
                break;
            case ConfigField.Universal:
                if (ButtonsPs3.TryGetValue(Type, out var value3))
                    outputButtons += $"\n{GetReportField(value3)} = true;";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }

        // XB1 and PS4 are RB by definition
        if (((Model.DeviceControllerType.IsRb() || mode == ConfigField.XboxOne || mode == ConfigField.Ps4) &&
             Type is DrumAxisType.Kick or DrumAxisType.Kick2))
        {
            return $$"""
                     if ({{ifStatement}}) {
                         {{outputButtons}}
                     }
                     """;
        }

        if (Model.DeviceControllerType.IsRb() && mode != ConfigField.XboxOne && mode != ConfigField.Ps4)
        {
            switch (Type)
            {
                case DrumAxisType.BlueCymbal or DrumAxisType.GreenCymbal or DrumAxisType.YellowCymbal:
                    outputButtons += "report->cymbalFlag = true;";
                    break;
                case DrumAxisType.Blue or DrumAxisType.Green or DrumAxisType.Red or DrumAxisType.Yellow:
                    outputButtons += "report->padFlag = true;";
                    break;
            }

            switch (Type)
            {
                case DrumAxisType.YellowCymbal:
                    outputButtons += "report->dpadUp = true;";
                    break;
                case DrumAxisType.BlueCymbal:
                    outputButtons += "report->dpadDown = true;";
                    break;
            }
        }

        var assignedVal = $"(lastDrum[{debounceIndex}])";
        assignedVal = mode switch
        {
            // Xbox one uses 4 bit velocities
            ConfigField.XboxOne => $"(lastDrum[{debounceIndex}]) >> 13",
            // PC HID uses 8 bit velocities
            ConfigField.Universal => $"(lastDrum[{debounceIndex}]) >> 8",
            // PS3 + Xbox360 GH uses 7 bit velocities (because midi)
            ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Xbox360 when
                Model.DeviceControllerType.IsGh() => $"(lastDrum[{debounceIndex}]) >> 9",
            // PS3 RB uses 8 bit velocities, but inverts
            ConfigField.Ps3 or ConfigField.Ps3WithoutCapture => $"255-((lastDrum[{debounceIndex}]) >> 8)",
            // And then 360 RB use inverted int16_t values, though the first bit is specified based on the type
            ConfigField.Xbox360 => Type switch
            {
                // Stuff mapped to the y axis is inverted
                DrumAxisType.GreenCymbal or DrumAxisType.Green or DrumAxisType.Yellow or DrumAxisType.YellowCymbal =>
                    $"-(0x7fff - ((lastDrum[{debounceIndex}]) >> 1))",
                DrumAxisType.Red or DrumAxisType.Blue or DrumAxisType.BlueCymbal =>
                    $"(0x7fff - ((lastDrum[{debounceIndex}]) >> 1))",
                _ => assignedVal
            },
            _ => assignedVal
        };

        if (Model.DeviceControllerType.IsRb() && Model.CymbalGlitchFix &&
            mode is ConfigField.Xbox360 or ConfigField.Ps3 && Type is DrumAxisType.GreenCymbal
                or DrumAxisType.BlueCymbal or DrumAxisType.YellowCymbal or DrumAxisType.Green)
        {
            var test = "";
            var test2 = "";
            
            var debounce = Debounce;
            if (!Model.LocalDebounceMode)
            {
                debounce = Model.Debounce / 10;
            }

            var dbBlob = debounce.ToString();
            if (writer != null)
            {
                dbBlob = _button.GetDebounceBlob(writer);
            }
            switch (Type)
            {
                // Green Pad + Red Cymbal need to be staggered, but it also needs a delay between the stagger
                case DrumAxisType.Green:
                    test = $"!greenCymbal && ((millis() - lastGreenOff) > {dbBlob})";
                    test2 = "greenPad";
                    break;
                // Any two cymbals need to be staggered
                case DrumAxisType.GreenCymbal:
                    test = $"!yellowCymbal && !blueCymbal && !greenPad && ((millis() - lastGreenOff) > {dbBlob})";
                    test2 = "greenCymbal";
                    break;
                case DrumAxisType.BlueCymbal:
                    test = "!greenCymbal && !yellowCymbal";
                    test2 = "blueCymbal";
                    break;
                case DrumAxisType.YellowCymbal:
                    test = "!greenCymbal && !blueCymbal";
                    test2 = "yellowCymbal";
                    break;
            }
            var reset = $"debounce[{debounceIndex}]={dbBlob};";
            return $$"""
                     if ({{ifStatement}}) {
                             if ({{test}}) {
                                 {{outputButtons}}
                                 {{GenerateOutput(mode)}} = {{assignedVal}};
                                 {{test2}} = true;
                             } else {
                                {{reset}}
                             }
                     }
                     """;
        }

        return $$"""
                 if ({{ifStatement}}) {
                     {{outputButtons}}
                     {{GenerateOutput(mode)}} = {{assignedVal}};
                 } 
                 """;
    }


    protected override string MinCalibrationText()
    {
        return "";
    }

    protected override string MaxCalibrationText()
    {
        return "";
    }


    public override void UpdateBindings()
    {
    }

    protected override bool SupportsCalibration()
    {
        return false;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedDrumAxis(Input.Serialise(), Enabled, Type, LedOn, LedOff, LedIndices.ToArray(),
            LedIndicesPeripheral.ToArray(), Min, Max,
            DeadZone, Debounce, OutputEnabled, OutputPin, OutputInverted, PeripheralOutput, ChildOfCombined,
            LedIndicesMpr121.ToArray());
    }
}