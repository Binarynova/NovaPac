using System;

public class SSTMachine : IMemoryProvider
{
    private byte[] Memory = new byte[0x10000];

    public byte ReadByte(ushort address) => Memory[address];

    public void WriteByte(ushort address, byte value)
    {
        Memory[address] = value;
    }

    public void ClearRAM()
    {
        Array.Clear(Memory, 0, Memory.Length);
    }
}