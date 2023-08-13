using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.Views;
using ReactiveUI;

namespace SantrollerConfiguratorBuilder.NetCore.Views;

public partial class BuilderConfigView : ConfigView
{
    public BuilderConfigView()
    {
        InitializeComponent();
    }
}