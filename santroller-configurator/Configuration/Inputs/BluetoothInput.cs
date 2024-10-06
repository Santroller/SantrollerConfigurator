using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Input;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public partial class BluetoothInput : HostInput
{
    public BluetoothInput(UsbHostInputType input, ConfigViewModel model, bool combined = false) : base(input, model, combined)
    {
    }

    public BluetoothInput(Key key, ConfigViewModel model, bool combined = false) : base(key, model, combined)
    {
    }

    public BluetoothInput(MouseButtonType mouseButtonType, ConfigViewModel model, bool combined = false) : base(mouseButtonType, model, combined)
    {
    }

    public BluetoothInput(MouseAxisType mouseAxisType, ConfigViewModel model, bool combined = false) : base(mouseAxisType, model, combined)
    {
    }

    public BluetoothInput(ProKeyType proKeyType, ConfigViewModel model, bool combined = false) : base(proKeyType, model, combined)
    {
    }
    public override string Field => "bt_data";
    public override InputType? InputType => Types.InputType.BluetoothInput;

    public override IList<DevicePin> Pins => [];
    public override IList<PinConfig> PinConfigs => [];

    public override IReadOnlyList<string> RequiredDefines()
    {
        return [];
    }
    public override SerializedInput Serialise()
    {
        return new SerializedBluetoothInput(Input, Key, MouseButtonType, MouseAxisType, ProKeyType, Combined);
    }

    public override void Update(Dictionary<int, int> analogRaw, Dictionary<int, bool> digitalRaw,
        ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw,
        ReadOnlySpan<byte> mpr121Raw, ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw)
    {
        Update(bluetoothInputsRaw);
    }
}