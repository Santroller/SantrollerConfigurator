using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
using GuitarConfigurator.NetCore.Configuration.Serialization;

namespace GuitarConfigurator.NetCore;

public class ExCrypt
{
    public enum XeKey : ushort
    {
        XekeyManufacturingMode = 0x0,
        XekeyAlternateKeyVault = 0x1,
        XekeyRestrictedPrivilegesFlags = 0x2,
        XekeyReservedByte3 = 0x3,
        XekeyOddFeatures = 0x4,
        XekeyOddAuthtype = 0x5,
        XekeyRestrictedHvextLoader = 0x6,
        XekeyPolicyFlashSize = 0x7,
        XekeyPolicyBuiltinUsbmuSize = 0x8,
        XekeyReservedDword4 = 0x9,
        XekeyRestrictedPrivileges = 0xA,
        XekeyReservedQword2 = 0xB,
        XekeyReservedQword3 = 0xC,
        XekeyReservedQword4 = 0xD,
        XekeyReservedKey1 = 0xE,
        XekeyReservedKey2 = 0xF,
        XekeyReservedKey3 = 0x10,
        XekeyReservedKey4 = 0x11,
        XekeyReservedRandomKey1 = 0x12,
        XekeyReservedRandomKey2 = 0x13,
        XekeyConsoleSerialNumber = 0x14,
        XekeyMoboSerialNumber = 0x15,
        XekeyGameRegion = 0x16,
        XekeyConsoleObfuscationKey = 0x17,
        XekeyKeyObfuscationKey = 0x18,
        XekeyRoamableObfuscationKey = 0x19,
        XekeyDvdKey = 0x1A,
        XekeyPrimaryActivationKey = 0x1B,
        XekeySecondaryActivationKey = 0x1C,
        XekeyGlobalDevice2DesKey1 = 0x1D,
        XekeyGlobalDevice2DesKey2 = 0x1E,
        XekeyWirelessControllerMs2DesKey1 = 0x1F,
        XekeyWirelessControllerMs2DesKey2 = 0x20,
        XekeyWiredWebcamMs2DesKey1 = 0x21,
        XekeyWiredWebcamMs2DesKey2 = 0x22,
        XekeyWiredControllerMs2DesKey1 = 0x23,
        XekeyWiredControllerMs2DesKey2 = 0x24,
        XekeyMemoryUnitMs2DesKey1 = 0x25,
        XekeyMemoryUnitMs2DesKey2 = 0x26,
        XekeyOtherXsm3DeviceMs2DesKey1 = 0x27,
        XekeyOtherXsm3DeviceMs2DesKey2 = 0x28,
        XekeyWirelessController3P2DesKey1 = 0x29,
        XekeyWirelessController3P2DesKey2 = 0x2A,
        XekeyWiredWebcam3P2DesKey1 = 0x2B,
        XekeyWiredWebcam3P2DesKey2 = 0x2C,
        XekeyWiredController3P2DesKey1 = 0x2D,
        XekeyWiredController3P2DesKey2 = 0x2E,
        XekeyMemoryUnit3P2DesKey1 = 0x2F,
        XekeyMemoryUnit3P2DesKey2 = 0x30,
        XekeyOtherXsm3Device3P2DesKey1 = 0x31,
        XekeyOtherXsm3Device3P2DesKey2 = 0x32,
        XekeyConsolePrivateKey = 0x33,
        XekeyXeikaPrivateKey = 0x34,
        XekeyCardeaPrivateKey = 0x35,
        XekeyConsoleCertificate = 0x36,
        XekeyXeikaCertificate = 0x37,
        XekeyCardeaCertificate = 0x38,
        XekeyMaxKeyIndex = 0x39,

        XekeyConstantPirsKey = 0x39,
        XekeyConstantAltMasterKey = 0x3A,
        XekeyConstantAltLiveKey = 0x3B,
        XekeyConstantMasterKey = 0x3C,
        XekeyConstantLiveKey = 0x3D,
        XekeyConstantXb1GreenKey = 0x3E,
        XekeyConstantSataDiskSecurityKey = 0x3F,
        XekeyConstantDeviceRevocationKey = 0x40,
        XekeyConstantXmacsKey = 0x41,
        XekeyConstantRevocationListNonce = 0x42,
        XekeyConstantCrossPlatformSyslinkKey = 0x43,

