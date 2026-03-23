using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.Xna.Framework.Input;

public class Pacman : IMemoryProvider
{
    private byte[] Memory = new byte[0x10000];
    public byte[] paletteMemory;
    public byte[] charMemory;
    public byte[] spriteMemory;
    private byte[] spriteram = new byte[0x10];
    private byte[] spriteram2 = new byte[0x10];
    private Z80 Cpu;

    public Pacman(string romFileName)
    {
        ClearRAM();
        LoadRom(romFileName);
    }
    
    public byte GetSpriteRam(int index)
    {
        return spriteram[index];
    }
    
    public byte GetSpriteRam2(int index)
    {
        return spriteram2[index];
    }

    public void AttachCPU(Z80 cpuInstance)
    {
        Cpu = cpuInstance;
    }
    public byte ReadByte(ushort address)
    {
        switch (address)
        {
            case >= 0x5000 and <= 0x503F: // IN0 (Joystick, Coin, etc.)
                return GetPort0();
            
            case >= 0x5040 and <= 0x507F: // IN1 (Player Start, Service, etc.)
                return GetPort1();
            
            case >= 0x5080 and <= 0x50BF: // DIP Switches
                return 0x89;
            
            default:
                return Memory[address];
        }
    }

    public void WriteByte(ushort address, byte value)
    {
        if (address < 0x4000)
            return;
        
        if (address is >= 0x4ff0 and <= 0x4fff)
        {
            spriteram[address - 0x4ff0] = value;
        }
        
        if (address is >= 0x5060 and <= 0x506f)
        {
            spriteram2[address - 0x5060] = value;
            return;
        }
            
        Memory[address] = value;
    }
    
    private void LoadRom(string romFileName)
    {
        using ZipArchive archive = ZipFile.OpenRead(romFileName);
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

        charMemory = ExtractRom(archive, "pacman.5e");
        spriteMemory = ExtractRom(archive, "pacman.5f");
        paletteMemory = ExtractRom(archive, "82s126.4a");
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

    public byte GetPort0()
    {
        byte port = 0xFF;
        var state = Keyboard.GetState();

        if (state.IsKeyDown(Keys.C))
            port &= 0xEF;
        
        return port;
    }
    
    public byte GetPort1()
    {
        byte port = 0xFF;
        var state = Keyboard.GetState();

        if (state.IsKeyDown(Keys.Enter))
            port &= 0x00;

        return port;
    }
}