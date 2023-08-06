using System.Linq;
using System.Reactive;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace SantrollerConfiguratorBranded.NetCore.ViewModels;

public partial class BrandedMainWindowViewModel : MainWindowViewModel
{
    public BrandedMainWindowViewModel()
    {
        GoBack = ReactiveCommand.CreateFromObservable(() =>
            Router.NavigateAndReset.Execute(Router.NavigationStack.First()));
        Router.Navigate.Execute(new BrandedMainViewModel(this));
    }

    public Interaction<(string yesText, string noText, string text), AreYouSureWindowViewModel>
        ShowYesNoDialog { get; } = new();

    // The command that navigates a user back.
    public ReactiveCommand<Unit, IRoutableViewModel> GoBack { get; }


    // The Router associated with this Screen.
    // Required by the IScreen interface.
    public RoutingState Router { get; } = new();
}