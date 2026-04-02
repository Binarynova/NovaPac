public static class Registers
{
    public static byte A, F, B, C, D, E, H, L, P, Q;
    private static byte A2, F2, B2, C2, D2, E2, H2, L2;
    public static byte I, R;
    public static ushort PC, SP, WZ;
    public static byte IXL, IXH, IYL, IYH;

    public static byte HighByte(ushort word)
    {
        return (byte)(word >> 8);
    }
    public static byte LowByte(ushort word)
    {
        return (byte)(word & 0xFF);
    }

    // reminder to self that the z80 is little-endian
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
    
    public static ushort AF2
    {
        get => (ushort)((A2 << 8) | F2);
        set
        {
            A2 = HighByte(value);
            F2 = LowByte(value);
        }
    }

    public static ushort BC2
    {
        get => (ushort)((B2 << 8) | C2);
        set
        {
            B2 = HighByte(value);
            C2 = LowByte(value);
        }
    }

    public static ushort DE2
    {
        get => (ushort)((D2 << 8) | E2);
        set
        {
            D2 = HighByte(value);
            E2 = LowByte(value);
        }
    }

    public static ushort HL2
    {
        get => (ushort)((H2 << 8) | L2);
        set
        {
            H2 = HighByte(value);
            L2 = LowByte(value);
        }
    }

    public static ushort IX
    {
        get => (ushort)((IXH << 8) | IXL);
        set
        {
            IXH = HighByte(value);
            IXL = LowByte(value);
        }
    }
    

    public static ushort IY
    {
        get => (ushort)((IYH << 8) | IYL);
        set
        {
            IYH = HighByte(value);
            IYL = LowByte(value);
        }
    }
}