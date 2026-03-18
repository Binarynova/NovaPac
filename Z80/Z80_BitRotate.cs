using Reg = Registers;

public partial class Z80
{
    private static int Op_RLCA() // Opcode: 07
    {
        if((Reg.A & 0x80) != 0)
        {
            Reg.A = (byte)((Reg.A << 1) | 1);
            SetFlag(Flags.C);
        }
        else
        {
            Reg.A = (byte)(Reg.A << 1);
            ClearFlag(Flags.C);
        }        
        
        WriteFlag(Flags.F5, (Reg.A & 0x20) != 0);
        WriteFlag(Flags.F3, (Reg.A & 0x08) != 0);
        ClearFlag(Flags.N | Flags.H);
        Reg.PC += 1;
        return 4;
    }
    
    private static int Op_RLA() // Opcode: 17
    {
        int oldBit7 = Reg.A & 0x80;
        int carry = GetFlag(Flags.C) ? 1 : 0;

        if(oldBit7 == 0)
            ClearFlag(Flags.C);
        else
            SetFlag(Flags.C);
        
        Reg.A = (byte)((Reg.A << 1) | carry);

        WriteFlag(Flags.F5, (Reg.A & 0x20) != 0);
        WriteFlag(Flags.F3, (Reg.A & 0x08) != 0);
        ClearFlag(Flags.H | Flags.N);
        Reg.PC += 1;
        return 4;
    }

    private int Op_DAA()
    {
        // 1. Capture the "Before" state entirely
        byte a = Reg.A;
        bool initialC = GetFlag(Flags.C);
        bool initialH = GetFlag(Flags.H);
        bool initialN = GetFlag(Flags.N);

        byte correction = 0;
        bool finalC = initialC;

        // 2. Determine Correction Factor
        // Low nibble correction
        if (initialH || (a & 0x0F) > 0x09)
        {
            correction |= 0x06;
        }

        // High nibble correction
        if (initialC || a > 0x99)
        {
            correction |= 0x60;
            finalC = true; // High nibble correction ALWAYS sets/keeps Carry
        }

        // 3. Apply correction based on N (Add vs Subtract)
        if (initialN)
        {
            Reg.A = (byte)(a - correction);
        }
        else
        {
            Reg.A = (byte)(a + correction);
        }

        // 4. Final Flag Logic (The "Test Pleasers")
        WriteFlag(Flags.C, finalC);
    
        // Half-carry logic is different for Add vs Sub
        bool finalH;
        if (initialN)
            finalH = initialH && (a & 0x0F) < 0x06;
        else
            finalH = (a & 0x0F) > 0x09;
    
        WriteFlag(Flags.H, finalH);

        // Use your existing helpers for the rest
        SetSZFlags(Reg.A); // Sets S, Z, F5, F3 based on NEW A
        SetParity(Reg.A);  // Sets P/V based on parity of NEW A
        // Note: N flag is NOT changed by DAA; it stays what it was!

        Reg.PC += 1;
        return 4;
    }

    private static int Op_RRCA() // Opcode: 0F
    {
        if((Reg.A & 0x01) != 0)
        {
            Reg.A = (byte)((Reg.A >> 1) | 0x80);
            SetFlag(Flags.C);
        }
        else
        {
            Reg.A = (byte)(Reg.A >> 1);
            ClearFlag(Flags.C);
        }        
        
        WriteFlag(Flags.F5, (Reg.A & 0x20) != 0);
        WriteFlag(Flags.F3, (Reg.A & 0x08) != 0);
        ClearFlag(Flags.N | Flags.H);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_RRA() // Opcode: 1F
    {
        int oldBit0 = Reg.A & 0x01;
        int carry = GetFlag(Flags.C) ? 1 : 0;

        if(oldBit0 == 0)
            ClearFlag(Flags.C);
        else
            SetFlag(Flags.C);
        
        Reg.A = (byte)((Reg.A >> 1) | (carry << 7));

        WriteFlag(Flags.F5, (Reg.A & 0x20) != 0);
        WriteFlag(Flags.F3, (Reg.A & 0x08) != 0);
        ClearFlag(Flags.H | Flags.N);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_CPL() // Opcode: 2F
    {
        // Bites of Reg.A are inverted (one's complement)
        Reg.A = (byte)~Reg.A;
        SetFlag(Flags.H | Flags.N);
        
        WriteFlag(Flags.F5, (Reg.A & 0x20) != 0); // Bit 5
        WriteFlag(Flags.F3, (Reg.A & 0x08) != 0); // Bit 3
        Reg.PC += 1;
        return 4;
    }
}