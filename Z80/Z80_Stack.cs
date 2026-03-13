using Reg = Registers;

public partial class z80Cpu
{
    private void PushWord(ushort value)
    {
        Reg.SP--;                     // decrement stack pointer
        machine.WriteByte(Reg.SP, Reg.HighByte(value), Reg.PC); // write high byte first
        Reg.SP--;
        machine.WriteByte(Reg.SP, Reg.LowByte(value), Reg.PC);  // then low byte
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
    
    private int Op_POP_BC() { Reg.BC = PopWord(); Reg.PC += 1; return 10; }
    private int Op_POP_DE() { Reg.DE = PopWord(); Reg.PC += 1; return 10; }
    private int Op_POP_HL() { Reg.HL = PopWord(); Reg.PC += 1; return 10; }
    private int Op_POP_AF() { Reg.AF = PopWord(); Reg.F &= 0xD7; Reg.PC += 1; return 10; }

    private int Op_PUSH_BC() { PushWord(Reg.BC); Reg.PC += 1; return 11; }
    private int Op_PUSH_DE() { PushWord(Reg.DE); Reg.PC += 1; return 11; }
    private int Op_PUSH_HL() { PushWord(Reg.HL); Reg.PC += 1; return 11; }
    private int Op_PUSH_AF() { PushWord(Reg.AF); Reg.PC += 1; return 11; }
}