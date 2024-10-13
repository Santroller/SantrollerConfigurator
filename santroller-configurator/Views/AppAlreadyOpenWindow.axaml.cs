using Avalonia.ReactiveUI;
using Avalonia.Styling;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Views;

public partial class AppAlreadyOpenWindow : ReactiveWindow<AppAlreadyOpenWindowViewModel>
{
    public AppAlreadyOpenWindow()
    {
        RequestedThemeVariant = ThemeVariant.Dark;
        InitializeComponent();
    }
}