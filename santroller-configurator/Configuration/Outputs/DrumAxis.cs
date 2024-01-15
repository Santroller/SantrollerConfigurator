using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public partial class DrumAxis : OutputAxis
{
    private const StandardButtonType BlueCymbalFlag = StandardButtonType.DpadDown;
    private const StandardButtonType YellowCymbalFlag = StandardButtonType.DpadUp;

    private static readonly Dictionary<DrumAxisType, StandardButtonType> ButtonsXbox360 = new()
    {
        {DrumAxisType.Green, StandardButtonType.A},
        {DrumAxisType.Red, StandardButtonType.B},
        {DrumAxisType.Blue, StandardButtonType.X},
        {DrumAxisType.Yellow, StandardButtonType.Y},
        {DrumAxisType.GreenCymbal, StandardButtonType.A},
        {DrumAxisType.BlueCymbal, StandardButtonType.X},
        {DrumAxisType.YellowCymbal, StandardButtonType.Y},
        {DrumAxisType.Orange, StandardButtonType.RightShoulder},
        {DrumAxisType.Kick, StandardButtonType.LeftShoulder},
        {DrumAxisType.Kick2, StandardButtonType.LeftThumbClick}
    };
    private static readonly Dictionary<DrumAxisType, Key> KeysFortnite = new()
    {
        {DrumAxisType.Green, Key.D},
        {DrumAxisType.Red, Key.F},
        {DrumAxisType.Yellow, Key.J},
        {DrumAxisType.Blue, Key.K},
        {DrumAxisType.Orange, Key.L},
    };

    private static readonly Dictionary<DrumAxisType, StandardButtonType> ButtonsXboxOne = new()
    {
        {DrumAxisType.Green, StandardButtonType.A},
        {DrumAxisType.Red, StandardButtonType.B},
        {DrumAxisType.Kick, StandardButtonType.LeftShoulder},
        {DrumAxisType.Kick2, StandardButtonType.RightShoulder}
    };

    private static readonly Dictionary<DrumAxisType, StandardButtonType> ButtonsPs3 = new()
    {
        {DrumAxisType.Green, StandardButtonType.A},
        {DrumAxisType.Red, StandardButtonType.B},
        {DrumAxisType.Blue, StandardButtonType.X},
        {DrumAxisType.Yellow, StandardButtonType.Y},
        {DrumAxisType.GreenCymbal, StandardButtonType.A},
        {DrumAxisType.BlueCymbal, StandardButtonType.X},
        {DrumAxisType.YellowCymbal, StandardButtonType.Y},
        {DrumAxisType.Kick, StandardButtonType.LeftShoulder},
        {DrumAxisType.Orange, StandardButtonType.RightShoulder},
        {DrumAxisType.Kick2, StandardButtonType.RightShoulder}
    };
    

    private static readonly Dictionary<DrumAxisType, string> AxisMappings = new()
    {
        {DrumAxisType.Green, "report->greenVelocity"},
        {DrumAxisType.Red, "report->redVelocity"},
        {DrumAxisType.Yellow, "report->yellowVelocity"},
        {DrumAxisType.Blue, "report->blueVelocity"},
        {DrumAxisType.Orange, "report->orangeVelocity"},
        {DrumAxisType.GreenCymbal, "report->greenVelocity"},
        {DrumAxisType.YellowCymbal, "report->yellowVelocity"},
        {DrumAxisType.BlueCymbal, "report->blueVelocity"},
        {DrumAxisType.Kick, "report->kickVelocity"},
        {DrumAxisType.Kick2, "report->kickVelocity"}
    };
    private static readonly Dictionary<DrumAxisType, string> UniversalAxisMappings = new()
    {
        {DrumAxisType.Green, "report->greenVelocity"},
        {DrumAxisType.Red, "report->redVelocity"},
        {DrumAxisType.Yellow, "report->yellowVelocity"},
        {DrumAxisType.Blue, "report->blueVelocity"},
        {DrumAxisType.Orange, "report->orangeVelocity"},
        {DrumAxisType.GreenCymbal, "report->greenCymbalVelocity"},
        {DrumAxisType.YellowCymbal, "report->yellowCymbalVelocity"},
        {DrumAxisType.BlueCymbal, "report->blueCymbalVelocity"},
        {DrumAxisType.Kick, "report->kickVelocity"},
        {DrumAxisType.Kick2, "report->kickVelocity"}
    };

    private static readonly Dictionary<DrumAxisType, string> AxisMappingsXb1 = new()
    {
        {DrumAxisType.Green, "report->greenVelocity"},
        {DrumAxisType.Red, "report->redVelocity"},
        {DrumAxisType.Yellow, "report->yellowVelocity"},
        {DrumAxisType.Blue, "report->blueVelocity"},
        
        // Map orange to green for rb
        {DrumAxisType.Orange, "report->greenVelocity"},
        {DrumAxisType.GreenCymbal, "report->greenCymbalVelocity"},
        {DrumAxisType.YellowCymbal, "report->yellowCymbalVelocity"},
        {DrumAxisType.BlueCymbal, "report->blueCymbalVelocity"},
    };

    public DrumAxis(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices, byte[] ledIndicesPeripheral, int min, int max,
        int deadZone, int debounce, DrumAxisType type, bool childOfCombined) : base(model, input, ledOn,
        ledOff, ledIndices, ledIndicesPeripheral,
        min, max, deadZone, true, childOfCombined)
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

    [Reactive] public int Debounce { get; set; }

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
        var val = (float) threshold / ushort.MaxValue * ProgressWidth;
        return new Thickness(val - 5, 0, val - 5, 0);
    }


    public override string GenerateOutput(ConfigField mode)
    {
        return mode switch
        {
            ConfigField.Universal => UniversalAxisMappings.GetValueOrDefault(Type, ""),
            ConfigField.XboxOne => AxisMappingsXb1.GetValueOrDefault(Type, ""),
            _ => AxisMappings.GetValueOrDefault(Type, "")
        };
    }

    public override bool ShouldFlip(ConfigField mode)
    {
        return false;
    }


    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        if (mode == ConfigField.Shared)
        {
            if (Input is WiiInput || (Model.DeviceControllerType.IsRb() && Type is DrumAxisType.Kick or DrumAxisType.Kick2) || Model.IsKeyboard || Model.IsFortniteFestival)
            {
                return new ControllerButton(Model, Input, LedOn, LedOff, LedIndices.ToArray(), LedIndicesPeripheral.ToArray(), (byte) Debounce, StandardButtonType.A,
                        false)
                    .Generate(mode, debounceIndex, extra, combinedExtra, strumIndexes, combinedDebounce, macros, writer);
            }

            return "";
        }
        

        if (Model.EmulationType is not EmulationType.FortniteFestival && (mode is not (ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.XboxOne or ConfigField.Xbox360 or ConfigField.Universal)) || (Model.EmulationType is EmulationType.FortniteFestival && mode is not ConfigField.Keyboard)) return "";
        if (string.IsNullOrEmpty(GenerateOutput(mode))) return "";
        var debounce = Debounce;
        if (!Model.IsAdvancedMode) debounce = (byte) Model.Debounce;
        if (!Model.Deque)
        {
            // If we aren't using queue based inputs, then we want ms based inputs, not ones based on 0.1ms
            debounce /= 10;
        }
        
        debounce += 1;

        var ifStatement = $"debounce[{debounceIndex}]";
        var input = Input;
        var reset = $"debounce[{debounceIndex}]={debounce};";
        if (Input is WiiInput wii)
        {
            // Wii inputs provide their own digital signals, so don't generate one ourselves.
            reset = "";
            // For wii stuff, generate the debounce based on the digital signal from the controller
            var type = wii.Input;
            var typeAxis = Enum.Parse<WiiInputType>($"{type}Pressure");
            input = new WiiInput(typeAxis, Model, wii.Peripheral);
        }
        var outputButtons = "";
        switch (mode)
        {
            case ConfigField.Xbox360:
                if (ButtonsXbox360.TryGetValue(Type, out var value))
                    outputButtons += $"\n{GetReportField(value)} = true;";
                break;
            case ConfigField.XboxOne:
                if (ButtonsXboxOne.TryGetValue(Type, out var value1))
                    outputButtons += $"\n{GetReportField(value1)} = true;";
                break;
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture:
                if (ButtonsPs3.TryGetValue(Type, out var value2))
                    outputButtons += $"\n{GetReportField(value2)} = true;";
                break;
            case ConfigField.Universal:
                if (ButtonsPs3.TryGetValue(Type, out var value3))
                    outputButtons += $"\n{GetReportField(value3)} = true;";
                break;
            case ConfigField.Keyboard:
                if (KeysFortnite.TryGetValue(Type, out var value4))
                    outputButtons += $"\n{GetReportField(value4)} = true;";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }

        if ((Model.DeviceControllerType.IsRb() && Type is DrumAxisType.Kick or DrumAxisType.Kick2) || mode is ConfigField.Keyboard)
        {
            return $$"""
                     if ({{ifStatement}}) {
                         {{outputButtons}}
                     }
                     """;
        }

        if (Model.DeviceControllerType.IsRb() && mode != ConfigField.XboxOne)
        {
            switch (Type)
            {
                case DrumAxisType.YellowCymbal:
                    outputButtons += $"\n{GetReportField(YellowCymbalFlag)} = true;";
                    outputButtons += $"\n{GetReportField("cymbalFlag")} = true;";

                    break;
                case DrumAxisType.BlueCymbal:
                    outputButtons += $"\n{GetReportField(BlueCymbalFlag)} = true;";
                    outputButtons += $"\n{GetReportField("cymbalFlag")} = true;";

                    break;
                case DrumAxisType.GreenCymbal:
                    outputButtons += $"\n{GetReportField("cymbalFlag")} = true;";
                    break;
                case DrumAxisType.Green:
                case DrumAxisType.Red:
                case DrumAxisType.Yellow:
                case DrumAxisType.Blue:
                    outputButtons += $"\n{GetReportField("padFlag")} = true;";

                    break;
            }
        }

        // If someone specified a digital input, then we need to take the value they have specified and convert it to the target consoles expected output
        var dtaVal = 0;
        if (input is DigitalToAnalog dta) dtaVal = dta.On;

        var assignedVal = $"(lastDrum[{debounceIndex}])";
        switch (mode)
        {
            // Xbox one uses 4 bit velocities
            case ConfigField.XboxOne:
                assignedVal = $"(lastDrum[{debounceIndex}]) >> 12";
                dtaVal >>= 12;
                break;
            // PS3 and PC HID uses 8 bit velocities
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Universal:
                assignedVal = $"(lastDrum[{debounceIndex}]) >> 8";
                dtaVal >>= 8;
                break;
            // Xbox 360 GH use uint8_t velocities
            default:
            {
                if (Model.DeviceControllerType.IsGh())
                {
                    assignedVal = $"(lastDrum[{debounceIndex}]) >> 8";
                    dtaVal >>= 8;
                }
                // And then 360 RB use inverted int16_t values, though the first bit is specified based on the type
                else
                {
                    switch (Type)
                    {
                        // Stuff mapped to the y axis is inverted
                        case DrumAxisType.GreenCymbal:
                        case DrumAxisType.Green:
                        case DrumAxisType.Yellow:
                        case DrumAxisType.YellowCymbal:
                            assignedVal = $"-(0x7fff - ((lastDrum[{debounceIndex}]) >> 1))";
                            dtaVal = -(0x7fff - (dtaVal >> 1));
                            break;
                        case DrumAxisType.Red:
                        case DrumAxisType.Blue:
                        case DrumAxisType.BlueCymbal:
                            assignedVal = $"(0x7fff - ((lastDrum[{debounceIndex}]) >> 1))";
                            dtaVal = 0x7fff - (dtaVal >> 1);
                            break;
                    }
                }

                break;
            }
        }

        // If someone has mapped digital inputs to the drums, then we can shortcut a bunch of the tests, and just need to use the calculated value from above
        if (input is DigitalToAnalog)
        {
            if (outputButtons.Any())
            {
                outputButtons = $$"""
                                  if ({{ifStatement}}) {
                                      {{outputButtons}}
                                  }
                                  """;
            }
            return $$"""
                     if ({{input.Generate()}}) {
                         {{reset}}
                         {{GenerateOutput(mode)}} = {{dtaVal}};
                     }
                     {{outputButtons}}
                     """;
        }

        // For drums, we want to do things based on a peak.
        // That means we ignore anything under Min, then when something peaks over Min, we capture that value and wait until we are back under Min before resetting.
        var check = $"{Input.Generate()} > {Min}";
        if (Min > Max)
        {
            check = $"({Input.Generate()} - {Min}) < {DeadZone}";
        }
        return $$"""
                 if ({{check}}) {
                     if (!{{ifStatement}}) {
                         lastDrum[{{debounceIndex}}] = {{GenerateAssignment("0", ConfigField.XboxOne, false, false, false, true, writer)}};
                     }
                     {{reset}}
                 }
                 if ({{ifStatement}}) {
                     {{outputButtons}}
                     {{GenerateOutput(mode)}} = {{assignedVal}};;
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
        return new SerializedDrumAxis(Input.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), LedIndicesPeripheral.ToArray(), Min, Max,
            DeadZone, Debounce, ChildOfCombined);
    }
}