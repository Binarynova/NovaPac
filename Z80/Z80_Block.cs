using Reg = Registers;

public partial class z80Cpu
{
    private int Op_LDIR()
    {
        // Transfer one byte
        byte value = machine.ReadByte(Reg.HL);
        machine.WriteByte(Reg.DE, value, Reg.PC);

        Reg.HL++;
        Reg.DE++;
        Reg.BC--;

        // Flags
        ClearFlag(Flags.N | Flags.H);
        WriteFlag(Flags.P, Reg.BC != 0);  // repeat flag

        // PC handling
        if (Reg.BC == 0)
        {
            Reg.PC += 2;  // move past ED B0
            return 16;    // last iteration cycles
        }
        else
        {
            // stay on ED B0 until BC == 0
            return 21;    // cycles per iteration
        }
    }
}