        XekeySpecialKeyVaultSignature = 0x44,
        XekeySpecialSecromDigest = 0x45,
        XekeySpecialSecdata = 0x46,
        XekeySpecialDvdFirmwareKey = 0x47,
        XekeySpecialDebugUnlock = 0x48,
        XekeySpecialDebugUnlockState = 0x49,
        XekeyMaxConstantIndex = 0x4A,

        XekeyTitleKeysBase = 0xE0,
        XekeyTitleKeysLimit = 0xE8,
        XekeyTitleKeysReset = 0xF0,

        XekeySecuredDataBase = 0x1000,
        XekeySecuredDataLimit = 0x2000,
    };

    static Dictionary<XeKey, ushort[]> _kExKeyProperties = new()
    {
        {XeKey.XekeyManufacturingMode, [0x8, 0x1]},
        {XeKey.XekeyAlternateKeyVault, [0x9, 0x1]},
        {XeKey.XekeyRestrictedPrivilegesFlags, [0xA, 0x1]},
        {XeKey.XekeyReservedByte3, [0xB, 0x1]},
        {XeKey.XekeyOddFeatures, [0xC, 0x2]},
        {XeKey.XekeyOddAuthtype, [0xE, 0x2]},
        {XeKey.XekeyRestrictedHvextLoader, [0x10, 0x4]},
        {XeKey.XekeyPolicyFlashSize, [0x14, 0x4]},
        {XeKey.XekeyPolicyBuiltinUsbmuSize, [0x18, 0x4]},
        {XeKey.XekeyReservedDword4, [0x1C, 0x4]},
        {XeKey.XekeyRestrictedPrivileges, [0x20, 0x8]},
        {XeKey.XekeyReservedQword2, [0x28, 0x8]},
        {XeKey.XekeyReservedQword3, [0x30, 0x8]},
        {XeKey.XekeyReservedQword4, [0x38, 0x8]},
        {XeKey.XekeyReservedKey1, [0x40, 0x10]},
        {XeKey.XekeyReservedKey2, [0x50, 0x10]},
        {XeKey.XekeyReservedKey3, [0x60, 0x10]},
        {XeKey.XekeyReservedKey4, [0x70, 0x10]},
        {XeKey.XekeyReservedRandomKey1, [0x80, 0x10]},
        {XeKey.XekeyReservedRandomKey2, [0x90, 0x10]},
        {XeKey.XekeyConsoleSerialNumber, [0xA0, 0xC]},
        {XeKey.XekeyMoboSerialNumber, [0xAC, 0xC]},
        {XeKey.XekeyGameRegion, [0xB8, 0x2]},
        // 6 bytes padding
        {XeKey.XekeyConsoleObfuscationKey, [0xC0, 0x10]},
        {XeKey.XekeyKeyObfuscationKey, [0xD0, 0x10]},
        {XeKey.XekeyRoamableObfuscationKey, [0xE0, 0x10]},
        {XeKey.XekeyDvdKey, [0xF0, 0x10]},
        {XeKey.XekeyPrimaryActivationKey, [0x100, 0x18]},
        {XeKey.XekeySecondaryActivationKey, [0x118, 0x10]},
        {XeKey.XekeyGlobalDevice2DesKey1, [0x128, 0x10]},
        {XeKey.XekeyGlobalDevice2DesKey2, [0x138, 0x10]},
        {XeKey.XekeyWirelessControllerMs2DesKey1, [0x148, 0x10]},
        {XeKey.XekeyWirelessControllerMs2DesKey2, [0x158, 0x10]},
        {XeKey.XekeyWiredWebcamMs2DesKey1, [0x168, 0x10]},
        {XeKey.XekeyWiredWebcamMs2DesKey2, [0x178, 0x10]},
        {XeKey.XekeyWiredControllerMs2DesKey1, [0x188, 0x10]},
        {XeKey.XekeyWiredControllerMs2DesKey2, [0x198, 0x10]},
        {XeKey.XekeyMemoryUnitMs2DesKey1, [0x1A8, 0x10]},
        {XeKey.XekeyMemoryUnitMs2DesKey2, [0x1B8, 0x10]},
        {XeKey.XekeyOtherXsm3DeviceMs2DesKey1, [0x1C8, 0x10]},
        {XeKey.XekeyOtherXsm3DeviceMs2DesKey2, [0x1D8, 0x10]},
        {XeKey.XekeyWirelessController3P2DesKey1, [0x1E8, 0x10]},
        {XeKey.XekeyWirelessController3P2DesKey2, [0x1F8, 0x10]},
        {XeKey.XekeyWiredWebcam3P2DesKey1, [0x208, 0x10]},
        {XeKey.XekeyWiredWebcam3P2DesKey2, [0x218, 0x10]},
        {XeKey.XekeyWiredController3P2DesKey1, [0x228, 0x10]},
        {XeKey.XekeyWiredController3P2DesKey2, [0x238, 0x10]},
        {XeKey.XekeyMemoryUnit3P2DesKey1, [0x248, 0x10]},
        {XeKey.XekeyMemoryUnit3P2DesKey2, [0x258, 0x10]},
        {XeKey.XekeyOtherXsm3Device3P2DesKey1, [0x268, 0x10]},
        {XeKey.XekeyOtherXsm3Device3P2DesKey2, [0x278, 0x10]},
        {XeKey.XekeyConsolePrivateKey, [0x288, 0x1D0]},
        {XeKey.XekeyXeikaPrivateKey, [0x458, 0x390]},
        {XeKey.XekeyCardeaPrivateKey, [0x7E8, 0x1D0]},
        {XeKey.XekeyConsoleCertificate, [0x9B8, 0x1A8]},
        {XeKey.XekeyXeikaCertificate, [0xB60, 0x1288]},
        {XeKey.XekeySpecialKeyVaultSignature, [0x1DF8, 0x100]},
        {XeKey.XekeyCardeaCertificate, [0x1EE8, 0x2108]},
    };

