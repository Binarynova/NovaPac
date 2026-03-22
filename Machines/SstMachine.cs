using System;

public class SSTMachine : IMemoryProvider
{
    private byte[] Memory = new byte[0x10000];
    
    public byte ForcedPortValue { get; set; }

    public byte ReadByte(ushort address) => Memory[address];

    public void WriteByte(ushort address, byte value)
    {
        Memory[address] = value;
    }

    public byte ReadPort(byte port)
    {
        return ForcedPortValue;
    }

    public void WritePort(byte port, byte value)
    {
        
    }

    public void ClearRAM()
    {
        Array.Clear(Memory, 0, Memory.Length);
    }
}