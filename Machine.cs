using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

public class Machine
{
    private byte[] RAM = new byte[0x10000];
    public byte[] paletteROM;
    public byte[] charROM;
    public byte[] spriteROM;
    public readonly bool Testing;
    private const ushort TILES_START = 0x4000;
    private const ushort TILES_END = 0x43FF;
    private const ushort PALETTE_START = 0x4400;
    private const ushort PALETTE_END = 0x47FF;

    public Machine(bool testing = false)
    {
        ClearRAM();
        Testing = testing;
    }

    public byte ReadByte(ushort address)
    {
        return RAM[address];
    }

    public void WriteByte(ushort address, byte value)
    {
        if (!Testing)
        {
            if(address < 0x4000)
                return;
        }
        
        switch (address)
        {
            case >= TILES_START and < TILES_END:
                //Console.WriteLine($"VRAM write at {address:X4}: {value:X2}");
                break;
            case >= PALETTE_START and < PALETTE_END:
                if (value > 0x1F)
                {
                    //Console.WriteLine($"PALETTE write of {value:X2} at {address:X4}. PC:{PC:X4}");
                    //Console.WriteLine($"HL: {Reg.HL:X4}");
                    //Console.WriteLine($"AF: {Reg.AF:X4}");
                    //Console.WriteLine($"BC: {Reg.BC:X4}");
                    //Console.WriteLine($"DE: {Reg.DE:X4}");
                    //System.Diagnostics.Debugger.Break();
                    //if (Console.ReadKey().Key == ConsoleKey.Escape)
                    //    Environment.Exit(0);
                }   
                break;
        }
        
        RAM[address] = value;
    }

    public void ReadROMsIntoMemory()
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
                Buffer.BlockCopy(buffer, 0, RAM, entry.Value, buffer.Length);
                Console.WriteLine($"Loaded {entry.Key} to {entry.Value:X4}");
            }
            else
            {
                throw new FileNotFoundException($"Required ROM file {entry.Key} not found in zip!");
            }
        }

        charROM = ExtractRom(archive, "pacman.5e");
        spriteROM = ExtractRom(archive, "pacman.5f");
        paletteROM = ExtractRom(archive, "82s126.4a");
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
        Array.Clear(RAM, 0, RAM.Length);
    }
}