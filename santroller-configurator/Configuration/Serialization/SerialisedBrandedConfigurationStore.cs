using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerialisedBrandedConfigurationStore
{
    public SerialisedBrandedConfigurationStore()
    {
        
    }
    public SerialisedBrandedConfigurationStore(BrandedConfigurationStore store)
    {
        ToolName = store.ToolName;
        WarningColor = store.WarningColor;
        PrimaryColor = store.PrimaryColor;
        ErrorColor = store.ErrorColor;
        Configurations.AddRange(store.Configurations.Select(s => new SerialisedBrandedConfiguration(s)).ToList());
        var stream = new MemoryStream();
        store.Logo.Save(stream);
        Logo = stream.GetBuffer();
        stream = new MemoryStream();
        store.Icon.Save(stream);
        Icon = stream.GetBuffer();
    }

    [ProtoMember(1)] public string ToolName { get; set; } = null!;
    [ProtoMember(4)] public List<SerialisedBrandedConfiguration> Configurations { get; set; } = new();
    
    [ProtoMember(5)] public Color WarningColor { get; set; }
    
    [ProtoMember(6)] public Color PrimaryColor { get; set; }
    
    [ProtoMember(7)] public Color ErrorColor { get; set; }
    [ProtoMember(8)] public byte[] Logo { get; set; } = Array.Empty<byte>();
    [ProtoMember(9)] public byte[] Icon { get; set; } = Array.Empty<byte>();
}