using Reg = Registers;

public partial class Z80
{
    private int Op_CB() // Opcode: CB
    {
        byte opcode = PeekNextByte();
        IncrementRegisterR();
        return _cbOpcodes[opcode]();
    }

    private int Op_SRL_E() // Opcode: CB 3B
    {
        byte carry = (byte)(Reg.E & 0x01);
        WriteFlag(Flags.C, carry != 0);

        Reg.E = (byte)(Reg.E >> 1);

        WriteFlag(Flags.C, carry != 0);
        ClearFlag(Flags.N); // N is always cleared
        ClearFlag(Flags.H); // H is always cleared
        SetSZFlags(Reg.E);  // Updates S, Z, F5, F3
        SetParity(Reg.E);   // P/V indicates parity for shift instructions
        
        Reg.PC += 2;
        return 8;
    }
    
    private int Op_BIT_7_ptrHL() // Opcode: CB 7E
    {
        byte value = _machine.ReadByte(Reg.HL);
        Reg.PC += 2;
        return BIT(7, value, isMemory: true);
    }
    
    #region Helper Methods
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
    #endregion
}