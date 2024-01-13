using System.Reactive;
using System.Windows.Input;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.ViewModels;

public class InformationWindowViewModel : ReactiveObject
{
    public readonly Interaction<Unit, Unit> CloseWindowInteraction = new();
    public string AccentedTextColor { get; }

    public InformationWindowViewModel(string accentedTextColor, string text)
    {
        AccentedTextColor = accentedTextColor;
        Text = text;
        YesCommand = ReactiveCommand.CreateFromObservable(() => CloseWindowInteraction.Handle(new Unit()));
    }

    public ICommand YesCommand { get; }
    public bool Response { get; set; }
    public string Text { get; }
}