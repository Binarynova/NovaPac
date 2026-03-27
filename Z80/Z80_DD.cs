using System;
using Reg = Registers;

public partial class Z80
{
    private int Op_DD() // Opcode: DD
    {
        byte opcode = PeekNextByte();

        if (opcode == 0xCB)
            return Op_DD_CB();
        IncrementRegisterR();
        return _ddOpcodes[opcode]();
    }
    
    private static int Op_ADD_IX_BC() // Opcode: DD 09
    {
        Reg.IX = ADDWord(Reg.IX, Reg.BC);
        Reg.PC += 2;
        return 15;
    }
    
    private static int Op_ADD_IX_DE() // Opcode: DD 19
    {
        Reg.IX = ADDWord(Reg.IX, Reg.DE);
        Reg.PC += 2;
        return 15;
    }

    private int Op_LD_IX_nn() // Opcode: DD 21
    {
        ushort operand = ImmediateWord(true);
        Reg.IX = operand;
        Reg.PC += 4;
        return 14;
    }

    private int Op_LD_ptrNN_IX() // Opcode: DD 22
    {
        ushort addr = ImmediateWord(true);
        
        _machine.WriteByte(addr, Reg.IXL);
        _machine.WriteByte((ushort)(addr + 1), Reg.IXH);
        
        Reg.PC += 4;
        return 20;
    }

    private int Op_INC_IX() // Opcode: DD 23
    {
        Reg.IX += 1;
        
        Reg.PC += 2;
        return 10;
    }
    
    private static int Op_ADD_IX_IX() // Opcode: DD 29
    {
        Reg.IX = ADDWord(Reg.IX, Reg.IX);
        Reg.PC += 2;
        return 15;
    }

    private int Op_LD_IX_ptrnn() // Opcode: DD 2A
    {
        ushort nn = ImmediateWord(true);
        
        Reg.IXL = _machine.ReadByte(nn);
        Reg.IXH = _machine.ReadByte((ushort)(nn + 1));
        
        Reg.PC += 4;
        return 20;
    }

    private int Op_DEC_IX() // Opcode: DD 2B
    {
        Reg.IX -= 1;
        
        Reg.PC += 2;
        return 10;
    }

