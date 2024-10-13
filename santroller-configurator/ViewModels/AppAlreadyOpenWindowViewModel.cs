using System;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.ViewModels;

public class AppAlreadyOpenWindowViewModel : ReactiveObject
{
    public void Close()
    {
        Environment.Exit(0);
    }
}