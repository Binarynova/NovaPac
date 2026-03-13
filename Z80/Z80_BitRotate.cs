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
    
    
}