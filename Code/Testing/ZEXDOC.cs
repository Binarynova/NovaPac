using System;
using System.IO;
using Reg = Registers;

public class ZEXDOC : IMemoryProvider
{
    public byte[] Memory = new byte[0x10000];
    Z80Cpu _cpu;

    public ZEXDOC()
    {
        _cpu = new Z80Cpu(this);
        LoadRom();
    }
    
    public byte ReadByte(ushort address) => Memory[address];
    public void WriteByte(ushort address, byte value)
    {
        Memory[address] = value;
    }

    private void LoadRom()
    {
        using FileStream fs = File.OpenRead("roms/zexdoc.com");
        fs.ReadExactly(Memory, 0x0100, (int)fs.Length);
    }

    public void ClearRAM()
    {
        Array.Clear(Memory, 0, Memory.Length);
    }
    
    public void Run()
    {
        // ... Initialization code ...
        Reg.SP = 0xF000;
        Reg.PC = 0x0100;

        while (true)
        {
            // 1. Intercept the CP/M Call 5 BEFORE executing the opcode
            if (Reg.PC == 0x0005)
            {
                HandleCpmCall();
                // This manually performs the RET and continues the loop
                continue; 
            }

            // 2. Safety check: Did we hit the exit?
            if (Reg.PC == 0x0000) break;

            // 3. Execute the instruction
            byte opcode = ReadByte(Reg.PC);
            _cpu._mainOpcodes[opcode]();
        
            // Note: Your opcodes handle the PC incrementing, so we don't do it here.
        }
    
        Console.WriteLine("\nTests Finished.");
    }

    private void HandleCpmCall()
    {
        if (Reg.C == 2) // Output character
        {
            Console.Write((char)Reg.E);
        }
        else if (Reg.C == 9) // Output string
        {
            ushort addr = Reg.DE;
            byte current;
            while ((current = ReadByte(addr++)) != (byte)'$')
            {
                Console.Write((char)current);
            }
        }

        // IMPORTANT: After handling the print, we must return to where 
        // the Zexdoc code called us from.
        _cpu.Op_RET(); 
    }
}