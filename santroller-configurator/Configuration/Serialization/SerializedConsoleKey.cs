using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerializedConsoleKey
{
    public SerializedConsoleKey()
    {
        ConsoleId = [];
        Key1 = [];
        Key2 = [];
    }
    public SerializedConsoleKey(byte[] consoleId, byte[] key1, byte[] key2, byte[] date)
    {
        ConsoleId = consoleId;
        Key1 = key1;
        Key2 = key2;
        Date = Encoding.UTF8.GetString(date);
    }

    private string GetConsoleId()
    {
        long counter = 0;
        for (var i = 0; i < 5; i++)
            counter = ConsoleId[i] + counter * 0x100;
        return $"{counter >> 4:D11}{counter & 0xF:X}";
    }

    public string Id => $"{GetConsoleId()} ({Date})";

    [ProtoMember(1)] public byte[] ConsoleId { get; }
    [ProtoMember(2)] public byte[] Key1 { get; }
    [ProtoMember(3)] public byte[] Key2 { get; }
    [ProtoMember(4)] public string Date { get; }
    public byte[] Combined => ConsoleId.Concat(Key1).Concat(Key2).ToArray();
    public string Format()
    {
        return $"{string.Join(",", ConsoleId)},{string.Join(",", Key1)},{string.Join(",", Key2)}";
    }
}