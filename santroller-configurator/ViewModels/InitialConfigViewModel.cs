using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Devices;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.ViewModels;

public class InitialConfigViewModel : ReactiveObject, IRoutableViewModel
{
    public InitialConfigViewModel(MainWindowViewModel screen, ConfigViewModel model)
    {
        Main = screen;
        Model = model;

        HostScreen = screen;
        if (IsPeripheral)
        {
            ConfigureCommand = ReactiveCommand.CreateFromObservable(
                () => Main.GoBack.Execute(), this.WhenAnyValue(x => x.Main.Working).Select(s => !s)
            );
        }
        else
        {
            ConfigureCommand = ReactiveCommand.CreateFromObservable(
                () => Main.Router.Navigate.Execute(model), this.WhenAnyValue(x => x.Main.Working).Select(s => !s)
            );
        }
        
        DfuImage = GetImage();
    }

    public bool IsPeripheral => Main.DeviceInputType is DeviceInputType.Peripheral;

    public MainWindowViewModel Main { get; }
    public ConfigViewModel Model { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> ConfigureCommand { get; }
    public Bitmap? DfuImage { get; }
    public string? UrlPathSegment => Guid.NewGuid().ToString()[..5];
    public IScreen HostScreen { get; }

    public IDisposable RegisterConnections()
    {
        return
            Main.AvailableDevices.Connect().ObserveOn(RxApp.MainThreadScheduler).Subscribe(s =>
            {
                foreach (var change in s)
                    switch (change.Reason)
                    {
                        case ListChangeReason.Add:
                            Model.AddDevice(change.Item.Current);
                            break;
                    }
            });
        ;
    }

    public Bitmap? GetImage()
    {
        if (Model.Device is not Arduino arduino) return null;
        var assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
        var bitmap = arduino.Board.ArdwiinoName switch
        {
            "mega2560" => "ArduinoMegaDFU.png",
            "megaadk" => "ArduinoMegaADKDFU.png",
            "uno" => "ArduinoUnoDFU.png",
            _ => null
        };
        try
        {
            return new Bitmap(AssetLoader.Open(new Uri($"avares://{assemblyName}/Assets/Images/{bitmap}")));
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }
}