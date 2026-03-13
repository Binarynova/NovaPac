using Reg = Registers;

public partial class Z80
{
    private static int Op_NOP() // Opcode: 00
    {
        Reg.PC += 1;
        return 4;
    }

    private int Op_HALT() // Opcode: 76
    {
        _halted = true;
        return 4;
    }

    private int Op_DI() // Opcode: F3
    {
        _iff1 = false;
        Reg.PC += 1;
        return 4;
    }

    private int Op_EI() // Opcode: FB
    {
        EI_Pending = true;
        Reg.PC += 1;
        return 4;
    }

    private static int Op_EXX() // Opcode: D9
    {
        (Reg.BC, Reg.DE, Reg.HL, Reg.BC2, Reg.DE2, Reg.HL2) = (Reg.BC2, Reg.DE2, Reg.HL2, Reg.BC, Reg.DE, Reg.HL);

        Reg.PC += 1;
        return 4;
    }

    private static int Op_EX_DE_HL() // Opcode: EB
    {
        (Reg.DE, Reg.HL) = (Reg.HL, Reg.DE); // tuples from .NET 7 allow swapping values without a temp var

        Reg.PC += 1;
        return 4;
    }

    private int Op_OUT_ptrn_A() // Opcode: D3
    {
        byte n = ReadImmediateByte();
        ushort port = LEWord(Reg.I, n);
        WritePort(port, Reg.A);
        Reg.PC += 2;
        return 11;
    }
}