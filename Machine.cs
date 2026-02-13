using System;
using System.Collections.Generic;
using System.IO;

public class Machine
{
    byte[] RAM = new byte[0x10000];
    public byte[] charROM;
    byte[] paletteROM;
    const ushort ROM_START = 0x0000;
    const ushort VRAM_START = 0x4000;
    const ushort VRAM_END = 0x43FF;
    public int InterruptMode;

    List<string> romFiles = new List<string> {"rom/pacman.6e", "rom/pacman.6f", "rom/pacman.6h", "rom/pacman.6j"};

    public byte ReadByte(ushort address)
    {
        return RAM[address];
    }

    public ushort ReadWord(ushort address)
    {
        return (ushort)(RAM[address] << 8 | RAM[address+1]);
    }

    public void WriteByte(ushort address, byte value)
    {
        byte oldValue = RAM[address];
        if(address < 0x4000)
            return;
        RAM[address] = value;
        if (oldValue != value)
        {
            Console.WriteLine($"VRAM write at {address:X4}: {oldValue:X2} -> {value:X2}");
        }
    }

    public byte ReadVRAM(ushort address)
    {
        return RAM[address];
    }

    public void ReadROMsIntoMemory()
    {
        LoadRom("rom/pacman.6e", RAM, 0x0000);
        LoadRom("rom/pacman.6f", RAM, 0x1000);
        LoadRom("rom/pacman.6h", RAM, 0x2000);
        LoadRom("rom/pacman.6j", RAM, 0x3000);
        LoadCharROM("rom/pacman.5e");
        LoadPaletteROM("rom/82s126.4a");
    }

    void LoadRom(string file, byte[] memory, int address)
    {
        using var fs = File.OpenRead(file);
        fs.Read(memory, address, (int)fs.Length);
    }
    
    void LoadPaletteROM(string path)
    {
        paletteROM = File.ReadAllBytes(path);

        if (paletteROM.Length != 256)
            throw new Exception($"Unexpected ROM size: {charROM.Length} bytes");
    }

    void LoadCharROM(string path)
    {
        charROM = File.ReadAllBytes(path);

        if (charROM.Length != 4096)
            throw new Exception($"Unexpected ROM size: {charROM.Length} bytes");
    }

    public void ClearRAM()
    {
        Array.Clear(RAM, 0, RAM.Length);
    }
}