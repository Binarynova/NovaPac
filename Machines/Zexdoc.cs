using System;
using System.IO;

public class Zexdoc : IMemoryProvider
{
    public byte[] Memory = new byte[0x10000];

    public Zexdoc()
    {
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
    public byte ReadPort(byte port) => 0xFF; // Zexdoc doesn't use ports
    public void WritePort(byte port, byte value) { }
}