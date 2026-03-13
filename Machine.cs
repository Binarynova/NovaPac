using System;
using System.IO;

public class Machine
{
    private byte[] RAM = new byte[0x10000];
    public byte[] paletteROM;
    public byte[] charROM;
    public byte[] spriteROM;
    private const ushort TILES_START = 0x4000;
    private const ushort TILES_END = 0x43FF;
    private const ushort PALETTE_START = 0x4400;
    private const ushort PALETTE_END = 0x47FF;

    public byte ReadByte(ushort address)
    {
        return RAM[address];
    }

    public ushort ReadWord(ushort address)
    {
        return (ushort)(RAM[address] << 8 | RAM[address+1]);
    }

    public void WriteByte(ushort address, byte value, ushort PC)
    {
        if(address < 0x4000)
            return;
        RAM[address] = value;
        
        // debug to fill tiles with numbers
        //for(int loc = 0x43C0; loc <= 0x43FF; loc++)
        //    RAM[loc] = 0x0;
        //for(int loc = 0x4040; loc <= 0x43BF; loc++)
        //    RAM[loc] = 0x1;
        //for(int loc = 0x4000; loc <= 0x403F; loc++)
        //    RAM[loc] = 0x2;

        switch (address)
        {
            case >= TILES_START and < TILES_END:
                Console.WriteLine($"VRAM write at {address:X4}: {value:X2}");
                //Console.ReadKey();    
                break;
            case >= PALETTE_START and < PALETTE_END:
                Console.WriteLine($"PALETTE write at {address:X4}: {value:X2}. PC:{PC:X4}");
                if (value > 0x1F)
                {
                    //Console.Clear();
                }
                //Console.ReadKey();    
                break;
        }
    }

    public void ReadROMsIntoMemory()
    {
        LoadRom("rom/pacman.6e", RAM, 0x0000);
        LoadRom("rom/pacman.6f", RAM, 0x1000);
        LoadRom("rom/pacman.6h", RAM, 0x2000);
        LoadRom("rom/pacman.6j", RAM, 0x3000);
        LoadCharROM("rom/pacman.5e");
        LoadSpriteROM("rom/pacman.5f");
        LoadPaletteROM("rom/82s126.4a");
    }

    private static void LoadRom(string file, byte[] memory, int address)
    {
        using var fs = File.OpenRead(file);
        fs.ReadExactly(memory, address, (int)fs.Length);
    }
    
    private void LoadPaletteROM(string path)
    {
        paletteROM = File.ReadAllBytes(path);

        if (paletteROM.Length != 256)
            throw new Exception($"Unexpected ROM size: {paletteROM.Length} bytes");
    }

    private void LoadCharROM(string path)
    {
        charROM = File.ReadAllBytes(path);

        if (charROM.Length != 4096)
            throw new Exception($"Unexpected ROM size: {charROM.Length} bytes");
    }

    private void LoadSpriteROM(string path)
    {
        spriteROM = File.ReadAllBytes(path);

        if (spriteROM.Length != 4096)
            throw new Exception($"Unexpected ROM size: {spriteROM.Length} bytes");
    }

    public void ClearRAM()
    {
        Array.Clear(RAM, 0, RAM.Length);
    }
}