using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Views;

public partial class SidebarView : ReactiveUserControl<ConfigViewModel>
{
    public SidebarView()
    {
        this.WhenActivated(disposables => { });
        InitializeComponent();
    }
    public ConfigViewModel Model => ViewModel!;
}