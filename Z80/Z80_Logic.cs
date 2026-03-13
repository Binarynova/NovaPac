using Reg = Registers;

public partial class Z80
{
    private void AND(byte value)
    {
        Reg.A &= value;

        ClearFlag(Flags.C | Flags.N);
        SetFlag(Flags.H);       
        SetSZFlags(Reg.A);
        SetParity(Reg.A);
        
        WriteFlag(Flags.F5, (Reg.A & 0x20) != 0);
        WriteFlag(Flags.F3, (Reg.A & 0x08) != 0);
    }
    private void OR(byte value)
    {
        Reg.A |= value;

        ClearFlag(Flags.C | Flags.N | Flags.H);
        SetSZFlags(Reg.A);
        SetParity(Reg.A);
    }
    
    private int Op_AND_A() { AND(Reg.A); Reg.PC += 1; return 4; }
    private int Op_AND_B() { AND(Reg.B); Reg.PC += 1; return 4; }
    private int Op_AND_C() { AND(Reg.C); Reg.PC += 1; return 4; }
    private int Op_AND_D() { AND(Reg.D); Reg.PC += 1; return 4; }
    private int Op_AND_E() { AND(Reg.E); Reg.PC += 1; return 4; }
    private int Op_AND_H() { AND(Reg.H); Reg.PC += 1; return 4; }
    private int Op_AND_L() { AND(Reg.L); Reg.PC += 1; return 4; }
    private int Op_AND_ptrHL() { AND(machine.ReadByte(Reg.HL)); Reg.PC += 1; return 7; }
    private int Op_AND_n() { byte value = ReadImmediateByte(); AND(value); Reg.PC += 1; return 7; }
    
    private int Op_OR_A() { OR(Reg.A); Reg.PC += 1; return 4; }
    private int Op_OR_B() { OR(Reg.B); Reg.PC += 1; return 4; }
    private int Op_OR_C() { OR(Reg.C); Reg.PC += 1; return 4; }
    private int Op_OR_D() { OR(Reg.D); Reg.PC += 1; return 4; }
    private int Op_OR_E() { OR(Reg.E); Reg.PC += 1; return 4; }
    private int Op_OR_H() { OR(Reg.H); Reg.PC += 1; return 4; }
    private int Op_OR_L() { OR(Reg.L); Reg.PC += 1; return 4; }
    private int Op_OR_ptrHL() { OR(machine.ReadByte(Reg.HL)); Reg.PC += 1; return 7; }
    
    private int Op_XOR_A_A()
    {
        // xor the accumulator with itself
        Reg.A = (byte)(Reg.A ^ Reg.A);

        SetSZFlags(Reg.A);
        ClearFlag(Flags.C | Flags.H | Flags.N);
        SetParity(Reg.A);

        Reg.PC += 1;
        return 4;
    }
    private int Op_XOR_A_n()
    {
        // xor the accumulator with itself
        Reg.A = (byte)(Reg.A ^ ReadImmediateByte());

        SetSZFlags(Reg.A);
        ClearFlag(Flags.C | Flags.H | Flags.N);
        SetParity(Reg.A);

        Reg.PC += 2;
        return 7;
    }
    private static int Op_CCF()
    {
        WriteFlag(Flags.H, GetFlag(Flags.C));
        ClearFlag(Flags.N);
        ToggleFlag(Flags.C);

        Reg.PC += 1;
        return 4;
    }
}