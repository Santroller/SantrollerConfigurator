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

public class StartSelectHomeInput : FixedInput
{
    public StartSelectHomeInput(ConfigViewModel model) : base(model, 0, false)
    {
    }

    public override string Title => Resources.StartSelectHomeTitle;
}

public class StartSelectHome : Output
{
    private static readonly List<WiiInputType> StartWii = new()
    {
        WiiInputType.ClassicPlus,
        WiiInputType.GuitarPlus,
        WiiInputType.DrumPlus,
        WiiInputType.DjHeroPlus
    };

    private static readonly List<WiiInputType> SelectWii = new()
    {
        WiiInputType.ClassicMinus,
        WiiInputType.GuitarMinus,
        WiiInputType.DrumMinus,
        WiiInputType.DjHeroMinus
    };

    private readonly List<ControllerButton> _outputs = new();

    private bool Peripheral { get; }

    public StartSelectHome(ConfigViewModel model, bool peripheral, bool wii) : base(
        model, new StartSelectHomeInput(model), Colors.Black, Colors.Black, Array.Empty<byte>(), Array.Empty<byte>(),
        false, false, peripheral, -1, true)
    {
        Wii = wii;
        Peripheral = peripheral;
        if (wii)
        {
            for (var i = 0; i < StartWii.Count; i++)
            {
                _outputs.Add(new ControllerButton(Model,
                    new MacroInput(new WiiInput(StartWii[i], model, peripheral),
                        new WiiInput(SelectWii[i], model, peripheral), Model), Colors.Black, Colors.Black,
                    Array.Empty<byte>(), Array.Empty<byte>(), 10, StandardButtonType.Guide, false, false ,false, -1, true));
            }
        }
        else
        {
            _outputs.Add(new ControllerButton(Model,
                new MacroInput(new Ps2Input(Ps2InputType.Start, model, peripheral),
                    new Ps2Input(Ps2InputType.Select, model, peripheral), Model), Colors.Black, Colors.Black,
                Array.Empty<byte>(), Array.Empty<byte>(), 10, StandardButtonType.Guide, false, false ,false, -1, true));
        }

        UpdateDetails();
    }

    private bool Wii { get; }

    public bool Active { get; set; } = false;
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
        return new SerializedStartSelectHome(Wii, Peripheral);
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return Resources.StartSelectHomeTitle;
    }

    public override Enum GetOutputType()
    {
        return StandardButtonType.Guide;
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
        ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw,
            digitalPeripheral, cloneRaw, adxlRaw);
        foreach (var output in _outputs)
            output.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
                ps2ControllerType, wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw,
                digitalPeripheral, cloneRaw, adxlRaw);

        if (!Enabled) return;
        Input.RawValue = _outputs.Any(x => x.ValueRaw != 0) ? 1 : 0;
        UpdateDetails();
    }
}