using System.Collections;
using System.IO;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoInclude(100, typeof(SerializedKeyboardButton))]
[ProtoInclude(101, typeof(SerializedMouseAxis))]
[ProtoInclude(102, typeof(SerializedMouseButton))]
[ProtoInclude(103, typeof(SerializedControllerAxis))]
[ProtoInclude(104, typeof(SerializedControllerButton))]
[ProtoInclude(105, typeof(SerializedDjCombinedOutput))]
[ProtoInclude(106, typeof(SerializedGh5CombinedOutput))]
[ProtoInclude(107, typeof(SerializedGhwtCombinedOutput))]
[ProtoInclude(108, typeof(SerializedPs2CombinedOutput))]
[ProtoInclude(109, typeof(SerializedWiiCombinedOutput))]
[ProtoInclude(110, typeof(SerializedDrumAxis))]
[ProtoInclude(111, typeof(SerializedPs3Axis))]
[ProtoInclude(112, typeof(SerializedDjButton))]
[ProtoInclude(113, typeof(SerializedGuitarAxis))]
[ProtoInclude(114, typeof(SerializedDjAxis))]
[ProtoInclude(115, typeof(SerializedGuitarButton))]
[ProtoInclude(117, typeof(SerializedLed))]
[ProtoInclude(118, typeof(SerializedEmulationMode))]
[ProtoInclude(119, typeof(SerializedJoystickToDpad))]
[ProtoInclude(120, typeof(SerializedCombinedUsbHostOutput))]
[ProtoInclude(121, typeof(SerializedCombinedBluetoothOutput))]
[ProtoInclude(122, typeof(SerializedCombinedUsbHostOutput))]
[ProtoInclude(123, typeof(SerializedReset))]
[ProtoInclude(124, typeof(SerializedCloneCombinedOutput))]
[ProtoInclude(125, typeof(SerializedStartSelectHome))]
[ProtoInclude(126, typeof(SerializedPianoKey))]
[ProtoInclude(127, typeof(SerializedCombinedMidiOutput))]
[ProtoInclude(128, typeof(SerializedPianoKeyButton))]
[ProtoInclude(129, typeof(SerializedProGuitarCombinedOutput))]
[ProtoInclude(130, typeof(SerializedWakeup360))]
[ProtoInclude(131, typeof(SerializedCrkdCombinedOutput))]
[ProtoContract]
public abstract class SerializedOutput
{
    public abstract Output Generate(ConfigViewModel model);
    protected static byte[] GetBytes(BitArray bits)
    {
        var ret = new byte[(bits.Length - 1) / 8 + 1];
        bits.CopyTo(ret, 0);
        return ret;
    }
}