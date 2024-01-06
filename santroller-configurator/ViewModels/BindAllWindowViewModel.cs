using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Devices;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.ViewModels;

public class BindAllWindowViewModel : ReactiveObject
{
    private readonly Santroller? _santroller;
    public readonly Interaction<Unit, Unit> CloseWindowInteraction = new();

    private volatile bool _picking = true;
    public string AccentedTextColor { get; }
    public BindAllWindowViewModel(ConfigViewModel model, Output output)
    {
        AccentedTextColor = model.Main.AccentedButtonTextColor;
        Model = model;
        Output = output;
        Input = (output.Input.InnermostInputs().First() as DirectInput)!;
        IsAnalog = Input.IsAnalog;
        LocalisedName = output.GetName(model.DeviceControllerType, model.LegendType, model.SwapSwitchFaceButtons);

        ContinueCommand = ReactiveCommand.CreateFromObservable(() => Close(true));
        AbortCommand = ReactiveCommand.CreateFromObservable(() => Close(false));
        this.WhenAnyValue(x => x.Input.RawValue, x => x.IsAnalog)
            .Select(s => s.Item2 ? s.Item1 : (s.Item1 * ushort.MaxValue))
            .ToPropertyEx(this, x => x.RawValue);

        if (Model.Device is not Santroller santroller)
        {
            CloseWindowInteraction.Handle(new Unit());
            _santroller = null;
            return;
        }

        _santroller = santroller;
        _ = Task.Run(async () =>
        {
            while (_picking)
            {
                var pin = await santroller.DetectPinAsync(IsAnalog, Input.Pin, Model.Microcontroller, Input.Peripheral);
                if (!_picking)
                {
                    return;
                }
                Input.Pin = pin;
                await Task.Delay(100);
            }
        });
    }

    public ConfigViewModel Model { get; }
    public Output Output { get; }
    public DirectInput Input { get; }
    public ICommand ContinueCommand { get; }
    public ICommand AbortCommand { get; }
    public bool Response { get; set; }
    public bool IsAnalog { get; }
    public string LocalisedName { get; }
    
    // ReSharper disable UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] public int RawValue { get; }
    // ReSharper enable UnassignedGetOnlyAutoProperty

    private IObservable<Unit> Close(bool response)
    {
        _picking = false;
        _santroller?.CancelDetection();
        Response = response;
        return CloseWindowInteraction.Handle(new Unit());
    }
}