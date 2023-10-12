using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.Utils;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;

public class BrandedConfiguration : ReactiveObject
{
    private const uint BlobOffset = 0x100A2000;
    private const uint ConfigOffset = 0x100A3000;
    [Reactive] public string VendorName { get; set; }
    [Reactive] public string ProductName { get; set; }
    public Uf2Block[] Uf2 { get; private set; }
    public ConfigViewModel Model { get; }

    public BrandedConfiguration(SerialisedBrandedConfiguration configuration, bool branded, MainWindowViewModel screen)
    {
        Model = new ConfigViewModel(screen, new EmptyDevice(), branded, !branded);
        configuration.Configuration.LoadConfiguration(Model);
        VendorName = configuration.VendorName;
        ProductName = configuration.ProductName;
        Uf2 = configuration.Uf2;
    }

    public BrandedConfiguration(string vendorName, string productName, MainWindowViewModel screen)
    {
        Model = new ConfigViewModel(screen, new EmptyDevice(), false, true);
        Model.SetDefaults();
        VendorName = vendorName;
        ProductName = productName;
        Uf2 = Array.Empty<Uf2Block>();
    }

    public string ExtraConfig()
    {
        return $"""
                #define DEVICE_VENDOR {string.Join(", ", VendorName.Select(s => $"'{s}'"))}
                #define DEVICE_PRODUCT {string.Join(", ", ProductName.Select(s => $"'{s}'"))}


                """;
    }

    public async Task BuildUf2(string outputFile)
    {
        var blocks = new List<Uf2Block>(Uf2);
        await using var stream = new FileStream(outputFile,  FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
        var block = new Uf2Block(Uf2.Last());
        // UF2s on the pico need to be continuous, so we have to pad between the defined sections
        for (var i = block.targetAddr + block.payloadSize; i < BlobOffset; i += block.payloadSize)
        {
            block = new Uf2Block(block)
            {
                targetAddr = i
            };
            block.blockNo++;
            blocks.Add(block);
        }

        await using (var streamBlob = new MemoryStream())
        {
            Model.Generate(streamBlob);
            streamBlob.Flush();
            streamBlob.Seek(0, SeekOrigin.Begin);
            block = new Uf2Block(block)
            {
                targetAddr = BlobOffset
            };
            while (true)
            {
                if (await streamBlob.ReadAsync(block.data.AsMemory(0, (int) block.payloadSize)) == 0)
                {
                    break;
                }
                block.blockNo++;
                blocks.Add(block);
                block = new Uf2Block(block);
                block.targetAddr += block.payloadSize;
            }
        }
        // UF2s on the pico need to be continuous, so we have to pad between the defined sections
        for (var i = block.targetAddr; i < ConfigOffset; i += block.payloadSize)
        {
            block = new Uf2Block(block)
            {
                targetAddr = i
            };
            block.blockNo++;
            blocks.Add(block);
        }

        await using (var outputStream = new MemoryStream())
        {
            await using (var compressStream = new BrotliStream(outputStream, CompressionLevel.SmallestSize))
            {
                Serializer.Serialize(compressStream, new SerializedConfiguration(Model));
                compressStream.Flush();
                outputStream.Seek(0, SeekOrigin.Begin);

                block = new Uf2Block(block)
                {
                    targetAddr = ConfigOffset
                };
                while (true)
                {
                    block.blockNo++;
                    if (await outputStream.ReadAsync(block.data.AsMemory(0, (int) block.payloadSize)) == 0)
                    {
                        break;
                    }

                    blocks.Add(block);
                    block = new Uf2Block(block);
                    block.targetAddr += block.payloadSize;
                }
            }
        }

        foreach (var uf2Block in blocks)
        {
            uf2Block.numBlocks = (uint) blocks.Count;
            await StructTools.RawSerialiseAsync(uf2Block, stream);
        }
    }

    public void LoadUf2()
    {
        var env = "pico";
        if (Model.IsBluetooth)
        {
            env = "picow";
        }

        var uf2File = Path.Combine(AssetUtils.GetAppDataFolder(), "Santroller", ".pio", "build", env,
            "firmware.uf2");
        var bytes = File.ReadAllBytes(uf2File);
        var blocks = new List<Uf2Block>();
        for (var i = 0; i < bytes.Length; i += 512)
        {
            blocks.Add(StructTools.RawDeserialize<Uf2Block>(bytes, i));
        }

        Uf2 = blocks.ToArray();
    }
}