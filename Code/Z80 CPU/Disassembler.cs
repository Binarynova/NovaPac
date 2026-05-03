public class Disassembler
{
    private Dictionary<byte, string> opcodes;
    
    public Disassembler()
    {
        opcodes = new Dictionary<byte, string>
        {
            { 0x00, $"NOP" },
            { 0x18, $"JR D" },
            { 0x26, $"LD H,n" },
            { 0x28, $"JR Z,D" },
            { 0x30, $"JR NC,D" },
            { 0x3A, $"LD A,(nn)" },
            { 0x47, $"LD B,A" },
            { 0x6F, $"LD L,A" },
            { 0x7E, $"LD A (HL)" },
            { 0x87, $"ADD A,A" },
            { 0xA7, $"AND A" },
            { 0xC8, $"RET Z" },
            { 0xCD, $"CALL nn" },
            { 0xE6, $"AND n" }
        };
    }

    public string GetAssemblyOP(byte opcode)
    {
        return opcodes.GetValueOrDefault(opcode, "");
    }
}