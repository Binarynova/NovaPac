public interface IMemoryProvider
{
    byte ReadByte(ushort address);
    void WriteByte(ushort address, byte value);
    void ClearRAM();
}