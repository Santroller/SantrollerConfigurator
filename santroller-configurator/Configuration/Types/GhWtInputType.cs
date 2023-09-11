using System;

namespace GuitarConfigurator.NetCore.Configuration.Types;

[Flags]
public enum GhWtInputType
{
     TapGreen,
     TapRed,
     TapYellow,
     TapBlue,
     TapOrange,
     TapAll,
     TapBar
}