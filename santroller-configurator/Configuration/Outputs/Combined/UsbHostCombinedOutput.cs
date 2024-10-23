using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public partial class UsbHostCombinedOutput : HostCombinedOutput
{
    public UsbHostCombinedOutput(ConfigViewModel model) : base(
        model)
    {
        _usbHostDm = model.WhenAnyValue(x => x.UsbHostDm).ToProperty(this, x => x.UsbHostDm);
        _usbHostDp = model.WhenAnyValue(x => x.UsbHostDp).ToProperty(this, x => x.UsbHostDp);
        UpdateDetails();
    }
    
    public override HostInput MakeInput(UsbHostInputType type)
    {
        return new UsbHostInput(type, Model, true);
    }
    public override HostInput MakeInput(ProKeyType type)
    {
        return new UsbHostInput(type, Model, true);
    }


    private readonly ObservableAsPropertyHelper<int> _usbHostDm;
    private readonly ObservableAsPropertyHelper<int> _usbHostDp;

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
}