    public static byte[] ReadKey(ref byte[] kv, XeKey key)
    {
        var prop = _kExKeyProperties[key];
        var offset = prop[0];
        var size = prop[1];
        var ret = new byte[size];
        Array.Copy(kv, offset, ret, 0, size);
        return ret;
    }

    public static string ByteArrayToString(byte[] ba, int startindex = 0, int length = 0)
    {
        if (ba == null) return "";
        string hex = BitConverter.ToString(ba);
        if (startindex == 0 && length == 0) hex = BitConverter.ToString(ba);
        else if (length == 0 && startindex != 0) hex = BitConverter.ToString(ba, startindex);
        else hex = BitConverter.ToString(ba, startindex, length);
        return hex.Replace("-", "");
    }

    public static byte[] Returnportion(byte[] data, int offset, int count)
    {
        if (data == null) return null;
        if (count < 0) count = 0;
        byte[] templist = new byte[count];
        if (offset + count > data.Length)
        {
            count = data.Length - offset;
        }

        if (count <= data.Length && count >= 0)
        {
            Buffer.BlockCopy(data, offset, templist, 0x00, count);
        }

        return templist;
    }

    public static bool Allsame(byte[] s, byte n)
    {
        return s.All(x => x == n);
    }

    public static bool Hasecc(byte[] data)
    {
        int i = 0x200, counter = 0;
        while (i < data.Length && counter <= 0x100)
        {
            byte[] sparedata = new byte[0x40];

            switch (i % 800)
            {
                case 0:
                    Buffer.BlockCopy(data, i, sparedata, 0, 0x40);
                    i += 0x40;
                    if (sparedata[0] == 0xFF && sparedata[0x10] == 0xFF &&
                        sparedata[0x20] == 0xFF && sparedata[0x30] == 0xFF &&
                        !Allsame(sparedata, 0xFF) && sparedata[3] == 0x00 && sparedata[4] == 0x00)
                    {
                        return true;
                    }

                    break;
                default:
                    Buffer.BlockCopy(data, i, sparedata, 0, 0x10);
                    i += 0x10;
                    if ((sparedata[0] == 0xFF || sparedata[5] == 0xFF) && !Allsame(Returnportion(sparedata, 0xC, 0x4),
                                                                           0xFF)
                                                                       && !Allsame(Returnportion(sparedata, 0xC, 0x4),
                                                                           0x00) && sparedata[3] == 0x00 &&
                                                                       sparedata[4] == 0x00)
                    {
                        return true;
                    }

                    break;
            }

            i += 0x200;
            if (i % 4200 == 0) counter++;
        }

        return false;
    }

