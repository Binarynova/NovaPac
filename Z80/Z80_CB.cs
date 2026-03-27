using Reg = Registers;

public partial class Z80
{
    private int Op_CB() // Opcode: CB
    {
        byte opcode = PeekNextByte();
        IncrementRegisterR();
        if (opcode > 0x3F)
        {
            return Handle_CB_Bitwise(opcode);
        }
        return _cbOpcodes[opcode]();
    }

    private int Op_RLC(ref byte register) // Opcodes: CB 00 01 02 03 04 05 07
    {
        // save off bit 7
        byte bit7 = (byte)((register & 0x80) >> 7);
        
        // rotate left (bit0 = bit7, Flags.C = bit7)
        register = (byte)((register << 1) | bit7);
        WriteFlag(Flags.C, bit7 != 0);
        
        ClearFlag(Flags.H | Flags.N);
        SetSZFlags(register);
        SetParity(register);
        
        Reg.PC += 2;
        return 8;
    }
    
    private int Op_RRC(ref byte register) // Opcodes: CB 08 09 0A 0B 0C 0D 0F
    {
        // save off bit 0
        byte origBit0 = (byte)(register & 0x01);
        
        // rotate right (bit 0 = origCarry, Flags.C = origBit7)
        register = (byte)((register >> 1) | (origBit0 << 7));
        WriteFlag(Flags.C, origBit0 != 0);
        
        ClearFlag(Flags.H | Flags.N);
        SetSZFlags(register);
        SetParity(register);
        
        Reg.PC += 2;
        return 8;
    }
    
    private int Op_RL(ref byte register) // Opcodes: CB 10 11 12 13 14 15 17
    {
        // save off the carry flag and the register's bit 7
        byte origCarry = (byte)(GetFlag(Flags.C) ? 1 : 0);
        byte origBit7 = (byte)((register & 0x80) >> 7);
        
        // rotate left (bit 0 = origCarry, Flags.C = origBit7)
        register = (byte)((register << 1) | origCarry);
        WriteFlag(Flags.C, origBit7 != 0);
        
        ClearFlag(Flags.H | Flags.N);
        SetSZFlags(register);
        SetParity(register);
        
        Reg.PC += 2;
        return 8;
    }
    
    private int Op_RR(ref byte register) // Opcodes: CB 18 19 1A 1B 1C 1D 1F
    {
        // save off the carry flag and the register's bit 0
        byte origCarry = (byte)(GetFlag(Flags.C) ? 1 : 0);
        byte origBit0 = (byte)(register & 0x01);
        
        // rotate right (bit 0 = origCarry, Flags.C = origBit7)
        register = (byte)((register >> 1) | (origCarry << 7));
        WriteFlag(Flags.C, origBit0 != 0);
        
        ClearFlag(Flags.H | Flags.N);
        SetSZFlags(register);
        SetParity(register);
        
        Reg.PC += 2;
        return 8;
    }
    
    private int Op_SLA(ref byte register) // Opcode: CB 20 21 22 23 24 25 27
    {
        byte carry = (byte)(register & 0x80);
        WriteFlag(Flags.C, carry != 0);

        register = (byte)(register << 1);
        
        ClearFlag(Flags.H | Flags.N);
        SetSZFlags(register);
        SetParity(register);

        Reg.PC += 2;
        return 8;
    }

    private int Op_SRA(ref byte register) // Opcode: CB 28 29 2A 2B 2C 2D 2F
    {
        byte bit7 = (byte)((register & 0x80) >> 7);
        byte carry = (byte)(register & 0x01);
        WriteFlag(Flags.C, carry != 0);

        register = (byte)(register >> 1);
        register |= (byte)(bit7 << 7);

        WriteFlag(Flags.C, carry != 0);
        ClearFlag(Flags.H | Flags.N);
        SetSZFlags(register);
        SetParity(register);
        
        Reg.PC += 2;
        return 8;
    }
    
    private int Op_SLL(ref byte register) // Opcode: CB 30 31 32 33 34 35 37
    {
        byte carry = (byte)(register & 0x80);
        WriteFlag(Flags.C, carry != 0);

        register = (byte)(register << 1);
        register |= 0x1;
        
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
        
        ClearFlag(Flags.H | Flags.N);
        SetSZFlags(register);
        SetParity(register);
        
        Reg.PC += 2;
        return 8;
    }
    
    private int Op_BIT_7_ptrHL() // Opcode: CB 7E
    {
        byte value = _machine.ReadByte(Reg.HL);
        Reg.PC += 2;
        return BIT(7, value, isMemory: true);
    }

    private int Op_SET_7(ref byte register) // Opcodes: Cb F8 F9 FA FB FC FD FF
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
        ClearFlag(Flags.N);
        WriteFlag(Flags.H, true);
        WriteFlag(Flags.S, n == 7 && bitSet);
        WriteFlag(Flags.Z, !bitSet);
        WriteFlag(Flags.P, !bitSet);
        WriteFlag(Flags.F5, (value & 0x20) != 0);
        WriteFlag(Flags.F3, (value & 0x08) != 0);

        // Return cycles
        return isMemory ? 12 : 8;
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