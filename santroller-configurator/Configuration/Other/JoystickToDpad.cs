using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Other;

public class JoystickToDpadInput : FixedInput
{
    public JoystickToDpadInput(ConfigViewModel model) : base(model, 0, false)
    {
    }

    public override string Title => Resources.JoystickToDpadTitle;
}

public class JoystickToDpad : Output
{
    private static readonly List<WiiInputType> JoystickToDpadXWii = new()
    {
        WiiInputType.ClassicLeftStickX,
        WiiInputType.NunchukStickX,
        WiiInputType.GuitarJoystickX,
        WiiInputType.DrumJoystickX
    };

    private static readonly List<WiiInputType> JoystickToDpadYWii = new()
    {
        WiiInputType.ClassicLeftStickY,
        WiiInputType.NunchukStickY,
        WiiInputType.GuitarJoystickY,
        WiiInputType.DrumJoystickY
    };

    private readonly List<ControllerButton> _outputs = new();
    
    private bool Peripheral { get; }

    public JoystickToDpad(ConfigViewModel model, bool peripheral, int threshold, bool wii) : base(
        model, new JoystickToDpadInput(model), Colors.Black, Colors.Black, Array.Empty<byte>(),Array.Empty<byte>(), true)
    {
        Threshold = threshold;
        Wii = wii;
        Peripheral = peripheral;
        if (wii)
        {
            foreach (var wiiInputType in JoystickToDpadXWii)
            {
                _outputs.Add(new ControllerButton(model,
                    new AnalogToDigital(new WiiInput(wiiInputType, model, peripheral), AnalogToDigitalType.JoyLow, Threshold,
                        model), Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), 10, StandardButtonType.DpadLeft,
                    true));
                _outputs.Add(new ControllerButton(model,
                    new AnalogToDigital(new WiiInput(wiiInputType, model, peripheral), AnalogToDigitalType.JoyHigh, Threshold,
                        model), Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), 10, StandardButtonType.DpadRight,
                    true));
            }

            foreach (var wiiInputType in JoystickToDpadYWii)
            {
                _outputs.Add(new ControllerButton(model,
                    new AnalogToDigital(new WiiInput(wiiInputType, model, peripheral), AnalogToDigitalType.JoyHigh, Threshold,
                        model), Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), 10, StandardButtonType.DpadUp, true));
                _outputs.Add(new ControllerButton(model,
                    new AnalogToDigital(new WiiInput(wiiInputType, model, peripheral), AnalogToDigitalType.JoyLow, Threshold,
                        model), Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), 10, StandardButtonType.DpadDown,
                    true));
            }
        }
        else
        {

            _outputs.Add(new ControllerButton(model,
                new AnalogToDigital(new Ps2Input(Ps2InputType.LeftStickX, model, peripheral), AnalogToDigitalType.JoyLow, Threshold,
                    model), Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), 10, StandardButtonType.DpadLeft, true));
            _outputs.Add(new ControllerButton(model,
                new AnalogToDigital(new Ps2Input(Ps2InputType.LeftStickX, model, peripheral), AnalogToDigitalType.JoyHigh, Threshold,
                    model), Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), 10, StandardButtonType.DpadRight, true));
            _outputs.Add(new ControllerButton(model,
                new AnalogToDigital(new Ps2Input(Ps2InputType.LeftStickY, model, peripheral), AnalogToDigitalType.JoyHigh, Threshold,
                    model), Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), 10, StandardButtonType.DpadUp, true));
            _outputs.Add(new ControllerButton(model,
                new AnalogToDigital(new Ps2Input(Ps2InputType.LeftStickY, model, peripheral), AnalogToDigitalType.JoyLow, Threshold,
                    model), Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(), 10, StandardButtonType.DpadDown, true));
        }

        UpdateDetails();
    }

    private bool Wii { get; }
    [Reactive] public int Threshold { get; set; }

    [Reactive] public bool Up { get; set; }

    [Reactive] public bool Down { get; set; }

    [Reactive] public bool Left { get; set; }

    [Reactive] public bool Right { get; set; }

    public override bool IsCombined => false;
    public override bool IsStrum => false;

    public override bool IsKeyboard => false;
    public override string LedOnLabel => "";
    public override string LedOffLabel => "";


    public override IEnumerable<Output> ValidOutputs()
    {
        return _outputs;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedJoystickToDpad(Threshold, Wii, Peripheral);
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return deviceControllerType is DeviceControllerType.Gamepad
            ? Resources.LeftJoystickToDpadTitle
            : Resources.JoystickToDpadTitle;
    }

    public override Enum GetOutputType()
    {
        if (!Up && !Down && !Left && !Right) return Wii ? DpadType.Wii : DpadType.Ps2;

        var buttons = Wii ? "Wii" : "Ps2";
        if (Up) buttons += "Up";
        if (Down) buttons += "Down";
        if (Left) buttons += "Left";
        if (Right) buttons += "Right";
        return Enum.Parse<DpadType>(buttons);
    }

    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        return "";
    }

    public override void UpdateBindings()
    {
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw, ReadOnlySpan<byte> wiiRaw,
        ReadOnlySpan<byte> djLeftRaw, ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw,
        ReadOnlySpan<byte> ghWtRaw, ReadOnlySpan<byte> ps2ControllerType,
        ReadOnlySpan<byte> wiiControllerType, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> bluetoothRaw,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral,
        ReadOnlySpan<byte> cloneRaw)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw, digitalPeripheral, cloneRaw);
        foreach (var output in _outputs)
            output.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
                ps2ControllerType, wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw, digitalPeripheral, cloneRaw);

        if (!Enabled) return;
        Up = _outputs.Where(s => s.Type is StandardButtonType.DpadUp)
            .Any(x => x.ValueRaw != 0);
        Down = _outputs.Where(s => s.Type is StandardButtonType.DpadDown)
            .Any(x => x.ValueRaw != 0);
        Left = _outputs.Where(s => s.Type is StandardButtonType.DpadLeft)
            .Any(x => x.ValueRaw != 0);
        Right = _outputs.Where(s => s.Type is StandardButtonType.DpadRight)
            .Any(x => x.ValueRaw != 0);
        UpdateDetails();
    }
}