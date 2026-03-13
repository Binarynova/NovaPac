using Reg = Registers;

public partial class Z80
{
    private static int Op_NOP() { Reg.PC += 1; return 4; }
    private int Op_HALT() { _halted = true; return 4; }
    
    private int Op_DI() { _iff1 = false; Reg.PC += 1; return 4; }
    private int Op_EI() { EI_Pending = true; Reg.PC += 1; return 4; }
    
    private static int Op_EXX()
    {
        ushort temp = Reg.BC;
        ushort temp2 = Reg.DE;
        ushort temp3 = Reg.HL;
        Reg.BC = Reg.BC2;
        Reg.DE = Reg.DE2;
        Reg.HL = Reg.HL2;
        Reg.BC2 = temp;
        Reg.DE2 = temp2;
        Reg.HL2 = temp3;
        Reg.PC += 1;
        return 4;
    }
    
    private static int Op_EX_DE_HL()
    {
        (Reg.DE, Reg.HL) = (Reg.HL, Reg.DE); // tuples from .NET 7 allow swapping values without a temp var

        Reg.PC += 1;
        return 4;
    }
    
    private int Op_OUT_ptrn_A()
    {
        byte n = ReadImmediateByte();
        ushort port = LEWord(Reg.I, n);
        WritePort(port, Reg.A);
        Reg.PC += 2;
        return 11;
    }
}