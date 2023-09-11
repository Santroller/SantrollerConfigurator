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
    public static readonly List<Key> Keys = new()
    {
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
        Key.VolumeDown,
    };

    public Key Key;

    public KeyboardButton(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte debounce, Key type) : base(model, input, ledOn, ledOff, ledIndices,
        debounce, false)
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
        switch (IsMediaKey)
        {
            case true when mode is not ConfigField.Consumer:
            case false when mode is not ConfigField.Keyboard:
                return "";
            default:
                return Key == Key.Delete ? GetReportField("Del") : GetReportField(Key);
        }
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedKeyboardButton(Input.Serialise(), LedOn, LedOff, LedIndices.ToArray(), Debounce, Key);
    }
}