    private int Op_INC_ptrIXd() // Opcode: DD 34
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IX);
        
        byte originalValue = _machine.ReadByte(addr);
        byte result = (byte)(_machine.ReadByte(addr) + 1);
        _machine.WriteByte(addr, result);
        
        SetSZFlags(result);
        ClearFlag(Flags.N);
        if ((result & 0x0F) == 0x0F) // Opcode: if lower nibble is F, half-carry will occur when adding 1
            SetFlag(Flags.H);
        else
            ClearFlag(Flags.H);
        SetParity(result);
        
        Reg.PC += 3;
        return 23;
    }
    
    private int Op_DEC_ptrIXd() // Opcode: DD 35
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IX);
        _machine.WriteByte(addr, (byte)(_machine.ReadByte(addr) - 1));
        
        Reg.PC += 3;
        return 23;
    }

    private int Op_LD_ptrIXd_n() // Opcode: DD 36
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IX);

        byte n = _machine.ReadByte((ushort)(Reg.PC + 3));
        _machine.WriteByte(addr, n);
        
        Reg.PC += 4;
        return 19;
    }
    
    private static int Op_ADD_IX_SP() // Opcode: DD 39
    {
        Reg.IX = ADDWord(Reg.IX, Reg.SP);
        Reg.PC += 2;
        return 15;
    }
    
    private int Op_LD_r_ptrIXd(ref byte register) // Opcode: DD 46 4E 56 5E 66 6E 7E
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IX);

        register = _machine.ReadByte(addr);
        
        Reg.PC += 3;
        return 19;
    }

    private int Op_LD_ptrIXd(byte register) // Opcode: DD 70 71 72 73 74 75 77
    {
        LOAD_ptrIXd(register);
        Reg.PC += 3;
        return 19;
    }

    private int Op_ADD_A_ptrIXd() // Opcode: DD 86
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IX);

        Reg.A = (byte)(Reg.A + _machine.ReadByte(addr));
        
        Reg.PC += 3;
        return 19;
    }

    private int Op_SUB_ptrIXd() // Opcode: DD 96
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IX);
        
        Reg.A = SUB(Reg.A, _machine.ReadByte(addr));

        Reg.PC += 3;
        return 19;
    }
    
    private int Op_AND_ptrIXd() // Opcode: DD A6
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IX);
        
        AND(_machine.ReadByte(addr));
        
        Reg.PC += 3;
        return 19;
    }

    private int OP_CP_ptrIXd() // Opcode: DD BE
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IX);
        
        SUB(_machine.ReadByte(addr), Reg.A);

        Reg.PC += 3;
        return 19;
    }

    private int Op_OR_ptrIXd() // Opcode: DD B6
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IX);
        
        OR(_machine.ReadByte(addr));
        
        Reg.PC += 3;
        return 19;
    }

    private int Op_POP_IX() // Opcode: DD E1
    {
        Reg.IX = PopWord();
        Reg.PC += 2;
        return 14;
    }

    private int Op_PUSH_IX() // Opcode: DD E5
    {
        PushWord(Reg.IX);
        Reg.PC += 2;
        return 15;
    }

    private int Op_ADC_A_ptrIXd() // Opcode: DD 8E
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IX);
        
        Reg.A = ADC(Reg.A, _machine.ReadByte(addr));
        
        Reg.PC += 3;
        return 19;
    }
    
    private int Op_SBC_A_ptrIXd() // Opcode: DD 9E
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IX);
        
        Reg.A = SBC(Reg.A, _machine.ReadByte(addr));
        
        Reg.PC += 3;
        return 19;
    }
    
    private int Op_XOR_A_ptrIXd() // Opcode: DD AE
    {
        ushort addr = IndexAddressingWithDisplacement(Reg.IX);
        
        Reg.A = (byte)(Reg.A ^ _machine.ReadByte(addr));

        SetSZFlags(Reg.A);
        ClearFlag(Flags.C | Flags.H | Flags.N);
        SetParity(Reg.A);

        Reg.PC += 3;
        return 19;
    }
    
    private int Op_EX_ptrSP_IX() // Opcode: DD E3
    {
        byte oldIXL = Reg.IXL;
        byte oldIXH = Reg.IXH;
        
        Reg.IXL = _machine.ReadByte(Reg.SP);
        Reg.IXH = _machine.ReadByte((ushort)(Reg.SP + 1));
        
        _machine.WriteByte(Reg.SP, oldIXL);
        _machine.WriteByte((ushort)(Reg.SP + 1), oldIXH);

        Reg.PC += 2;
        return 23;
    }

    private int Op_JP_ptrIX()
    {
        Reg.PC = Reg.IX;

        return 8;
    }

    private int Op_LD_SP_IX()
    {
        Reg.SP = Reg.IX;
        Reg.PC += 2;
        return 10;
    }

    private static int Op_DD_ADD_A(byte register) // Opcodes: DD 80 81 82 83 84 85 87
    {
        Reg.A = ADD(Reg.A, register);
        Reg.PC += 2;
        return 8;
    }

    private static int Op_DD_ADC_A(byte register) // Opcodes: DD 88 89 8A 8B 8C 8D 8F
    {
        Reg.A = ADC(Reg.A, register);
        Reg.PC += 2;
        return 8;
    }

    private static int Op_DD_SUB(byte register) // Opcodes: DD 90 91 92 93 94 95 97
    {
        Reg.A = SUB(Reg.A, register);
        Reg.PC += 2;
        return 8;
    }

    private static int Op_DD_SBC(byte register) // Opcodes: DD 98 99 9A 9B 9C 9D 9F
    {
        Reg.A = SBC(Reg.A, register);
        Reg.PC += 2;
        return 8;
    }

    private int Op_DD_AND(byte register) // Opcodes: DD A0 A1 A2 A3 A4 A5 A7
    {
        AND(register);
        Reg.PC += 2;
        return 8;
    }

    private int Op_DD_XOR(byte register) // Opcodes: DD A8 A9 AA AB AC AD AF
    {
        Reg.A = (byte)(Reg.A ^ register);

        SetSZFlags(Reg.A);
        ClearFlag(Flags.C | Flags.H | Flags.N);
        SetParity(Reg.A);

        Reg.PC += 2;
        return 8;
    }

    private int Op_DD_OR(byte register) // Opcodes: DD B0 B1 B2 B3 B4 B5 B7
    {
        OR(register);
        Reg.PC += 2;
        return 8;
    }

    private int Op_DD_CP(byte register) // Opcodes: DD B8 B9 BA BB BC BD BF
    {
        InternalCP(register); ;

        Reg.PC += 2;
        return 8;
    }
    
    #region Helper Methods

    private void LOAD_ptrIXd(byte reg)
    {
        sbyte d = (sbyte)_machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        _machine.WriteByte(addr, reg);
    }
    #endregion
}