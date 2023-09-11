using System;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.ViewModels;

public class MainViewModel : ReactiveObject, IRoutableViewModel
{
    public MainViewModel(MainWindowViewModel screen)
    {
        Main = screen;
        HostScreen = screen;
    }

    public MainWindowViewModel Main { get; }

    public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

    public IScreen HostScreen { get; }
}