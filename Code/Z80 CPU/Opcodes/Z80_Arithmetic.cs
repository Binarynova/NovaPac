using Reg = Registers;

public partial class Z80Cpu
{
    private static int Op_ADD_HL(ushort registerPair) // Opcodes: 09 19 29 39
    {
        Reg.WZ = (ushort)(Reg.HL + 1);
        
        Reg.HL = ADDWord(Reg.HL, registerPair);
        
        Reg.P = 0;
        Reg.Q = Reg.F;
        
        Reg.PC += 1;
        return 11;
    }

    private static int Op_ADD_A(byte register) // Opcodes: 80 81 82 83 84 85 87
    {
        Reg.A = ADD(Reg.A, register);

        Reg.PC += 1;
        return 4;
    }

    private int Op_ADD_A_ptrHL() // Opcode: 86
    {
        Reg.A = ADD(Reg.A, _machine.ReadByte(Reg.HL));
        Reg.PC += 1;
        return 7;
    }

    private int Op_ADD_A_n() // Opcode: C6
    {
        Reg.A = ADD(Reg.A, ImmediateByte());
        Reg.PC += 2;
        return 7;
    }

    private static int Op_ADC_A(byte register) // Opcodes: 88 89 8A 8B 8C 8D 8F
    {
        Reg.A = ADC(Reg.A, register);
        Reg.PC += 1;
        return 4;
    }

    private int Op_ADC_A_ptrHL() // Opcode: 8E
    {
        Reg.A = ADC(Reg.A, _machine.ReadByte(Reg.HL));
        Reg.PC += 1;
        return 7;
    }
    
    private int Op_ADC_A_n() // Opcode: CE
    {
        Reg.A = ADC(Reg.A, ImmediateByte());
        Reg.PC += 2;
        return 7;
    }

    private static int Op_SUB(byte register) // Opcodes: 90 91 92 93 94 95 97
    {
        Reg.A = SUB(Reg.A, register);
        Reg.PC += 1;
        return 4;
    }

    private int Op_SUB_A_ptrHL() // Opcode: 96
    {
        Reg.A = SUB(Reg.A, _machine.ReadByte(Reg.HL));
        Reg.PC += 1;
        return 7;
    }

    private int Op_SUB_n() // Opcode: D6
    {
        byte value = ImmediateByte();
        Reg.A = SUB(Reg.A, value);
        Reg.PC += 2;
        return 7;
    }

    private static int Op_SBC(byte register) // Opcodes: 98 99 9A 9B 9C 9D 9F
    {
        Reg.A = SBC(Reg.A, register);
        Reg.PC += 1;
        return 4;
    }

    private int Op_SBC_A_ptrHL() // Opcode: 9E
    {
        Reg.A = SBC(Reg.A, _machine.ReadByte(Reg.HL));
        Reg.PC += 1;
        return 7;
    }
    
    private int Op_SBC_n() // Opcode: DE
    {
        byte value = ImmediateByte();
        Reg.A = SBC(Reg.A, value);
        Reg.PC += 2;
        return 7;
    }

    private static int Op_INC_r(ref byte register) // Opcodes: 04 0C 14 1C 24 2C
    {
        SetIncFlags(register);
        register += 1;
        Reg.PC += 1;
        return 4;
    }
    
    private static int Op_INC_BC() // Opcode: 03
    {
        Reg.BC++;
        
        Reg.P = Reg.Q = 0;
        
        Reg.PC += 1;
        return 6;
    }

    private static int Op_INC_DE() // Opcode: 13
    {
        Reg.DE++;
        
        Reg.P = Reg.Q = 0;

        Reg.PC += 1;
        return 6;
    }

    private static int Op_INC_HL() // Opcode: 23
    {
        Reg.HL++;
        
        Reg.P = Reg.Q = 0;

        Reg.PC += 1;
        return 6;
    }

    private static int Op_INC_SP() // Opcode: 33
    {
        Reg.SP++;
        
        Reg.P = Reg.Q = 0;

        Reg.PC += 1;
        return 6;
    }

