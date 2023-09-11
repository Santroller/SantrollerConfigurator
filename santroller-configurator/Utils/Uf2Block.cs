using System.Runtime.InteropServices;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Utils;

[ProtoContract]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record Uf2Block
{
    [ProtoMember(1)] public uint magicStart0 = 0;
    [ProtoMember(2)] public uint magicStart1 = 0;
    [ProtoMember(3)] public uint flags = 0;
    [ProtoMember(4)] public uint targetAddr = 0;
    [ProtoMember(5)] public uint payloadSize = 0;
    [ProtoMember(6)] public uint blockNo = 0;
    [ProtoMember(7)] public uint numBlocks = 0;
    [ProtoMember(8)] public uint familyId = 0; 

    [ProtoMember(9)] [MarshalAs(UnmanagedType.ByValArray, SizeConst = 476)]
    public byte[] data = null!;

    [ProtoMember(10)] public uint magicEnd = 0;

    public Uf2Block()
    {
    }

    public Uf2Block(Uf2Block last)
    {
        magicStart0 = last.magicStart0;
        magicStart1 = last.magicStart1;
        flags = last.flags;
        targetAddr = last.targetAddr;
        payloadSize = last.payloadSize;
        blockNo = last.blockNo;
        numBlocks = last.numBlocks;
        familyId = last.familyId;
        magicEnd = last.magicEnd;
        data = new byte[476];
    }
}