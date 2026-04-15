public partial class Z80Cpu
{
    private int Op_RLCA() // Opcode: 07
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

        // this instruction touches flags so:
        Reg.P = 0;
        Reg.Q = Reg.F;
        
        Reg.PC += 1;
        return 4;
    }
    
    private int Op_RLA() // Opcode: 17
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

        Reg.Q = Reg.F;
        Reg.P = 0;
        Reg.PC += 1;
        return 4;
    }

    private int Op_DAA()
    {
        byte a = Reg.A;
        bool initialC = GetFlag(Flags.C);
        bool initialH = GetFlag(Flags.H);
        bool initialN = GetFlag(Flags.N);

        byte correction = 0;
        bool finalC = initialC;

        if (initialH || (a & 0x0F) > 0x09)
        {
            correction |= 0x06;
        }

        if (initialC || a > 0x99)
        {
            correction |= 0x60;
            finalC = true;
        }

        if (initialN)
        {
            Reg.A = (byte)(a - correction);
        }
        else
        {
            Reg.A = (byte)(a + correction);
        }

        WriteFlag(Flags.C, finalC);
    
        bool finalH;
        if (initialN)
            finalH = initialH && (a & 0x0F) < 0x06;
        else
            finalH = (a & 0x0F) > 0x09;
    
        WriteFlag(Flags.H, finalH);
        SetSZFlags(Reg.A);
        SetParity(Reg.A);

        Reg.P = 0;

        Reg.PC += 1;
        return 4;
    }

    private int Op_RRCA() // Opcode: 0F
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

        Reg.P = 0;
        Reg.Q = Reg.F;
        
        Reg.PC += 1;
        return 4;
    }

    private int Op_RRA() // Opcode: 1F
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
        
        Reg.Q = Reg.F;
        Reg.P = 0;
        Reg.PC += 1;
        return 4;
    }

    private int Op_CPL() // Opcode: 2F
    {
        Reg.A = (byte)~Reg.A;
        SetFlag(Flags.H | Flags.N);
        
        Reg.PC += 1;
        return 4;
    }
}