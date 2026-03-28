using Reg = Registers;

public partial class Z80
{
    private int Op_FD() // Opcode: FD
    {
        byte opcode = PeekNextByte();
        
        if (opcode == 0xCB)
            return Op_FD_CB();
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

    private int Op_LD_ptrNN_IY() // Opcode: DD 22
    {
        ushort addr = ImmediateWord(true);
        
        _machine.WriteByte(addr, Reg.IYL);
        _machine.WriteByte((ushort)(addr + 1), Reg.IYH);
        
        Reg.PC += 4;
        return 20;
    }
    
    private int Op_ADD_IY_IY() // Opcode: FD 29
    {
        Reg.IY = ADDWord(Reg.IY, Reg.IY);
        
        Reg.PC += 2;
        return 15;
    }

    private int Op_LD_IY_ptrnn() // Opcode: DD 2A
    {
        ushort nn = ImmediateWord(true);
        
        Reg.IYL = _machine.ReadByte(nn);
        Reg.IYH = _machine.ReadByte((ushort)(nn + 1));
        
        Reg.PC += 4;
        return 20;
    }

    private int Op_INC_ptrIYd() // Opcode: DD 34
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IY);
        byte val = _machine.ReadByte(addr);
        byte result = (byte)(val + 1);
        _machine.WriteByte(addr, result);

        // Documented Flag Logic
        WriteFlag(Flags.S, (result & 0x80) != 0);
        WriteFlag(Flags.Z, result == 0);
        WriteFlag(Flags.H, (val & 0x0F) == 0x0F); // Check original nibble
        WriteFlag(Flags.P, val == 0x7F);          // V-flag: 127 -> -128
        ClearFlag(Flags.N);
        // Carry is NOT affected

        Reg.PC += 3;
        return 23;
    }
    
    private int Op_ADD_IY_SP() // Opcode: FD 39
    {
        Reg.IY = ADDWord(Reg.IY, Reg.SP);
        
        Reg.PC += 2;
        return 15;
    }

    private int Op_LD_IY_nn() // Opcode: FD 21
    {
        Reg.IY = ImmediateWord(true);
        Reg.PC += 4;
        return 14;
    }
    
    private int Op_LD_ptrIYd_n() // Opcode: FD 36
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IY);
        
        byte n = _machine.ReadByte((ushort)(Reg.PC + 3));
        
        _machine.WriteByte(addr, n);
        Reg.PC += 4;
        return 19;
    }
    
    private int Op_LD_r_ptrIYd(ref byte register) // Opcode: FD 46 4E 56 5E 66 6E 7E
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IY);

        register = _machine.ReadByte(addr);
        
        Reg.PC += 3;
        return 19;
    }
    
    private int Op_LD_ptrIYd_r(byte register) // Opcode: FD 70 71 72 73 74 75 77
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IY);
        
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
        ushort addr = IndexAddressingWithDisplacement(Reg.IY);
        
        InternalCP(_machine.ReadByte(addr));

        Reg.PC += 3;
        return 19;
    }
    
    private int Op_DEC_ptrIYd() // Opcode: FD 35
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IY);
        byte val = _machine.ReadByte(addr);
        byte result = (byte)(val - 1);
        _machine.WriteByte(addr, result);

        // Documented Flag Logic
        WriteFlag(Flags.S, (result & 0x80) != 0);
        WriteFlag(Flags.Z, result == 0);
        WriteFlag(Flags.H, (val & 0x0F) == 0x00); // Check original nibble
        WriteFlag(Flags.P, val == 0x80);          // V-flag: -128 -> 127
        SetFlag(Flags.N);
        // Carry is NOT affected

        Reg.PC += 3;
        return 23;
    }
    
    private int Op_LD_IYH_r(byte register)
    {
        Reg.IYH = register;
        Reg.PC += 2;
        return 8;
    }
    
    private int Op_LD_IYL_r(byte register)
    {
        Reg.IYL = register;
        Reg.PC += 2;
        return 8;
    }
    
    private int Op_LD_r_IYH(ref byte register)
    {
        register = Reg.IYH;
        Reg.PC += 2;
        return 8;
    }
    
    private int Op_LD_r_IYL(ref byte register)
    {
        register = Reg.IYL;
        Reg.PC += 2;
        return 8;
    }

    private int Op_ADD_A_ptrIYd() // Opcode: FD 86
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IY);

        Reg.A = ADD(Reg.A, _machine.ReadByte(addr));
        
        Reg.PC += 3;
        return 19;
    }

    private int Op_SUB_ptrIYd() // Opcode: DD 96
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IY);
        
        Reg.A = SUB(Reg.A, _machine.ReadByte(addr));

        Reg.PC += 3;
        return 19;
    }
    
    private int Op_AND_ptrIYd() // Opcode: DD A6
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IY);
        
        AND(_machine.ReadByte(addr));
        
        Reg.PC += 3;
        return 19;
    }

    private int Op_ADC_A_ptrIYd() // Opcode: DD 8E
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IY);
        
        Reg.A = ADC(Reg.A, _machine.ReadByte(addr));
        
        Reg.PC += 3;
        return 19;
    }
    
    private int Op_SBC_A_ptrIYd() // Opcode: DD 9E
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IY);
        
        Reg.A = SBC(Reg.A, _machine.ReadByte(addr));
        
        Reg.PC += 3;
        return 19;
    }
    
    private int Op_XOR_A_ptrIYd() // Opcode: DD AE
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IY);
        XOR(_machine.ReadByte(addr));

        Reg.PC += 3;
        return 19;
    }
    
    private int Op_EX_ptrSP_IY() // Opcode: DD E3
    {
        byte oldIYL = Reg.IYL;
        byte oldIYH = Reg.IYH;
        
        Reg.IYL = _machine.ReadByte(Reg.SP);
        Reg.IYH = _machine.ReadByte((ushort)(Reg.SP + 1));
        
        _machine.WriteByte(Reg.SP, oldIYL);
        _machine.WriteByte((ushort)(Reg.SP + 1), oldIYH);

        Reg.PC += 2;
        return 23;
    }

    private int Op_JP_ptrIY()
    {
        Reg.PC = Reg.IY;

        return 8;
    }

    private int Op_LD_SP_IY()
    {
        Reg.SP = Reg.IY;
        Reg.PC += 2;
        return 10;
    }

    private int Op_OR_ptrIYd() // Opcode: DD B6
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IY);
        
        OR(_machine.ReadByte(addr));
        
        Reg.PC += 3;
        return 19;
    }
}