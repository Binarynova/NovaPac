using Reg = Registers;

public partial class Z80Cpu
{
    private int Op_POP_BC() // Opcode: C1
    {
        Reg.BC = PopWord();
        Reg.PC += 1;
        return 10;
    }

    private int Op_POP_DE() // Opcode: D1
    {
        Reg.DE = PopWord();
        Reg.PC += 1;
        return 10;
    }

    private int Op_POP_HL() // Opcode: E1
    {
        Reg.HL = PopWord();
        Reg.PC += 1;
        return 10;
    }

    private int Op_POP_AF() // Opcode: F1
    {
        Reg.AF = PopWord();
        Reg.PC += 1;
        return 10;
    }

    private int Op_PUSH_BC() // Opcode: C5
    {
        PushWord(Reg.BC);
        Reg.PC += 1;
        return 11;
    }

    private int Op_PUSH_DE() // Opcode: D5
    {
        PushWord(Reg.DE);
        Reg.PC += 1;
        return 11;
    }

    private int Op_PUSH_HL() // Opcode: E5
    {
        PushWord(Reg.HL);
        Reg.PC += 1;
        return 11;
    }

    private int Op_PUSH_AF() // Opcode: F5
    {
        PushWord(Reg.AF);
        Reg.PC += 1;
        return 11;
    }
    
    #region Helper Methods
    
    private void PushWord(ushort value)
    {
        Reg.SP--; // decrement stack pointer
        _machine.WriteByte(Reg.SP, Reg.HighByte(value)); // write high byte first
        Reg.SP--;
        _machine.WriteByte(Reg.SP, Reg.LowByte(value)); // then low byte
    }

    private ushort PopWord()
    {
        byte low = _machine.ReadByte(Reg.SP);
        Reg.SP++;
        byte high = _machine.ReadByte(Reg.SP);
        Reg.SP++;

        ushort value = (ushort)((high << 8) | low);
        return value;
    }
    
    #endregion
}