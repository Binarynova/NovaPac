public static class Registers
{
    public static byte A, F, B, C, D, E, H, L;
    public static byte Aa, Fa, Ba, Ca, Da, Ea, Ha, La;
    public static byte I, R;
    public static ushort PC, SP, IX, IY;

    public static byte HighByte(ushort word)
    {
        return (byte)(word >> 8);
    }
    public static byte LowByte(ushort word)
    {
        return (byte)(word & 0xFF);
    }

    // reminder that the z80 is little-endian
    public static ushort AF
    {
        get => (ushort)((A << 8) | F);
        set
        {
            A = HighByte(value);
            F = LowByte(value);
        }
    }

    public static ushort BC
    {
        get => (ushort)((B << 8) | C);
        set
        {
            B = HighByte(value);
            C = LowByte(value);
        }
    }

    public static ushort DE
    {
        get => (ushort)((D << 8) | E);
        set
        {
            D = HighByte(value);
            E = LowByte(value);
        }
    }

    public static ushort HL
    {
        get => (ushort)((H << 8) | L);
        set
        {
            H = HighByte(value);
            L = LowByte(value);
        }
    }
}