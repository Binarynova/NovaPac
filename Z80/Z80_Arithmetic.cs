using Reg = Registers;

public partial class Z80
{
    private static byte ADD(byte acc, byte value)
    {
        ushort sum = (ushort)(acc + value);
        byte result = (byte)sum;

        WriteFlag(Flags.C, sum > 0xFF);
        WriteFlag(Flags.H, ((acc & 0x0F) + (value & 0x0F)) > 0x0F);                
        ClearFlag(Flags.N);        
        SetSZFlags(result);
        WriteFlag(Flags.P, ((acc ^ result) & (value ^ result) & 0x80) != 0);

        return (byte)sum;
    }
    private static ushort ADDWord(ushort acc, ushort value)
    {
        uint sum = (uint)(acc + value);

        WriteFlag(Flags.C, sum > 0xFFFF);
        WriteFlag(Flags.H, ((acc & 0x0FFF) + (value & 0x0FFF)) > 0x0FFF);                
        ClearFlag(Flags.N);

        return (ushort)sum;
    }
    private static byte SUB(byte acc, byte value)
    {
        byte result = (byte)(acc - value);

        WriteFlag(Flags.C, acc < value);
        WriteFlag(Flags.H, ((acc ^ value ^ result) & 0x10) != 0);
        SetFlag(Flags.N); 
        SetSZFlags(result);
        WriteFlag(Flags.P, ((acc ^ value) & (acc ^ result) & 0x80) != 0);

        return result;
    }
    private static byte ADC(byte acc, byte value)
    {
        int carry = GetFlag(Flags.C) ? 1 : 0;
        ushort sum = (ushort)(acc + value + carry);
        byte result = (byte)sum;

        WriteFlag(Flags.C, sum > 0xFF);
        WriteFlag(Flags.H, ((acc & 0x0F) + (value & 0x0F) + carry) > 0x0F);                
        ClearFlag(Flags.N);        
        SetSZFlags(result);
        WriteFlag(Flags.P, ((acc ^ result) & (value ^ result) & 0x80) != 0);

        return (byte)sum;        
    }
    private static byte SBC(byte acc, byte value)
    {
        int carry = GetFlag(Flags.C) ? 1 : 0;
        byte result = (byte)(acc - value - carry);

        WriteFlag(Flags.C, acc < (value + carry));
        WriteFlag(Flags.H, (acc & 0x0F) < ((value & 0x0F) + carry));
        SetFlag(Flags.N); 
        SetSZFlags(result);
        WriteFlag(Flags.P, ((acc ^ value) & (acc ^ result) & 0x80) != 0);

        return result;
    }
    private static ushort SBCWord(ushort acc, ushort value)
    {
        int carry = GetFlag(Flags.C) ? 1 : 0;
        ushort result = (ushort)(acc - value - carry);

        WriteFlag(Flags.C, acc < (value + carry));
        WriteFlag(Flags.H, (acc & 0x0FFF) < ((value & 0x0FFF) + carry));
        SetFlag(Flags.N);

        // 16-bit signed overflow detection
        bool overflow = ((acc ^ value) & (acc ^ result) & 0x8000) != 0;
        WriteFlag(Flags.P, overflow);

        // S/Z for 16-bit
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
    
    private int Op_ADD_A_n()
    {
        Reg.A = ADD(Reg.A, ReadImmediateByte());
        Reg.PC += 2;
        return 7;
    }

    private static int Op_ADD_A_A() { Reg.A = ADD(Reg.A, Reg.A); Reg.PC += 1; return 4; }
    private static int Op_ADD_A_B() { Reg.A = ADD(Reg.A, Reg.B); Reg.PC += 1; return 4; }
    private static int Op_ADD_A_C() { Reg.A = ADD(Reg.A, Reg.C); Reg.PC += 1; return 4; }
    private static int Op_ADD_A_D() { Reg.A = ADD(Reg.A, Reg.D); Reg.PC += 1; return 4; }
    private static int Op_ADD_A_E() { Reg.A = ADD(Reg.A, Reg.E); Reg.PC += 1; return 4; }
    private static int Op_ADD_A_H() { Reg.A = ADD(Reg.A, Reg.H); Reg.PC += 1; return 4; }
    private static int Op_ADD_A_L() { Reg.A = ADD(Reg.A, Reg.L); Reg.PC += 1; return 4; }
    private int Op_ADD_A_ptrHL() { Reg.A = ADD(Reg.A, machine.ReadByte(Reg.HL)); Reg.PC += 1; return 7; }
    private static int Op_ADD_HL_DE() { Reg.HL = ADDWord(Reg.HL, Reg.DE); Reg.PC += 1; return 11; }

    private static int Op_ADC_A_A() { Reg.A = ADC(Reg.A, Reg.A); Reg.PC += 1; return 4; }
    private static int Op_ADC_A_B() { Reg.A = ADC(Reg.A, Reg.B); Reg.PC += 1; return 4; }
    private static int Op_ADC_A_C() { Reg.A = ADC(Reg.A, Reg.C); Reg.PC += 1; return 4; }
    private static int Op_ADC_A_D() { Reg.A = ADC(Reg.A, Reg.D); Reg.PC += 1; return 4; }
    private static int Op_ADC_A_E() { Reg.A = ADC(Reg.A, Reg.E); Reg.PC += 1; return 4; }
    private static int Op_ADC_A_H() { Reg.A = ADC(Reg.A, Reg.H); Reg.PC += 1; return 4; }
    private static int Op_ADC_A_L() { Reg.A = ADC(Reg.A, Reg.L); Reg.PC += 1; return 4; }
    private int Op_ADC_A_ptrHL() { Reg.A = ADC(Reg.A, machine.ReadByte(Reg.HL)); Reg.PC += 1; return 7; }

    private static int Op_SUB_A_A() { Reg.A = SUB(Reg.A, Reg.A); Reg.PC += 1; return 4; }
    private static int Op_SUB_A_B() { Reg.A = SUB(Reg.A, Reg.B); Reg.PC += 1; return 4; }
    private static int Op_SUB_A_C() { Reg.A = SUB(Reg.A, Reg.C); Reg.PC += 1; return 4; }
    private static int Op_SUB_A_D() { Reg.A = SUB(Reg.A, Reg.D); Reg.PC += 1; return 4; }
    private static int Op_SUB_A_E() { Reg.A = SUB(Reg.A, Reg.E); Reg.PC += 1; return 4; }
    private static int Op_SUB_A_H() { Reg.A = SUB(Reg.A, Reg.H); Reg.PC += 1; return 4; }
    private static int Op_SUB_A_L() { Reg.A = SUB(Reg.A, Reg.L); Reg.PC += 1; return 4; }
    private int Op_SUB_A_ptrHL() { Reg.A = SUB(Reg.A, machine.ReadByte(Reg.HL)); Reg.PC += 1; return 7; }
    private int Op_SUB_n()
    {
        byte value = ReadImmediateByte();
        Reg.A = SUB(Reg.A, value);
        Reg.PC += 2;
        return 7;
    }
    private static int Op_SBC_A_A() { Reg.A = SBC(Reg.A, Reg.A); Reg.PC += 1; return 4; }
    private static int Op_SBC_A_B() { Reg.A = SBC(Reg.A, Reg.B); Reg.PC += 1; return 4; }
    private static int Op_SBC_A_C() { Reg.A = SBC(Reg.A, Reg.C); Reg.PC += 1; return 4; }
    private static int Op_SBC_A_D() { Reg.A = SBC(Reg.A, Reg.D); Reg.PC += 1; return 4; }
    private static int Op_SBC_A_E() { Reg.A = SBC(Reg.A, Reg.E); Reg.PC += 1; return 4; }
    private static int Op_SBC_A_H() { Reg.A = SBC(Reg.A, Reg.H); Reg.PC += 1; return 4; }
    private static int Op_SBC_A_L() { Reg.A = SBC(Reg.A, Reg.L); Reg.PC += 1; return 4; }
    private int Op_SBC_A_ptrHL() { Reg.A = SBC(Reg.A, machine.ReadByte(Reg.HL)); Reg.PC += 1; return 7; }

    
    private static void SetIncFlags(byte target)
    {
        if ((target & 0x0F) == 0x0F)  // if lower nibble is F, half-carry will occur when adding 1
            SetFlag(Flags.H);
        else
            ClearFlag(Flags.H);
        SetSZFlags((byte)(target+1)); // adding one because this needs to be checked AFTER the addition
        ClearFlag(Flags.N);
        CheckINCOverflow(target);
    }
    
    
    private static int Op_INC_BC() { Reg.BC++; Reg.PC += 1; return 6; }
    private static int Op_INC_DE() { Reg.DE++; Reg.PC += 1; return 6; }
    private static int Op_INC_HL() { Reg.HL++; Reg.PC += 1; return 6; }
    private static int Op_INC_SP() { Reg.SP++; Reg.PC += 1; return 6; }
    
    private static int Op_INC_A() { SetIncFlags(Reg.A); Reg.A += 1; Reg.PC += 1; return 4; }
    private static int Op_INC_B() { SetIncFlags(Reg.B); Reg.B += 1; Reg.PC += 1; return 4; }
    private static int Op_INC_C() { SetIncFlags(Reg.C); Reg.C += 1; Reg.PC += 1; return 4; }
    private static int Op_INC_D() { SetIncFlags(Reg.D); Reg.D += 1; Reg.PC += 1; return 4; }
    private static int Op_INC_E() { SetIncFlags(Reg.E); Reg.E += 1; Reg.PC += 1; return 4; }
    private static int Op_INC_H() { SetIncFlags(Reg.H); Reg.H += 1; Reg.PC += 1; return 4; }
    private static int Op_INC_L() { SetIncFlags(Reg.L); Reg.L += 1; Reg.PC += 1; return 4; }
    private int Op_INC_ptrHL()
    {
        byte target = machine.ReadByte(Reg.HL);
        if ((target & 0x0F) == 0x0F)  // if lower nibble is F, half-carry will occur when adding 1
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
    private static int Op_DEC_A() { SetDecFlags(Reg.A); Reg.A--; Reg.PC += 1; return 4; }
    private static int Op_DEC_B() { SetDecFlags(Reg.B); Reg.B--; Reg.PC += 1; return 4; }
    private static int Op_DEC_C() { SetDecFlags(Reg.C); Reg.C--; Reg.PC += 1; return 4; }
    private static int Op_DEC_D() { SetDecFlags(Reg.D); Reg.D--; Reg.PC += 1; return 4; }
    private static int Op_DEC_E() { SetDecFlags(Reg.E); Reg.E--; Reg.PC += 1; return 4; }
    private static int Op_DEC_H() { SetDecFlags(Reg.H); Reg.H--; Reg.PC += 1; return 4; }
    private static int Op_DEC_L() { SetDecFlags(Reg.L); Reg.L--; Reg.PC += 1; return 4; }

    private static int Op_DEC_SP() { Reg.SP--; Reg.PC += 1; return 6; }
    private static int Op_DEC_BC()
    {
        Reg.BC--;
        Reg.PC += 1;
        return 6;
    }
    private int Op_DEC_ptrHL()
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
    
    private int Op_CP_n()
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
    private static int Op_CP_C()
    {        
        byte result = (byte)(Reg.A - Reg.C);        

        WriteFlag(Flags.C, Reg.A < Reg.C);
        WriteFlag(Flags.H, (Reg.A & 0x0F) < (Reg.C & 0x0F));
        SetFlag(Flags.N);
        SetSZFlags(result);
        WriteFlag(Flags.P, ((Reg.A ^ Reg.C) & (Reg.A ^ result) & 0x80) != 0);

        Reg.PC += 1;
        return 4;
    }
    private int Op_CP_ptrHL()
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
}