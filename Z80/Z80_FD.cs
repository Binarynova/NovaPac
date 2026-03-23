using Reg = Registers;

public partial class Z80
{
    private int Op_FD() // Opcode: FD
    {
        byte opcode = PeekNextByte();
        IncrementRegisterR();
        return _fdOpcodes[opcode]();
    }

    private int Op_ADD_IY_BC() // Opcode: FD 09
    {
        Reg.IY = ADDWord(Reg.IY, Reg.BC);
        
        Reg.PC += 2;
        return 15;
    }
    
    private int Op_ADD_IY_DE() // Opcode: FD 19
    {
        Reg.IY = ADDWord(Reg.IY, Reg.DE);
        
        Reg.PC += 2;
        return 15;
    }
    
    private int Op_ADD_IY_IY() // Opcode: FD 29
    {
        Reg.IY = ADDWord(Reg.IY, Reg.IY);
        
        Reg.PC += 2;
        return 15;
    }
    
    private int Op_ADD_IY_SP() // Opcode: FD 39
    {
        Reg.IY = ADDWord(Reg.IY, Reg.SP);
        
        Reg.PC += 2;
        return 15;
    }

    private int Op_LD_IY_nn() // Opcode: FD 21
    {
        Reg.IY = ReadImmediateWord(true);
        Reg.PC += 4;
        return 14;
    }
    
    private int Op_LD_ptrIYd_n() // Opcode: FD 36
    {
        sbyte d = (sbyte)_machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IY + d);
        
        byte n = _machine.ReadByte((ushort)(Reg.PC + 3));
        
        _machine.WriteByte(addr, n);
        Reg.PC += 4;
        return 19;
    }
    
    private int Op_LD_r_ptrIYd(ref byte register) // Opcode: FD 46 4E 56 5E 66 6E 7E
    {
        sbyte d = (sbyte)_machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IY + d);

        register = _machine.ReadByte(addr);
        
        Reg.PC += 3;
        return 19;
    }
    
    private int Op_LD_ptrIYd(byte register) // Opcode: FD 70 71 72 73 74 75 77
    {
        sbyte d = (sbyte)_machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IY + d);
        
        _machine.WriteByte(addr, register);
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
    
    private int Op_INC_IY() // Opcode: FD 23
    {
        Reg.IY += 1;
        
        Reg.PC += 2;
        return 10;
    }
    
    private int Op_DEC_IY() // Opcode: FD 2B
    {
        Reg.IY -= 1;
        
        Reg.PC += 2;
        return 10;
    }

    private int OP_CP_ptrIYd() // Opcode: FD BE
    {
        sbyte d = (sbyte)_machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IY + d);
        
        SUB(Reg.A, _machine.ReadByte(addr));

        Reg.PC += 3;
        return 19;
    }
    
    private int Op_DEC_ptrIYd() // Opcode: FD 35
    {
        sbyte d = (sbyte)_machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IY + d);
        
        _machine.WriteByte(addr, (byte)(_machine.ReadByte(addr) - 1));
        SetDecFlags((byte)(_machine.ReadByte(addr) - 1));
        Reg.PC += 3;
        return 23;
    }
}