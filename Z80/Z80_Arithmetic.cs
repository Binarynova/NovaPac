using Reg = Registers;

public partial class Z80
{
    #region ADD

    private static int Op_ADD_A(byte register) // Opcodes: 80 81 82 83 84 85 87
    {
        Reg.A = ADD(Reg.A, register);
        Reg.PC += 1;
        return 4;
    }

    private int Op_ADD_A_n() // Opcode: C6
    {
        Reg.A = ADD(Reg.A, ReadImmediateByte());
        Reg.PC += 2;
        return 7;
    }

    private int Op_ADD_A_ptrHL() // Opcode: 86
    {
        Reg.A = ADD(Reg.A, machine.ReadByte(Reg.HL));
        Reg.PC += 1;
        return 7;
    }

    private static int Op_ADD_HL_BC() // Opcode: 09
    {
        Reg.HL = ADDWord(Reg.HL, Reg.BC);
        Reg.PC += 1;
        return 11;
    }

    private static int Op_ADD_HL_DE() // Opcode: 19
    {
        Reg.HL = ADDWord(Reg.HL, Reg.DE);
        Reg.PC += 1;
        return 11;
    }

    private static int Op_ADD_HL_HL() // Opcode: 29
    {
        Reg.HL = ADDWord(Reg.HL, Reg.HL);
        Reg.PC += 1;
        return 11;
    }
    
    private static int Op_ADD_HL_SP() // Opcode: 39
    {
        Reg.HL = ADDWord(Reg.HL, Reg.SP);
        Reg.PC += 1;
        return 11;
    }

    #endregion

    #region ADC

    private static int Op_ADC_A(byte register) // Opcodes: 88 89 8A 8B 8C 8D 8F
    {
        Reg.A = ADC(Reg.A, register);
        Reg.PC += 1;
        return 4;
    }

    private int Op_ADC_A_ptrHL() // Opcode: 8E
    {
        Reg.A = ADC(Reg.A, machine.ReadByte(Reg.HL));
        Reg.PC += 1;
        return 7;
    }
    
    private int Op_ADC_A_n() // Opcode: CE
    {
        Reg.A = ADC(Reg.A, ReadImmediateByte());
        Reg.PC += 2;
        return 7;
    }

    #endregion

    #region SUB

    private static int Op_SUB(byte register) // Opcodes: 90 91 92 93 94 95 97
    {
        Reg.A = SUB(Reg.A, register);
        Reg.PC += 1;
        return 4;
    }

    private int Op_SUB_A_ptrHL() // Opcode: 96
    {
        Reg.A = SUB(Reg.A, machine.ReadByte(Reg.HL));
        Reg.PC += 1;
        return 7;
    }

    private int Op_SUB_n() // Opcode: D6
    {
        byte value = ReadImmediateByte();
        Reg.A = SUB(Reg.A, value);
        Reg.PC += 2;
        return 7;
    }

    #endregion

    #region SBC

    private static int Op_SBC(byte register) // Opcodes: 98 99 9A 9B 9C 9D 9F
    {
        Reg.A = SBC(Reg.A, register);
        Reg.PC += 1;
        return 4;
    }

    private int Op_SBC_A_ptrHL() // Opcode: 9E
    {
        Reg.A = SBC(Reg.A, machine.ReadByte(Reg.HL));
        Reg.PC += 1;
        return 7;
    }
    
    private int Op_SBC_n() // Opcode: DE
    {
        byte value = ReadImmediateByte();
        Reg.A = SBC(Reg.A, value);
        Reg.PC += 2;
        return 7;
    }

    #endregion

    #region INC

    private static int Op_INC(ref byte register) // Opcodes: 04 0C 14 1C 24 2C
    {
        SetIncFlags(register);
        register += 1;
        Reg.PC += 1;
        return 4;
    }
    
    private static int Op_INC_BC() // Opcode: 03
    {
        Reg.BC++;
        Reg.PC += 1;
        return 6;
    }

    private static int Op_INC_DE() // Opcode: 13
    {
        Reg.DE++;
        Reg.PC += 1;
        return 6;
    }

    private static int Op_INC_HL() // Opcode: 23
    {
        Reg.HL++;
        Reg.PC += 1;
        return 6;
    }

    private static int Op_INC_SP() // Opcode: 33
    {
        Reg.SP++;
        Reg.PC += 1;
        return 6;
    }

    private int Op_INC_ptrHL() // Opcode: 34
    {
        byte target = machine.ReadByte(Reg.HL);
        if ((target & 0x0F) == 0x0F) // Opcode: if lower nibble is F, half-carry will occur when adding 1
            SetFlag(Flags.H);
        else
            ClearFlag(Flags.H);

        target += 1;

        SetSZFlags(target);
        ClearFlag(Flags.N);
        CheckINCOverflow(target);
        Reg.PC += 1;

        machine.WriteByte(Reg.HL, target);
        return 11;
    }

    #endregion

    #region DEC

