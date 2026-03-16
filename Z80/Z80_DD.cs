using Reg = Registers;

public partial class Z80
{
    private int Op_DD() // Opcode: DD
    {
        byte opcode = PeekNextByte();
        IncrementRegisterR();
        return _ddOpcodes[opcode]();
    }

    private static int Op_ADD_IX_DE() // Opcode: DD 19
    {
        Reg.IX = ADDWord(Reg.IX, Reg.DE);
        Reg.PC += 2;
        return 15;
    }

    private int Op_LD_IX_nn() // Opcode: DD 21
    {
        // Bytes: 4, Cycles: 14
        // Fetch > Execute (registers, memory, flags) > Advance PC > Return cycles
        ushort operand = ReadImmediateWord();
        Reg.IX = operand;
        Reg.PC += 4;
        return 14;
    }

    private int Op_LD_ptrIXd_A() // Opcode: DD 36
    {
        LOAD_ptrIXd(Reg.A);
        Reg.PC += 3;
        return 19;
    }

    private int Op_LD_ptrIXd_B() // Opcode: DD 70
    {
        LOAD_ptrIXd(Reg.B);
        Reg.PC += 3;
        return 19;
    }

    private int Op_LD_ptrIXd_C() // Opcode: DD 71
    {
        LOAD_ptrIXd(Reg.C);
        Reg.PC += 3;
        return 19;
    }

    private int Op_LD_ptrIXd_D() // Opcode: DD 72
    {
        LOAD_ptrIXd(Reg.D);
        Reg.PC += 3;
        return 19;
    }

    private int Op_LD_ptrIXd_E() // Opcode: DD 73
    {
        LOAD_ptrIXd(Reg.E);
        Reg.PC += 3;
        return 19;
    }

    private int Op_LD_ptrIXd_H() // Opcode: DD 74
    {
        LOAD_ptrIXd(Reg.H);
        Reg.PC += 3;
        return 19;
    }

    private int Op_LD_ptrIXd_L() // Opcode: DD 75
    {
        LOAD_ptrIXd(Reg.L);
        Reg.PC += 3;
        return 19;
    }

    private int Op_LD_A_ptrIXd() // Opcode: DD 7E
    {
        sbyte d = (sbyte)machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        Reg.A = machine.ReadByte(addr);

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
        sbyte d = (sbyte)machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        machine.WriteByte(addr, reg);
    }
    #endregion
}