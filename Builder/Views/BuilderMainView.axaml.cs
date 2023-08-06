using System;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using SantrollerConfiguratorBuilder.NetCore.ViewModels;

namespace SantrollerConfiguratorBuilder.NetCore.Views;

public partial class BuilderMainView : ReactiveUserControl<BuilderMainViewModel>
{
    public BuilderMainView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}