    private static int Op_DEC(ref byte register) // Opcodes: 05 0D 15 1D 25 2D
    {
        SetDecFlags(register);
        register--;
        Reg.PC += 1;
        return 4;
    }

    private static int Op_DEC_BC() // Opcode: 0B
    {
        Reg.BC--;
        Reg.PC += 1;
        return 6;
    }
    
    private static int Op_DEC_DE() // Opcode: 1B
    {
        Reg.DE--;
        Reg.PC += 1;
        return 6;
    }
    
    private static int Op_DEC_HL() // Opcode: 2B
    {
        Reg.HL--;
        Reg.PC += 1;
        return 6;
    }
    
    private static int Op_DEC_SP() // Opcode: 3B
    {
        Reg.SP--;
        Reg.PC += 1;
        return 6;
    }

    private int Op_DEC_ptrHL() // Opcode: 35
    {
        byte value = machine.ReadByte(Reg.HL);
        WriteFlag(Flags.H, (value & 0x0F) == 0);

        value--;
        machine.WriteByte(Reg.HL, value);

        CheckDECOverflow(value);
        SetFlag(Flags.N);
        SetSZFlags(value);

        Reg.PC += 1;
        return 11;
    }

    #endregion

    #region CP

    private static int Op_CP(byte register) // Opcodes: B8 B9 BA BB BC BD BF
    {
        byte result = (byte)(Reg.A - register);

        WriteFlag(Flags.C, Reg.A < register);
        WriteFlag(Flags.H, (Reg.A & 0x0F) < (register & 0x0F));
        SetFlag(Flags.N);
        SetSZFlags(result);
        WriteFlag(Flags.P, ((Reg.A ^ register) & (Reg.A ^ result) & 0x80) != 0);

        Reg.PC += 1;
        return 4;
    }
    
    private int Op_CP_n() // Opcode: FE
    {
        byte n = ReadImmediateByte();
        byte result = (byte)(Reg.A - n);

        WriteFlag(Flags.C, Reg.A < n);
        WriteFlag(Flags.H, (Reg.A & 0x0F) < (n & 0x0F));
        SetFlag(Flags.N);
        SetSZFlags(result);
        WriteFlag(Flags.P, ((Reg.A ^ n) & (Reg.A ^ result) & 0x80) != 0);

        Reg.PC += 2;
        return 7;
    }

    private int Op_CP_ptrHL() // Opcode: BE
    {
        byte value = machine.ReadByte(Reg.HL);
        byte result = (byte)(Reg.A - value);

        WriteFlag(Flags.C, Reg.A < value);
        WriteFlag(Flags.H, (Reg.A & 0x0F) < (value & 0x0F));
        SetFlag(Flags.N);
        SetSZFlags(result);
        WriteFlag(Flags.P, ((Reg.A ^ value) & (Reg.A ^ result) & 0x80) != 0);

        Reg.PC += 1;
        return 7;
    }

    #endregion

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
        byte result = (byte)(acc - value - carry);

        WriteFlag(Flags.C, acc < value + carry);
        WriteFlag(Flags.H, (acc & 0x0F) < (value & 0x0F) + carry);
        WriteFlag(Flags.P, ((acc ^ value) & (acc ^ result) & 0x80) != 0);
        SetFlag(Flags.N);
        SetSZFlags(result);

        return result;
    }

    private static ushort SBCWord(ushort acc, ushort value)
    {
        int carry = GetFlag(Flags.C) ? 1 : 0;
        ushort result = (ushort)(acc - value - carry);

        WriteFlag(Flags.C, acc < value + carry);
        WriteFlag(Flags.H, (acc & 0x0FFF) < (value & 0x0FFF) + carry);
        SetFlag(Flags.N);

        // Opcode: 16-bit signed overflow detection
        bool overflow = ((acc ^ value) & (acc ^ result) & 0x8000) != 0;
        WriteFlag(Flags.P, overflow);

        // Opcode: S/Z for 16-bit
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
        if (value == 0x7F)
            SetFlag(Flags.P);
        else
            ClearFlag(Flags.P);
    }

    private static void SetIncFlags(byte target)
    {
        if ((target & 0x0F) == 0x0F) // Opcode: if lower nibble is F, half-carry will occur when adding 1
            SetFlag(Flags.H);
        else
            ClearFlag(Flags.H);
        SetSZFlags((byte)(target + 1)); // Opcode: adding one because this needs to be checked AFTER the addition
        ClearFlag(Flags.N);
        CheckINCOverflow(target);
    }

    private static void SetDecFlags(byte target)
    {
        byte result = (byte)(target - 1);

        if ((target & 0x0F) == 0x00)
            SetFlag(Flags.H);
        else
            ClearFlag(Flags.H);

        SetSZFlags(result);

        SetFlag(Flags.N);

        CheckDECOverflow(target);

        WriteFlag(Flags.F5, (result & 0x20) != 0);
        WriteFlag(Flags.F3, (result & 0x08) != 0);
    }

    #endregion
}