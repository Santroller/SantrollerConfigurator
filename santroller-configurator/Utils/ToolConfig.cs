using System;
using System.Collections.Generic;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Utils;

[ProtoContract]
public class ToolConfig
{
    [ProtoMember(1)] public LegendType LegendType { get; set; } = LegendType.Xbox;
    [ProtoMember(2)] public List<Tuple<string, SerializedConfiguration>> Presets { get; set; } = [];
}