    public static bool hasecc_v2(ref byte[] data)
    {
        int blockOffsetB = Convert.ToInt32(ByteArrayToString(Returnportion(data, 0x8, 4)), 16);
        if (data.Length < blockOffsetB + 2) return Hasecc(data);
        if ((data[blockOffsetB] == 0x43 && data[blockOffsetB + 1] == 0x42) ||
            (data[blockOffsetB] == 0x53 && data[blockOffsetB + 1] == 0x42)) // Check for text 'CB' or 'SB'
        {
            int length = Convert.ToInt32(ByteArrayToString(Returnportion(data, blockOffsetB + 0xC, 4)), 16);
            if (data.Length < blockOffsetB + length || length < 0) return Hasecc(data);
            blockOffsetB += length;
            if (data[blockOffsetB] == 0x43 &&
                (data[blockOffsetB + 1] == 0x42 || data[blockOffsetB + 1] == 0x44))
                return false; // Retail: Cx
            if (data[blockOffsetB] == 0x53 && (data[blockOffsetB + 1] == 0x42 ||
                                               data[blockOffsetB + 1] == 0x43 ||
                                               data[blockOffsetB + 1] == 0x44))
                return false; // Dev: Sx
            return true;
        }

        return true;
    }
    public static byte[] HMAC_SHA1(byte[] key, byte[] message)
    {
        if (key.Length < 0x10) return null;
        byte[] k = new byte[0x40];
        byte[] opad = new byte[20 + 0x40];
        byte[] ipad = new byte[message.Length + 0x40];

        Array.Copy(key, k, 16);

        for (int i = 0; i < 64; i++)
        {
            opad[i] = (byte) (k[i] ^ 0x5C);
            ipad[i] = (byte) (k[i] ^ 0x36);
        }

        // Copy Buffer
        Array.Copy(message, 0, ipad, 0x40, message.Length);

        // Get First Hash
        var sha = SHA1.Create();
        byte[] hash1 = sha.ComputeHash(ipad);

        // Copy to OPad
        Array.Copy(hash1, 0, opad, 0x40, 20);

        return sha.ComputeHash(opad);
    }

    public static void RC4_v(ref Byte[] bytes, Byte[] key)
    {
        Byte[] s = new Byte[256];
        Byte[] k = new Byte[256];
        Byte temp;
        int i, j;

        for (i = 0; i < 256; i++)
        {
            s[i] = (Byte) i;
            k[i] = key[i % key.GetLength(0)];
        }

        j = 0;
        for (i = 0; i < 256; i++)
        {
            j = (j + s[i] + k[i]) % 256;
            temp = s[i];
            s[i] = s[j];
            s[j] = temp;
        }

        i = j = 0;
        for (int x = 0; x < bytes.GetLength(0); x++)
        {
            i = (i + 1) % 256;
            j = (j + s[i]) % 256;
            temp = s[i];
            s[i] = s[j];
            s[j] = temp;
            int t = (s[i] + s[j]) % 256;
            bytes[x] ^= s[t];
        }
    }

