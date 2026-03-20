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

    public Pacman(bool testing = false)
    {
        ClearRAM();
        ReadROMsIntoMemory();
    }

    public byte ReadByte(ushort address)
    {
        return Memory[address];
    }

    public void WriteByte(ushort address, byte value)
    {
        Memory[address] = value;
    }
    
    private void ReadROMsIntoMemory()
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
                
                // Copy into your flat memory array at the specified offset
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
    public byte ReadPort(byte port)
    {
        // Return those safe defaults we talked about!
        if (port == 0) return 0xBF; 
        if (port == 1) return 0xFF;
        if (port == 2) return 0xC9;
        return 0xFF;
    }

    public void WritePort(byte port, byte value) 
    { 
        // Handle hardware writes here (like sound/dips) if needed
    }
}