using System;
using System.Linq;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.Input;
using GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
#if Windows
using Microsoft.Win32;
#endif

namespace SantrollerConfiguratorBuilder.NetCore.ViewModels;

public partial class BuilderMainWindowViewModel : GuitarConfigurator.NetCore.ViewModels.MainWindowViewModel
{
    public BuilderConfig Config { get; }
    
    [Reactive]
    public BrandedConfigurationStore? SelectedTool { get; set; }
    [Reactive]
    public BrandedConfiguration? Selected { get; set; }

    public BuilderMainWindowViewModel()
    {
        Config = new BuilderConfig(this);
        if (Config.Configurations.Any())
        {
            SelectedTool = Config.Configurations.First();
        }

        Router.Navigate.Execute(new BuilderMainViewModel(this));
    }

    [RelayCommand]
    public void AddConfig()
    {
        var item = new BrandedConfigurationStore("Tool Name", "Provide Help");
        Config.Configurations.Add(item);
        SelectedTool = item;
    }

    [RelayCommand]
    public void RemoveConfig()
    {
        if (SelectedTool == null) return;
        Config.Configurations.Remove(SelectedTool);
        SelectedTool = Config.Configurations.Any() ? Config.Configurations.First() : null;
    }
    [RelayCommand]
    public void AddDevice()
    {
        if (SelectedTool == null) return;
        var item = new BrandedConfiguration("Vendor Name", "Product Name", this);
        SelectedTool.Configurations.Add(item);
        Selected = item;
    }

    [RelayCommand]
    public void RemoveDevice()
    {
        if (SelectedTool == null) return;
        if (Selected == null) return;
        SelectedTool.Configurations.Remove(Selected);
        Selected = SelectedTool.Configurations.Any() ? SelectedTool.Configurations.First() : null;
    }

    [RelayCommand]
    public void StartConfiguring()
    {
        Console.WriteLine("Configure!");
        if (SelectedTool == null || Selected == null) return;
        Router.Navigate.Execute(Selected.Model);
    }
}