    private int Op_INC_ptrHL() // Opcode: 34
    {
        byte target = _machine.ReadByte(Reg.HL);
        if ((target & 0x0F) == 0x0F) // Opcode: if lower nibble is F, half-carry will occur when adding 1
            SetFlag(Flags.H);
        else
            ClearFlag(Flags.H);

        CheckINCOverflow(target);
        target += 1;

        SetSZFlags(target);
        ClearFlag(Flags.N);
        Reg.PC += 1;

        _machine.WriteByte(Reg.HL, target);
        return 11;
    }

    private static int Op_DEC_r(ref byte register) // Opcodes: 05 0D 15 1D 25 2D
    {
        SetDecFlags(register);
        register--;
        Reg.PC += 1;
        return 4;
    }

    private static int Op_DEC_BC() // Opcode: 0B
    {
        Reg.BC--;

        Reg.P = Reg.Q = 0;
        Reg.PC += 1;
        return 6;
    }
    
    private static int Op_DEC_DE() // Opcode: 1B
    {
        Reg.DE--;
        
        Reg.P = Reg.Q = 0;
        Reg.PC += 1;
        return 6;
    }
    
    private static int Op_DEC_HL() // Opcode: 2B
    {
        Reg.HL--;
        
        Reg.P = Reg.Q = 0;
        Reg.PC += 1;
        return 6;
    }
    
    private static int Op_DEC_SP() // Opcode: 3B
    {
        Reg.SP--;
        
        Reg.P = Reg.Q = 0;
        Reg.PC += 1;
        return 6;
    }

    private int Op_DEC_ptrHL() // Opcode: 35
    {
        byte value = _machine.ReadByte(Reg.HL);
        WriteFlag(Flags.H, (value & 0x0F) == 0);

        CheckDECOverflow(value);
        value--;
        _machine.WriteByte(Reg.HL, value);

        SetFlag(Flags.N);
        SetSZFlags(value);

        Reg.PC += 1;
        return 11;
    }

    private int Op_CP(byte register) // Opcodes: B8 B9 BA BB BC BD BF
    {
        InternalCP(register); ;

        Reg.PC += 1;
        return 4;
    }
    
    private int Op_CP_n() // Opcode: FE
    {
        byte n = ImmediateByte();
        InternalCP(n);

        Reg.PC += 2;
        return 7;
    }

    private int Op_CP_ptrHL() // Opcode: BE
    {
        byte value = _machine.ReadByte(Reg.HL);
        InternalCP(value);

        Reg.PC += 1;
        return 7;
    }

    #region HelperMethods

    private static byte ADD(byte acc, byte value)
    {
        ushort sum = (ushort)(acc + value);
        byte result = (byte)sum;

        WriteFlag(Flags.C, sum > 0xFF);
        WriteFlag(Flags.H, (acc & 0x0F) + (value & 0x0F) > 0x0F);
        WriteFlag(Flags.P, ((acc ^ result) & (value ^ result) & 0x80) != 0);
        ClearFlag(Flags.N);
        SetSZFlags(result);

        return (byte)sum;
    }

    private static ushort ADDWord(ushort acc, ushort value)
    {
        uint sum = (uint)(acc + value);

        WriteFlag(Flags.C, sum > 0xFFFF);
        WriteFlag(Flags.H, (acc & 0x0FFF) + (value & 0x0FFF) > 0x0FFF);
        ClearFlag(Flags.N);
        
        byte highByte = (byte)(sum >> 8);
        Reg.F = (byte)((Reg.F & 0xD7) | (highByte & 0x28));
        
        return (ushort)sum;
    }

    private static byte SUB(byte acc, byte value)
    {
        byte result = (byte)(acc - value);

        WriteFlag(Flags.C, acc < value);
        WriteFlag(Flags.H, ((acc ^ value ^ result) & 0x10) != 0);
        WriteFlag(Flags.P, ((acc ^ value) & (acc ^ result) & 0x80) != 0);
        SetFlag(Flags.N);
        SetSZFlags(result);

        return result;
    }

