using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using SantrollerConfiguratorBuilder.NetCore.ViewModels;

namespace SantrollerConfiguratorBuilder.NetCore.Views;

public partial class BuilderMainWindow : ReactiveWindow<BuilderMainWindowViewModel>
{
    public BuilderMainWindow()
    {
        Console.WriteLine("Main Window");
        this.WhenActivated(disposables =>
        {
            ViewModel!.Begin();
        });
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDevTools();
#endif
    }

}