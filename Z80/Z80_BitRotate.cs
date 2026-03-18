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
        byte a = Reg.A;
        int correction = 0;
        bool n = GetFlag(Flags.N);
        bool h = GetFlag(Flags.H);
        bool c = GetFlag(Flags.C);

        // Determine the correction factor
        if (h || (!n && (a & 0x0F) > 9))
        {
            correction |= 0x06;
        }

        if (c || (!n && a > 0x99))
        {
            correction |= 0x60;
            WriteFlag(Flags.C, true); // Carry stays set
        }

        // Determine the NEW Half-Carry flag before we modify A
        // For addition: H is set if lower nibble > 9
        // For subtraction: H is set if borrow was needed (low nibble < 6 and H was set)
        bool nextH;
        if (!n)
        {
            nextH = (a & 0x0F) > 9;
        }
        else
        {
            nextH = h && (a & 0x0F) < 6;
        }

        WriteFlag(Flags.H, nextH);

        // Apply the correction
        if (n)
        {
            Reg.A = (byte)(a - correction);
        }
        else
        {
            Reg.A = (byte)(a + correction);
        }

        // Update remaining flags
        SetSZFlags(Reg.A);
        SetParity(Reg.A); // DAA updates P/V to reflect parity of the result

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

        Reg.PC += 1;
        return 4;
    }
}