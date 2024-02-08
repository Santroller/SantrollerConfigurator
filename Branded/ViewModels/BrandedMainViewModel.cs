using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using GuitarConfigurator.NetCore.Devices;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SantrollerConfiguratorBranded.NetCore.ViewModels;

public partial class BrandedMainViewModel : ReactiveObject, IRoutableViewModel
{
    private readonly HashSet<string> _validDevices = new();
    public BrandedMainViewModel(BrandedMainWindowViewModel screen)
    {
        BrandedMain = screen;
        HostScreen = screen;
        _validDevices.UnionWith(BrandedMain.Config.Configurations.SelectMany(s => s.Configurations).Select(s => $"{s.VendorName}_{s.ProductName}"));
        this.WhenAnyValue(s => s.BrandedMain.SelectedDevice).Select(s =>
                s is Santroller santroller && _validDevices.Contains($"{santroller.Manufacturer}_{santroller.Product}"))
            .ToPropertyEx(this, x => x.Configurable);
    }
    [ObservableAsProperty] public bool Configurable { get; }
    public BrandedMainWindowViewModel BrandedMain { get; }

    public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

    public IScreen HostScreen { get; }

    public string WelcomeText => string.Format(Resources.WelcomeText, BrandedMain.Config.ToolName);
    public string EraseText => string.Format(Resources.EraseText, BrandedMain.Config.ToolName);
}