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
        Configurations.AddRange(store.Configurations.Select(s => new SerialisedBrandedConfigurationSection(s)).ToList()); 
        
        var stream = new MemoryStream();
        store.Logo.Save(stream);
        Logo = stream.ToArray();
        stream = new MemoryStream();
        store.Icon.Save(stream);
        Icon = stream.ToArray();
    }

    [ProtoMember(1)] public string ToolName { get; set; } = null!;
    
    [ProtoMember(5)] public Color WarningColor { get; set; }
    
    [ProtoMember(6)] public Color PrimaryColor { get; set; }
    
    [ProtoMember(7)] public Color ErrorColor { get; set; }
    [ProtoMember(8)] public byte[] Logo { get; set; } = Array.Empty<byte>();
    [ProtoMember(9)] public byte[] Icon { get; set; } = Array.Empty<byte>();
    [ProtoMember(4)] public List<SerialisedBrandedConfiguration> OldConfigurations { get; set; } = new();
    [ProtoMember(10)] public List<SerialisedBrandedConfigurationSection> Configurations { get; set; } = new();
}