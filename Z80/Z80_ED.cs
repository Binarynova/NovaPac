using Reg = Registers;

public partial class z80Cpu
{
    private int Op_ED()
    {
        byte opcode = PeekOpcode();
        IncrementRegisterR();
        return _edOpcodes[opcode]();
    }
    
    private static int Op_SBC_HL_BC() { Reg.HL = SBCWord(Reg.HL, Reg.BC); Reg.PC += 2; return 15; }
    private static int Op_SBC_HL_DE() { Reg.HL = SBCWord(Reg.HL, Reg.DE); Reg.PC += 2; return 15; }
    private static int Op_SBC_HL_HL() { Reg.HL = SBCWord(Reg.HL, Reg.HL); Reg.PC += 2; return 15; }
    private static int Op_SBC_HL_SP() { Reg.HL = SBCWord(Reg.HL, Reg.SP); Reg.PC += 2; return 15; }
    
    private static int Op_LD_I_A() { Reg.I = Reg.A; Reg.PC += 2; return 9; }
    
    private int Op_IM_0() { _interruptMode = 0; Reg.PC += 2; return 8; }
    private int Op_IM_1() { _interruptMode = 1; Reg.PC += 2; return 8; }
    private int Op_IM_2() { _interruptMode = 2; Reg.PC += 2; return 8; }
}