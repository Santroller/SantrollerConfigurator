using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public partial class BluetoothOutput : CombinedOutput
{
    private readonly DispatcherTimer _timer = new();


    public BluetoothOutput(ConfigViewModel model) : base(model)
    {
        Input = new BluetoothInput(this);
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Tick;
        _scanTextHelper = this.WhenAnyValue(s => s.ScanTimer)
            .Select(scanTimer =>
                scanTimer == 11 ? Resources.BluetoothStartScan : string.Format(Resources.BluetoothScanning, scanTimer))
            .ToProperty(this, x => x.ScanText);
        _scanningHelper = this.WhenAnyValue(s => s.ScanTimer).Select(scanTimer => scanTimer != 11).ToProperty(this, x => x.Scanning);
        Addresses.Add(model.BtRxAddr.Length != 0 ? model.BtRxAddr : Resources.BluetoothNoDevice);
        if (Model.Device is Santroller santroller)
            LocalAddress = santroller.GetBluetoothAddress();
        else
            LocalAddress = Resources.BluetoothWriteConfigMessage;
    }

    [Reactive] private string _localAddress;

    public AvaloniaList<string> Addresses { get; } = new();

    private class BluetoothInput : Input
    {
        public BluetoothInput(BluetoothOutput bluetoothOutput) : base(bluetoothOutput.Model)
        {
            BluetoothOutput = bluetoothOutput;
        }

        public BluetoothOutput BluetoothOutput { get; }

        public override bool IsUint => false;
        public override bool Peripheral => false;
        public override IList<DevicePin> Pins => new List<DevicePin>();
        public override IList<PinConfig> PinConfigs => new List<PinConfig>();
        public override InputType? InputType => Types.InputType.BluetoothInput;

        public override string Title => Resources.BluetoothTitle;

        public override IReadOnlyList<string> RequiredDefines()
        {
            return new[] {"BLUETOOTH_RX"};
        }

        public override string Generate(BinaryWriter? writer)
        {
            return "";
        }

        public override SerializedInput Serialise()
        {
            throw new NotImplementedException();
        }

        public override void Update(Dictionary<int, int> analogRaw,
            Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw, ReadOnlySpan<byte> wiiRaw,
            ReadOnlySpan<byte> djLeftRaw, ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw,
            ReadOnlySpan<byte> ghWtRaw, ReadOnlySpan<byte> ps2ControllerType,
            ReadOnlySpan<byte> wiiControllerType, ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw,
            ReadOnlySpan<byte> peripheralWtRaw, Dictionary<int, bool> digitalPeripheral,
            ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw, ReadOnlySpan<byte> mpr121Raw,
            ReadOnlySpan<byte> midiRaw)
        {
        }

        public override string GenerateAll(List<Tuple<Input, string>> bindings,
            ConfigField mode)
        {
            return "";
        }
    }
    [ObservableAsProperty] private string _scanText = "";

    [ObservableAsProperty] private bool _scanning;

    [Reactive] private int _scanTimer = 11;

    [Reactive] private bool _connected;

    public override bool IsCombined => true;
    public override bool IsStrum => false;

    public override bool IsKeyboard => false;
    public override string LedOnLabel => "";
    public override string LedOffLabel => "";

    public override SerializedOutput Serialize()
    {
        return new SerializedBluetoothOutput();
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return Resources.BluetoothCombinedTitle;
    }

    public override Enum GetOutputType()
    {
        return SimpleType.Bluetooth;
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw, ReadOnlySpan<byte> wiiRaw,
        ReadOnlySpan<byte> djLeftRaw, ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw,
        ReadOnlySpan<byte> ghWtRaw, ReadOnlySpan<byte> ps2ControllerType,
        ReadOnlySpan<byte> wiiControllerType, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> bluetoothRaw,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral,
        ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw, ReadOnlySpan<byte> mpr121Raw,
        ReadOnlySpan<byte> midiRaw)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw, peripheralWtRaw, digitalPeripheral, cloneRaw, adxlRaw, mpr121Raw, midiRaw);
        if (LocalAddress == Resources.BluetoothWriteConfigMessage && Model.Device is Santroller santroller)
            LocalAddress = santroller.GetBluetoothAddress();
        if (bluetoothRaw.IsEmpty) return;
        Connected = bluetoothRaw[0] != 0;
    }


    [RelayCommand]
    public void Scan()
    {
        if (Model.Device is not Santroller santroller) return;

        _timer.Start();
        ScanTimer--;
        santroller.StartScan();
    }

    private void Tick(object? sender, EventArgs e)
    {
        // Abort scan when someone hits write
        if (Model.Main.Working)
        {
            ScanTimer = 11;
            _timer.Stop();
            return;
        }
        if (Model.Device is not Santroller santroller) return;

        ScanTimer--;
        var addresses = santroller.GetBtScanResults();
        if (addresses.Count == 0)
        {
            Addresses.Clear();
            Addresses.Add(Resources.BluetoothNoDevice);
            Model.BtRxAddr = Resources.BluetoothNoDevice;
            
        }
        else
        {
            Addresses.Clear();
            Addresses.AddRange(addresses);
            if (string.IsNullOrWhiteSpace(Model.BtRxAddr) || !Model.BtRxAddr.Contains(":"))
            {
                Model.BtRxAddr = Addresses.First();
            }
        }

        if (ScanTimer != 0) return;
        ScanTimer = 11;
        _timer.Stop();
    }


    public override void SetOutputsOrDefaults(IReadOnlyCollection<Output> outputs)
    {
    }

    public override string Generate(ConfigField mode, int debounceIndex, int ledIndex, string extra,
        string combinedExtra,
        List<int> strumIndexes,
        bool combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        return "";
    }


    public override void UpdateBindings()
    {
    }
}