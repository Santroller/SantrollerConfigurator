using System.Reactive;
using System.Windows.Input;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.ViewModels;

public enum ResetType
{
    Clear,
    Defaults,
    Cancel
}
public class ResetWindowViewModel : ReactiveObject
{
    public readonly Interaction<Unit, Unit> CloseWindowInteraction = new();

    public string AccentedTextColor { get; }
    public ResetWindowViewModel(string accentedTextColor)
    {
        AccentedTextColor = accentedTextColor;
        ClearCommand = ReactiveCommand.CreateFromObservable(() =>
        {
            Response = ResetType.Clear;
            return CloseWindowInteraction.Handle(new Unit());
        });
        DefaultCommand = ReactiveCommand.CreateFromObservable(() =>
        {
            Response = ResetType.Defaults;
            return CloseWindowInteraction.Handle(new Unit());
        });
        CancelCommand = ReactiveCommand.CreateFromObservable(() =>
        {
            Response = ResetType.Cancel;
            return CloseWindowInteraction.Handle(new Unit());
        });
    }

    public ICommand ClearCommand { get; }
    public ICommand DefaultCommand { get; }
    public ICommand CancelCommand { get; }
    public ResetType Response { get; set; }
}