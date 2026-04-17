using System.Collections.Generic;

public class Dissassembler
{
    public Dissassembler()
    {
        var opcodes = new Dictionary<byte, string>
        {
            { 0x00, "NOP" },
            { 0x7E, "LD A (HL)"},
            { 0x87, "ADD A,A" }
        };
    }
}