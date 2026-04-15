public partial class Z80Cpu
{
    private int Op_ED() // Opcode: ED
    {
        byte opcode = PeekNextByte();
        IncrementRegisterR();
        return _edOpcodes[opcode]();
    }

    private int Op_SBC_HL_BC() // Opcode: ED 42
    {
        Reg.HL = SBCWord(Reg.HL, Reg.BC);
        Reg.PC += 2;
        return 15;
    }
    
    private int Op_LD_ptrNN_BC() // Opcode: ED 43
    {
        ushort addr = ImmediateWord(true);
        _bus.WriteByte(addr, Reg.C);
        _bus.WriteByte((ushort)(addr + 1), Reg.B);
        Reg.PC += 4;
        return 20;
    }

    private int Op_NEG() // Opcode: ED 44
    {
        byte originalA = Reg.A;
    
        int result = 0 - originalA;
        Reg.A = (byte)result;

        WriteFlag(Flags.C, originalA != 0x00);
        WriteFlag(Flags.H, (0 & 0x0F) < (originalA & 0x0F));
        WriteFlag(Flags.P, originalA == 0x80);
        SetFlag(Flags.N);
        SetSZFlags(Reg.A);

        Reg.PC += 2;
        return 8;
    }

    private int Op_RETN() // Opcode: ED 45
    {
        _iff1 = _iff2;

        Reg.PC = PopWord();
        return 14;
    }

    private int Op_LD_BC_ptrNN() // Opcode: ED 4B
    {
        ushort nn = ImmediateWord(true);

        Reg.C = _bus.ReadByte(nn);
        Reg.B = _bus.ReadByte((ushort)(nn + 1));
        
        Reg.PC += 4;
        return 20;
    }
    
    private int Op_RETI() // Opcode: ED 4D
    {
        Reg.PC = PopWord();

        return 14;
    }
    
    private int Op_LD_ptrNN_DE() // Opcode: ED 53
    {
        ushort addr = ImmediateWord(true);
        _bus.WriteByte(addr, Reg.E);
        _bus.WriteByte((ushort)(addr + 1), Reg.D);
        Reg.PC += 4;
        return 20;
    }
    
    private int Op_LD_DE_ptrNN() // Opcode: ED 5B
    {
        ushort nn = ImmediateWord(true);

        Reg.E = _bus.ReadByte(nn);
        Reg.D = _bus.ReadByte((ushort)(nn + 1));
        
        Reg.PC += 4;
        return 20;
    }
    
    private int Op_LD_ptrNN_SP() // Opcode: ED 73
    {
        ushort addr = ImmediateWord(true);
        
        _bus.WriteByte(addr, (byte)(Reg.SP & 0x00FF));
        _bus.WriteByte((ushort)(addr + 1), (byte)((Reg.SP & 0xFF00) >> 8));
        Reg.PC += 4;
        return 20;
    }
    
    private int Op_LD_SP_ptrNN() // Opcode: ED 7B
    {
        ushort nn = ImmediateWord(true);

        byte low = _bus.ReadByte(nn);
        byte high = _bus.ReadByte((ushort)(nn + 1));
        Reg.SP = (ushort)((high << 8) | low);
        
        Reg.PC += 4;
        return 20;
    }

    private int Op_SBC_HL_DE() // Opcode: ED 52
    {
        Reg.HL = SBCWord(Reg.HL, Reg.DE);
        Reg.PC += 2;
        return 15;
    }

    private int Op_SBC_HL_HL() // Opcode: ED 62
    {
        Reg.HL = SBCWord(Reg.HL, Reg.HL);
        Reg.PC += 2;
        return 15;
    }

    private int Op_SBC_HL_SP() // Opcode: ED 72
    {
        Reg.HL = SBCWord(Reg.HL, Reg.SP);
        Reg.PC += 2;
        return 15;
    }
    
    private int Op_ADC_HL_BC() // Opcode: ED 4A
    {
        ushort val1 = Reg.HL;
        ushort val2 = Reg.BC;
        int carry = GetFlag(Flags.C) ? 1 : 0;
        int result = val1 + val2 + carry;

        WriteFlag(Flags.N, false);
        WriteFlag(Flags.C, result > 0xFFFF);
        WriteFlag(Flags.Z, (ushort)result == 0);
        WriteFlag(Flags.S, (result & 0x8000) != 0);
        WriteFlag(Flags.H, ((val1 & 0x0FFF) + (val2 & 0x0FFF) + carry) > 0x0FFF);
        WriteFlag(Flags.P, ((val1 ^ result) & (val2 ^ result) & 0x8000) != 0);
    
        Reg.HL = (ushort)result;
        Reg.PC += 2;
        return 15;
    }
    
    private int Op_ADC_HL_DE() // Opcode: ED 5A
    {
        ushort val1 = Reg.HL;
        ushort val2 = Reg.DE;
        int carry = GetFlag(Flags.C) ? 1 : 0;
        int result = val1 + val2 + carry;

        WriteFlag(Flags.N, false);
        WriteFlag(Flags.C, result > 0xFFFF);
        WriteFlag(Flags.Z, (ushort)result == 0);
        WriteFlag(Flags.S, (result & 0x8000) != 0);
        WriteFlag(Flags.H, ((val1 & 0x0FFF) + (val2 & 0x0FFF) + carry) > 0x0FFF);
        WriteFlag(Flags.P, ((val1 ^ result) & (val2 ^ result) & 0x8000) != 0);
    
        Reg.HL = (ushort)result;
        Reg.PC += 2;
        return 15;
    }
    
    private int Op_ADC_HL_HL() // Opcode: ED 6A
    {
        ushort val1 = Reg.HL;
        ushort val2 = Reg.HL;
        int carry = GetFlag(Flags.C) ? 1 : 0;
        int result = val1 + val2 + carry;

        WriteFlag(Flags.N, false);
        WriteFlag(Flags.C, result > 0xFFFF);
        WriteFlag(Flags.Z, (ushort)result == 0);
        WriteFlag(Flags.S, (result & 0x8000) != 0);
        WriteFlag(Flags.H, ((val1 & 0x0FFF) + (val2 & 0x0FFF) + carry) > 0x0FFF);
        WriteFlag(Flags.P, ((val1 ^ result) & (val2 ^ result) & 0x8000) != 0);
    
        Reg.HL = (ushort)result;
        Reg.PC += 2;
        return 15;
    }
    
    private int Op_ADC_HL_SP() // Opcode: ED 7A
    {
        ushort val1 = Reg.HL;
        ushort val2 = Reg.SP;
        int carry = GetFlag(Flags.C) ? 1 : 0;
        int result = val1 + val2 + carry;

        WriteFlag(Flags.N, false);
        WriteFlag(Flags.C, result > 0xFFFF);
        WriteFlag(Flags.Z, (ushort)result == 0);
        WriteFlag(Flags.S, (result & 0x8000) != 0);
        WriteFlag(Flags.H, ((val1 & 0x0FFF) + (val2 & 0x0FFF) + carry) > 0x0FFF);
        WriteFlag(Flags.P, ((val1 ^ result) & (val2 ^ result) & 0x8000) != 0);
    
        Reg.HL = (ushort)result;
        Reg.PC += 2;
        return 15;
    }

    private int Op_LD_I_A() // Opcode: ED 47
    {
        Reg.I = Reg.A;
        Reg.PC += 2;
        return 9;
    }

    private int Op_LD_A_I() // Opcode: ED 47
    {
        Reg.A = Reg.I;
        Reg.PC += 2;
        return 9;
    }

    private int Op_LD_R_A() // Opcode: ED 47
    {
        Reg.R = Reg.A;
        Reg.PC += 2;
        return 9;
    }

    private int Op_LD_A_R() // Opcode: ED 47
    {
        Reg.A = Reg.R;
        Reg.PC += 2;
        return 9;
    }

    private int Op_IM_0() // Opcode: ED 46
    {
        _interruptMode = 0;
        Reg.PC += 2;
        return 8;
    }

    private int Op_IM_1() // Opcode: ED 56
    {
        _interruptMode = 1;
        Reg.PC += 2;
        return 8;
    }

    private int Op_IM_2() // Opcode: ED 5E
    {
        _interruptMode = 2;
        Reg.PC += 2;
        return 8;
    }
    
    private int Op_LDI() // Opcode: ED A0
    {
        byte value = _bus.ReadByte(Reg.HL);
        _bus.WriteByte(Reg.DE, value);

        Reg.HL++;
        Reg.DE++;
        Reg.BC--;
        
        ClearFlag(Flags.N | Flags.H);
        WriteFlag(Flags.P, Reg.BC != 0);
        
        Reg.PC += 2;
        return 16;
    }

    private int Op_LDIR() // Opcode: ED B0
    {
        byte value = _bus.ReadByte(Reg.HL);
        _bus.WriteByte(Reg.DE, value);

        Reg.HL++;
        Reg.DE++;
        Reg.BC--;

        ClearFlag(Flags.N | Flags.H);
        WriteFlag(Flags.P, Reg.BC != 0);

        if (Reg.BC != 0) return 21;
        
        Reg.PC += 2; // move past ED B0
        return 16;

        // stay on ED B0 until BC == 0
    }
    
    private int Op_LDD() // Opcode: ED A8
    {
        byte value = _bus.ReadByte(Reg.HL);
        _bus.WriteByte(Reg.DE, value);

        Reg.HL--;
        Reg.DE--;
        Reg.BC--;
        
        ClearFlag(Flags.N | Flags.H);
        WriteFlag(Flags.P, Reg.BC != 0);
        
        Reg.PC += 2;
        return 16;
    }

    private int Op_LDDR() // Opcode: ED B8
    {
        byte value = _bus.ReadByte(Reg.HL);
        _bus.WriteByte(Reg.DE, value);

        Reg.HL--;
        Reg.DE--;
        Reg.BC--;

        ClearFlag(Flags.N | Flags.H);
        WriteFlag(Flags.P, Reg.BC != 0);

        if (Reg.BC != 0) return 21;
        
        Reg.PC += 2; // move past ED B0
        return 16;

        // stay on ED B0 until BC == 0
    }

    private int Op_CPI()
    {
        byte value = _bus.ReadByte(Reg.HL);
        int result = Reg.A - value;

        Reg.HL++;
        Reg.BC--;

        WriteFlag(Flags.S, (result & 0x80) != 0);
        WriteFlag(Flags.Z, (result & 0xFF) == 0);
        WriteFlag(Flags.H, (Reg.A & 0x0F) < (value & 0x0F));
        WriteFlag(Flags.P, Reg.BC != 0);
        SetFlag(Flags.N);

        Reg.PC += 2;
        return 16;
    }

    private int Op_CPIR() // Opcode: ED B1
    {
        int cycles = Op_CPI();
    
        if (Reg.BC != 0 && !GetFlag(Flags.Z))
        {
            Reg.PC -= 2;
            return 21;
        }
    
        return 16;
    }

    private int Op_CPD() // ED A9
    {
        byte value = _bus.ReadByte(Reg.HL);
        int result = Reg.A - value;

        Reg.HL--;
        Reg.BC--;

        WriteFlag(Flags.S, (result & 0x80) != 0);
        WriteFlag(Flags.Z, (result & 0xFF) == 0);
        WriteFlag(Flags.H, (Reg.A & 0x0F) < (value & 0x0F));
        WriteFlag(Flags.P, Reg.BC != 0);
        SetFlag(Flags.N);

        Reg.PC += 2;
        return 16;
    }

    private int Op_CPDR() // Opcode: ED B9
    {
        int cycles = Op_CPD();
    
        if (Reg.BC != 0 && !GetFlag(Flags.Z))
        {
            Reg.PC -= 2;
            return 21;
        }
    
        return 16;
    }

    private int Op_RLD()
    {
        // save off bits that are moving
        byte lower4ofA = (byte)(Reg.A & 0x0F);
        byte upper4ofHL = (byte)((_bus.ReadByte(Reg.HL) & 0xF0) >> 4);

        byte value = _bus.ReadByte(Reg.HL); // save value
        value <<= 4; // shift it left 4 bits, leaving 0s behind
        value |= lower4ofA; // because lower 4 of value are 0, this sets those 4 to match
        Reg.A &= 0xF0;      // clear lower 4 of Reg.A
        Reg.A |= upper4ofHL; // same as above
        _bus.WriteByte(Reg.HL, value);
        
        SetSZFlags(Reg.A);
        ClearFlag(Flags.H | Flags.N);
        SetParity(Reg.A);
        
        Reg.PC += 2;
        return 18;
    }

    private int Op_RRD()
    {
        // save off bits that are moving
        byte lower4ofA = (byte)(Reg.A & 0x0F);
        byte lower4ofHL = (byte)(_bus.ReadByte(Reg.HL) & 0x0F);

        byte value = _bus.ReadByte(Reg.HL); // save value
        value >>= 4; // shift it right 4 bits, leaving 0s behind
        value |= (byte)(lower4ofA << 4); // because upper 4 of value are 0, this sets those 4 to match
        Reg.A &= 0xF0;      // clear lower 4 of Reg.A
        Reg.A |= lower4ofHL; // same as above
        _bus.WriteByte(Reg.HL, value);
        
        SetSZFlags(Reg.A);
        ClearFlag(Flags.H | Flags.N);
        SetParity(Reg.A);
        
        Reg.PC += 2;
        return 18;
    }
    
    private int Op_ED_LD_HL_ptrNN() // Opcode: ED 6B
    {
        ushort addr = ImmediateWord(prefixed:true);
        Reg.L = _bus.ReadByte(addr);
        Reg.H = _bus.ReadByte((ushort)(addr + 1));
        Reg.PC += 4;
        return 20;
    }

    private int Op_ED_LD_ptrNN_HL() // Opcode: ED 63
    {
        ushort address = ImmediateWord(prefixed:true);
        _bus.WriteByte(address, Reg.L);
        _bus.WriteByte((ushort)(address + 1), Reg.H);
        Reg.PC += 4;
        return 20;
    }
}