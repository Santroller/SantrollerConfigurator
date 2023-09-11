using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Styling;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Views;

public partial class BindAllWindow : ReactiveWindow<BindAllWindowViewModel>
{
    public BindAllWindow()
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

    public Output Output => ViewModel!.Output;
    public ConfigViewModel Model => ViewModel!.Model;
}