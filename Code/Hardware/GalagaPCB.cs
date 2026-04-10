public class GalagaPCB : IMemoryProvider
{
    private Z80Cpu maincpu, sub, sub2;
    private NamcoWSG soundChip;
    
    public byte ReadByte(ushort address)
    {
        throw new System.NotImplementedException();
    }

    public void WriteByte(ushort address, byte value)
    {
        throw new System.NotImplementedException();
    }

    public void ClearRAM()
    {
        throw new System.NotImplementedException();
    }
}