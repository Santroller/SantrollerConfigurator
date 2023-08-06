using System;
using ReactiveUI;

namespace SantrollerConfiguratorBuilder.NetCore.ViewModels;

public class BuilderMainViewModel : ReactiveObject, IRoutableViewModel
{
    public BuilderMainViewModel(BuilderMainWindowViewModel screen)
    {
        BuilderMain = screen;
        HostScreen = screen;
        Console.WriteLine(screen);
    }

    public BuilderMainWindowViewModel BuilderMain { get; }

    public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

    public IScreen HostScreen { get; }
}