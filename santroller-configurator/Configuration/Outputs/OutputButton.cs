using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Input;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Exceptions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using static GuitarConfigurator.NetCore.ViewModels.ConfigViewModel;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public abstract partial class OutputButton : Output
{
    private readonly ObservableAsPropertyHelper<float> _debounceDisplay;
    protected OutputButton(ConfigViewModel model, bool enabled, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte[] ledIndicesPeripheral,
        byte[] ledIndicesMpr121,
        int debounce, bool outputEnabled, bool outputInverted, bool outputPeripheral, int outputPin,
        bool childOfCombined) : base(model, enabled, input, ledOn, ledOff, ledIndices, ledIndicesPeripheral, ledIndicesMpr121, outputEnabled, outputInverted, outputPeripheral, outputPin, childOfCombined)
    {
        Debounce = debounce;
        _debounceDisplay = this.WhenAnyValue(x => x.Debounce)
            .Select(x => x / 10.0f)
            .ToProperty(this, x => x.DebounceDisplay);
    }

    public override bool UsesPwm => false;

    [Reactive] public int _debounce;

    public float DebounceDisplay
    {
        get => _debounceDisplay.Value;
        set => Debounce = (byte) (value * 10);
    }

    public override bool IsCombined => false;
    public override string LedOnLabel => Resources.LedColourActiveButtonColour;
    public override string LedOffLabel => Resources.LedColourInactiveButtonColour;


    /// <summary>
    ///     Generate bindings
    /// </summary>
    /// <param name="mode"></param>
    /// <param name="debounceIndex"></param>
    /// <param name="ledIndex"></param>
    /// <param name="extra">Used to provide extra statements that are called if the button is pressed</param>
    /// <param name="combinedExtra"></param>
    /// <param name="strumIndexes"></param>
    /// <param name="combinedDebounce"></param>
    /// <param name="macros"></param>
    /// <param name="writer"></param>
    /// <returns></returns>
    /// <exception cref="IncompleteConfigurationException"></exception>
    public override string Generate(ConfigField mode, int debounceIndex, int ledIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        if (Model is {Branded: false, Builder: false} && !Enabled)
        {
            return "";
        }
        var ifStatement = $"debounce[{debounceIndex}]";
        var extraStatement = "";
        if (mode == ConfigField.Shared && combinedExtra.Length != 0) extraStatement = $" && ({combinedExtra})";

        var debounce = Debounce;
        if (!Model.LocalDebounceMode)
        {
            if (this is GuitarButton {IsStrum: true} && Model.StrumDebounce > 0)
            {
                debounce = Model.StrumDebounce;
            }
            else
            {
                debounce = Model.Debounce;
            }
        }

        if (!Model.Deque)
        {
            // If we aren't using queue based inputs, then we want ms based inputs, not ones based on 0.1ms
            debounce /= 10;
        }

        if (mode == ConfigField.Reset)
        {
            return debounce == 0 ? $"debounce[{debounceIndex}]=0;" : "";
        }
        debounce += 1;
        
        if (mode != ConfigField.Shared)
        {
            if (Model.Deque && this is GuitarButton {IsStrum: false})
            {
                ifStatement = $"{GenerateOutput(ConfigField.Ps3).Replace("report->", "current_queue_report.")}";
            }
            var outputVar = GenerateOutput(mode);
            if (outputVar.Length == 0) return "";
            var keyCode = KeyboardButton.KeyCodes.IndexOf(outputVar);
            // Modifiers still go via the normal system, only standard keys go via 6kro mode.
            if ((Model.IsKeyboard || Model.IsFortniteFestivalPro) && Model.RolloverMode == RolloverMode.SixKro && keyCode != -1 && mode == ConfigField.Keyboard)
            {
                if (Model.IsFortniteFestivalPro && this is KeyboardButton {Key: Key.PageDown})
                {
                    return  $$"""
                              if ({{ifStatement}} && !lastTilt) {
                                lastTilt = millis();
                              }
                              if ({{ifStatement}}) {
                                tiltActive = true;
                              }
                              if ({{ifStatement}} && ((millis() - lastTilt) < 1000)) {
                                 setKey({{debounceIndex}},{{keyCode}},report);
                                 {{extra}}
                              }
                              """;
                }
                return  $$"""
                          if ({{ifStatement}}) {
                              setKey({{debounceIndex}},{{keyCode}},report);
                              {{extra}}
                          } 
                          """;
            }

            if (mode == ConfigField.Xbox && !outputVar.Contains("dpad") && !outputVar.Contains("start") && !outputVar.Contains("back") && !outputVar.Contains("Thumb"))
            {
                return  $$"""
                          if ({{ifStatement}}) {
                              {{outputVar}} = 0xFF;
                              {{extra}}
                          }
                          """;
            }
            
            if (Model.IsFortniteFestivalPro && this is KeyboardButton {Key: Key.PageDown} && mode == ConfigField.Keyboard)
            {
                return  $$"""
                          if ({{ifStatement}}) {
                              if (!lastTilt ) {
                                lastTilt = millis();
                              }
                              if ((millis() - lastTilt) < 1000) {
                                  {{outputVar}} = true;
                                  {{extra}}
                              }
                              tiltActive = true;
                          }
                          """;
            }

            if (Model.DeviceControllerType is DeviceControllerType.LiveGuitar && this is GuitarButton {IsStrum: true} && mode != ConfigField.Universal && mode != ConfigField.Xbox360)
            {
                
                return  $$"""
                          if ({{ifStatement}}) {
                              {{extra}}
                          }
                          """;
            }
            return  $$"""
                      if ({{ifStatement}}) {
                          {{outputVar}} = true;
                          {{extra}}
                      }
                      """;

        }
        
        var gen = Input.Generate();
        var reset = $"debounce[{debounceIndex}]={debounce};";

        if (Model.LedType != LedType.None || Model.LedTypePeripheral != LedType.None || OutputEnabled || Model.HasMpr121)
        {
            reset += $"ledDebounce[{ledIndex}]={debounce};";
        }
        if (writer != null)
        {
            reset = $"debounce[{debounceIndex}]={WriteBlob(writer, (byte)debounce)};";
            if (Model.LedType != LedType.None || Model.LedTypePeripheral != LedType.None || OutputEnabled || Model.HasMpr121)
            {
                reset += $"ledDebounce[{debounceIndex}]={WriteBlob(writer, (byte) debounce)};";
            }
        }

        if (Input is MacroInput)
        {
            foreach (var input in Input.Inputs())
            {
                var gen2 = input.Generate();
                if (!macros.TryGetValue(gen2, out var inputs2)) continue;
                extra += string.Join("\n    ", inputs2.Select(s => $"debounce[{s.Item1}]=0;"));
            }
        }
        foreach (var input in Input.InnermostInputs())
        {
            if (input is MidiInput midiInput && Model.IsDrum)
            {
                extra += $"midiData.midiVelocitiesTemp[{midiInput.Key}] = 0;";
            }
        }
        
        var ret = $$"""
                 if (({{gen}} {{extraStatement}})) {
                     {{reset}} {{extra}}
                 }
                 """;
        if (Model.Deque && this is GuitarButton {IsStrum: false}) {
            ret += $$"""

                      if ({{ifStatement}}) {
                          {{GenerateOutput(ConfigField.Ps3).Replace("report->", "current_queue_report.")}} = true;
                      }
                      """;
        }
        return ret;
    }

    public override void UpdateBindings()
    {
    }
}