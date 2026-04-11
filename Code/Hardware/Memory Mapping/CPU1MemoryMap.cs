using System.Runtime.InteropServices;

public class CPU1MemoryMap : IMemoryBus
{
    private byte[] _mainRom;
    private byte[] _sharedRam;

    public CPU1MemoryMap(byte[] mainRom, byte[] sharedRam)
    {
        _mainRom = mainRom;
        _sharedRam = sharedRam;
    }
    
    public byte ReadByte(ushort address)
    {
        // 0x0000 - 0x3fff is unique to each CPU
        if (address is < 0x4000)
            return _mainRom[address];
        
        // everything else is shared
        return _sharedRam[address - 0x6800];
    }

    public void WriteByte(ushort address, byte value)
    {
        if (address is >= 0x6800)
            _sharedRam[address - 0x6800] = value;
    }
}