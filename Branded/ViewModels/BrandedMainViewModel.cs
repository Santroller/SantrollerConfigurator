using System;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace SantrollerConfiguratorBranded.NetCore.ViewModels;

public class BrandedMainViewModel : MainViewModel
{
    public BrandedMainViewModel(BrandedMainWindowViewModel screen): base(screen)
    {
    }
}