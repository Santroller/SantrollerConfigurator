namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum AccelInputType
{
    AccelX,
    AccelY,
    AccelZ,
    Adc0,
    Adc1,
    Adc2
}

public static class AccelInputTypeExtensions
{
    public static bool IsAdc(this AccelInputType type)
    {
        return type is AccelInputType.Adc0 or AccelInputType.Adc1 or AccelInputType.Adc2;
    }
}