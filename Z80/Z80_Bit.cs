using Reg = Registers;

public partial class Z80
{
    private int Op_CB()
    {
        byte opcode = PeekOpcode();
        IncrementRegisterR();
        return _cbOpcodes[opcode]();
    }
    
    private int BIT(byte n, byte value, bool isMemory = false)
    {
        // Test the bit
        bool bitSet = (value & (1 << n)) != 0;

        // Flags
        ClearFlag(Flags.N);            // N always cleared
        WriteFlag(Flags.H, true);              // H always set
        WriteFlag(Flags.S, n == 7 && bitSet); // S only set for bit 7
        WriteFlag(Flags.Z, !bitSet);         // Z set if bit is 0
        WriteFlag(Flags.P, !bitSet);       // PV mirrors Z
        WriteFlag(Flags.F5, (value & 0x20) != 0); // undocumented F5
        WriteFlag(Flags.F3, (value & 0x08) != 0); // undocumented F3

        // Return cycles
        return isMemory ? 12 : 8; // 12 cycles if operand is (HL), else 8
    }
    
    private int Op_BIT_7_ptrHL()
    {
        byte value = machine.ReadByte(Reg.HL);
        Reg.PC += 2;
        return BIT(7, value, isMemory: true);
    }
    private static int Op_RLCA()
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
    private static int Op_RRCA()
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

    private static int Op_RRA()
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