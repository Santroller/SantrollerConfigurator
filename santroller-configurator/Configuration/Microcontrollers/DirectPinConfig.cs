using System.Collections.Generic;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public class DirectPinConfig : PinConfig
{
    private int _pin;

    public DirectPinConfig(ConfigViewModel model, string type, int pin, bool peripheral,  DevicePinMode pinMode) : base(model, peripheral)
    {
        Type = type;
        PinMode = pinMode;
        Pin = pin;
    }

    public override string Type { get; }
    public override string Definition => "";

    [Reactive] public DevicePinMode PinMode { get; set; }

    public int Pin
    {
        get => _pin;
        set
        {
            this.RaiseAndSetIfChanged(ref _pin, value);
            Update();
        }
    }

    public override IEnumerable<int> Pins => new List<int> {Pin};


    public override string Generate()
    {
        return "";
    }
}