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

public partial class UsbHostInput : HostInput
{
    public UsbHostInput(UsbHostInputType input, ConfigViewModel model, bool combined = false) : base(input, model, combined)
    {
        _usbHostDm = model.WhenAnyValue(x => x.UsbHostDm).ToProperty(this, x => x.UsbHostDm);
        _usbHostDp = model.WhenAnyValue(x => x.UsbHostDp).ToProperty(this, x => x.UsbHostDp);
    }
    public UsbHostInput(Key key, ConfigViewModel model, bool combined = false) : base(key, model, combined)
    {
        _usbHostDm = model.WhenAnyValue(x => x.UsbHostDm).ToProperty(this, x => x.UsbHostDm);
        _usbHostDp = model.WhenAnyValue(x => x.UsbHostDp).ToProperty(this, x => x.UsbHostDp);
    }

    public UsbHostInput(MouseButtonType mouseButtonType, ConfigViewModel model, bool combined = false) : base(mouseButtonType, model, combined)
    {
        _usbHostDm = model.WhenAnyValue(x => x.UsbHostDm).ToProperty(this, x => x.UsbHostDm);
        _usbHostDp = model.WhenAnyValue(x => x.UsbHostDp).ToProperty(this, x => x.UsbHostDp);
    }

    public UsbHostInput(MouseAxisType mouseAxisType, ConfigViewModel model, bool combined = false) : base(mouseAxisType, model, combined)
    {
        _usbHostDm = model.WhenAnyValue(x => x.UsbHostDm).ToProperty(this, x => x.UsbHostDm);
        _usbHostDp = model.WhenAnyValue(x => x.UsbHostDp).ToProperty(this, x => x.UsbHostDp);
    }

    // Since DM and DP need to be next to eachother, you cannot use pins at the far ends
    public List<int> AvailablePinsDm => Model.AvailablePinsDigital.Skip(1).ToList();
    public List<int> AvailablePinsDp => Model.AvailablePinsDigital.Where(s => AvailablePinsDm.Contains(s + 1)).ToList();
    private readonly ObservableAsPropertyHelper<int> _usbHostDm;
    private readonly ObservableAsPropertyHelper<int> _usbHostDp;

    public override IList<DevicePin> Pins => Array.Empty<DevicePin>();
    public override IList<PinConfig> PinConfigs => Model.UsbHostPinConfigs();
    public int UsbHostDm
    {
        get => _usbHostDm.Value;
        set => Model.UsbHostDm = value;
    }

    public int UsbHostDp
    {
        get => _usbHostDp.Value;
        set => Model.UsbHostDp = value;
    }
    public override InputType? InputType => Types.InputType.UsbHostInput;
    public override IReadOnlyList<string> RequiredDefines()
    {
        return ["INPUT_USB_HOST"];
    }
    
    public override string Field => "usb_host_data";

    public override SerializedInput Serialise()
    {
        return new SerializedUsbHostInput(Input, Key, MouseButtonType, MouseAxisType, Combined);
    }
}