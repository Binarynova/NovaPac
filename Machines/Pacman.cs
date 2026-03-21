using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

public class Pacman : IMemoryProvider
{
    private byte[] Memory = new byte[0x10000];
    public byte[] paletteRAM;
    public byte[] charRAM;
    public byte[] spriteRAM;
    private Z80 Cpu;

    public Pacman()
    {
        ClearRAM();
        LoadRom();
    }

    public void AttachCPU(Z80 cpuInstance)
    {
        Cpu = cpuInstance;
    }
    public byte ReadByte(ushort address)
    {
        // 0x5000 - 0x503F: IN0 (Joystick, Coin, etc.)
        if (address >= 0x5000 && address <= 0x503F) 
            return 0xBF; // Default for no buttons pressed

        // 0x5040 - 0x507F: IN1 (Player Start, Service, etc.)
        if (address >= 0x5040 && address <= 0x507F) 
            return 0xFF; 

        // 0x5080 - 0x50BF: DIP Switches
        if (address >= 0x5080 && address <= 0x50BF) 
            return 0x89; // Normal game settings (No Test Mode)
        
        return Memory[address];
    }

    public void WriteByte(ushort address, byte value)
    {
        if (address < 0x4000)
            return;
        Memory[address] = value;
    }
    
    private void LoadRom()
    {
        using ZipArchive archive = ZipFile.OpenRead("roms/pacman.zip");
        var romMap = new Dictionary<string, int>
        {
            { "pacman.6e", 0x0000 },
            { "pacman.6f", 0x1000 },
            { "pacman.6h", 0x2000 },
            { "pacman.6j", 0x3000 }
        };

        foreach (var entry in romMap)
        {
            ZipArchiveEntry romEntry =  archive.GetEntry(entry.Key);

            if (romEntry != null)
            {
                using Stream s = romEntry.Open();
                byte[] buffer = new byte[romEntry.Length];
                s.ReadExactly(buffer, 0, buffer.Length);
                
                Buffer.BlockCopy(buffer, 0, Memory, entry.Value, buffer.Length);
            }
            else
            {
                throw new FileNotFoundException($"Required ROM file {entry.Key} not found in zip!");
            }
        }

        charRAM = ExtractRom(archive, "pacman.5e");
        spriteRAM = ExtractRom(archive, "pacman.5f");
        paletteRAM = ExtractRom(archive, "82s126.4a");
    }

    private static byte[] ExtractRom(ZipArchive archive, string fileName)
    {
        ZipArchiveEntry entry = archive.GetEntry(fileName);
        if (entry == null) throw new FileNotFoundException($"Missing {fileName}");

        using Stream s = entry.Open();
        byte[] data = new byte[entry.Length];
        s.ReadExactly(data, 0, data.Length);
        return data;
    }

    public void ClearRAM()
    {
        Array.Clear(Memory, 0, Memory.Length);
    }
}