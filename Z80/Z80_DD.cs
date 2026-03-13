using Reg = Registers;

public partial class z80Cpu
{
    private int Op_DD()
    {
        byte opcode = PeekOpcode();
        IncrementRegisterR();
        return _ddOpcodes[opcode]();
    }
    
    private static int Op_ADD_IX_DE() { Reg.IX = ADDWord(Reg.IX, Reg.DE); Reg.PC += 2; return 15; }
    private int Op_LD_IX_nn()
    {
        // Bytes: 4, Cycles: 14
        // Fetch > Execute (registers, memory, flags) > Advance PC > Return cycles
        ushort operand = ReadImmediateWord();
        Reg.IX = operand;
        Reg.PC += 4;
        return 14;
    }
    private void LOAD_ptrIXd(byte reg)
    {
        sbyte d = (sbyte)machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        machine.WriteByte(addr, reg, Reg.PC);
    }
    private int Op_LD_ptrIXd_A() { LOAD_ptrIXd(Reg.A); Reg.PC += 3; return 19; }
    private int Op_LD_ptrIXd_B() { LOAD_ptrIXd(Reg.B); Reg.PC += 3; return 19; }
    private int Op_LD_ptrIXd_C() { LOAD_ptrIXd(Reg.C); Reg.PC += 3; return 19; }
    private int Op_LD_ptrIXd_D() { LOAD_ptrIXd(Reg.D); Reg.PC += 3; return 19; }
    private int Op_LD_ptrIXd_E() { LOAD_ptrIXd(Reg.E); Reg.PC += 3; return 19; }
    private int Op_LD_ptrIXd_H() { LOAD_ptrIXd(Reg.H); Reg.PC += 3; return 19; }
    private int Op_LD_ptrIXd_L() { LOAD_ptrIXd(Reg.L); Reg.PC += 3; return 19; }
    private int Op_LD_A_ptrIXd()
    {
        sbyte d = (sbyte)machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        Reg.A = machine.ReadByte(addr);
        
        Reg.PC += 3;
        return 19;
    }
    
    private int Op_POP_IX() { Reg.IX = PopWord(); Reg.PC += 2; return 14; }
    private int Op_PUSH_IX() { PushWord(Reg.IX); Reg.PC += 2; return 15; }
}