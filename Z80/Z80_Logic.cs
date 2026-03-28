using Reg = Registers;

public partial class Z80
{
    #region AND

    private int Op_AND(byte register) // Opcodes: A0 A1 A2 A3 A4 A5 A7
    {
        AND(register);
        Reg.PC += 1;
        return 4;
    }

    private int Op_AND_ptrHL() // Opcode: A6
    {
        AND(_machine.ReadByte(Reg.HL));
        Reg.PC += 1;
        return 7;
    }

    private int Op_AND_n() // Opcode: E6
    {
        byte value = ImmediateByte();
        AND(value);
        Reg.PC += 2;
        return 7;
    }

    #endregion AND

    #region OR

    private int Op_OR(byte register) // Opcodes: B0 B1 B2 B3 B4 B5 B7
    {
        OR(register);
        Reg.PC += 1;
        return 4;
    }

    private int Op_OR_ptrHL() // Opcode: B6
    {
        OR(_machine.ReadByte(Reg.HL));
        Reg.PC += 1;
        return 7;
    }
    
    private int Op_OR_n() // Opcode: F6
    {
        byte value = ImmediateByte();
        OR(value);
        Reg.PC += 2;
        return 7;
    }

    #endregion

    #region XOR

    private int Op_XOR(byte register) // Opcodes: A8 A9 AA AB AC AD AF
    {
        XOR(register);

        Reg.PC += 1;
        return 4;
    }

    private int Op_XOR_A_n() // Opcode: EE
    {
        XOR(ImmediateByte());

        Reg.PC += 2;
        return 7;
    }
    
    private int Op_XOR_A_ptrHL() // Opcode: AE
    {
        XOR(_machine.ReadByte(Reg.HL));

        Reg.PC += 1;
        return 7;
    }

    #endregion

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

    private void XOR(byte value)
    {
        Reg.A ^= value;

        SetSZFlags(Reg.A);
        ClearFlag(Flags.C | Flags.H | Flags.N);
        SetParity(Reg.A);
    }

    #endregion
}