    private static byte ADC(byte acc, byte value)
    {
        int carry = GetFlag(Flags.C) ? 1 : 0;
        ushort sum = (ushort)(acc + value + carry);
        byte result = (byte)sum;

        WriteFlag(Flags.C, sum > 0xFF);
        WriteFlag(Flags.H, (acc & 0x0F) + (value & 0x0F) + carry > 0x0F);
        WriteFlag(Flags.P, ((acc ^ result) & (value ^ result) & 0x80) != 0);
        ClearFlag(Flags.N);
        SetSZFlags(result);

        return (byte)sum;
    }

    private static byte SBC(byte acc, byte value)
    {
        int carry = GetFlag(Flags.C) ? 1 : 0;
    
        // Perform math in 'int' to preserve the negative sign for the Carry check
        int fullResult = acc - value - carry;
        byte result = (byte)fullResult;

        WriteFlag(Flags.C, fullResult < 0);
        WriteFlag(Flags.H, (acc & 0x0F) < (value & 0x0F) + carry);
        WriteFlag(Flags.P, ((acc ^ value) & (acc ^ result) & 0x80) != 0);
        SetFlag(Flags.N);
        SetSZFlags(result);

        return result;
    }

    private static ushort SBCWord(ushort acc, ushort value)
    {
        int carry = GetFlag(Flags.C) ? 1 : 0;
        int fullResult = acc - value - carry;
        ushort result = (ushort)fullResult;

        WriteFlag(Flags.C, fullResult < 0);
        WriteFlag(Flags.H, (acc & 0x0FFF) < (value & 0x0FFF) + carry);
        SetFlag(Flags.N);

        bool overflow = ((acc ^ value) & (acc ^ result) & 0x8000) != 0;
        WriteFlag(Flags.P, overflow);
        WriteFlag(Flags.S, (result & 0x8000) != 0);
        WriteFlag(Flags.Z, result == 0);

        return result;
    }

    private static void CheckINCOverflow(byte value)
    {
        if (value == 0x7F)
            SetFlag(Flags.P);
        else
            ClearFlag(Flags.P);
    }

    private static void CheckDECOverflow(byte value)
    {
        if (value == 0x80)
            SetFlag(Flags.P);
        else
            ClearFlag(Flags.P);
    }

    private static void SetIncFlags(byte target)
    {
        byte result = (byte)(target + 1);
        CheckINCOverflow(target);
        
        WriteFlag(Flags.H, (target & 0x0F) == 0x0F);
        ClearFlag(Flags.N);
        SetSZFlags(result);

        Reg.P = 0;
        Reg.F = (byte)((Reg.F & 0xD7) | (result & 0x28));
        Reg.Q = Reg.F;
        
        ClearFlag(Flags.N);
    }

    private static void SetDecFlags(byte target)
    {
        byte original = target;
        target--;
    
        WriteFlag(Flags.H, (original & 0x0F) == 0x0);
        WriteFlag(Flags.P, original == 0x80);
        SetFlag(Flags.N);
        WriteFlag(Flags.S, (target & 0x80) != 0);
        WriteFlag(Flags.Z, target == 0);
        
        Reg.P = 0;
        Reg.F = (byte)((Reg.F & 0xD7) | (target & 0x28));
        Reg.Q = Reg.F;
    }
    
    private void InternalCP(byte val)
    {
        int res = Reg.A - val;
    
        WriteFlag(Flags.S, (res & 0x80) != 0);
        WriteFlag(Flags.Z, (res & 0xFF) == 0);
        WriteFlag(Flags.H, (Reg.A & 0x0F) < (val & 0x0F));
        WriteFlag(Flags.P, ((Reg.A ^ val) & (Reg.A ^ (res & 0xFF)) & 0x80) != 0);
        SetFlag(Flags.N); // Always 1 for CP
        WriteFlag(Flags.C, Reg.A < val);
    }

    #endregion
}