using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reactive;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.ViewModels;

public class RaiseIssueWindowViewModel : ReactiveObject
{
    private readonly ConfigViewModel _model;
    public Interaction<Unit, Unit> CloseWindowInteraction = new();

    public RaiseIssueWindowViewModel((string _platformIOText, ConfigViewModel) text)
    {
        Text = text._platformIOText;
        _model = text.Item2;
        RaiseIssueCommand = ReactiveCommand.CreateFromTask(RaiseIssueAsync);
        CloseWindowCommand = ReactiveCommand.CreateFromObservable(() => CloseWindowInteraction.Handle(new Unit()));
        var os = Environment.OSVersion;
        IncludedInfo = $"""
                        Santroller Version: {GitVersionInformation.SemVer}
                        OS Version: {os.Version}
                        OS Platform: {os.Platform}
                        OS Service Pack: {os.ServicePack}
                        OS VersionString: {os.VersionString}

                        Device Type: {_model.DeviceControllerType}
                        Led Type: {_model.LedType}

                        Microcontroller Type: {_model.Microcontroller.Board.Name}
                        Microcontroller Frequency: {_model.Microcontroller.Board.CpuFreq / 1000}mhz
                        """;
    }

    public string Text { get; }
    public string IncludedInfo { get; }
    public ICommand RaiseIssueCommand { get; }
    public ICommand CloseWindowCommand { get; }

    private async Task RaiseIssueAsync()
    {
        var os = Environment.OSVersion;
        var body = $"""
                    Santroller Version: {GitVersionInformation.SemVer}
                    OS Version: {os.Version}
                    OS Platform: {os.Platform}
                    OS Service Pack: {os.ServicePack}
                    OS VersionString: {os.VersionString}

                    Device Type: {_model.DeviceControllerType}
                    Led Type: {_model.LedType}

                    Microcontroller Type: {_model.Microcontroller.Board.Name}
                    Microcontroller Frequency: {_model.Microcontroller.Board.CpuFreq / 1000}mhz
                    """;
        var title = "Error building";
        body = HttpUtility.UrlEncode(body);
        title = HttpUtility.UrlEncode(title);
        var url =
            $"https://github.com/Santroller/Santroller/issues/new?title={title}&body={body}";
        Process.Start(new ProcessStartInfo {FileName = url, UseShellExecute = true});
    }
}