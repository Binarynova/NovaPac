using Reg = Registers;

public partial class Z80
{
    private int Op_FD() // Opcode: FD
    {
        byte opcode = PeekNextByte();
        IncrementRegisterR();
        return _fdOpcodes[opcode]();
    }

    private int Op_LD_IY_nn() // Opcode: FD 21
    {
        Reg.IY = ReadImmediateWord(true);
        Reg.PC += 4;
        return 14;
    }
    
    
    private int Op_LD_ptrIYd(byte register) // Opcode: FD 70 71 72 73 74 75 77
    {
        sbyte d = (sbyte)_machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IY + d);
        
        _machine.WriteByte(addr, register);
        Reg.PC += 3;
        return 19;
    }
    
    private int Op_LD_L_ptrIYd() // Opcode: FD 6E
    {
        sbyte d = (sbyte)_machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IY + d);
        Reg.L = _machine.ReadByte(addr);

        Reg.PC += 3;
        return 19;
    }

    private int Op_POP_IY() // Opcode: FD E1
    {
        Reg.IY = PopWord();
        Reg.PC += 2;
        return 14;
    }

    private int Op_PUSH_IY() // Opcode: FD E5
    {
        PushWord(Reg.IY);
        Reg.PC += 2;
        return 15;
    }
}