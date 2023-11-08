using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SantrollerConfiguratorBuilder.NetCore.ViewModels;

public partial class BuilderAuthViewModel : ReactiveObject, IRoutableViewModel
{
    [Reactive] public bool LoggedIn { get; set; }
    [Reactive] public bool Authenticating { get; set; }
    [Reactive] public bool InsufficientAccess { get; set; }
    [Reactive] public string ErrorMessage { get; set; } = "";

    public BuilderAuthViewModel(BuilderMainWindowViewModel screen)
    {
        BuilderMain = screen;
        HostScreen = screen;
    }

    [RelayCommand]
    public async Task Setup()
    {
        InsufficientAccess = false;
        Authenticating = true;
        ErrorMessage = "";
        try
        {
            if (await GithubAuthenticator.RefreshToken())
            {
                LoggedIn = await GithubAuthenticator.CheckAccess();
                InsufficientAccess = !LoggedIn;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = string.Format(Resources.GitHubAuthError, ex.Message);
        }

        Authenticating = false;
    }

    [RelayCommand]
    public async Task LogIn()
    {
        InsufficientAccess = false;
        Authenticating = true;
        ErrorMessage = "";

        try
        {
            await GithubAuthenticator.SignIn();
            LoggedIn = await GithubAuthenticator.CheckAccess();
        }
        catch (Exception ex)
        {
            ErrorMessage = string.Format(Resources.GitHubAuthError, ex.Message);
        }

        InsufficientAccess = !LoggedIn;
        Authenticating = false;
    }

    [RelayCommand]
    public void LogOut()
    {
        GithubAuthenticator.SignOut();
        LoggedIn = false;
    }

    [RelayCommand]
    public void Continue()
    {
        BuilderMain.Router.NavigateAndReset.Execute(new BuilderMainViewModel(BuilderMain));
    }

    public BuilderMainWindowViewModel BuilderMain { get; }

    public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

    public IScreen HostScreen { get; }
}