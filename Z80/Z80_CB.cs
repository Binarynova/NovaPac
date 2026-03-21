using Reg = Registers;

public partial class Z80
{
    private int Op_CB() // Opcode: CB
    {
        byte opcode = PeekNextByte();
        IncrementRegisterR();
        if (opcode >= 0x40)
        {
            return Handle_CB_Bitwise(opcode);
        }
        return _cbOpcodes[opcode]();
    }

    private int Op_RLC(ref byte register) // Opcodes: 00 01 02 03 04 05 07
    {
        // extract bit 7
        byte bit7 = (byte)((register & 0x80) >> 7);
        // rotate (including wrapping bit 7)
        register = (byte)((register << 1) | bit7);
        
        WriteFlag(Flags.C, bit7 != 0);
        ClearFlag(Flags.H | Flags.N);
        SetSZFlags(register);
        SetParity(register);
        
        Reg.PC += 2;
        return 8;
    }
    
    private int Op_RL(ref byte register) // Opcodes: 10 11 12 13 14 15 17
    {
        byte oldCarry = (byte)(GetFlag(Flags.C) ? 1 : 0);
        // extract bit 7
        byte newCarry = (byte)((register & 0x80) >> 7);
        // rotate (including wrapping bit 7)
        register = (byte)((register << 1) | oldCarry);
        
        WriteFlag(Flags.C, newCarry != 0);
        ClearFlag(Flags.H | Flags.N);
        SetSZFlags(register);
        SetParity(register);
        
        Reg.PC += 2;
        return 8;
    }

    private int Op_SRL(ref byte register) // Opcode: CB 38 39 3A 3B 3C 3D 3F
    {
        byte carry = (byte)(register & 0x01);
        WriteFlag(Flags.C, carry != 0);

        register = (byte)(register >> 1);

        WriteFlag(Flags.C, carry != 0);
        ClearFlag(Flags.N); // N is always cleared
        ClearFlag(Flags.H); // H is always cleared
        SetSZFlags(register);  // Updates S, Z, F5, F3
        SetParity(register);   // P/V indicates parity for shift instructions
        
        Reg.PC += 2;
        return 8;
    }

    private int Op_SLA(ref byte register)
    {
        byte carry = (byte)(register & 0x80);
        WriteFlag(Flags.C, carry != 0);

        register = (byte)(register << 1);
        
        ClearFlag(Flags.N); // N always 0
        ClearFlag(Flags.H); // H always 0
        SetSZFlags(register);    // S updated (new bit 7), Z updated
        SetParity(register);     // P/V is parity of the result

        Reg.PC += 2;
        return 8;
    }
    
    private int Op_BIT_7_ptrHL() // Opcode: CB 7E
    {
        byte value = _machine.ReadByte(Reg.HL);
        Reg.PC += 2;
        return BIT(7, value, isMemory: true);
    }

    private int Op_SET_7(ref byte register)
    {
        register = (byte)(register | 0x80);

        Reg.PC += 2;
        return 8;
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
    
    private int Handle_CB_Bitwise(byte opcode)
    {
        int group = opcode >> 6;      // 1=BIT, 2=RES, 3=SET
        int bit = (opcode >> 3) & 0x07; // Which bit (0-7)
        int regIdx = opcode & 0x07;   // Which register (0-7)

        byte val = GetRegisterByIndex(regIdx);

        switch (group)
        {
            case 1: // BIT n, r
                bool isSet = (val & (1 << bit)) != 0;
                WriteFlag(Flags.Z, !isSet);
                WriteFlag(Flags.H, true);
                WriteFlag(Flags.N, false);
                WriteFlag(Flags.P, !isSet); // P/V mirrors Z for BIT
                WriteFlag(Flags.S, (bit == 7 && isSet));
            
                // Undocumented: F5/F3 mirror the register's bits 5/3
                WriteFlag(Flags.F5, (val & 0x20) != 0);
                WriteFlag(Flags.F3, (val & 0x08) != 0);
                break;

            case 2: // RES n, r
                val &= (byte)~(1 << bit);
                SetRegisterByIndex(regIdx, val);
                break;

            case 3: // SET n, r
                val |= (byte)(1 << bit);
                SetRegisterByIndex(regIdx, val);
                break;
        }

        Reg.PC += 2;
        // (HL) operations take 12 or 15 cycles, registers take 8
        return (regIdx == 6) ? (group == 1 ? 12 : 15) : 8;
    }
    private byte GetRegisterByIndex(int index)
    {
        return index switch
        {
            0 => Reg.B,
            1 => Reg.C,
            2 => Reg.D,
            3 => Reg.E,
            4 => Reg.H,
            5 => Reg.L,
            6 => _machine.ReadByte(Reg.HL),
            7 => Reg.A,
            _ => 0
        };
    }
    #endregion
}