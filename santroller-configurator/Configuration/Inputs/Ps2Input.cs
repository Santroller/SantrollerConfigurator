using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DynamicData.Kernel;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class Ps2Input : SpiInput
{
    public static readonly string Ps2SpiType = "ps2";
    public static readonly uint Ps2SpiFreq = 500000;
    public static readonly bool Ps2SpiCpol = true;
    public static readonly bool Ps2SpiCpha = true;
    public static readonly bool Ps2SpiMsbFirst = false;
    public static readonly string Ps2AckType = "ps2_ack";
    public static readonly string Ps2AttType = "ps2_att";

    public static readonly List<Ps2InputType> Dualshock2Order = new()
    {
        Ps2InputType.RightStickX,
        Ps2InputType.RightStickY,
        Ps2InputType.LeftStickX,
        Ps2InputType.LeftStickY,
        Ps2InputType.Dualshock2RightButton,
        Ps2InputType.Dualshock2LeftButton,
        Ps2InputType.Dualshock2UpButton,
        Ps2InputType.Dualshock2DownButton,
        Ps2InputType.Dualshock2Triangle,
        Ps2InputType.Dualshock2Circle,
        Ps2InputType.Dualshock2Cross,
        Ps2InputType.Dualshock2Square,
        Ps2InputType.Dualshock2L1,
        Ps2InputType.Dualshock2R1,
        Ps2InputType.Dualshock2L2,
        Ps2InputType.Dualshock2R2
    };

    public static readonly List<Ps2InputType> GuitarButtons = new()
    {
        Ps2InputType.GuitarGreen,
        Ps2InputType.GuitarRed,
        Ps2InputType.GuitarYellow,
        Ps2InputType.GuitarBlue,
        Ps2InputType.GuitarOrange,
        Ps2InputType.GuitarSelect,
        Ps2InputType.GuitarStart,
        Ps2InputType.GuitarTilt,
        Ps2InputType.GuitarStrumUp,
        Ps2InputType.GuitarStrumDown,
        Ps2InputType.DpadUp,
        Ps2InputType.DpadRight,
        Ps2InputType.DpadDown,
        Ps2InputType.DpadLeft
    };

    public static readonly List<Ps2InputType> DigitalButtons = new()
    {
        Ps2InputType.L3,
        Ps2InputType.R3,
        Ps2InputType.Start,
        Ps2InputType.Select,
        Ps2InputType.DpadUp,
        Ps2InputType.DpadRight,
        Ps2InputType.DpadDown,
        Ps2InputType.DpadLeft,
        Ps2InputType.L2,
        Ps2InputType.R2,
        Ps2InputType.L1,
        Ps2InputType.R1,
        Ps2InputType.Triangle,
        Ps2InputType.Circle,
        Ps2InputType.Cross,
        Ps2InputType.Square
    };

    private static readonly List<Ps2InputType> IntInputs = new()
    {
        Ps2InputType.LeftStickX,
        Ps2InputType.LeftStickY,
        Ps2InputType.MouseX,
        Ps2InputType.MouseY,
        Ps2InputType.RightStickX,
        Ps2InputType.RightStickY,
        Ps2InputType.NegConTwist
    };
    
    // Guitar dpad is special because why wouldn't it be
    private static readonly Dictionary<Ps2InputType, string> MappingsDpadGuitar = new()
    {

        {Ps2InputType.DpadUp, $"(ps2Data[5] < {255 / 4})"},
        {Ps2InputType.DpadLeft, $"(ps2Data[5] < {255 / 4 * 2} && ps2Data[5] > {255 / 4})"},
        {Ps2InputType.DpadDown, $"(ps2Data[5] < {255 / 4 * 3} && ps2Data[5] > {255 / 4 * 2})"},
        {Ps2InputType.DpadRight, $"(ps2Data[5] > {255 / 4 * 3} && ps2Data[5] < 250 && ps2Data[5] != 127)"},
    };

    private static readonly Dictionary<Ps2InputType, string> Mappings = new()
    {
        {Ps2InputType.LeftStickX, "(ps2Data[7] - 128) << 8"},
        {Ps2InputType.LeftStickY, "-(ps2Data[8] - 127) << 8"},
        {Ps2InputType.MouseLeft, "(~ps2Data[3]) & (1 << 3)"},
        {Ps2InputType.MouseRight, "(~ps2Data[3]) & (1 << 2)"},
        {Ps2InputType.MouseX, "(ps2Data[5] - 128) << 8"},
        {Ps2InputType.MouseY, "-(ps2Data[6] - 127) << 8"},
        {Ps2InputType.RightStickX, "(ps2Data[5] - 128) << 8"},
        {Ps2InputType.RightStickY, "-(ps2Data[6] - 127) << 8"},
        {Ps2InputType.NegConTwist, "(ps2Data[5] - 128) << 8"},
        {Ps2InputType.NegConI, "ps2Data[6]"},
        {Ps2InputType.NegConIi, "ps2Data[7]"},
        {Ps2InputType.NegConL, "ps2Data[8]"},
        {Ps2InputType.NegConR, "(~ps2Data[4]) & (1 << 3)"},
        {Ps2InputType.NegConA, "(~ps2Data[4]) & (1 << 5)"},
        {Ps2InputType.NegConB, "(~ps2Data[4]) & (1 << 4)"},
        {Ps2InputType.GunConHSync, "(ps2Data[6] << 8) | ps2Data[5]"},
        {Ps2InputType.GunConVSync, "(ps2Data[8] << 8) | ps2Data[7]"},
        {Ps2InputType.JogConWheel, "(ps2Data[6] << 8) | ps2Data[5]"},
        {Ps2InputType.GuitarTapBar, "lastTapPS2GH5"},
        {Ps2InputType.GuitarWhammy, "-(ps2Data[8] - 127) << 9"},
        {Ps2InputType.Dualshock2RightButton, "ps2Data[9]"},
        {Ps2InputType.Dualshock2LeftButton, "ps2Data[10]"},
        {Ps2InputType.Dualshock2UpButton, "ps2Data[11]"},
        {Ps2InputType.Dualshock2DownButton, "ps2Data[12]"},
        {Ps2InputType.Dualshock2Triangle, "ps2Data[13]"},
        {Ps2InputType.Dualshock2Circle, "ps2Data[14]"},
        {Ps2InputType.Dualshock2Cross, "ps2Data[15]"},
        {Ps2InputType.Dualshock2Square, "ps2Data[16]"},
        {Ps2InputType.Dualshock2L1, "ps2Data[17]"},
        {Ps2InputType.Dualshock2R1, "ps2Data[18]"},
        {Ps2InputType.Dualshock2L2, "ps2Data[19] << 8"},
        {Ps2InputType.Dualshock2R2, "ps2Data[20] << 8"},
        {Ps2InputType.GuitarGreen, "(~ps2Data[4]) & (1 << 1)"},
        {Ps2InputType.GuitarRed, "(~ps2Data[4]) & (1 << 5)"},
        {Ps2InputType.GuitarYellow, "(~ps2Data[4]) & (1 << 4)"},
        {Ps2InputType.GuitarBlue, "(~ps2Data[4]) & (1 << 6)"},
        {Ps2InputType.GuitarOrange, "(~ps2Data[4]) & (1 << 7)"},
        {Ps2InputType.GuitarTilt, "(~ps2Data[4]) & (1 << 0)"},
        {Ps2InputType.GuitarSelect, "(~ps2Data[3]) & (1 << 0)"},
        {Ps2InputType.GuitarStart, "(~ps2Data[3]) & (1 << 3)"},
        {Ps2InputType.NegConStart, "(~ps2Data[3]) & (1 << 3)"},
        {Ps2InputType.L3, "(~ps2Data[3]) & (1 << 1)"},
        {Ps2InputType.R3, "(~ps2Data[3]) & (1 << 2)"},
        {Ps2InputType.Start, "(~ps2Data[3]) & (1 << 3)"},
        {Ps2InputType.GuitarStrumUp, "(~ps2Data[3]) & (1 << 4)"},
        {Ps2InputType.GuitarStrumDown, "(~ps2Data[3]) & (1 << 6)"},
        {Ps2InputType.Select, "(~ps2Data[3]) & (1 << 0)"},
        {Ps2InputType.DpadUp, "(~ps2Data[3]) & (1 << 4)"},
        {Ps2InputType.DpadRight, "(~ps2Data[3]) & (1 << 5)"},
        {Ps2InputType.DpadDown, "(~ps2Data[3]) & (1 << 6)"},
        {Ps2InputType.DpadLeft, "(~ps2Data[3]) & (1 << 7)"},
        {Ps2InputType.L2, "(~ps2Data[4]) & (1 << 0)"},
        {Ps2InputType.R2, "(~ps2Data[4]) & (1 << 1)"},
        {Ps2InputType.L1, "(~ps2Data[4]) & (1 << 2)"},
        {Ps2InputType.R1, "(~ps2Data[4]) & (1 << 3)"},
        {Ps2InputType.Triangle, "(~ps2Data[4]) & (1 << 4)"},
        {Ps2InputType.Circle, "(~ps2Data[4]) & (1 << 5)"},
        {Ps2InputType.Cross, "(~ps2Data[4]) & (1 << 6)"},
        {Ps2InputType.Square, "(~ps2Data[4]) & (1 << 7)"},
        {Ps2InputType.GuitarTapGreen, GetMappingForTapBar(0x95, 0xB0)},
        {Ps2InputType.GuitarTapRed, GetMappingForTapBar(0xB0, 0xCD, 0xE6)},
        {Ps2InputType.GuitarTapYellow, GetMappingForTapBar(0xE6, 0x1A, 0x2F)},
        {Ps2InputType.GuitarTapBlue, GetMappingForTapBar(0x2F, 0x49, 0x66)},
        {Ps2InputType.GuitarTapOrange, GetMappingForTapBar(0x66, 0x7F)}
    };

    private static readonly Dictionary<Ps2InputType, Ps2ControllerType> AxisToType =
        new()
        {
            {Ps2InputType.GunConHSync, Ps2ControllerType.GunCon},
            {Ps2InputType.GunConVSync, Ps2ControllerType.GunCon},
            {Ps2InputType.MouseX, Ps2ControllerType.Mouse},
            {Ps2InputType.MouseY, Ps2ControllerType.Mouse},
            {Ps2InputType.NegConTwist, Ps2ControllerType.NegCon},
            {Ps2InputType.NegConI, Ps2ControllerType.NegCon},
            {Ps2InputType.NegConIi, Ps2ControllerType.NegCon},
            {Ps2InputType.NegConL, Ps2ControllerType.NegCon},
            {Ps2InputType.JogConWheel, Ps2ControllerType.JogCon},
            {Ps2InputType.GuitarWhammy, Ps2ControllerType.Guitar},
            {Ps2InputType.GuitarTapAll, Ps2ControllerType.Guitar},
            {Ps2InputType.GuitarTapBar, Ps2ControllerType.Guitar}
        };

    private static readonly IReadOnlyList<Ps2InputType> Dualshock = new[]
    {
        Ps2InputType.LeftStickX,
        Ps2InputType.LeftStickY,
        Ps2InputType.RightStickX,
        Ps2InputType.RightStickY
    };

    private static readonly Dictionary<Ps2ControllerType, string> CType = new()
    {
        {Ps2ControllerType.Digital, "PSX_DIGITAL"},
        {Ps2ControllerType.Dualshock, "PSX_DUALSHOCK_1_CONTROLLER"},
        {Ps2ControllerType.Dualshock2, "PSX_DUALSHOCK_2_CONTROLLER"},
        {Ps2ControllerType.FlightStick, "PSX_FLIGHTSTICK"},
        {Ps2ControllerType.NegCon, "PSX_NEGCON"},
        {Ps2ControllerType.JogCon, "PSX_JOGCON"},
        {Ps2ControllerType.GunCon, "PSX_GUNCON"},
        {Ps2ControllerType.Guitar, "PSX_GUITAR_HERO_CONTROLLER"},
        {Ps2ControllerType.Mouse, "PSX_MOUSE"}
    };

    private static readonly Dictionary<Ps2InputType, Ps2ControllerType> ControllerTypes = Enum.GetValues<Ps2InputType>()
        .ToDictionary(s => s,
            ps2InputType => Enum.GetValues<Ps2ControllerType>()
                .FirstOrDefault(ps2ControllerType => ps2InputType.ToString().StartsWith(ps2ControllerType.ToString()),
                    Ps2ControllerType.Digital));

    private readonly DirectPinConfig _ackConfig;
    private readonly DirectPinConfig _attConfig;

    public Ps2Input(Ps2InputType input, ConfigViewModel model, bool peripheral, int miso = -1,
        int mosi = -1,
        int sck = -1, int att = -1, int ack = -1, bool combined = false) : base(
        Ps2SpiType, peripheral,
        Ps2SpiFreq, Ps2SpiCpol,
        Ps2SpiCpha, Ps2SpiMsbFirst, miso: miso, mosi: mosi, sck: sck, model: model)
    {
        Combined = combined;
        BindableSpi = !Combined && !model.Branded && Model.Microcontroller.SpiAssignable;
        BindableAtt = !Combined && !model.Branded && Model.Microcontroller is not (Uno or Mega);
        Input = input;
        _ackConfig = Model.GetPinForType(Ps2AckType, peripheral, ack, DevicePinMode.Floating);
        _attConfig = Model.GetPinForType(Ps2AttType, peripheral, att, DevicePinMode.Floating);
        this.WhenAnyValue(x => x._attConfig.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Att)));
        this.WhenAnyValue(x => x._ackConfig.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Ack)));
        IsAnalog = Input <= Ps2InputType.Dualshock2R2;
        if (Model.Microcontroller is (Uno or Mega))
        {
            Att = 10;
        }
    }

    public int Ack
    {
        get => _ackConfig.Pin;
        set => _ackConfig.Pin = value;
    }

    public int Att
    {
        get => _attConfig.Pin;
        set => _attConfig.Pin = value;
    }

    public Ps2InputType Input { get; }

    public bool Combined { get; }
    public bool ShouldShowPins => !Combined && !Model.Branded;

    public bool BindableSpi { get; }
    public bool BindableAtt { get; }

    public override bool IsUint => !IntInputs.Contains(Input);

    public override InputType? InputType => Types.InputType.Ps2Input;
    public ReadOnlyObservableCollection<int> AvailablePins => Model.AvailablePinsDigital;
    public ReadOnlyObservableCollection<int> AvailablePinsInterrupt => Model.AvailablePinsInterrupt;

    public override IList<DevicePin> Pins => new List<DevicePin>
    {
        new(Att, DevicePinMode.Output),
        new(Ack, DevicePinMode.Floating)
    };

    public override IList<PinConfig> PinConfigs =>
        base.PinConfigs.Concat(new List<PinConfig> {_ackConfig, _attConfig}).ToList();

    public override string Title => EnumToStringConverter.Convert(Input);

    public override string Generate()
    {
        return Mappings[Input];
    }

    public override SerializedInput Serialise()
    {
        if (Combined)
            return new SerializedPs2InputCombined(Input, Peripheral);
        return new SerializedPs2Input(Peripheral, Miso, Mosi, Sck, Att, Ack, Input);
    }

    private static string GetMappingForTapBar(params int[] mappings)
    {
        return string.Join(" || ", mappings.Select(s2 => $"(lastTapPS2GH5 == {s2})"));
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Data,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> peripheralWtRaw,
        Dictionary<int, bool> digitalPeripheral, ReadOnlySpan<byte> cloneRaw, ReadOnlySpan<byte> adxlRaw,
        ReadOnlySpan<byte> mpr121Raw)
    {
        if (ps2ControllerType.IsEmpty || ps2Data.IsEmpty) return;
        var type = ps2ControllerType[0];
        if (!Enum.IsDefined(typeof(Ps2ControllerType), type)) return;
        var realType = (Ps2ControllerType) type;
        var mouse = realType == Ps2ControllerType.Mouse;
        var guitar = realType == Ps2ControllerType.Guitar;
        var basicAxis = realType is Ps2ControllerType.Dualshock or Ps2ControllerType.Dualshock2
            or Ps2ControllerType.FlightStick;
        var digital = realType is Ps2ControllerType.Dualshock or Ps2ControllerType.Dualshock2
            or Ps2ControllerType.FlightStick or Ps2ControllerType.Digital;
        var negcon = realType is Ps2ControllerType.NegCon;
        var jogcon = realType is Ps2ControllerType.JogCon;
        var guncon = realType is Ps2ControllerType.GunCon;
        var ds2 = realType is Ps2ControllerType.Dualshock2;
        BarButton lastTapPs2 = 0;
        switch (ps2Data[7])
        {
            case <= 0x2F:
                lastTapPs2 = BarButton.Green;
                break;
            case <= 0x3F:
                lastTapPs2 = BarButton.Green | BarButton.Red;
                break;
            case <= 0x5F:
                lastTapPs2 = BarButton.Red;
                break;
            case <= 0x6F:
                lastTapPs2 = BarButton.Red | BarButton.Yellow;
                break;
            case <= 0x8F:
                break;
            case <= 0x9F:
                lastTapPs2 = BarButton.Yellow;
                break;
            case <= 0xBF:
                lastTapPs2 = BarButton.Yellow | BarButton.Blue;
                break;
            case <= 0xCF:
                lastTapPs2 = BarButton.Blue;
                break;
            case <= 0xEF:
                lastTapPs2 = BarButton.Blue | BarButton.Orange;
                break;
            default:
                lastTapPs2 = BarButton.Orange;
                break;
        }
        RawValue = Input switch
        {
            Ps2InputType.Dualshock2RightButton when ds2 => ps2Data[9] << 8,
            Ps2InputType.Dualshock2LeftButton when ds2 => ps2Data[10] << 8,
            Ps2InputType.Dualshock2UpButton when ds2 => ps2Data[11] << 8,
            Ps2InputType.Dualshock2DownButton when ds2 => ps2Data[12] << 8,
            Ps2InputType.Dualshock2Triangle when ds2 => ps2Data[13] << 8,
            Ps2InputType.Dualshock2Circle when ds2 => ps2Data[14] << 8,
            Ps2InputType.Dualshock2Cross when ds2 => ps2Data[15] << 8,
            Ps2InputType.Dualshock2Square when ds2 => ps2Data[16] << 8,
            Ps2InputType.Dualshock2L1 when ds2 => ps2Data[17] << 8,
            Ps2InputType.Dualshock2R1 when ds2 => ps2Data[18] << 8,
            Ps2InputType.Dualshock2L2 when ds2 => ps2Data[19] << 8,
            Ps2InputType.Dualshock2R2 when ds2 => ps2Data[20] << 8,
            Ps2InputType.MouseLeft when mouse => ~ps2Data[3] & (1 << 3),
            Ps2InputType.MouseRight when mouse => ~ps2Data[3] & (1 << 2),
            Ps2InputType.MouseX when mouse => (ps2Data[4] - 128) << 8,
            Ps2InputType.MouseY when mouse => -(ps2Data[5] - 127) << 8,
            Ps2InputType.LeftStickX when basicAxis => (ps2Data[7] - 128) << 8,
            Ps2InputType.LeftStickY when basicAxis => -(ps2Data[8] - 127) << 8,
            Ps2InputType.RightStickX when basicAxis => (ps2Data[5] - 128) << 8,
            Ps2InputType.RightStickY when basicAxis => -(ps2Data[6] - 127) << 8,
            Ps2InputType.NegConTwist when negcon => (ps2Data[5] - 128) << 8,
            Ps2InputType.NegConI when negcon => ps2Data[6],
            Ps2InputType.NegConIi when negcon => ps2Data[7],
            Ps2InputType.NegConL when negcon => ps2Data[8],
            Ps2InputType.NegConR when negcon => ~ps2Data[4] & (1 << 3),
            Ps2InputType.NegConA when negcon => ~ps2Data[4] & (1 << 5),
            Ps2InputType.NegConB when negcon => ~ps2Data[4] & (1 << 4),
            Ps2InputType.GunConHSync when guncon => (ps2Data[6] << 8) | ps2Data[5],
            Ps2InputType.GunConVSync when guncon => (ps2Data[8] << 8) | ps2Data[7],
            Ps2InputType.JogConWheel when jogcon => (ps2Data[6] << 8) | ps2Data[5],
            Ps2InputType.GuitarWhammy when guitar => -(ps2Data[8] - 127) << 9,
            Ps2InputType.GuitarGreen when guitar => ~ps2Data[4] & (1 << 1),
            Ps2InputType.GuitarRed when guitar => ~ps2Data[4] & (1 << 5),
            Ps2InputType.GuitarYellow when guitar => ~ps2Data[4] & (1 << 4),
            Ps2InputType.GuitarBlue when guitar => ~ps2Data[4] & (1 << 6),
            Ps2InputType.GuitarOrange when guitar => ~ps2Data[4] & (1 << 7),
            Ps2InputType.GuitarSelect when guitar => ~ps2Data[3] & (1 << 0),
            Ps2InputType.GuitarTilt when guitar => ~ps2Data[4] & (1 << 0),
            Ps2InputType.GuitarStart when guitar => ~ps2Data[3] & (1 << 3),
            Ps2InputType.GuitarStrumUp when guitar => ~ps2Data[3] & (1 << 4),
            Ps2InputType.GuitarStrumDown when guitar => ~ps2Data[3] & (1 << 6),
            Ps2InputType.GuitarTapBar => Gh5NeckInput.Gh5MappingsReversed.GetValueOrDefault(lastTapPs2),
            Ps2InputType.GuitarTapAll => Gh5NeckInput.Gh5MappingsReversed.GetValueOrDefault(lastTapPs2),
            Ps2InputType.GuitarTapGreen => lastTapPs2.HasFlag(BarButton.Green) ? 1 : 0,
            Ps2InputType.GuitarTapRed => lastTapPs2.HasFlag(BarButton.Red) ? 1 : 0,
            Ps2InputType.GuitarTapYellow => lastTapPs2.HasFlag(BarButton.Yellow) ? 1 : 0,
            Ps2InputType.GuitarTapBlue => lastTapPs2.HasFlag(BarButton.Blue) ? 1 : 0,
            Ps2InputType.GuitarTapOrange => lastTapPs2.HasFlag(BarButton.Orange) ? 1 : 0,
            Ps2InputType.NegConStart => ~ps2Data[3] & (1 << 3),
            Ps2InputType.L3 when digital => ~ps2Data[3] & (1 << 1),
            Ps2InputType.R3 when digital => ~ps2Data[3] & (1 << 2),
            Ps2InputType.Start when digital => ~ps2Data[3] & (1 << 3),
            Ps2InputType.Select when digital => ~ps2Data[3] & (1 << 0),
            Ps2InputType.DpadUp when digital => ~ps2Data[3] & (1 << 4),
            Ps2InputType.DpadRight when digital => ~ps2Data[3] & (1 << 5),
            Ps2InputType.DpadDown when digital => ~ps2Data[3] & (1 << 6),
            Ps2InputType.DpadLeft when digital => ~ps2Data[3] & (1 << 7),
            Ps2InputType.DpadUp when guitar => ps2Data[5] < 255 / 4 ? 1 : 0,
            Ps2InputType.DpadLeft when guitar => ps2Data[5] < 255 / 4 * 2 && ps2Data[5] > 255 / 4 ? 1 : 0,
            Ps2InputType.DpadDown when guitar => ps2Data[5] != 127 && ps2Data[5] < 255 / 4 * 3 && ps2Data[5] > 255 / 4 * 2 ? 1 : 0,
            Ps2InputType.DpadRight when guitar => ps2Data[5] > 255 / 4 * 3 && ps2Data[5] < 250 ? 1 : 0,
            Ps2InputType.L2 when digital => ~ps2Data[4] & (1 << 0),
            Ps2InputType.R2 when digital => ~ps2Data[4] & (1 << 1),
            Ps2InputType.L1 when digital => ~ps2Data[4] & (1 << 2),
            Ps2InputType.R1 when digital => ~ps2Data[4] & (1 << 3),
            Ps2InputType.Triangle when digital => ~ps2Data[4] & (1 << 4),
            Ps2InputType.Circle when digital => ~ps2Data[4] & (1 << 5),
            Ps2InputType.Cross when digital => ~ps2Data[4] & (1 << 6),
            Ps2InputType.Square when digital => ~ps2Data[4] & (1 << 7),
            _ => RawValue
        };
    }

    public bool SupportsType(Ps2ControllerType type)
    {
        var types = new List<Ps2ControllerType>();
        if (AxisToType.TryGetValue(Input, out var value))
        {
            types.Add(value);
        }
        else if (Dualshock2Order.Contains(Input))
        {
            types.Add(Ps2ControllerType.Dualshock2);
        }
        else if (DigitalButtons.Contains(Input))
        {
            types.Add(Ps2ControllerType.Digital);
            types.Add(Ps2ControllerType.Dualshock);
            types.Add(Ps2ControllerType.Dualshock2);
            types.Add(Ps2ControllerType.FlightStick);
        }
        else
        {
            types.Add(ControllerTypes[Input]);
        }

        if (Dualshock.Contains(Input))
        {
            types.Add(Ps2ControllerType.Dualshock);
            types.Add(Ps2ControllerType.FlightStick);
        }
        
        if (GuitarButtons.Contains(Input))
        {
            types.Add(Ps2ControllerType.Guitar);
        }

        return types.Contains(type);
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        Dictionary<Ps2InputType, List<string>> ds2Axis = new();
        Dictionary<Ps2ControllerType, List<string>> mappedBindings = new();
        foreach (var binding in bindings)
            if (binding.Item1.InnermostInputs().First() is Ps2Input input)
            {
                var types = new List<Ps2ControllerType>();
                if (AxisToType.TryGetValue(input.Input, out var value))
                {
                    types.Add(value);
                }
                else if (Dualshock2Order.Contains(input.Input))
                {
                    if (!ds2Axis.ContainsKey(input.Input)) ds2Axis.Add(input.Input, new List<string>());
                    ds2Axis[input.Input].Add(binding.Item2);
                }
                else if (DigitalButtons.Contains(input.Input))
                {
                    types.Add(Ps2ControllerType.Digital);
                    types.Add(Ps2ControllerType.Dualshock);
                    types.Add(Ps2ControllerType.Dualshock2);
                    types.Add(Ps2ControllerType.FlightStick);
                }

                // Only do this binding on controllers without analog pressures
                if (input.Input is Ps2InputType.L2 or Ps2InputType.R2 && binding.Item1 is DigitalToAnalog)
                {
                    types.Remove(Ps2ControllerType.Dualshock2);
                }

                if (GuitarButtons.Contains(input.Input)) types.Add(Ps2ControllerType.Guitar);

                if (Dualshock.Contains(input.Input))
                {
                    types.Add(Ps2ControllerType.Dualshock);
                    types.Add(Ps2ControllerType.FlightStick);
                }

                foreach (var type in types)
                {
                    if (!mappedBindings.ContainsKey(type)) mappedBindings.Add(type, new List<string>());
                    var mapping = binding.Item2;
                    // Guitar dpad uses a totally different set of bindings, so we need to swap em out
                    if (type == Ps2ControllerType.Guitar && input.Input is Ps2InputType.DpadDown
                            or Ps2InputType.DpadLeft or Ps2InputType.DpadRight or Ps2InputType.DpadUp)
                    {
                        mapping = mapping.Replace(Mappings[input.Input], MappingsDpadGuitar[input.Input]);
                    }
                    mappedBindings[type].Add(mapping);
                }
            }

        var retDs2 = "";
        foreach (var binding in Dualshock2Order)
        {
            if (ds2Axis.TryGetValue(binding, out var axi))
            {
                retDs2 += string.Join("\n", axi) + "\n";
            }
        }

        if (!string.IsNullOrEmpty(retDs2))
        {
            var mappings = mappedBindings.GetValueOrDefault(Ps2ControllerType.Dualshock2, new List<string>());
            mappings.Add(retDs2);
            mappedBindings[Ps2ControllerType.Dualshock2] = mappings;
        }

        var ret = "";
        foreach (var (input, mappings) in mappedBindings)
        {
            ret += $"""
                    
                           case {CType[input]}:
                              {string.Join("\n          ", mappings.Select(s => s.Replace("\n", "\n          ")))}
                              break;
                    """;
        }

        return ret == ""
            ? ""
            : $$"""
                if (ps2Valid) {
                   switch (ps2ControllerType) {
                       {{ret}}
                   }
                }
                """;
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        var defines = base.RequiredDefines().ToList();
        defines.Add("INPUT_PS2");
        defines.Add($"PS2_ACK {Ack}");
        defines.Add($"INPUT_PS2_ATT_SET() {Model.Microcontroller.GenerateDigitalWrite(Att, true, Peripheral)}");
        defines.Add($"INPUT_PS2_ATT_CLEAR() {Model.Microcontroller.GenerateDigitalWrite(Att, false, Peripheral)}");
        defines.AddRange(Model.Microcontroller.GenerateAckDefines(Ack));

        return defines;
    }
}