namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum MultiplexerType
{
    EightChannel,
    SixteenChannel,
    EightChannelSlow,
    SixteenChannelSlow,
}

public static class MultiplexerTypeExtensions
{
    public static bool IsEightChannel(this MultiplexerType type)
    {
        return type is MultiplexerType.EightChannel or MultiplexerType.EightChannelSlow;
    }
    
    public static bool IsSixteenChannel(this MultiplexerType type)
    {
        return type is MultiplexerType.SixteenChannel or MultiplexerType.SixteenChannelSlow;
    }
}