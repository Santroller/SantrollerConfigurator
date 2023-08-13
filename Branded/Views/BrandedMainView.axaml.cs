using System;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using SantrollerConfiguratorBranded.NetCore.ViewModels;

namespace SantrollerConfiguratorBranded.NetCore.Views;

public partial class BrandedMainView : ReactiveUserControl<BrandedMainViewModel>
{
    public BrandedMainView()
    {
        this.WhenActivated(disposables => { });
        InitializeComponent();
    }
}