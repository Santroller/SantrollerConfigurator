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
using ReactiveUI.SourceGenerators;
using static GuitarConfigurator.NetCore.ViewModels.ConfigViewModel;

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
        {DrumAxisType.Kick, "digitalOnly"},
        {DrumAxisType.Kick2, "digitalOnly"}
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
        var val = (float) threshold / ushort.MaxValue * ProgressWidth;
        return new Thickness(val - 5, 0, val - 5, 0);
    }


    public override string GenerateOutput(ConfigField mode)
    {
        if (mode is ConfigField.Shared && Type is DrumAxisType.Kick2)
        {
            return "kick2";
        }

        return mode switch
        {
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


    public override string Generate(ConfigField mode, int debounceIndex, int ledIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        if (Model is {Branded: false, Builder: false} && !Enabled)
        {
            return "";
        }
        var input = Input;

        if (mode == ConfigField.Shared)
        {
            if (Input is not WiiInput &&
                (!Model.DeviceControllerType.IsRb() || Type is not (DrumAxisType.Kick or DrumAxisType.Kick2)) &&
                Model is {IsKeyboard: false} && Input is not UsbHostInput) return "";

            if (input.IsAnalog)
            {
                input = new AnalogToDigital(input, AnalogToDigitalType.Drum, Min, Model);
            }

            return new ControllerButton(Model, Enabled, input, LedOn, LedOff, LedIndices.ToArray(),
                    LedIndicesPeripheral.ToArray(), LedIndicesMpr121.ToArray(),
                    (byte) Debounce, StandardButtonType.A,
                    false, false, false, -1, false)
                .Generate(mode, debounceIndex, ledIndex, extra, combinedExtra, strumIndexes, combinedDebounce,
                    macros, writer).Replace("midiVelocities","midiVelocitiesTemp");
        }

        if (mode is not (ConfigField.Ps3 or ConfigField.Ps4
            or ConfigField.Ps3WithoutCapture or ConfigField.XboxOne or ConfigField.Xbox360
            or ConfigField.Universal or ConfigField.Xbox or ConfigField.Wii)) return "";
        if (string.IsNullOrEmpty(GenerateOutput(mode))) return "";
        var debounce = Debounce;
        if (!Model.LocalDebounceMode) debounce = Model.Debounce;
        if (!Model.Deque)
        {
            // If we aren't using queue based inputs, then we want ms based inputs, not ones based on 0.1ms
            debounce /= 10;
        }

        debounce += 1;

        var ifStatement = $"debounce[{debounceIndex}]";
        var reset = $"debounce[{debounceIndex}]={debounce};";
        if (Model.LedType != LedType.None || Model.LedTypePeripheral != LedType.None || OutputEnabled ||
            Model.HasMpr121)
        {
            reset += $"ledDebounce[{ledIndex}]={debounce};";
        }

        if (writer != null)
        {
            reset = $"debounce[{debounceIndex}]={WriteBlob(writer, (byte) debounce)};";
            if (Model.LedType != LedType.None || Model.LedTypePeripheral != LedType.None || OutputEnabled ||
                Model.HasMpr121)
            {
                reset += $"ledDebounce[{debounceIndex}]={WriteBlob(writer, (byte) debounce)};";
            }
        }

        if (((Model.DeviceControllerType.IsRb() || mode == ConfigField.XboxOne) &&
             Type is DrumAxisType.Kick or DrumAxisType.Kick2))
        {
            reset = "";
        }

        if (Input is MidiInput midiInput)
        {
            reset += $"midiData.midiVelocitiesTemp[{midiInput.Key}] = 0;";
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
            case ConfigField.Keyboard:
                if (KeysFortnite.TryGetValue(Type, out var value4))
                    outputButtons += $"\n{GetReportField(value4)} = true;";
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
                         {{reset}}
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

        // If someone specified a digital input, then we need to take the value they have specified and convert it to the target consoles expected output
        var dtaVal = 0;
        if (Input is DigitalToAnalog dta) dtaVal = dta.On;

        var assignedVal = $"(lastDrum[{debounceIndex}])";
        switch (mode)
        {
            // Xbox one uses 4 bit velocities
            case ConfigField.XboxOne:
                assignedVal = $"(lastDrum[{debounceIndex}]) >> 12";
                dtaVal >>= 12;
                break;
            // PC HID uses 8 bit velocities
            case ConfigField.Universal:
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture when Model.DeviceControllerType.IsGh():
                assignedVal = $"(lastDrum[{debounceIndex}]) >> 8";
                dtaVal >>= 8;
                break;
            // PS3 uses 8 bit velocities, but inverts
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture:
                assignedVal = $"255-((lastDrum[{debounceIndex}]) >> 8)";
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
        if (Input is DigitalToAnalog)
        {
            if (outputButtons.Length != 0)
            {
                outputButtons = $$"""
                                  if ({{ifStatement}}) {
                                      {{outputButtons}}
                                  }
                                  """;
            }

            return $$"""
                     if ({{Input.Generate(writer)}}) {
                         {{reset}}
                         {{GenerateOutput(mode)}} = {{dtaVal}};
                     }
                     {{outputButtons}}
                     """;
        }

        if (!Input.IsAnalog)
        {
            return $$"""
                     if ({{ifStatement}}) {
                         {{GenerateOutput(mode)}} = {{GenerateAssignment("0", ConfigField.XboxOne, false, false, false, true, writer)}};
                         {{outputButtons}}
                     }
                     """;
        }

        // For drums, we want to do things based on a peak.
        // That means we ignore anything under Min, then when something peaks over Min, we capture that value and wait until we are back under Min before resetting.
        var check = $"{Input.Generate(writer)} > {Min}";
        if (Min > Max)
        {
            check = $"({Input.Generate(writer)} - {Min}) < {DeadZone}";
        }

        check = check.Replace("midiVelocities", "midiVelocitiesTemp");

         if (Model.DeviceControllerType.IsRb() && Model.CymbalGlitchFix && mode is ConfigField.Xbox360 or ConfigField.Ps3 && Type is DrumAxisType.GreenCymbal or DrumAxisType.BlueCymbal or DrumAxisType.YellowCymbal or DrumAxisType.Green)
         {
             var test = "";
             var test2 = "";
             switch (Type)
             {
                 // Green Pad + Green Cymbal need to be staggered
                 case DrumAxisType.Green:
                     test = "!greenCymbal";
                     test2 = "greenPad";
                     break;
                 // Any two cymbals need to be staggered
                 case DrumAxisType.GreenCymbal:
                     test = "!yellowCymbal && !blueCymbal && !greenPad";
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
             return $$"""
                      if ({{check}}) {
                         lastDrum[{{debounceIndex}}] = {{GenerateAssignment($"lastDrum[{debounceIndex}]", ConfigField.XboxOne, false, false, false, true, writer).Replace("midiVelocities","midiVelocitiesTemp")}};
                         {{reset}}
                      }
                      if ({{ifStatement}}) {
                              if ({{test}}) {
                                  {{outputButtons}}
                                  {{GenerateOutput(mode)}} = {{assignedVal}};
                                  {{test2}} = true;
                              } else {
                                  {{reset}}
                              }
                      } else {
                        {{GenerateOutput(mode)}} = 0;
                        lastDrum[{{debounceIndex}}] = 0;
                      }
                      """;
         }
        return $$"""
                 if ({{check}}) {
                     lastDrum[{{debounceIndex}}] = {{GenerateAssignment($"lastDrum[{debounceIndex}]", ConfigField.XboxOne, false, false, false, true, writer).Replace("midiVelocities","midiVelocitiesTemp")}};
                     {{reset}}
                 }
                 if ({{ifStatement}}) {
                     {{outputButtons}}
                     {{GenerateOutput(mode)}} = {{assignedVal}};
                 } else {
                   {{GenerateOutput(mode)}} = 0;
                   lastDrum[{{debounceIndex}}] = 0;
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