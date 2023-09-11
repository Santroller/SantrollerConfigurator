using System;

namespace GuitarConfigurator.NetCore.Configuration.Exceptions;

public class IncompleteConfigurationException : Exception
{
    public IncompleteConfigurationException(string message) : base(message)
    {
    }
}