using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Input;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public partial class GuitarAxis : OutputAxis
{
    public static readonly Dictionary<int, int> PickupSelectorRangesPS = new()
    {
        {1, 0x19},
        {2, 0x4C},
        {3, 0x96},
        {4, 0xB2},
        {5, 0xE5},
    };

    public static readonly Dictionary<int, int> PickupSelectorRangesXb1 = new()
    {
        {1, 0x0},
        {2, 0x10},
        {3, 0x20},
        {4, 0x30},
        {5, 0x40},
    };

    public static readonly Dictionary<int, int> WiiGh5Mappings = new()
    {
        {0x15, 0x04},
        {0x30, 0x07},
        {0x4D, 0x0a},
        {0x65, 0x0c},
        {0x66, 0x0c},
        {0x80, 0x0f},
        {0x99, 0x12},
        {0x9A, 0x12},
        {0xAC, 0x14},
        {0xAD, 0x14},
        {0xAE, 0x14},
        {0xAF, 0x14},
        {0xC6, 0x17},
        {0xC7, 0x17},
        {0xC8, 0x17},
        {0xC9, 0x17},
        {0xDF, 0x1A},
        {0xE0, 0x1A},
        {0xE1, 0x1A},
        {0xE2, 0x1A},
        {0xE3, 0x1A},
        {0xE4, 0x1A},
        {0xE5, 0x1A},
        {0xE6, 0x1A},
        {0xF8, 0x1F},
        {0xF9, 0x1F},
        {0xFA, 0x1F},
        {0xFB, 0x1F},
        {0xFC, 0x1F},
        {0xFD, 0x1F},
        {0xFE, 0x1F},
        {0xFF, 0x1F},
    };
    public int PickupSelectorNotch2 { get; set; }
    public int PickupSelectorNotch3 { get; set; }
    public int PickupSelectorNotch4 { get; set; }
    public int PickupSelectorNotch5 { get; set; }


    public GuitarAxis(ConfigViewModel model, Input input, Color ledOn, Color ledOff,
        byte[] ledIndices, byte[] ledIndicesPeripheral, byte[] ledIndicesMpr121, int min, int max, int deadZone,
        bool invert,
        GuitarAxisType type, bool outputEnabled, bool outputPeripheral, bool outputInverted, int outputPin,
        bool childOfCombined) : base(model,
        input, ledOn,
        ledOff, ledIndices, ledIndicesPeripheral, ledIndicesMpr121, min, max, 0,deadZone,
        type is GuitarAxisType.Slider or GuitarAxisType.Whammy, outputEnabled, outputInverted, outputPeripheral,
        outputPin, childOfCombined)
    {
        Type = type;
        Inverted = invert;
        UpdateDetails();
        _namedAxisInfoHelper = this.WhenAnyValue(x => x.Value).Select(GetNamedAxisInfo).ToProperty(this, x => x.NamedAxisInfo);
    }

    public GuitarAxis(ConfigViewModel model, Input input, int pickupSelectorNotch2,
        int pickupSelectorNotch3, int pickupSelectorNotch4, int pickupSelectorNotch5, Color ledOn, Color ledOff,
        byte[] ledIndices, byte[] ledIndicesPeripheral, byte[] ledIndicesMpr121, int min, int max, int deadZone,
        bool invert,
        GuitarAxisType type, bool outputEnabled, bool outputPeripheral, bool outputInverted, int outputPin,
        bool childOfCombined) : base(model,
        input, ledOn,
        ledOff, ledIndices, ledIndicesPeripheral, ledIndicesMpr121, min, max,0, deadZone,
        true, outputEnabled, outputInverted, outputPeripheral,
        outputPin, childOfCombined)
    {
        PickupSelectorNotch2 = pickupSelectorNotch2;
        PickupSelectorNotch3 = pickupSelectorNotch3;
        PickupSelectorNotch4 = pickupSelectorNotch4;
        PickupSelectorNotch5 = pickupSelectorNotch5;
        Type = type;
        Inverted = invert;
        UpdateDetails();
        _namedAxisInfoHelper = this.WhenAnyValue(x => x.Value).Select(GetNamedAxisInfo).ToProperty(this, x => x.NamedAxisInfo);
    }

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] private string _namedAxisInfo = "";

    [Reactive] private bool _inverted;

    public GuitarAxisType Type { get; }
    public bool HasNamedAxis => Type is GuitarAxisType.Slider or GuitarAxisType.Pickup;
    public bool IsPickup => Type is GuitarAxisType.Pickup;

    public bool AllowInvert => Type is GuitarAxisType.Pickup;

    public override bool IsKeyboard => false;

    public override string LedOnLabel
    {
        get
        {
            return Type switch
            {
                GuitarAxisType.Tilt => Resources.LEDColourActiveTilt,
                GuitarAxisType.Whammy => Resources.LEDColourActiveWhammy,
                GuitarAxisType.Pickup => Resources.LEDColourActivePickup,
                GuitarAxisType.Slider => Resources.LEDColourActiveSlider,
                _ => ""
            };
        }
    }

    protected override int Calculate(bool enabled, int value, int min, int max, int center, int deadZone, bool trigger, DeviceControllerType
        deviceControllerType)
    {
        return Type switch
        {
            GuitarAxisType.Slider or GuitarAxisType.Pickup => value,
            _ => base.Calculate(enabled, value, min, max, center, deadZone, trigger, deviceControllerType)
        };
    }

    public override string LedOffLabel
    {
        get
        {
            return Type switch
            {
                GuitarAxisType.Tilt => Resources.LEDColourInctiveTilt,
                GuitarAxisType.Whammy => Resources.LEDColourInctiveWhammy,
                GuitarAxisType.Pickup => Resources.LEDColourInctivePickup,
                GuitarAxisType.Slider => Resources.LEDColourInctiveSlider,
                _ => ""
            };
        }
    }

    private int GetPickupSelectorValue(int val)
    {
        if (Input is DigitalToAnalog or FixedInput)
        {
            return Math.Min(val / (ushort.MaxValue / 5) + 1, 5);
        }

        if (val < PickupSelectorNotch2)
        {
            return 1;
        }

        if (val < PickupSelectorNotch3)
        {
            return 2;
        }

        if (val < PickupSelectorNotch4)
        {
            return 3;
        }

        return val < PickupSelectorNotch5 ? 4 : 5;
    }

    private string GetNamedAxisInfo(int val)
    {
        if (Type is GuitarAxisType.Pickup)
        {
            if (!Input.IsUint)
            {
                val += short.MaxValue;
            }

            if (Inverted)
            {
                val = ushort.MaxValue - val;
            }

            return $"Notch {GetPickupSelectorValue(val)}";
        }

        var ret = "";
        if (!ChildOfCombined)
        {
            ret = Resources.TapBarCurrentFrets;
        }

        val &= 0xFF;
        if (Type is not GuitarAxisType.Slider || !Gh5NeckInput.Gh5Mappings.TryGetValue(val, out var info))
            return ret + Resources.TapBarCurrentFretsNone;
        if (info.HasFlag(BarButton.Green)) ret += $"{Resources.TapBarCurrentFretsGreen} ";
        if (info.HasFlag(BarButton.Red)) ret += $"{Resources.TapBarCurrentFretsRed} ";
        if (info.HasFlag(BarButton.Yellow)) ret += $"{Resources.TapBarCurrentFretsYellow} ";
        if (info.HasFlag(BarButton.Blue)) ret += $"{Resources.TapBarCurrentFretsBlue} ";
        if (info.HasFlag(BarButton.Orange)) ret += $"{Resources.TapBarCurrentFretsOrange} ";
        return ret.Trim();
    }

    public override bool ShouldFlip(ConfigField mode)
    {
        return false;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedGuitarAxis(Input!.Serialise(), Type, PickupSelectorNotch2, PickupSelectorNotch3,
            PickupSelectorNotch4, PickupSelectorNotch5, LedOn, LedOff, LedIndices.ToArray(),
            LedIndicesPeripheral.ToArray(), Inverted, Min, Max,
            DeadZone, OutputEnabled, OutputPin, OutputInverted, PeripheralOutput, ChildOfCombined,
            LedIndicesMpr121.ToArray());
    }

    public override string GenerateOutput(ConfigField mode)
    {
        return GetReportField(Type);
    }

    public override Enum GetOutputType()
    {
        return Type;
    }

    public override string Generate(ConfigField mode, int debounceIndex, int ledIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        if (mode is ConfigField.Keyboard or ConfigField.Shared or ConfigField.Festival && Model.IsFortniteFestivalPro)
        {
            var input = Input;
            input = input is DigitalToAnalog ? input.InnermostInputs().First() : input;
            var debounce = Model.Debounce;
            if (!Model.Deque)
            {
                // If we aren't using queue based inputs, then we want ms based inputs, not ones based on 0.1ms
                debounce /= 10;
            }

            debounce += 1;
            if (Input is not DigitalToAnalog && mode is ConfigField.Shared)
            {
                var ledDebounce = "";
                if (Model.LedType != LedType.None || Model.LedTypePeripheral != LedType.None || OutputEnabled || Model.HasMpr121)
                {
                    ledDebounce = $"ledDebounce[{ledIndex}]={debounce};";
                }
                switch (Type)
                {
                    case GuitarAxisType.Tilt:
                        return $$"""
                                 if ({{GenerateAssignment("0", ConfigField.Ps3, false, true, false, false, writer)}} > 240) {
                                    debounce[{{debounceIndex}}]={{debounce}};
                                    {{ledDebounce}}
                                 }
                                 """;
                    case GuitarAxisType.Whammy:
                        return $$"""
                                 if ({{GenerateAssignment("0", ConfigField.Ps3, false, true, false, false, writer)}} > {{byte.MaxValue / 2}}) {
                                    debounce[{{debounceIndex}}]={{debounce}};
                                    {{ledDebounce}}
                                 }
                                 """;
                }
            }

            var ifStatement = $"debounce[{debounceIndex}]";
            switch (Type)
            {
                case GuitarAxisType.Tilt when mode is ConfigField.Festival:
                    return $$"""
                             if ({{ifStatement}}) {
                                 report->tilt = true;
                             }
                             """;
                case GuitarAxisType.Whammy when mode is ConfigField.Festival:
                    return $$"""
                             if ({{ifStatement}}) {
                                 report->whammy = true;
                             }
                             """;
                case GuitarAxisType.Tilt:
                    return new KeyboardButton(Model, input, LedOn, LedOff, LedIndices.ToArray(),
                        LedIndicesPeripheral.ToArray(), LedIndicesMpr121.ToArray(), Model.Debounce, Key.PageDown,
                        OutputEnabled, PeripheralOutput, OutputInverted, OutputPin).Generate(mode, debounceIndex, ledIndex, extra,
                        combinedExtra, strumIndexes, combinedDebounce, macros, writer);
                case GuitarAxisType.Whammy:
                    return new KeyboardButton(Model, input, LedOn, LedOff, LedIndices.ToArray(),
                        LedIndicesPeripheral.ToArray(), LedIndicesMpr121.ToArray(), Model.Debounce, Key.RightCtrl,
                        OutputEnabled, PeripheralOutput, OutputInverted, OutputPin).Generate(mode, debounceIndex, ledIndex, extra,
                        combinedExtra, strumIndexes, combinedDebounce, macros, writer);
            }
            
        }

        if (mode == ConfigField.Shared)
            return base.Generate(mode, debounceIndex, ledIndex, extra, combinedExtra, strumIndexes, combinedDebounce, macros,
                writer);

        if (mode is not (ConfigField.Ps3 or ConfigField.Ps2 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4 or ConfigField.Xbox360
            or ConfigField.XboxOne
            or ConfigField.Universal or ConfigField.Xbox or ConfigField.Wii)) return "";
        // The below is a mess... but essentially we have to handle converting the input to its respective output depending on console
        // We have to do some hyper specific stuff for digital to analog here too so its easiest to capture its value once
        var analogOn = 0;
        if (Input is DigitalToAnalog dta)
        {
            analogOn = dta.On;
            // Slider is really a uint8_t, so just cut off the extra bits
            if (Type == GuitarAxisType.Slider) analogOn &= 0xFF;
        }

        if (Type == GuitarAxisType.Slider && Model.DeviceControllerType == DeviceControllerType.LiveGuitar)
        {
            return "";
        }

        switch (mode)
        {
            case ConfigField.Xbox360 or ConfigField.Xbox
                when Type == GuitarAxisType.Whammy && Input is DigitalToAnalog:
                return $$"""
                         if ({{Input.Generate(writer)}}) {
                             {{GenerateOutput(mode)}} = {{short.MaxValue}};
                         }
                         """;
            case ConfigField.XboxOne
                when Type == GuitarAxisType.Whammy && Input is DigitalToAnalog:
                return $$"""
                         if ({{Input.Generate(writer)}}) {
                             {{GenerateOutput(mode)}} = {{byte.MaxValue}};
                         }
                         """;
            case ConfigField.XboxOne when Model.DeviceControllerType is DeviceControllerType.LiveGuitar:
                return "";
            case ConfigField.Wii
                when Type is GuitarAxisType.Tilt && Input is DigitalToAnalog:
                // Wii tilt doesn't exist so instead map to select
                return $$"""
                         if (TILT && {{Input.Generate(writer)}}) {
                             report->back = true;
                         }
                         """;
            case ConfigField.Wii when Type is GuitarAxisType.Tilt:
                // Wii tilt doesn't exist so instead map to select
                return
                    $$"""
                      if (TILT) {
                          uint8_t tilt_test = {{GenerateAssignment("0", mode, false, false, false, false, writer)}};
                          if (tilt_test > 0xF0) {
                              report->back = true;
                          }
                      }
                      """;
            case ConfigField.Ps2
                when Type is GuitarAxisType.Tilt && Input is DigitalToAnalog:
                // PS2 tilt is digital
                return $$"""
                         if (TILT && {{Input.Generate(writer)}}) {
                             report->tilt = true;
                         }
                         """;
            case ConfigField.Ps2 when Type is GuitarAxisType.Tilt:
                // PS2 tilt is digital
                return
                    $$"""
                      if (TILT) {
                          uint8_t tilt_test = {{GenerateAssignment("0", mode, false, false, false, false, writer)}};
                          if (tilt_test > 0xF0) {
                              report->tilt = true;
                          }
                      }
                      """;
            
            case ConfigField.Wii when Type is GuitarAxisType.Whammy && Input is DigitalToAnalog:
                return base.Generate(mode, debounceIndex, ledIndex, extra, combinedExtra, strumIndexes, combinedDebounce,
                    macros, writer);
            case ConfigField.Wii when Type is GuitarAxisType.Whammy:
                return $"{GenerateOutput(mode)} = {GenerateAssignment(GenerateOutput(mode), mode, false, false, Type is GuitarAxisType.Whammy, false, writer)} >> 3;";
            case ConfigField.XboxOne or ConfigField.Universal
                when Type is GuitarAxisType.Tilt && Input is DigitalToAnalog:
                // XB1 tilt is uint8_t
                return $$"""
                         if (TILT && {{Input.Generate(writer)}}) {
                             {{GenerateOutput(mode)}} = 255;
                         }
                         """;
            case ConfigField.XboxOne or ConfigField.Universal when Type is GuitarAxisType.Tilt:
                return
                    $$"""
                      if (TILT) {
                        {{GenerateOutput(mode)}} = {{GenerateAssignment(GenerateOutput(mode), mode, true, false, false, false, writer)}};
                      }
                      """;
            case ConfigField.Xbox360 or ConfigField.Xbox when Type == GuitarAxisType.Slider && Input is DigitalToAnalog:
                // x360 slider is actually a int16_t BUT there is a mechanism to convert the uint8 value to its uint16_t version
                analogOn = -((sbyte) (analogOn ^ 0x80) * -257);

                return $$"""
                         if (SLIDER_BAR && {{Input.Generate(writer)}}) {
                             {{GenerateOutput(mode)}} = {{analogOn}};
                         }
                         """;
            case ConfigField.Xbox360 or ConfigField.Xbox
                when Type == GuitarAxisType.Slider && Input is not DigitalToAnalog:
                // x360 slider is actually a int16_t BUT there is a mechanism to convert the uint8 value to its uint16_t version
                return $$"""
                         if (SLIDER_BAR && ({{Input.Generate(writer)}} != PS3_STICK_CENTER)) {
                             {{GenerateOutput(mode)}} = -((int8_t)(({{Input.Generate(writer)}}) ^ 0x80) * -257);
                         }
                         """;
            // Xb1 is RB only, so no slider
            case ConfigField.XboxOne or ConfigField.Ps4 when Type == GuitarAxisType.Slider:
                return "";
            // Wii and ps2 are GH only, so no pickup
            case ConfigField.Wii or ConfigField.Ps2 when Type == GuitarAxisType.Pickup:
                return "";
            case ConfigField.Universal or ConfigField.Ps2 or ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4
                when Type == GuitarAxisType.Slider && Input is DigitalToAnalog:
                return $$"""
                         if (SLIDER_BAR && {{Input.Generate(writer)}}) {
                             {{GenerateOutput(mode)}} = {{analogOn & 0xFF}};
                         }
                         """;
            case ConfigField.Ps3 or ConfigField.Ps2 or ConfigField.Ps3WithoutCapture or ConfigField.Universal
                when Type == GuitarAxisType.Slider && Input is not DigitalToAnalog:
                return $$"""
                         if (SLIDER_BAR && ({{Input.Generate(writer)}} != PS3_STICK_CENTER)) {
                            {{GenerateOutput(mode)}} = {{Input.Generate(writer)}};
                         }
                         """;
            
            case ConfigField.Wii
                when Type == GuitarAxisType.Slider && Input is DigitalToAnalog:
                // Convert gh5 mappings back to wt
                return $$"""
                         if (SLIDER_BAR && {{Input.Generate(writer)}}) {
                             {{GenerateOutput(mode)}} = {{WiiGh5Mappings[analogOn & 0xFF]}};
                         }
                         """;
            case ConfigField.Wii
                when Type == GuitarAxisType.Slider && Input is not DigitalToAnalog:
                // Convert gh5 mappings back to wt
                return $$"""
                         if (SLIDER_BAR && ({{Input.Generate(writer)}} != PS3_STICK_CENTER)) {
                            uint8_t slider_tmp = {{Input.Generate(writer)}};
                            if (slider_tmp <= 0x15) {
                               {{GenerateOutput(mode)}} = 0x04;
                            } else if (slider_tmp <= 0x30) {
                               {{GenerateOutput(mode)}} = 0x07;
                            } else if (slider_tmp <= 0x4D) {
                               {{GenerateOutput(mode)}} = 0x0a;
                            } else if (slider_tmp <= 0x66) {
                               {{GenerateOutput(mode)}} = 0x0c;
                            } else if (slider_tmp <= 0x9A) {
                               {{GenerateOutput(mode)}} = 0x12;
                            } else if (slider_tmp <= 0xAF) {
                               {{GenerateOutput(mode)}} = 0x14;
                            } else if (slider_tmp <= 0xC9) {
                               {{GenerateOutput(mode)}} = 0x17;
                            } else if (slider_tmp <= 0xE6) {
                               {{GenerateOutput(mode)}} = 0x1A;
                            } else {
                               {{GenerateOutput(mode)}} = 0x1F;
                            }
                         }
                         """;

            // PS3 GH expects tilt on the tilt axis
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture
                when Model is
                     {
                         DeviceControllerType: DeviceControllerType.GuitarHeroGuitar
                     } &&
                     Type == GuitarAxisType.Tilt && Input is DigitalToAnalog:
            {
                return $$"""
                         if (TILT && {{Input.Generate(writer)}}) {
                             report->tilt = 0x180;
                         }
                         """;
            }
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture
                when Model is
                     {
                         DeviceControllerType: DeviceControllerType.GuitarHeroGuitar
                     } &&
                     Type == GuitarAxisType.Tilt && Input is not DigitalToAnalog:
                return $$"""
                         if (TILT && {{Input.Generate(writer)}}) {
                             {{GenerateOutput(mode)}} = {{GenerateAssignment(GenerateOutput(mode), mode, true, false, false, false, writer)}};
                         }
                         """;
            // PS3 GHL expects tilt on accelerometer AND right stick x
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture
                when Model is
                     {
                         DeviceControllerType: DeviceControllerType.GuitarHeroGuitar or DeviceControllerType.LiveGuitar
                     } &&
                     Type == GuitarAxisType.Tilt && Input is DigitalToAnalog:
            {
                return $$"""
                         if (TILT && {{Input.Generate(writer)}}) {
                             report->tilt = 0x180;
                             report->tilt2 = 0xFF;
                         }
                         """;
            }
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture
                when Model is
                     {
                         DeviceControllerType: DeviceControllerType.GuitarHeroGuitar or DeviceControllerType.LiveGuitar
                     } &&
                     Type == GuitarAxisType.Tilt && Input is not DigitalToAnalog:

                // GHL expects right stick x to go to 0xFF or 0x00 to signify tilt being active
                return $$"""
                         if (TILT && {{Input.Generate(writer)}}) {
                             {{GenerateOutput(mode)}} = {{GenerateAssignment(GenerateOutput(mode), mode, true, false, false, false, writer)}};
                             uint8_t tilt_test = {{GenerateAssignment(GenerateOutput(mode), mode, false, false, false, false, writer)}};
                             if (tilt_test > 0xF0) {
                                 report->tilt2 = 0xFF;
                             }
                             if (tilt_test < 0x10) {
                                 report->tilt2 = 0x00;
                             }
                         }
                         """;
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture
                when Model is {DeviceControllerType: DeviceControllerType.RockBandGuitar} &&
                     Type == GuitarAxisType.Tilt && Input is DigitalToAnalog:
                // PS3 rb uses a digital bit, so just map the bit right across and skip the analog conversion
                return $$"""
                         if (TILT && {{Input.Generate(writer)}}) {
                             {{GenerateOutput(mode)}} = true;
                         }
                         """;
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture
                when Model is {DeviceControllerType: DeviceControllerType.RockBandGuitar} &&
                     Type == GuitarAxisType.Tilt && Input is not DigitalToAnalog:
                // PS3 RB expects tilt as a digital bit, so map that here.
                return $$"""
                         if (TILT && {{Input.Generate(writer)}}) {
                             {{GenerateOutput(mode)}} |= {{GenerateAssignment("0", ConfigField.XboxOne, true, false, false, false, writer)}} > 0xE0;
                         }
                         """;
            case ConfigField.Xbox360 or ConfigField.Xbox or ConfigField.Ps3 or ConfigField.Ps3WithoutCapture
                when Model is {DeviceControllerType: DeviceControllerType.RockBandGuitar} &&
                     Type == GuitarAxisType.Pickup && Input is DigitalToAnalog:
                // only keep the first byte
                return $$"""
                         if ({{Input.Generate(writer)}}) {
                             {{GenerateOutput(mode)}} = {{PickupSelectorRangesPS[GetPickupSelectorValue(analogOn)]}};
                         }
                         """;
            case ConfigField.Xbox360 or ConfigField.Xbox or ConfigField.Ps3 or ConfigField.Ps3WithoutCapture
                or ConfigField.Universal
                when Model is {DeviceControllerType: DeviceControllerType.RockBandGuitar} &&
                     Type == GuitarAxisType.Pickup && Input is not DigitalToAnalog:
                var gen2 = $"({Input.Generate(writer)})";
                if (Inverted)
                {
                    gen2 = $"({ushort.MaxValue} - {gen2})";
                }

                return $$"""
                         if ({{gen2}}) {
                             if ({{gen2}} < {{PickupSelectorNotch2}}) {
                                {{GenerateOutput(mode)}} = {{PickupSelectorRangesPS[1]}};
                             } else if ({{gen2}} < {{PickupSelectorNotch3}}) {
                                {{GenerateOutput(mode)}} = {{PickupSelectorRangesPS[2]}};
                             } else if ({{gen2}} < {{PickupSelectorNotch4}}) {
                                {{GenerateOutput(mode)}} = {{PickupSelectorRangesPS[3]}};
                             } else if ({{gen2}} < {{PickupSelectorNotch5}}) {
                                {{GenerateOutput(mode)}} = {{PickupSelectorRangesPS[4]}};
                             } else {
                                {{GenerateOutput(mode)}} = {{PickupSelectorRangesPS[5]}};
                             }
                         }
                         """;
            // Xbox One pickup selector ranges from 0 - 64, so we need to map it correctly.
            case ConfigField.XboxOne
                when Model is {DeviceControllerType: DeviceControllerType.RockBandGuitar} &&
                     Type == GuitarAxisType.Pickup && Input is DigitalToAnalog:
                return $$"""
                         if ({{Input.Generate(writer)}}) {
                             {{GenerateOutput(mode)}} = {{PickupSelectorRangesXb1[GetPickupSelectorValue(analogOn)]}};
                         }
                         """;
            case ConfigField.XboxOne
                when Model is {DeviceControllerType: DeviceControllerType.RockBandGuitar} &&
                     Type == GuitarAxisType.Pickup:
                var gen = $"({Input.Generate(writer)})";
                if (Inverted)
                {
                    gen = $"({ushort.MaxValue} - {gen})";
                }

                return $$"""
                         if ({{gen}}) {
                             if ({{gen}} < {{PickupSelectorNotch2}}) {
                                {{GenerateOutput(mode)}} = {{PickupSelectorRangesXb1[1]}};
                             } else if ({{gen}} < {{PickupSelectorNotch3}}) {
                                {{GenerateOutput(mode)}} = {{PickupSelectorRangesXb1[2]}};
                             } else if ({{gen}} < {{PickupSelectorNotch4}}) {
                                {{GenerateOutput(mode)}} = {{PickupSelectorRangesXb1[3]}};
                             } else if ({{gen}} < {{PickupSelectorNotch5}}) {
                                {{GenerateOutput(mode)}} = {{PickupSelectorRangesXb1[4]}};
                             } else {
                                {{GenerateOutput(mode)}} = {{PickupSelectorRangesXb1[5]}};
                             }
                         }
                         """;
            case ConfigField.Xbox or ConfigField.Xbox360 or ConfigField.Ps4 when Type is GuitarAxisType.Tilt:
                return Input is DigitalToAnalog
                    ? $$"""
                        if (TILT) {
                            {{base.Generate(mode, debounceIndex, ledIndex, extra, combinedExtra, strumIndexes, combinedDebounce, macros, writer)}};
                        }
                        """
                    : $$"""
                        if (TILT) {
                            {{GenerateOutput(mode)}} = {{GenerateAssignment(GenerateOutput(mode), mode, false, false, false, false, writer)}};
                        }
                        """;
            default:
                if (Input is DigitalToAnalog)
                    return base.Generate(mode, debounceIndex, ledIndex, extra, combinedExtra, strumIndexes, combinedDebounce,
                        macros, writer);
                return
                    $"{GenerateOutput(mode)} = {GenerateAssignment(GenerateOutput(mode), mode, false, false, Type is GuitarAxisType.Whammy, false, writer)};";
        }
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        if (deviceControllerType is DeviceControllerType.RockBandGuitar && Type is GuitarAxisType.Slider)
        {
            return "Solo Frets";
        }

        return EnumToStringConverter.Convert(Type);
    }

    protected override string MinCalibrationText()
    {
        return Type switch
        {
            GuitarAxisType.Tilt => Resources.AxisMinCalibrationTilt,
            GuitarAxisType.Whammy => Resources.AxisMinCalibrationWhammy,
            _ => ""
        };
    }

    protected override string MaxCalibrationText()
    {
        return Type switch
        {
            GuitarAxisType.Tilt => Resources.AxisMaxCalibrationTilt,
            GuitarAxisType.Whammy => Resources.AxisMaxCalibrationWhammy,
            _ => ""
        };
    }

    protected override bool SupportsCalibration()
    {
        return Type is not GuitarAxisType.Slider;
    }
}