    public static byte[] Decryptkv(byte[] kv, byte[] key)
    {
        try
        {
            if (kv == null || key == null) return null;
            byte[] message = new byte[16];
            message = Returnportion(kv, 0, 0x10);
            byte[] rc4Key = HMAC_SHA1(key, message);
            if (rc4Key == null) return null;
            byte[] restofkv = Returnportion(kv, 0x10, kv.Length - 0x10);
            RC4_v(ref restofkv, Returnportion(rc4Key, 0, 0x10));
            byte[] finalimage = new byte[message.Length + restofkv.Length];
            for (int i = 0; i < message.Length + restofkv.Length; i++)
            {
                if (i < message.Length) finalimage[i] = message[i];
                else finalimage[i] = restofkv[i - message.Length];
            }

            return finalimage;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        return null;
    }
    
    public static byte[] StringToByteArray(String hex)
    {
        int numberChars = hex.Length;
        if (numberChars % 2 != 0)
        {
            hex = "0" + hex;
            numberChars++;
        }
        if (numberChars % 4 != 0)
        {
            hex = "00" + hex;
            numberChars += 2;
        }
        byte[] bytes = new byte[numberChars / 2];
        for (int i = 0; i < numberChars; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }
    public static void Unecc(ref byte[] data)
    {
        try
        {
            if (data[0x205] == 0xFF || data[0x415] == 0xFF || data[0x200] == 0xFF)
            {
                byte[] res = new byte[(data.Length / 0x210) * 0x200];
                for (int counter = 0; counter < res.Length; counter += 0x200)
                {
                    if (((counter / 0x200) * 0x210) + 0x200 <= data.Length) Buffer.BlockCopy(data, (counter / 0x200) * 0x210, res, counter, 0x200);
                }
                data = res;
            }
            else if (data[0x800] == 0xFF && data[0x810] == 0xFF && data[0x820] == 0xFF)
            {
                byte[] res = new byte[(data.Length / 0x840) * 0x800];
                for (int counter = 0; counter < res.Length; counter += 0x800)
                {
                    if (((counter / 0x800) * 0x840) + 0x800 <= data.Length) Buffer.BlockCopy(data, (counter / 0x800) * 0x840, res, counter, 0x800);
                }
                data = res;
            }
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }
    }
    
    public static byte[] ReadFully(Stream input)
    {
        using var ms = new MemoryStream();
        input.CopyTo(ms);
        return ms.ToArray();
    }
    
    public static SerializedConsoleKey LoadKeys(Stream nandPath, Stream cpukeyPath)
    {
        var image = ReadFully(nandPath);
        if (hasecc_v2(ref image)) Unecc(ref image);
        var kvEn = new byte[0x4000];
        Buffer.BlockCopy(image, 0x4000, kvEn, 0, 0x4000);
        var kv = Decryptkv(kvEn, StringToByteArray(new StreamReader(cpukeyPath).ReadToEnd()));
        Console.WriteLine(Convert.ToString(kv.Length, 16));
        if (kv.Length >= 0x4000)
        {
            // skip over digest
            Array.Copy(kv, 0x10, kv, 0, kv.Length - 0x10);
            Array.Resize(ref kv, kv.Length - 0x10);
        }

        var key1 = ReadKey(ref kv, XeKey.XekeyWiredControllerMs2DesKey1);
        var key2 = ReadKey(ref kv, XeKey.XekeyWiredControllerMs2DesKey2);
        var cert = ReadKey(ref kv, XeKey.XekeyConsoleCertificate);
        var id = new byte[5];
        var date = new byte[8];
        Array.Copy(cert, 2, id, 0, 5);
        Array.Copy(cert, 0x1c, date, 0, 8);
        return new SerializedConsoleKey(id, key1, key2, date);
    }
    
    public static void LoadKeys(string kvPath)
    {
        var kv = File.ReadAllBytes(kvPath);
        if (kv.Length >= 0x4000)
        {
            // skip over digest
            Array.Copy(kv, 0x10, kv, 0, kv.Length - 0x10);
            Array.Resize(ref kv, kv.Length - 0x10);
        }

        var key1 = ReadKey(ref kv, XeKey.XekeyWiredControllerMs2DesKey1);
        var key2 = ReadKey(ref kv, XeKey.XekeyWiredControllerMs2DesKey2);
        var cert = ReadKey(ref kv, XeKey.XekeyConsoleCertificate);
        var id = new byte[5];
        Array.Copy(cert, 2, id, 0, 5);
        Console.WriteLine(string.Join(",", key1.Select(s => "0x" + Convert.ToString(s, 16))));
        Console.WriteLine(string.Join(",", key2.Select(s => "0x" + Convert.ToString(s, 16))));
        Console.WriteLine(string.Join(",", id.Select(s => "0x" + Convert.ToString(s, 16))));
    }
}