public interface IMemoryProvider
{
    byte ReadByte(ushort address);
    void WriteByte(ushort address, byte value);
    void ClearRAM();
    
    // Add these so the CPU can talk to Pacman hardware!
    byte ReadPort(byte port);
    void WritePort(byte port, byte value);
}