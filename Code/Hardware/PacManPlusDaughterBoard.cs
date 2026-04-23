public class PacManPlusDaughterBoard
{
    private readonly byte[] _rawMainCpu;
    
    public byte[] DecryptedMemory { get; private set; }

    public PacManPlusDaughterBoard(byte[] rawMainCpu)
    {
        _rawMainCpu = rawMainCpu;
        DecryptedMemory = new byte[(16 + 10) * 1024];
    }

    // TODO: replace all this Ms. Pac-Man code with appropriate Pac-Man Plus code.
    
    public void Initialize()
    {
        for (int i = 0; i < 0x1000; i++)
        {
            DecryptedMemory[decryptAddr1((uint)i) + 0x4000] = (byte)decryptData(_rawMainCpu[i + 0xB000]);
            DecryptedMemory[decryptAddr1((uint)i) + 0x5000] = (byte)decryptData(_rawMainCpu[i + 0x9000]);
        }
        for (int i = 0; i < 0x0800; i++)
        {
            DecryptedMemory[decryptAddr2((uint)i) + 0x6000] = (byte)decryptData(_rawMainCpu[i + 0x8000]);
        }
        Array.Copy(_rawMainCpu, 0x0000, DecryptedMemory, 0x0000, 0x3000);
        Array.Copy(DecryptedMemory, 0x4000, DecryptedMemory, 0x3000, 0x1000);
        
        ApplySubroutinePatches();
    }

    private void ApplySubroutinePatches()
    {
        Patch(0x0410, 0x6008); Patch(0x08E0, 0x61D8); Patch(0x0A30, 0x6118); Patch(0x0BD0, 0x60D8);
        Patch(0x0C20, 0x6120); Patch(0x0E58, 0x6168); Patch(0x0EA8, 0x6198); Patch(0x1000, 0x6020);
        Patch(0x1008, 0x6010); Patch(0x1288, 0x6098); Patch(0x1348, 0x6048); Patch(0x1688, 0x6088);
        Patch(0x16B0, 0x6188); Patch(0x16D8, 0x60C8); Patch(0x16F8, 0x61C8); Patch(0x19A8, 0x60A8);
        Patch(0x19B8, 0x61A8); Patch(0x2060, 0x6148); Patch(0x2108, 0x6018); Patch(0x21A0, 0x61A0);
        Patch(0x2298, 0x60A0); Patch(0x23E0, 0x60E8); Patch(0x2418, 0x6000); Patch(0x2448, 0x6058);
        Patch(0x2470, 0x6140); Patch(0x2488, 0x6080); Patch(0x24B0, 0x6180); Patch(0x24D8, 0x60C0);
        Patch(0x24F8, 0x61C0); Patch(0x2748, 0x6050); Patch(0x2780, 0x6090); Patch(0x27B8, 0x6190);
        Patch(0x2800, 0x6028); Patch(0x2B20, 0x6100); Patch(0x2B30, 0x6110); Patch(0x2BF0, 0x61D0);
        Patch(0x2CC0, 0x60D0); Patch(0x2CD8, 0x60E0); Patch(0x2CF0, 0x61E0); Patch(0x2D60, 0x6160);
    }
    
    private void Patch(int dest, int src) => Array.Copy(DecryptedMemory, src, DecryptedMemory, dest, 8);
    
    // methods for decrypting Ms. Pac-Man data and applying the data to Pac-Man
    // referenced from JustinCredible and MAME
    private static uint decryptData(uint data)
    {
        uint decryptedData = (data & 0xC0) >> 3;
        decryptedData |= (data & 0x10) << 2;
        decryptedData |= (data & 0x0E) >> 1;
        decryptedData |= (data & 0x01) << 7;
        decryptedData |= data & 0x20;
        
        return decryptedData;
    }

    private static uint decryptAddr1(uint data)
    {
        uint decryptedData = data & 0x807;
        decryptedData |= (data & 0x400) >> 7;
        decryptedData |= (data & 0x200) >> 2;
        decryptedData |= (data & 0x080) << 3;
        decryptedData |= (data & 0x040) << 2;
        decryptedData |= (data & 0x138) << 1;
        
        return decryptedData;
    }

    private static uint decryptAddr2(uint data)
    {
        uint decryptedData = data & 0x807;
        decryptedData |= (data & 0x040) << 4;
        decryptedData |= (data & 0x100) >> 3;
        decryptedData |= (data & 0x080) << 2;
        decryptedData |= (data & 0x600) >> 2;
        decryptedData |= (data & 0x028) << 1;
        decryptedData |= (data & 0x010) >> 1;
        
        return decryptedData;
    }
}