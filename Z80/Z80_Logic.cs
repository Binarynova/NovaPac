using Reg = Registers;

public partial class Z80
{
    #region AND

    private int Op_AND_A() // Opcode: A7
    {
        AND(Reg.A);
        Reg.PC += 1;
        return 4;
    }

    private int Op_AND_B() // Opcode: A0
    {
        AND(Reg.B);
        Reg.PC += 1;
        return 4;
    }

    private int Op_AND_C() // Opcode: A1
    {
        AND(Reg.C);
        Reg.PC += 1;
        return 4;
    }

    private int Op_AND_D() // Opcode: A2
    {
        AND(Reg.D);
        Reg.PC += 1;
        return 4;
    }

    private int Op_AND_E() // Opcode: A3
    {
        AND(Reg.E);
        Reg.PC += 1;
        return 4;
    }

    private int Op_AND_H() // Opcode: A4
    {
        AND(Reg.H);
        Reg.PC += 1;
        return 4;
    }

    private int Op_AND_L() // Opcode: A5
    {
        AND(Reg.L);
        Reg.PC += 1;
        return 4;
    }

    private int Op_AND_ptrHL() // Opcode: A6
    {
        AND(machine.ReadByte(Reg.HL));
        Reg.PC += 1;
        return 7;
    }

    private int Op_AND_n() // Opcode: E6
    {
        byte value = ReadImmediateByte();
        AND(value);
        Reg.PC += 2;
        return 7;
    }

    #endregion AND

    #region OR

    private int Op_OR_A() // Opcode: B7
    {
        OR(Reg.A);
        Reg.PC += 1;
        return 4;
    }

    private int Op_OR_B() // Opcode: B0
    {
        OR(Reg.B);
        Reg.PC += 1;
        return 4;
    }

    private int Op_OR_C() // Opcode: B1
    {
        OR(Reg.C);
        Reg.PC += 1;
        return 4;
    }

    private int Op_OR_D() // Opcode: B2
    {
        OR(Reg.D);
        Reg.PC += 1;
        return 4;
    }

    private int Op_OR_E() // Opcode: B3
    {
        OR(Reg.E);
        Reg.PC += 1;
        return 4;
    }

    private int Op_OR_H() // Opcode: B4
    {
        OR(Reg.H);
        Reg.PC += 1;
        return 4;
    }

    private int Op_OR_L() // Opcode: B5
    {
        OR(Reg.L);
        Reg.PC += 1;
        return 4;
    }

    private int Op_OR_ptrHL() // Opcode: B6
    {
        OR(machine.ReadByte(Reg.HL));
        Reg.PC += 1;
        return 7;
    }

    #endregion

    #region XOR

    private int Op_XOR_A_A() // Opcode: AF
    {
        // Opcode: xor the accumulator with itself
        Reg.A = (byte)(Reg.A ^ Reg.A);

        SetSZFlags(Reg.A);
        ClearFlag(Flags.C | Flags.H | Flags.N);
        SetParity(Reg.A);

        Reg.PC += 1;
        return 4;
    }

    private int Op_XOR_A_n() // Opcode: EE
    {
        // Opcode: xor the accumulator with itself
        Reg.A = (byte)(Reg.A ^ ReadImmediateByte());

        SetSZFlags(Reg.A);
        ClearFlag(Flags.C | Flags.H | Flags.N);
        SetParity(Reg.A);

        Reg.PC += 2;
        return 7;
    }

    #endregion

    private static int Op_CCF() // Opcode: 3F
    {
        WriteFlag(Flags.H, GetFlag(Flags.C));
        ClearFlag(Flags.N);
        ToggleFlag(Flags.C);

        Reg.PC += 1;
        return 4;
    }

    #region Helper Methods

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

    #endregion
}