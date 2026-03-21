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

    private int Op_DEC_ptrIXd() // Opcode: DD 35
    {
        sbyte d = (sbyte)_machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        _machine.WriteByte(addr, (byte)(_machine.ReadByte(addr) - 1));
        
        Reg.PC += 3;
        return 23;
    }

    private int Op_LD_ptrIXd_n() // Opcode: DD 36
    {
        sbyte d = (sbyte)_machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);

        byte n = _machine.ReadByte((ushort)(Reg.PC + 3));
        _machine.WriteByte(addr, n);
        
        Reg.PC += 4;
        return 23;
    }

    private int Op_ADD_A_ptrIXd() // Opcode: DD 86
    {
        sbyte d = (sbyte)_machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);

        Reg.A = (byte)(Reg.A + _machine.ReadByte(addr));
        
        Reg.PC += 3;
        return 19;
    }
    
    private static int Op_ADD_IX_DE() // Opcode: DD 19
    {
        Reg.IX = ADDWord(Reg.IX, Reg.DE);
        Reg.PC += 2;
        return 15;
    }

    private int Op_LD_IX_nn() // Opcode: DD 21
    {
        ushort operand = ReadImmediateWord(true);
        Reg.IX = operand;
        Reg.PC += 4;
        return 14;
    }

    private int Op_LD_ptrIXd(byte register) // Opcode: DD 70 71 72 73 74 75 77
    {
        LOAD_ptrIXd(register);
        Reg.PC += 3;
        return 19;
    }

    private int Op_LD_A_ptrIXd() // Opcode: DD 7E
    {
        sbyte d = (sbyte)_machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        Reg.A = _machine.ReadByte(addr);

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
    
    #region Helper Methods
    private void LOAD_ptrIXd(byte reg)
    {
        sbyte d = (sbyte)_machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        _machine.WriteByte(addr, reg);
    }
    #endregion
}