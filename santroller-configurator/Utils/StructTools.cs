using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

namespace GuitarConfigurator.NetCore;

public static class StructTools
{
    /// <summary>
    ///     converts byte[] to struct
    /// </summary>
    public static T RawDeserialize<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        T>(ReadOnlySpan<byte> rawData, int position)
    {
        var rawsize = Marshal.SizeOf<T>();
        if (rawsize > rawData.Length - position)
            throw new ArgumentException("Not enough data to fill struct. Array length from position: " +
                                        (rawData.Length - position) + ", Struct length: " + rawsize);
        var buffer = Marshal.AllocHGlobal(rawsize);
        Marshal.Copy(rawData.ToArray(), position, buffer, rawsize);
        var retobj = (T) Marshal.PtrToStructure<T>(buffer)!;
        Marshal.FreeHGlobal(buffer);
        return retobj;
    }
    
    public static void RawSerialise<T>(T data, Stream dest) where T : notnull {
        var size = Marshal.SizeOf(data);
        var arr = new byte[size];

        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            dest.Write(arr);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }



    public static string RawDeserializeStr(byte[] buffer)
    {
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStringAnsi(handle.AddrOfPinnedObject())!;
        }
        finally
        {
            handle.Free();
        }
    }
}