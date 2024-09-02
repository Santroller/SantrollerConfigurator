using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class KeyboardButton : OutputButton
{
    // 6KRO doesn't use the standard bitfield logic everything else uses. Its easier to use the same logic anyways
    // and then do a lookup to resolve the generated bitfields back to standard keycodes when doing the final build.
    public static readonly List<string> KeyCodes =
    [
        "blank",
        "blank",
        "blank",
        "blank",
        "report->a",
        "report->b",
        "report->c",
        "report->d",
        "report->e",
        "report->f",
        "report->g",
        "report->h",
        "report->i",
        "report->j",
        "report->k",
        "report->l",
        "report->m",
        "report->n",
        "report->o",
        "report->p",
        "report->q",
        "report->r",
        "report->s",
        "report->t",
        "report->u",
        "report->v",
        "report->w",
        "report->x",
        "report->y",
        "report->z",
        "report->d1",
        "report->d2",
        "report->d3",
        "report->d4",
        "report->d5",
        "report->d6",
        "report->d7",
        "report->d8",
        "report->d9",
        "report->d0",
        "report->enter",
        "report->escape",
        "report->back",
        "report->tab",
        "report->space",
        "report->oemMinus",
        "report->oemPlus",
        "report->oemOpenBrackets",
        "report->oemCloseBrackets",
        "report->oemPipe",
        "unused ansi pipe",
        "report->oemSemicolon",
        "report->oemQuotes",
        "report->oemTilde",
        "report->oemComma",
        "report->oemPeriod",
        "report->oemQuestion",
        "report->capsLock",
        "report->f1",
        "report->f2",
        "report->f3",
        "report->f4",
        "report->f5",
        "report->f6",
        "report->f7",
        "report->f8",
        "report->f9",
        "report->f10",
        "report->f11",
        "report->f12",
        "report->printScreen",
        "report->scroll",
        "report->pause",
        "report->insert",
        "report->home",
        "report->pageUp",
        "report->del",
        "report->end",
        "report->pageDown",
        "report->right",
        "report->left",
        "report->down",
        "report->up",
        "report->numLock",
        "report->divide",
        "report->multiply",
        "report->subtract",
        "report->add",
        "report->numpad enter",
        "report->numPad1",
        "report->numPad2",
        "report->numPad3",
        "report->numPad4",
        "report->numPad5",
        "report->numPad6",
        "report->numPad7",
        "report->numPad8",
        "report->numPad9",
        "report->numPad0",
        "report->decimal",
        "blank",
        "report->apps",
        "blank",
        "blank",
        "report->f13",
        "report->f14",
        "report->f15",
        "report->f16",
        "report->f17",
        "report->f18",
        "report->f19",
        "report->f20",
        "report->f21",
        "report->f22",
        "report->f23",
        "report->f24"
    ];

    public static readonly List<Key> Keys =
    [
        Key.LeftCtrl,
        Key.LeftAlt,
        Key.LeftShift,
        Key.LWin,
        Key.RightCtrl,
        Key.RightAlt,
        Key.RightShift,
        Key.RWin,
        Key.A,
        Key.B,
        Key.C,
        Key.D,
        Key.E,
        Key.F,
        Key.G,
        Key.H,
        Key.I,
        Key.J,
        Key.K,
        Key.L,
        Key.M,
        Key.N,
        Key.O,
        Key.P,
        Key.Q,
        Key.R,
        Key.S,
        Key.T,
        Key.U,
        Key.V,
        Key.W,
        Key.X,
        Key.Y,
        Key.Z,
        Key.D0,
        Key.D1,
        Key.D2,
        Key.D3,
        Key.D4,
        Key.D5,
        Key.D6,
        Key.D7,
        Key.D8,
        Key.D9,
        Key.Enter,
        Key.Escape,
        Key.Back,
        Key.Tab,
        Key.Space,
        Key.OemMinus,
        Key.OemPlus,
        Key.OemOpenBrackets,
        Key.OemCloseBrackets,
        Key.OemPipe,
        Key.OemSemicolon,
        Key.OemQuotes,
        Key.OemTilde,
        Key.OemComma,
        Key.OemPeriod,
        Key.OemQuestion,
        Key.CapsLock,
        Key.F1,
        Key.F2,
        Key.F3,
        Key.F4,
        Key.F5,
        Key.F6,
        Key.F7,
        Key.F8,
        Key.F9,
        Key.F10,
        Key.F11,
        Key.F12,
        Key.F13,
        Key.F14,
        Key.F15,
        Key.F16,
        Key.F17,
        Key.F18,
        Key.F19,
        Key.F20,
        Key.F21,
        Key.F22,
        Key.F23,
        Key.F24,
        Key.PrintScreen,
        Key.Scroll,
        Key.Pause,
        Key.Insert,
        Key.Home,
        Key.PageUp,
        Key.PageDown,
        Key.Delete,
        Key.End,
        Key.Right,
        Key.Left,
        Key.Up,
        Key.Down,
        Key.NumLock,
        Key.Divide,
        Key.Multiply,
        Key.Subtract,
        Key.Add,
        Key.NumPad0,
        Key.NumPad1,
        Key.NumPad2,
        Key.NumPad3,
        Key.NumPad4,
        Key.NumPad5,
        Key.NumPad6,
        Key.NumPad7,
        Key.NumPad8,
        Key.NumPad9,
        Key.Decimal,
        Key.MediaNextTrack,
        Key.MediaPreviousTrack,
        Key.MediaStop,
        Key.MediaPlayPause,
        Key.VolumeMute,
        Key.VolumeUp,
        Key.VolumeDown
    ];

    public Key Key;

    public KeyboardButton(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte[] ledIndicesPeripheral, byte[] ledIndicesMpr121, 
        int debounce, Key type, bool outputEnabled, bool outputPeripheral, bool outputInverted, int outputPin) : base(model, input, ledOn, ledOff, ledIndices, ledIndicesPeripheral, ledIndicesMpr121,
        debounce, outputEnabled, outputInverted, outputPeripheral, outputPin, false)
    {
        Key = type;
        UpdateDetails();
    }

    public bool IsMediaKey => Key is Key.MediaStop or Key.MediaNextTrack or Key.MediaPlayPause or Key.VolumeDown
        or Key.VolumeMute or Key.VolumeUp;

    public override bool IsKeyboard => true;
    public virtual bool IsController => false;

    public override bool IsStrum => false;

    public override bool IsCombined => false;

    public override void UpdateBindings()
    {
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

    public override string GenerateOutput(ConfigField mode)
    {
        // Standard keys need to be handled in keyboard
        if (!IsMediaKey && mode is not ConfigField.Keyboard)
        {
            return "";
        }
        // Media keys need to be handled in consumer
        if (IsMediaKey && mode is not ConfigField.Consumer)
        {
            return "";
        }
        // Some keys have multiple names, so we need to make sure the correct one is used.
        return Key switch
        {
            Key.Return => GetReportField("Enter"),
            Key.Next => GetReportField("PageDown"),
            Key.Prior => GetReportField("PageUp"),
            Key.Oem1 => GetReportField("OemSemicolon"),
            Key.Oem2 => GetReportField("OemQuestion"),
            Key.Oem3 => GetReportField("OemTilde"),
            Key.Oem4 => GetReportField("OemOpenBrackets"),
            Key.Oem5 => GetReportField("OemPipe"),
            Key.Oem6 => GetReportField("OemCloseBrackets"),
            Key.Delete => GetReportField("Del"),
            _ => GetReportField(Key)
        };
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedKeyboardButton(Input.Serialise(), LedOn, LedOff, LedIndices.ToArray(),
            LedIndicesPeripheral.ToArray(), Debounce, Key, OutputEnabled, OutputPin, OutputInverted, PeripheralOutput, LedIndicesMpr121.ToArray());
    }
}