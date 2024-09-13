using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using GuitarConfigurator.NetCore.Devices;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace SantrollerConfiguratorBranded.NetCore.ViewModels;

public partial class BrandedMainViewModel : ReactiveObject, IRoutableViewModel
{
    public BrandedMainViewModel(BrandedMainWindowViewModel screen)
    {
        BrandedMain = screen;
        HostScreen = screen;
    }
    public BrandedMainWindowViewModel BrandedMain { get; }

    public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

    public IScreen HostScreen { get; }

    public string WelcomeText => string.Format(Resources.WelcomeText, BrandedMain.Config.ToolName);
    public string EraseText => string.Format(Resources.EraseText, BrandedMain.Config.ToolName);
}