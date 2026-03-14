using Reg = Registers;

public partial class Z80
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
        Reg.F &= 0xD7;
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
        machine.WriteByte(Reg.SP, Reg.HighByte(value), Reg.PC); // write high byte first
        Reg.SP--;
        machine.WriteByte(Reg.SP, Reg.LowByte(value), Reg.PC); // then low byte
    }

    private ushort PopWord()
    {
        byte low = machine.ReadByte(Reg.SP);
        Reg.SP++;
        byte high = machine.ReadByte(Reg.SP);
        Reg.SP++;

        ushort value = LEWord(high, low);
        return value;
    }
    #endregion
}