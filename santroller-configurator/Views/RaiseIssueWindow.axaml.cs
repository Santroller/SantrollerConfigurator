using System.Reactive;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Styling;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Views;

public partial class RaiseIssueWindow : ReactiveWindow<RaiseIssueWindowViewModel>
{
    public RaiseIssueWindow()
    {
        this.WhenActivated(disposables =>
        {
            disposables(ViewModel!.CloseWindowInteraction.RegisterHandler(context =>
            {
                context.SetOutput(new Unit());
                Close();
            }));
        });

        RequestedThemeVariant = ThemeVariant.Dark;
        InitializeComponent();
    }
}