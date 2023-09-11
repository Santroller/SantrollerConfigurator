using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Styling;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Views;

public partial class ResetWindow : ReactiveWindow<ResetWindowViewModel>
{
    public ResetWindow()
    {
        this.WhenActivated(disposables =>
        {
            disposables(ViewModel!.CloseWindowInteraction.RegisterHandler(context =>
            {
                context.SetOutput(context.Input);
                Close();
            }));
        });
        RequestedThemeVariant = ThemeVariant.Dark;
        InitializeComponent();
    }
}