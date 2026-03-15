using Reg = Registers;

public partial class Z80
{
    #region ADD

    private int Op_ADD_A(byte register)
    {
        Reg.A = ADD(Reg.A, register);
        Reg.PC += 1;
        return 4;
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

    private int Op_ADD_A_n() // Opcode: C6
    {
        Reg.A = ADD(Reg.A, ReadImmediateByte());
        Reg.PC += 2;
        return 7;
    }

    #endregion

    #region ADC

    private static int Op_ADC_A_A() // Opcode: 8F
    {
        Reg.A = ADC(Reg.A, Reg.A);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_ADC_A_B() // Opcode: 88
    {
        Reg.A = ADC(Reg.A, Reg.B);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_ADC_A_C() // Opcode: 89
    {
        Reg.A = ADC(Reg.A, Reg.C);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_ADC_A_D() // Opcode: 8A
    {
        Reg.A = ADC(Reg.A, Reg.D);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_ADC_A_E() // Opcode: 8B
    {
        Reg.A = ADC(Reg.A, Reg.E);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_ADC_A_H() // Opcode: 8C
    {
        Reg.A = ADC(Reg.A, Reg.H);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_ADC_A_L() // Opcode: 8D
    {
        Reg.A = ADC(Reg.A, Reg.L);
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

    private static int Op_SUB_A_A() // Opcode: 97
    {
        Reg.A = SUB(Reg.A, Reg.A);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_SUB_A_B() // Opcode: 90
    {
        Reg.A = SUB(Reg.A, Reg.B);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_SUB_A_C() // Opcode: 91
    {
        Reg.A = SUB(Reg.A, Reg.C);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_SUB_A_D() // Opcode: 92
    {
        Reg.A = SUB(Reg.A, Reg.D);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_SUB_A_E() // Opcode: 93
    {
        Reg.A = SUB(Reg.A, Reg.E);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_SUB_A_H() // Opcode: 94
    {
        Reg.A = SUB(Reg.A, Reg.H);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_SUB_A_L() // Opcode: 95
    {
        Reg.A = SUB(Reg.A, Reg.L);
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

    private static int Op_SBC_A_A() // Opcode: 9F
    {
        Reg.A = SBC(Reg.A, Reg.A);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_SBC_A_B() // Opcode: 98
    {
        Reg.A = SBC(Reg.A, Reg.B);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_SBC_A_C() // Opcode: 99
    {
        Reg.A = SBC(Reg.A, Reg.C);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_SBC_A_D() // Opcode: 9A
    {
        Reg.A = SBC(Reg.A, Reg.D);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_SBC_A_E()  // Opcode: 9B
    {
        Reg.A = SBC(Reg.A, Reg.E);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_SBC_A_H() // Opcode: 9C
    {
        Reg.A = SBC(Reg.A, Reg.H);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_SBC_A_L() // Opcode: 9D
    {
        Reg.A = SBC(Reg.A, Reg.L);
        Reg.PC += 1;
        return 4;
    }

    private int Op_SBC_A_ptrHL() // Opcode: 9E
    {
        Reg.A = SBC(Reg.A, machine.ReadByte(Reg.HL));
        Reg.PC += 1;
        return 7;
    }

    #endregion

    #region INC

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

    private static int Op_INC_A() // Opcode: 3C
    {
        SetIncFlags(Reg.A);
        Reg.A += 1;
        Reg.PC += 1;
        return 4;
    }

    private static int Op_INC_B() // Opcode: 04
    {
        SetIncFlags(Reg.B);
        Reg.B += 1;
        Reg.PC += 1;
        return 4;
    }

    private static int Op_INC_C() // Opcode: 0C
    {
        SetIncFlags(Reg.C);
        Reg.C += 1;
        Reg.PC += 1;
        return 4;
    }

    private static int Op_INC_D() // Opcode: 14
    {
        SetIncFlags(Reg.D);
        Reg.D += 1;
        Reg.PC += 1;
        return 4;
    }

    private static int Op_INC_E() // Opcode: 1C
    {
        SetIncFlags(Reg.E);
        Reg.E += 1;
        Reg.PC += 1;
        return 4;
    }

    private static int Op_INC_H() // Opcode: 24
    {
        SetIncFlags(Reg.H);
        Reg.H += 1;
        Reg.PC += 1;
        return 4;
    }

    private static int Op_INC_L() // Opcode: 2C
    {
        SetIncFlags(Reg.L);
        Reg.L += 1;
        Reg.PC += 1;
        return 4;
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

        machine.WriteByte(Reg.HL, target, Reg.PC);
        return 11;
    }

    #endregion

    #region DEC

    private static int Op_DEC_Reg(ref byte register)
    {
        SetDecFlags(register);
        register--;
        Reg.PC += 1;
        return 4;
    }
    
    private static int Op_DEC_SP() // Opcode: 3B
    {
        Reg.SP--;
        Reg.PC += 1;
        return 6;
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
    
    private static int Op_DEC_HL() // Opcode: 1B
    {
        Reg.HL--;
        Reg.PC += 1;
        return 6;
    }

    private int Op_DEC_ptrHL() // Opcode: 35
    {
        byte value = machine.ReadByte(Reg.HL);
        WriteFlag(Flags.H, (value & 0x0F) == 0);

        value--;
        machine.WriteByte(Reg.HL, value, Reg.PC);

        CheckDECOverflow(value);
        SetFlag(Flags.N);
        SetSZFlags(value);

        Reg.PC += 1;
        return 11;
    }

    #endregion

    #region CP

    private static int Op_CP(byte register)
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