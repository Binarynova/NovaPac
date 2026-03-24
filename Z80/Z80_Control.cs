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
        Reg.PC += 1;
        return 4;
    }

    private int Op_DI() // Opcode: F3
    {
        _iff1 = false;
        _iff2 = false;
        Reg.PC += 1;
        return 4;
    }

    private int Op_EI() // Opcode: FB
    {
        EI_Pending = true;
        _iff1 = true;
        _iff2 = true;
        Reg.PC += 1;
        return 4;
    }

    private static int Op_EXX() // Opcode: D9
    {
        (Reg.BC, Reg.DE, Reg.HL, Reg.BC2, Reg.DE2, Reg.HL2) = (Reg.BC2, Reg.DE2, Reg.HL2, Reg.BC, Reg.DE, Reg.HL);

        Reg.PC += 1;
        return 4;
    }
    
    private static int Op_EX_AF_AF2() // Opcode: 08
    {
        (Reg.AF, Reg.AF2) = (Reg.AF2, Reg.AF); // tuples from .NET 7 allow swapping values without a temp var

        Reg.PC += 1;
        return 4;
    }

    private static int Op_EX_DE_HL() // Opcode: EB
    {
        (Reg.DE, Reg.HL) = (Reg.HL, Reg.DE); // tuples from .NET 7 allow swapping values without a temp var

        Reg.PC += 1;
        return 4;
    }
    
    private int Op_EX_ptrSP_HL() // Opcode: E3
    {
        byte low = _machine.ReadByte(Reg.SP);
        _machine.WriteByte(Reg.SP, Reg.L);
        Reg.L = low;
        
        byte high = _machine.ReadByte((ushort)(Reg.SP + 1));
        _machine.WriteByte((ushort)(Reg.SP+1), Reg.H);
        Reg.H = high;

        Reg.PC += 1;
        return 19;
    }

    private int Op_OUT_ptrn_A() // Opcode: D3
    {
        byte n = ImmediateByte();
        ushort port = (ushort)((Reg.I << 8) | n);
        WritePort(port, Reg.A);
        Reg.PC += 2;
        return 11;
    }

    private int Op_IN_A_n() // Opcode: DB
    {
        byte n = ImmediateByte();
        ushort port = (ushort)((Reg.A << 8) | n);
        Reg.A = ReadPort(port);

        Reg.PC += 2;
        return 11;
    }

    private static int Op_SCF() // Opcode: 37
    {
        WriteFlag(Flags.H, false);
        WriteFlag(Flags.N, false);
        WriteFlag(Flags.C, true);

        Reg.PC += 1;
        return 4;
    }

    private static int Op_CCF() // Opcode: 3F
    {
        bool carry = GetFlag(Flags.C);
        
        WriteFlag(Flags.H, carry);
        WriteFlag(Flags.N, false);
        WriteFlag(Flags.C, !carry);

        Reg.PC += 1;
        return 4;
    }
}