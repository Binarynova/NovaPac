using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using pacman;

public class Pacman : IMemoryProvider
{
    private byte[] Memory = new byte[0x10000];
    public byte[] paletteMemory;
    public byte[] charMemory;
    public byte[] spriteMemory;
    private byte[] u5, u6, u7;
    private byte[] spriteram = new byte[0x10];
    private byte[] spriteram2 = new byte[0x10];
    private WSG soundGenerator;
    Z80 cpu;
    
    public Pacman(string romFileName)
    {
        cpu = new Z80(this);
        soundGenerator = new WSG(romFileName);
        
        ClearRAM();
        LoadRom(romFileName);
    }

    public int Step(bool steppingThrough)
    {
        int cycles = cpu.Step(steppingThrough);

        return cycles;
    }

    public void TriggerVBlankInterrupt()
    {
        cpu.RequestInterrupt();
    }
    
    public byte GetSpriteRam(int index)
    {
        return spriteram[index];
    }
    
    public byte GetSpriteRam2(int index)
    {
        return spriteram2[index];
    }
    
    private ushort NormalizeAddress(ushort address)
    {
        // 1. ROM (0x0000-0x3FFF) and I/O (0x5000-0x50FF) are NOT mirrored RAM.
        if (address < 0x4000 || (address >= 0x5000 && address <= 0x50FF))
        {
            return address;
        }

        // 2. Everything else (0x4000-0x4FFF, 0x8000-0x8FFF, 0xC000-0xCFFF, etc.)
        // maps down to the primary 4KB RAM block at 0x4000.
        // (address & 0x0FFF) gets the offset within any 4KB bank.
        return (ushort)(0x4000 | (address & 0x0FFF));
    }

    public byte ReadByte(ushort address)
    {
        // Check hardware ports first (using the original address)
        if (address >= 0x5000 && address <= 0x503F) return GetPort0();
        if (address >= 0x5040 && address <= 0x507F) return GetPort1();
        if (address >= 0x5080 && address <= 0x50BF) return 0xC9; // DIPs

        // For all other memory (ROM or RAM), use the normalized address
        return Memory[NormalizeAddress(address)];
    }

    public void WriteByte(ushort address, byte value)
    {
        if (address < 0x4000) return; // Protect ROM

        // Intercept sound writes
        if (address >= 0x5040 && address <= 0x505F)
        {
            soundGenerator.UpdateRegister(address, value);
            return;
        }
        
        // Handle special non-mirrored I/O writes (Sync bus)
        if (address >= 0x5060 && address <= 0x506F)
        {
            spriteram2[address - 0x5060] = value;
            return;
        }

        ushort normAddr = NormalizeAddress(address);

        // Sync Sprite RAM 1
        // 0x4FF0 is the canonical location for sprite data
        if (normAddr >= 0x4FF0 && normAddr <= 0x4FFF)
        {
            spriteram[normAddr - 0x4FF0] = value;
        }

        // Store the value in our normalized "canonical" RAM block
        Memory[normAddr] = value;
    }
    
    private void LoadRom(string romFileName)
    {
        if (romFileName == "roms/pacman.zip" || romFileName == "roms/matrix.zip" || romFileName == "roms/newpuckx.zip")
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
        else if (romFileName == "roms/mspacman.zip")
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

            charMemory = ExtractRom(archive, "5e");
            spriteMemory = ExtractRom(archive, "5f");
            paletteMemory = ExtractRom(archive, "82s126.4a");
            
            u5 = ExtractRom(archive, "u5");
            u6 = ExtractRom(archive, "u6");
            u7 = ExtractRom(archive, "u7");
        }
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

    private static byte GetPort0()
    {
        KeyboardState state = Keyboard.GetState();
        GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);

        return (byte)
        (
            ((state.IsKeyDown(Keys.Up) || gamePadState.DPad.Up == ButtonState.Pressed || gamePadState.ThumbSticks.Left.Y >= 0.5f ? 0 : 1) << 0)
            | ((state.IsKeyDown(Keys.Left) || gamePadState.DPad.Left == ButtonState.Pressed || gamePadState.ThumbSticks.Left.X <= -0.5f ? 0 : 1) << 1)
            | ((state.IsKeyDown(Keys.Right) || gamePadState.DPad.Right == ButtonState.Pressed || gamePadState.ThumbSticks.Left.X >= 0.5f ? 0 : 1) << 2)
            | ((state.IsKeyDown(Keys.Down) || gamePadState.DPad.Down == ButtonState.Pressed || gamePadState.ThumbSticks.Left.Y <= -0.5f ? 0 : 1) << 3)
            | ((state.IsKeyDown(Keys.S) ? 0 : 1) << 4)
            | ((state.IsKeyDown(Keys.C) || gamePadState.Buttons.Back == ButtonState.Pressed ? 0 : 1) << 5)
            | ((state.IsKeyDown(Keys.D) ? 0 : 1) << 6)
            | ((state.IsKeyDown(Keys.M) ? 0 : 1) << 7)
        );
    }
    
    private static byte GetPort1()
    {
        KeyboardState state = Keyboard.GetState();
        GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);
        
        return (byte)
        (
            ((state.IsKeyDown(Keys.NumPad8) ? 0 : 1) << 0)
            | ((state.IsKeyDown(Keys.NumPad4) ? 0 : 1) << 1)
            | ((state.IsKeyDown(Keys.NumPad6) ? 0 : 1) << 2)
            | ((state.IsKeyDown(Keys.NumPad2) ? 0 : 1) << 3)
            | ((state.IsKeyDown(Keys.T) ? 0 : 1) << 4)
            | ((state.IsKeyDown(Keys.Enter) || gamePadState.Buttons.Start == ButtonState.Pressed ? 0 : 1) << 5)
            | ((state.IsKeyDown(Keys.Tab) ? 0 : 1) << 6)
            | (0x1 << 7) // 1 for upright, 0 for cocktail
        );
    }

    // methods for decrypting Ms. Pac-Man data and applying the data to Pac-Man
    // referenced from JustinCredible and MAME
    private static uint decryptData(uint data)
    {
        uint decryptedData = (data & 0x80) >> 3;
        decryptedData |= (data & 0x40) >> 3;
        decryptedData |= data & 0x20;
        decryptedData |= (data & 0x10) << 2;
        decryptedData |= (data & 0x08) >> 1;
        decryptedData |= (data & 0x04) >> 1;
        decryptedData |= (data & 0x02) >> 1;
        decryptedData |= (data & 0x01) << 7;
        
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