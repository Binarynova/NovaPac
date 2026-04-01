using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using pacman;

public class PacManPCB : IMemoryProvider
{
    GraphicsDevice _graphicsDevice;
    private byte[] Memory = new byte[0x10000];
    public byte[] paletteMemory;
    public byte[] charMemory;
    public byte[] spriteMemory;
    private byte[] u5, u6, u7;
    private byte[] spriteram = new byte[0x10];
    private byte[] spriteram2 = new byte[0x10];
    private NamcoWSG wsg;
    private Z80Cpu cpu;
    
    public List<int[,]> tiles = [];
    public Texture2D[,] TileTextures = new Texture2D[256, 32];
    public List<int[,]> sprites = [];
    public Texture2D[,] SpriteTextures = new Texture2D[64, 32];
    public List<Color> colors = [];
    public List<List<Color>> palettes = [];
    
    public PacManPCB(string romFileName)
    {
        cpu = new Z80Cpu(this);
        wsg = new NamcoWSG(romFileName);
        
        ClearRAM();
        LoadRom(romFileName);
    }

    public void InitializeGraphics(GraphicsDevice device)
    {
        _graphicsDevice = device;
        
        PrepareColors();
        PreparePalettes();
        PrepareTileTextures(_graphicsDevice);
        PrepareSpriteTextures(_graphicsDevice);
    }

    public int Step(bool steppingThrough)
    {
        int cycles = cpu.Step(steppingThrough);
        wsg.Update(cycles);

        return cycles;
    }

    public short[] GetAudioSamples()
    {
        return wsg.DumpSamples();
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
            wsg.UpdateRegister(address, value);
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

    private void PreparePalettes()
    {
        for (int i = 0; i < 32; i++)
        {
            palettes.Add([
                colors[paletteMemory[4*i + 0]],
                colors[paletteMemory[4*i + 1]],
                colors[paletteMemory[4*i + 2]],
                colors[paletteMemory[4*i + 3]]
            ]);
        }
        // second 32 palettes are just black
    }

    public void PrepareTileTextures(GraphicsDevice graphicsDevice)
    {
        for (int tileIndex = 0; tileIndex < 256; tileIndex++)
        {
            int[,] rawTile = ExtractRawTileData(tileIndex);
            for (int paletteIndex = 0; paletteIndex < 32; paletteIndex++)
            {
                Texture2D tileTexture = new Texture2D(graphicsDevice, 8, 8);
                Color[] colorData = new Color[8 * 8];

                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        int colorId = rawTile[x, y];
                        colorData[y * 8 + x] = palettes[paletteIndex][colorId];
                    }
                }
                
                tileTexture.SetData(colorData);
                TileTextures[tileIndex, paletteIndex] = tileTexture;
            }
        }
    }
    
    private int[,] ExtractRawTileData(int tileIndex)
    {
        int[,] tile = new int[8,8];
        for(int i = 0; i < 8; i++) // first 8 bytes of tile
        {
            byte pixelQuad = charMemory[i + (tileIndex * 16)];
            for(int r = 4; r < 8; r++)
            {
                tile[7-i,r] = GetPixelValue(pixelQuad,r);
            }
        }
        for(int i = 8; i < 16; i++) // second 8 bytes of tile
        {
            byte pixelQuad = charMemory[i + (tileIndex * 16)];
            for(int r = 0; r < 4; r++)
            {
                tile[15-i,r] = GetPixelValue(pixelQuad, r);
            }
        }
        
        return tile;
    }

    public void PrepareSpriteTextures(GraphicsDevice graphicsDevice)
    {
        for (int spriteIndex = 0; spriteIndex < 64; spriteIndex++)
        {
            int[,] rawSprite = ExtractRawSpriteData(spriteIndex);
            for (int paletteIndex = 0; paletteIndex < 32; paletteIndex++)
            {
                Texture2D spriteTexture = new Texture2D(graphicsDevice, 16, 16);
                Color[] colorData = new Color[16 * 16];

                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        int colorId = rawSprite[x, y];
                        colorData[y * 16 + x] = palettes[paletteIndex][colorId];
                    }
                }
                
                spriteTexture.SetData(colorData);
                SpriteTextures[spriteIndex, paletteIndex] = spriteTexture;
            }
        }
    }
    
    private int[,] ExtractRawSpriteData(int spriteIndex)
    {
        //for(int spIndex = 0; spIndex < 64; spIndex++)
        //{
            int[,] sprite = new int[16,16];
            for(int i = 0; i < 8; i++) // bottom right
            {
                byte spriteQuad = spriteMemory[(0x00 + i) + (0x40 * spriteIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[15-i,12+r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // top right
            {
                byte spriteQuad = spriteMemory[(0x08 + i) + (0x40 * spriteIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[15-i,r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // top right 2
            {
                byte spriteQuad = spriteMemory[(0x10 + i) + (0x40 * spriteIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[15-i,4+r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // top right 3
            {
                byte spriteQuad = spriteMemory[(0x18 + i) + (0x40 * spriteIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[15-i,8+r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // bottom right
            {
                byte spriteQuad = spriteMemory[(0x20 + i) + (0x40 * spriteIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[7-i,12+r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // top right
            {
                byte spriteQuad = spriteMemory[(0x28 + i) + (0x40 * spriteIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[7-i,r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // top right 2
            {
                byte spriteQuad = spriteMemory[(0x30 + i) + (0x40 * spriteIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[7-i,4+r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // top right 3
            {
                byte spriteQuad = spriteMemory[(0x38 + i) + (0x40 * spriteIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[7-i,8+r] = GetPixelValue(spriteQuad, r);
                }
            }

            return sprite;
            //}
    }

    void PrepareColors()
    {
        // hard-coded because the ROM stores them as intensities of output on hardware, not as color
        colors =
        [
            new Color(0, 0, 0, 0), // 0 alpha to produce transparency
            new Color(255, 0, 0, 255),
            new Color(222, 151, 81, 255),
            new Color(255, 184, 255, 255),
            new Color(0, 0, 0, 255),
            new Color(0, 255, 255, 255),
            new Color(71, 184, 255, 255),
            new Color(255, 184, 81, 255),
            new Color(0, 0, 0, 255),
            new Color(255, 255, 0, 255),
            new Color(0, 0, 0, 255),
            new Color(33, 33, 255, 255),
            new Color(0, 255, 0, 255),
            new Color(71, 184, 174, 255),
            new Color(255, 184, 174, 255),
            new Color(222, 222, 255, 255)
        ];
    }

    int GetPixelValue(byte pixelData, int pixelIndex)
    {
        int paletteValue = 0;
        // returns the palette value of a pixel
        switch(pixelIndex)
        {
            case 0 or 4:
                if((pixelData & 0x80) != 0)
                    paletteValue += 2;
                if((pixelData & 0x08) != 0)
                    paletteValue += 1;
                break;
            case 1 or 5:
                if((pixelData & 0x40) != 0)
                    paletteValue += 2;
                if((pixelData & 0x04) != 0)
                    paletteValue += 1;
                break;
            case 2 or 6:
                if((pixelData & 0x20) != 0)
                    paletteValue += 2;
                if((pixelData & 0x02) != 0)
                    paletteValue += 1;
                break;
            case 3 or 7:
                if((pixelData & 0x10) != 0)
                    paletteValue += 2;
                if((pixelData & 0x01) != 0)
                    paletteValue += 1;
                break;
        }

        return paletteValue;
    }
}