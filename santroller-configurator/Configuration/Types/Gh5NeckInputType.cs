using System;

namespace GuitarConfigurator.NetCore.Configuration.Types;

[Flags]
public enum BarButton 
{
    Green = 1,
    Red = 2,
    Yellow = 4,
    Blue = 8,
    Orange = 16
}

public enum Gh5NeckInputType
{
     Green,
     Red,
     Yellow,
     Blue,
     Orange,
     TapGreen,
     TapRed,
     TapYellow,
     TapBlue,
     TapOrange,
     TapAll,
     TapBar
}