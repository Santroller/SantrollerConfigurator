using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace GuitarConfigurator.NetCore;

public class OutputTemplateSelector : IDataTemplate
{
    public bool SupportsRecycling => false;

    [Content] public List<IDataTemplate> Templates { get; } = new();

    public Control Build(object? data)
    {
        return Templates.First(t => t.Match(data)).Build(data)!;
    }

    public bool Match(object? data)
    {
        return Templates.Any(t => t.Match(data));
    }
}