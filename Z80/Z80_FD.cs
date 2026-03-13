using Reg = Registers;

public partial class Z80
{
    private int Op_FD()
    {
        byte opcode = PeekNextByte();
        IncrementRegisterR();
        return _fdOpcodes[opcode]();
    }
    
    private int Op_LD_IY_nn()
    {
        Reg.IY = ReadImmediateWord();
        Reg.PC += 4;
        return 14;
    }
    private int Op_LD_L_ptrIYd()
    {
        sbyte d = (sbyte)machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        Reg.L = machine.ReadByte(addr);
        
        Reg.PC += 3;
        return 19;
    }
    
    private int Op_POP_IY() { Reg.IY = PopWord(); Reg.PC += 2; return 14; }
    private int Op_PUSH_IY() { PushWord(Reg.IY); Reg.PC += 2; return 15; }
}