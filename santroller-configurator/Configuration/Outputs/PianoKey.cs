using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

public partial class PianoKey : OutputAxis
{
    public readonly ProKeyType Key;
    [Reactive] private int _threshold;

    public PianoKey(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte[] ledIndicesPeripheral, byte[] ledIndicesMpr121, ProKeyType key, int threshold, bool outputEnabled, bool outputPeripheral,
        bool outputInverted,
        int outputPin, bool childOfCombined) : base(model, input, ledOn,
        ledOff, ledIndices, ledIndicesPeripheral, ledIndicesMpr121,
        0, ushort.MaxValue, 0, 0, true, outputEnabled, outputInverted, outputPeripheral, outputPin, childOfCombined)
    {
        Threshold = threshold;
        Key = key;
        UpdateDetails();
        _hasThresholdHelper =
            this.WhenAnyValue(x => x.Key, x => x.IsDigitalToAnalog, (pianoKey, dta) => !dta && pianoKey is ProKeyType.Overdrive or ProKeyType.Pedal)
                .ToProperty(this, x => x.HasThreshold);
    }

    [ObservableAsProperty] private bool _hasThreshold;

    public override string LedOnLabel => Resources.LedColourActiveButtonColour;
    public override string LedOffLabel => Resources.LedColourInactiveButtonColour;

    public override bool IsKeyboard => false;


    public override bool IsStrum => false;

    public override string GenerateOutput(ConfigField mode)
    {
        return mode is not (ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4 or ConfigField.XboxOne
            or ConfigField.Xbox360 or ConfigField.Universal or ConfigField.Xbox)
            ? ""
            : GetReportField(Key);
    }

    public override bool ShouldFlip(ConfigField mode)
    {
        return false;
    }

    protected override string MinCalibrationText()
    {
        return "";
    }

    protected override string MaxCalibrationText()
    {
        return "";
    }

    protected override bool SupportsCalibration()
    {
        return false;
    }


    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return EnumToStringConverter.Convert(Key);
    }

    public override Enum GetOutputType()
    {
        return Key;
    }

    public override string Generate(ConfigField mode, int debounceIndex, int ledIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        if (mode == ConfigField.Shared)
        {
            return "";
        }

        var output = GenerateOutput(mode);
        if (output.Length == 0) return "";

        if (Input is not DigitalToAnalog dta)
        {
            return Key switch
            {
                ProKeyType.TouchPad => $$"""
                                                if ({{Input.Generate(writer)}}) {
                                                    report->touchPad = {{GenerateAssignment("report->touchPad", mode, false, false, false, false, writer)}};
                                                }
                                                """,
                ProKeyType.Overdrive when writer != null => $$"""
                                          if ({{Input.Generate(writer)}} > ({{WriteBlob(writer, Threshold)}})) {
                                              {{output}} = true;
                                          }
                                          """,
                ProKeyType.Overdrive => $$"""
                                          if ({{Input.Generate(writer)}} > {{Threshold}}) {
                                              {{output}} = true;
                                          }
                                          """,
                ProKeyType.Pedal when writer != null => $$"""
                                                          if ({{Input.Generate(writer)}}) {
                                                              report->pedalAnalog = {{GenerateAssignment("report->pedalAnalog", mode, false, false, false, false, writer)}};
                                                              report->pedalDigital = {{Input.Generate(writer)}} > ({{WriteBlob(writer, Threshold)}});
                                                          }
                                                          """,
                ProKeyType.Pedal => $$"""
                                      if ({{Input.Generate(writer)}}) {
                                          report->pedalAnalog = {{GenerateAssignment("report->pedalAnalog", mode, false, false, false, false, writer)}};
                                          report->pedalDigital = {{Input.Generate(writer)}} > {{Threshold}};
                                      }
                                      """,
                _ => $$"""
                       if ({{Input.Generate(writer)}}) {
                           proKeyVelocities[{{(int) Key}}] = {{GenerateAssignment($"proKeyVelocities[{(int) Key}]", mode, false, false, false, false, writer)}};
                           {{output}} = true;
                       }
                       """
            };
        }

        return Key switch
        {
            ProKeyType.TouchPad => $$"""
                                            if ({{Input.Generate(writer)}}) {
                                                report->touchPad = {{dta.On >> 9}};
                                            }
                                            """,
            ProKeyType.Overdrive => $$"""
                                      if ({{Input.Generate(writer)}}) {
                                          {{output}} = true;
                                      }
                                      """,
            ProKeyType.Pedal => $$"""
                                  if ({{Input.Generate(writer)}}) {
                                      report->pedalAnalog = {{dta.On >> 9}};
                                      report->pedalDigital = true;
                                  }
                                  """,
            _ => $$"""
                   if ({{Input.Generate(writer)}}) {
                       proKeyVelocities[{{(int) Key}}] = {{dta.On >> 9}};
                       {{output}} = true;
                   }
                   """
        };
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedPianoKey(Input!.Serialise(), Threshold, LedOn, LedOff, LedIndices.ToArray(),
            LedIndicesPeripheral.ToArray(), Key, OutputEnabled, OutputPin, OutputInverted, PeripheralOutput,
            ChildOfCombined, LedIndicesMpr121.ToArray());
    }
}