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

        Router.NavigateAndReset.Execute(new BuilderMainViewModel(this));
        this.WhenAnyValue(s => s.SelectedDevice).Subscribe(s =>
        {
            if (Selected != null && SelectedDevice != null)
            {
                Selected.Model.Device = SelectedDevice;
            }
        });
        this.WhenAnyValue(s => s.SelectedTool).Subscribe(s =>
        {
            if (SelectedTool != null && SelectedTool.Configurations.Any())
            {
                Selected = SelectedTool.Configurations.First();
            }
        });
    }

    [RelayCommand]
    public void AddConfig()
    {
        var item = new BrandedConfigurationStore("Tool Name", "Get support by visiting example.com");
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
        if (SelectedTool == null || Selected == null) return;
        Router.Navigate.Execute(Selected.Model);
    }
    
    [RelayCommand]
    public void Save()
    {
        Config.Save();
    }
    [RelayCommand]
    public void Package()
    {
        
    }
}