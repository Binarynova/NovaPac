using System;

public class SSTMachine : IMemoryProvider
{
    public byte[] Memory = new byte[0x10000];
    
    // We'll use this to inject values for 'IN' instructions during tests
    public byte ForcedPortValue { get; set; }

    public byte ReadByte(ushort address) => Memory[address];

    public void WriteByte(ushort address, byte value)
    {
        Memory[address] = value;
    }

    public byte ReadPort(byte port)
    {
        // When a test executes 'IN A, (n)', it will return this value
        return ForcedPortValue;
    }

    public void WritePort(byte port, byte value)
    {
        // Tests usually don't verify physical output, but you could log it
    }

    public void ClearRAM()
    {
        Array.Clear(Memory, 0, Memory.Length);
    }
}