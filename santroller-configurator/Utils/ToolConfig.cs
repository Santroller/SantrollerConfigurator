using System.Text.Json.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;

namespace GuitarConfigurator.NetCore.Utils;

public class ToolConfig
{
    [JsonPropertyName("viewType")] public LegendType LegendType { get; set; } = LegendType.Xbox;
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ToolConfig))]
internal partial class SourceGenerationContext2 : JsonSerializerContext
{
}