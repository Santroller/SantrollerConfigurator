using System;
using System.Collections.Generic;
using System.IO;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public abstract partial class Input : ReactiveObject
{
    protected Input(ConfigViewModel model)
    {
        Model = model;
    }

    protected ConfigViewModel Model { get; }

    [Reactive] private bool _isAnalog;
    [Reactive] private int _rawValue;
    public abstract bool IsUint { get; }

    public abstract IList<DevicePin> Pins { get; }
    public abstract IList<PinConfig> PinConfigs { get; }
    public abstract InputType? InputType { get; }
    public abstract bool Peripheral { get; }

    public abstract string Title { get; }

    public abstract IReadOnlyList<string> RequiredDefines();
    public abstract string Generate();

    public abstract SerializedInput Serialise();

    public virtual IEnumerable<Input> InnermostInputs()
    {
        return [this];
    }

    public virtual IList<Input> Inputs()
    {
        return new List<Input> {this};
    }

    public abstract void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw, ReadOnlySpan<byte> djRightRaw,
        ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw, ReadOnlySpan<byte> ps2ControllerType,
        ReadOnlySpan<byte> wiiControllerType, ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw,
        ReadOnlySpan<byte> peripheralWtRaw, Dictionary<int, bool> digitalPeripheral,
        ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw, ReadOnlySpan<byte> mpr121Raw,
        ReadOnlySpan<byte> midiRaw, ReadOnlySpan<byte> bluetoothInputsRaw, bool peripheralConnected);

    public abstract string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode);

    public virtual void SetWriter(BinaryWriter? writer)
    {
        
    }
}