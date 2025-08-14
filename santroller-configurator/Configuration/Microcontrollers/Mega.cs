using System.Collections.Generic;
using System.Linq;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public class Mega : AvrController
{
    private static readonly int[] PinIndex =
    [
        0, // PE 0 ** 0 ** USART0_RX	
        1, // PE 1 ** 1 ** USART0_TX	
        4, // PE 4 ** 2 ** PWM2	
        5, // PE 5 ** 3 ** PWM3	
        5, // PG 5 ** 4 ** PWM4	
        3, // PE 3 ** 5 ** PWM5	
        3, // PH 3 ** 6 ** PWM6	
        4, // PH 4 ** 7 ** PWM7	
        5, // PH 5 ** 8 ** PWM8	
        6, // PH 6 ** 9 ** PWM9	
        4, // PB 4 ** 10 ** PWM10	
        5, // PB 5 ** 11 ** PWM11	
        6, // PB 6 ** 12 ** PWM12	
        7, // PB 7 ** 13 ** PWM13	
        1, // PJ 1 ** 14 ** USART3_TX	
        0, // PJ 0 ** 15 ** USART3_RX	
        1, // PH 1 ** 16 ** USART2_TX	
        0, // PH 0 ** 17 ** USART2_RX	
        3, // PD 3 ** 18 ** USART1_TX	
        2, // PD 2 ** 19 ** USART1_RX	
        1, // PD 1 ** 20 ** I2C_SDA	
        0, // PD 0 ** 21 ** I2C_SCL	
        0, // PA 0 ** 22 ** D22	
        1, // PA 1 ** 23 ** D23	
        2, // PA 2 ** 24 ** D24	
        3, // PA 3 ** 25 ** D25	
        4, // PA 4 ** 26 ** D26	
        5, // PA 5 ** 27 ** D27	
        6, // PA 6 ** 28 ** D28	
        7, // PA 7 ** 29 ** D29	
        7, // PC 7 ** 30 ** D30	
        6, // PC 6 ** 31 ** D31	
        5, // PC 5 ** 32 ** D32	
        4, // PC 4 ** 33 ** D33	
        3, // PC 3 ** 34 ** D34	
        2, // PC 2 ** 35 ** D35	
        1, // PC 1 ** 36 ** D36	
        0, // PC 0 ** 37 ** D37	
        7, // PD 7 ** 38 ** D38	
        2, // PG 2 ** 39 ** D39	
        1, // PG 1 ** 40 ** D40	
        0, // PG 0 ** 41 ** D41	
        7, // PL 7 ** 42 ** D42	
        6, // PL 6 ** 43 ** D43	
        5, // PL 5 ** 44 ** D44	
        4, // PL 4 ** 45 ** D45	
        3, // PL 3 ** 46 ** D46	
        2, // PL 2 ** 47 ** D47	
        1, // PL 1 ** 48 ** D48	
        0, // PL 0 ** 49 ** D49	
        3, // PB 3 ** 50 ** SPI_MISO	
        2, // PB 2 ** 51 ** SPI_MOSI	
        1, // PB 1 ** 52 ** SPI_SCK	
        0, // PB 0 ** 53 ** SPI_SS	
        0, // PF 0 ** 54 ** A0	
        1, // PF 1 ** 55 ** A1	
        2, // PF 2 ** 56 ** A2	
        3, // PF 3 ** 57 ** A3	
        4, // PF 4 ** 58 ** A4	
        5, // PF 5 ** 59 ** A5	
        6, // PF 6 ** 60 ** A6	
        7, // PF 7 ** 61 ** A7	
        0, // PK 0 ** 62 ** A8	
        1, // PK 1 ** 63 ** A9	
        2, // PK 2 ** 64 ** A10	
        3, // PK 3 ** 65 ** A11	
        4, // PK 4 ** 66 ** A12	
        5, // PK 5 ** 67 ** A13	
        6, // PK 6 ** 68 ** A14	
        7 // PK 7 ** 69 ** A15	
    ];

    public static readonly Dictionary<int, int> Interrupts = new()
    {
        {2, 0},
        {3, 1},
        {18, 5},
        {19, 4},
        {20, 3},
        {21, 2}
    };

    private static readonly char[] Ports =
    [
        'E', // 'E' 0 ** 0 ** USART0_RX	
        'E', // 'E' 1 ** 1 ** USART0_TX	
        'E', // 'E' 4 ** 2 ** 'W'M2	
        'E', // 'E' 5 ** 3 ** 'W'M3	
        'G', // 'G' 5 ** 4 ** 'W'M4	
        'E', // 'E' 3 ** 5 ** 'W'M5	
        'H', // 'H' 3 ** 6 ** 'W'M6	
        'H', // 'H' 4 ** 7 ** 'W'M7	
        'H', // 'H' 5 ** 8 ** 'W'M8	
        'H', // 'H' 6 ** 9 ** 'W'M9	
        'B', // 'B' 4 ** 10 ** 'W'M10	
        'B', // 'B' 5 ** 11 ** 'W'M11	
        'B', // 'B' 6 ** 12 ** 'W'M12	
        'B', // 'B' 7 ** 13 ** 'W'M13	
        'J', // 'J' 1 ** 14 ** USART3_TX	
        'J', // 'J' 0 ** 15 ** USART3_RX	
        'H', // 'H' 1 ** 16 ** USART2_TX	
        'H', // 'H' 0 ** 17 ** USART2_RX	
        'D', // 'D' 3 ** 18 ** USART1_TX	
        'D', // 'D' 2 ** 19 ** USART1_RX	
        'D', // 'D' 1 ** 20 ** I2C_SDA	
        'D', // 'D' 0 ** 21 ** I2C_SCL	
        'A', // 'A' 0 ** 22 ** D22	
        'A', // 'A' 1 ** 23 ** D23	
        'A', // 'A' 2 ** 24 ** D24	
        'A', // 'A' 3 ** 25 ** D25	
        'A', // 'A' 4 ** 26 ** D26	
        'A', // 'A' 5 ** 27 ** D27	
        'A', // 'A' 6 ** 28 ** D28	
        'A', // 'A' 7 ** 29 ** D29	
        'C', // 'C' 7 ** 30 ** D30	
        'C', // 'C' 6 ** 31 ** D31	
        'C', // 'C' 5 ** 32 ** D32	
        'C', // 'C' 4 ** 33 ** D33	
        'C', // 'C' 3 ** 34 ** D34	
        'C', // 'C' 2 ** 35 ** D35	
        'C', // 'C' 1 ** 36 ** D36	
        'C', // 'C' 0 ** 37 ** D37	
        'D', // 'D' 7 ** 38 ** D38	
        'G', // 'G' 2 ** 39 ** D39	
        'G', // 'G' 1 ** 40 ** D40	
        'G', // 'G' 0 ** 41 ** D41	
        'L', // 'L' 7 ** 42 ** D42	
        'L', // 'L' 6 ** 43 ** D43	
        'L', // 'L' 5 ** 44 ** D44	
        'L', // 'L' 4 ** 45 ** D45	
        'L', // 'L' 3 ** 46 ** D46	
        'L', // 'L' 2 ** 47 ** D47	
        'L', // 'L' 1 ** 48 ** D48	
        'L', // 'L' 0 ** 49 ** D49	
        'B', // 'B' 3 ** 50 ** SPI_MISO	
        'B', // 'B' 2 ** 51 ** SPI_MOSI	
        'B', // 'B' 1 ** 52 ** SPI_SCK	
        'B', // 'B' 0 ** 53 ** SPI_SS	
        'F', // 'F' 0 ** 54 ** A0	
        'F', // 'F' 1 ** 55 ** A1	
        'F', // 'F' 2 ** 56 ** A2	
        'F', // 'F' 3 ** 57 ** A3	
        'F', // 'F' 4 ** 58 ** A4	
        'F', // 'F' 5 ** 59 ** A5	
        'F', // 'F' 6 ** 60 ** A6	
        'F', // 'F' 7 ** 61 ** A7	
        'K', // 'K' 0 ** 62 ** A8	
        'K', // 'K' 1 ** 63 ** A9	
        'K', // 'K' 2 ** 64 ** A10	
        'K', // 'K' 3 ** 65 ** A11	
        'K', // 'K' 4 ** 66 ** A12	
        'K', // 'K' 5 ** 67 ** A13	
        'K', // 'K' 6 ** 68 ** A14	
        'K' // 'K' 7 ** 69 ** A15            
    ];

    public Mega(Board board)
    {
        Board = board;
    }

    protected override int SpiMiso => 50;

    protected override int SpiMosi => 51;

    protected override int SpiCSn => 53;
    protected override int SpiSck => 52;

    protected override int I2CSda => 20;

    protected override int I2CScl => 21;
    protected override int UartTx => 1;
    protected override int UartRx => 0;

    public override int PinCount => PinIndex.Length;

    protected override char[] PortNames => ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'L'];

    protected override int PinA0 => 54;

    public override Board Board { get; }

    public override List<int> AnalogPins => Enumerable.Range(PinA0, 16).ToList();

    protected override Dictionary<(char, int), int> PinByMask { get; } = Ports.Zip(PinIndex)
        .Select((tuple, i) => (tuple.First, tuple.Second, i))
        .ToDictionary(s => (s.Item1, s.Item2), s => s.Item3);

    public override List<int> PwmPins => Enumerable.Range(2, 12).Concat(Enumerable.Range(44, 3)).ToList();


    protected override int GetInterruptForPin(int ack)
    {
        return Interrupts[ack];
    }

    public override List<int> SupportedAckPins()
    {
        return Interrupts.Keys.ToList();
    }

    public override List<int> GetAllPins()
    {
        return Enumerable.Range(0, PinIndex.Length).ToList();
    }

    public override bool FilterPin(bool isAnalog, bool isBluetooth, bool isInterrupt, int pin)
    {
        if (isInterrupt)
        {
            return Interrupts.ContainsKey(pin);
        }
        return !isAnalog || AnalogPins.Contains(pin);
    }

    public override int GetFirstDigitalPin()
    {
        return 2;
    }

    public override int GetIndex(int pin)
    {
        return PinIndex[pin];
    }

    public override char GetPort(int pin)
    {
        return Ports[pin];
    }

    public override int GetChannel(int pin, bool reconfigurePin)
    {
        var chan = PinA0 - pin;
        if (reconfigurePin) chan |= 1 << 7;
        return chan;
    }

    public override AvrPinMode? ForcedMode(int pin)
    {
        switch (pin)
        {
            case 0:
                return AvrPinMode.Input;
            case 1:
                return AvrPinMode.Output;
            case 13:
                return AvrPinMode.Input;
            default:
                return null;
        }
    }
}