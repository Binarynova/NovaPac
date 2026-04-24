public class PacManPlusDaughterBoard
{
    private readonly byte[] _rawMainCpu;
    
    public byte[] DecryptedMemory { get; }

    public PacManPlusDaughterBoard(byte[] rawMainCpu)
    {
        _rawMainCpu = rawMainCpu;
        DecryptedMemory = new byte[0x4000];
    }
    
    public void Initialize()
    {
        decodePacPlus();
    }

    void decodePacPlus()
    {
        for (int i = 0; i < 0x04000; i++)
        {
            DecryptedMemory[i] = decryptPacPlus((ushort)i, _rawMainCpu[i]);
        }
    }

    static byte decryptPacPlus(ushort addr, byte e)
    {
        byte[,] swap_xor_table = new byte[,]
        {
            { 7,6,5,4,3,2,1,0, 0x00 },
            { 7,6,5,4,3,2,1,0, 0x28 },
            { 6,1,3,2,5,7,0,4, 0x96 },
            { 6,1,5,2,3,7,0,4, 0xBE },
            { 0,3,7,6,4,2,1,5, 0xD5 },
            { 0,3,4,6,7,2,1,5, 0xDD }
        };

        byte[] pickTable =
        [
            0,2,4,2,4,0,4,2,2,0,2,2,4,0,4,2,
            2,2,4,0,4,2,4,0,0,4,0,4,4,2,4,2
        ];

        byte method = pickTable [
            (addr & 0x001) |
            ((addr & 0x004) >> 1) |
            ((addr & 0x020) >> 3) |
            ((addr & 0x080) >> 4) |
            ((addr & 0x200) >> 5)
        ];

        if ((addr & 0x800) == 0x800)
            method ^= 1;
        
        return (byte)(BitSwap8(e, swap_xor_table[method,0], swap_xor_table[method,1], swap_xor_table[method,2], swap_xor_table[method,3], swap_xor_table[method,4], swap_xor_table[method,5], swap_xor_table[method,6], swap_xor_table[method,7]) ^ swap_xor_table[method,8]);
    }

    static byte BitSwap8(byte val, byte b7, byte b6, byte b5, byte b4, byte b3, byte b2, byte b1, byte b0)
    {
        return (byte)(
            (((val >> b7) & 1) << 7) |
            (((val >> b6) & 1) << 6) |
            (((val >> b5) & 1) << 5) |
            (((val >> b4) & 1) << 4) |
            (((val >> b3) & 1) << 3) |
            (((val >> b2) & 1) << 2) |
            (((val >> b1) & 1) << 1) |
            (((val >> b0) & 1) << 0)
        );
    }
}