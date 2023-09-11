using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Views;

public partial class RestoreView : ReactiveUserControl<RestoreViewModel>
{
    public RestoreView()
    {
        this.WhenActivated(disposables =>
            disposables(ViewModel!.RegisterConnections()));
        InitializeComponent();
    }
}