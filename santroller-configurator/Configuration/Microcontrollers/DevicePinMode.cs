namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public enum DevicePinMode
{
    PullUp = 0,
    Floating = 1,
    PullDown = 2,
    BusKeep = 3,
    Analog = 4,
    Output = 5,
    Skip = 6
}