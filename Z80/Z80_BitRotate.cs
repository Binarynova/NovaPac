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

    private int Op_DAA() // Opcode: 27
    {
        int correction = 0;
        bool carry = GetFlag(Flags.C);

        if (GetFlag(Flags.H) || (!GetFlag(Flags.N) && (Reg.A & 0x0F) > 9))
            correction |= 0x06;

        if (carry || (!GetFlag(Flags.N) && Reg.A > 0x99))
        {
            correction |= 0x60;
            carry = true;
        }

        if (GetFlag(Flags.N))
            Reg.A -= (byte)correction;
        else
            Reg.A += (byte)correction;
        
        WriteFlag(Flags.C, carry);
        ClearFlag(Flags.H);
        
        SetSZFlags(Reg.A);
        SetParity(Reg.A);
        WriteFlag(Flags.F5, (Reg.A & 0x20) != 0);
        WriteFlag(Flags.F3, (Reg.A & 0x08) != 0);

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