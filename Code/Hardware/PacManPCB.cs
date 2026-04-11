using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class PacManPCB : IArcadeMachine
{
    GraphicsDevice _graphicsDevice;
    private PacManMemoryMap memoryBus;
    private byte[] Memory = new byte[0x10000];
    byte[] paletteMemory;
    byte[] charMemory;
    byte[] spriteMemory;
    private byte[] u5, u6, u7;
    private byte[] spriteram = new byte[0x10];
    private byte[] spriteram2 = new byte[0x10];
    private NamcoWSG wsg;
    private Z80Cpu cpu;
    byte[] AuxROMs = null;

    Texture2D[,] TileTextures = new Texture2D[256, 32];
    Texture2D[,] SpriteTextures = new Texture2D[64, 32];
    List<Color> colors = [];
    List<List<Color>> palettes = [];

    public int mode { get; set; } = 0;
    public int graphicsViewerMode { get; set; } = 0;
    const int tileWidth = 8;
    const int spriteWidth = 16;
    List<int> tileViewerPalettes = [1, 3, 5, 7, 9, 14, 15, 16, 17, 18, 20, 21, 22, 23, 24, 25, 26, 27, 29, 30, 31];
    public int tileViewerPaletteIndex { get; set; } = 0;
    bool neonHackEnabled = false;
    public bool secondPlayerFlip
    {
        get => memoryBus.SecondPlayerFlip;
        set => memoryBus.SecondPlayerFlip = value;
    }
    public List<int> subOptionIndices
    {
        get => memoryBus.SubOptionIndices;
        set => memoryBus.SubOptionIndices = value;
    }
    
    private List<DrawRequest> requests = new ();
    public struct DrawRequest
    {
        public Texture2D Texture;
        public Vector2 Position;
        public SpriteEffects Effects;
    }
    
    public PacManPCB(string romFileName, bool twoPlayerScreenFlip)
    {
        LoadRom(romFileName);
        wsg = new NamcoWSG(romFileName);
        memoryBus = new PacManMemoryMap(Memory, AuxROMs, spriteram, spriteram2, wsg);
        memoryBus.AuxBoardEnabled = (romFileName == "roms/mspacman.zip");
        memoryBus.DecryptEnabled = (romFileName == "roms/mspacman.zip");
        memoryBus.SecondPlayerFlip = twoPlayerScreenFlip;
        memoryBus.SteamDeckTwoPlayerMode = twoPlayerScreenFlip;
        cpu = new Z80Cpu(memoryBus);
        
        mode = 0;
    }
    
    private void DrawGameSprites(bool secondPlayFlip)
    {
        for (int sprite = 7; sprite >= 0; sprite--)
        {
            int offset = sprite * 2;
            int attr = GetSpriteRam(offset);
            int paletteIndex = GetSpriteRam(offset + 1) & 0x1F;
                
            int spriteIndex = (attr & 0xFC) >> 2;
            bool xFlip = (attr & 0x02) != 0;
            bool yFlip = (attr & 0x01) != 0;

            // So, it seems that Pac-Man is written some way that automatically flips the sprites
            // but not the tiles, for the second player in cocktail mode.
            // How do the tiles flip then? I don't know.
            // But either way, this is to compensate for that, since I'm flipping everything,
            // the sprites have to be un-flipped.
            int screenX, screenY;
            if (secondPlayFlip)
            {
                screenX = GetSpriteRam2(offset) - 31;
                screenY = GetSpriteRam2(offset + 1);
            }
            else
            {
                screenX = 224 - GetSpriteRam2(offset) + 15;
                screenY = 288 - 16 - GetSpriteRam2(offset + 1);
            }

            SpriteEffects effects = SpriteEffects.None;

            if (xFlip) effects |= SpriteEffects.FlipHorizontally;
            if (yFlip) effects |= SpriteEffects.FlipVertically;
            
            // See above.
            if (secondPlayFlip)
            {
                effects = ~effects;
            }
        
            requests.Add(new DrawRequest {
                Texture = SpriteTextures[spriteIndex, paletteIndex],
                Position = new Vector2(screenX, screenY),
                Effects = effects
            });
        }
    }
    
    private void DrawGameTiles()
    {
        // Row 1 and 2
        for (int i = 0x3DF; i >= 0x3C0; i--)
        {
            for (int row = 0; row < 2; row++)
            {
                int tileAddress = 0x4000 + i + 0x20 * row;
                int paletteAddress = 0x4400 + i + 0x20 * row;
            
                byte tileIndex = memoryBus.ReadByte((ushort)tileAddress);
                int paletteIndex = memoryBus.ReadByte((ushort)paletteAddress) & 0x1F;

                int yPos = 0 + 8 * row;
                int xPos = 232 - (i - 0x3C0) * 8;
            
                requests.Add(new DrawRequest {
                    Texture = TileTextures[tileIndex, paletteIndex],
                    Position = new Vector2(xPos, yPos)
                });
            }
        }
        
        // Main Grid
        for (int i = 0x05F; i >= 0x040; i--)
        {
            for (int col = 0; col < 28; col++)
            {
                int tileAddress = 0x4000 + i + 0x20 * col;
                int paletteAddress = 0x4400 + i + 0x20 * col;
            
                byte tileIndex = memoryBus.ReadByte((ushort)tileAddress);
                int paletteIndex = memoryBus.ReadByte((ushort)paletteAddress) & 0x1F;
        
                int yPos = 8 * (i - 0x040) + 16;
                int xPos = 216 - (col) * 8;
            
                requests.Add(new DrawRequest {
                    Texture = TileTextures[tileIndex, paletteIndex],
                    Position = new Vector2(xPos, yPos)
                });
            }
        }
        
        // Bottom 2 Rows
        for (int i = 0x01F; i >= 0x000; i--)
        {
            for (int row = 0; row < 2; row++)
            {
                int tileAddress = 0x4000 + i + 0x20 * row;
                int paletteAddress = 0x4400 + i + 0x20 * row;
            
                byte tileIndex = memoryBus.ReadByte((ushort)tileAddress);
                int paletteIndex = memoryBus.ReadByte((ushort)paletteAddress) & 0x1F;
        
                int yPos = 272 + 8 * row;
                int xPos = 232 - (i - 0x000) * 8;
            
                requests.Add(new DrawRequest {
                    Texture = TileTextures[tileIndex, paletteIndex],
                    Position = new Vector2(xPos, yPos)
                });
            }
        }
    }
    
    void DrawTileAndSpriteViewer()
    {
        const int padding = 1;
        switch (graphicsViewerMode)
        {
            // view tiles
            case 0:
            {
                for(int i = 0; i < 16; i++)
                {
                    for(int j = 0; j < 16; j++)
                    {
                        int tileIndex = j * 16 + i;
                        int tileXPos = i * (tileWidth + padding) + padding;
                        int tileYPos = j * (tileWidth + padding) + padding;
                
                        requests.Add(new DrawRequest {
                            Texture = TileTextures[tileIndex, tileViewerPalettes[tileViewerPaletteIndex]],
                            Position = new Vector2(tileXPos, tileYPos)
                        });
                    }
                }

                break;
            }
            // view sprites
            case 1:
            {
                for(int i = 0; i < 8; i++)
                {
                    for(int j = 0; j < 8; j++)
                    {
                        int spriteIndex = j * 8 + i;
                        int spriteXPos = i * (spriteWidth + padding) + padding;
                        int spriteYPos = j * (spriteWidth + padding) + padding;
        
                        requests.Add(new DrawRequest {
                            Texture = SpriteTextures[spriteIndex, tileViewerPalettes[tileViewerPaletteIndex]],
                            Position = new Vector2(spriteXPos, spriteYPos),
                            Effects = SpriteEffects.None
                        });
                    }
                }

                break;
            }
        }
    }
    
    public List<DrawRequest> GetDrawRequests(bool secondPlayFlip)
    {
        requests.Clear();

        switch (mode)
        {
            case 1:
                DrawGameTiles();
                DrawGameSprites(secondPlayFlip);
                break;
            case 2:
                DrawTileAndSpriteViewer();
                break;
        }

        return requests;
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
    
    private void LoadRom(string romFileName)
    {
        switch (romFileName)
        {
            case "roms/pacman.zip" or "roms/matrix.zip":
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
                if (romFileName == "roms/pacman.zip")
                {
                    if(neonHackEnabled)
                        LoadPacmanNeonHack();
                }

                break;
            }
            case "roms/pacplus.zip":
            {
                using ZipArchive archive = ZipFile.OpenRead(romFileName);
                var romMap = new Dictionary<string, int>
                {
                    { "pacplus.6e", 0x0000 },
                    { "pacplus.6f", 0x1000 },
                    { "pacplus.6h", 0x2000 },
                    { "pacplus.6j", 0x3000 }
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

                charMemory = ExtractRom(archive, "pacplus.5e");
                spriteMemory = ExtractRom(archive, "pacplus.5f");
                paletteMemory = ExtractRom(archive, "pacplus.4a");

                for (int i = 0; i < charMemory.Length; i++) charMemory[i] = PacPlusDecryptGraphics(charMemory[i]);
                for (int i = 0; i < spriteMemory.Length; i++) spriteMemory[i] = PacPlusDecryptGraphics(spriteMemory[i]);
            
                for (int i = 0; i < 0x4000; i++)
                {
                    Memory[i] = (byte)pacPlusDecrypt(i, Memory[i]);
                }

                break;
            }
            case "roms/newpuckx.zip":
            {
                using ZipArchive archive = ZipFile.OpenRead(romFileName);
                var romMap = new Dictionary<string, int>
                {
                    { "puckman.6e", 0x0000 },
                    { "pacman.6f", 0x1000 },
                    { "puckman.6h", 0x2000 },
                    { "puckman.6j", 0x3000 }
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
                break;
            }
            case "roms/mspacman.zip":
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
                byte[] codeRom1 = ExtractRom(archive, "pacman.6e");
                byte[] codeRom2 = ExtractRom(archive, "pacman.6f");
                byte[] codeRom3 = ExtractRom(archive, "pacman.6h");

                AuxROMs = new byte[(16 + 10) * 1024];

                for (int i = 0; i < 0x1000; i++)
                {
                    AuxROMs[decryptAddr1((uint)i) + 0x4000] = (byte)decryptData(u7[i]);
                    AuxROMs[decryptAddr1((uint)i) + 0x5000] = (byte)decryptData(u6[i]);
                }

                for (int i = 0; i < 0x0800; i++)
                {
                    AuxROMs[decryptAddr2((uint)i) + 0x6000] = (byte)decryptData(u5[i]);
                }
            
                Array.Copy(codeRom1, 0, AuxROMs, 0x0000, 0x1000);
                Array.Copy(codeRom2, 0, AuxROMs, 0x1000, 0x1000);
                Array.Copy(codeRom3, 0, AuxROMs, 0x2000, 0x1000);
                Array.Copy(AuxROMs, 0x4000, AuxROMs, 0x3000, 0x1000);

                for (int i = 0; i < 8; i++)
                {
                    AuxROMs[0x0410 + i] = AuxROMs[0x6008 + i];
                    AuxROMs[0x08E0 + i] = AuxROMs[0x61D8 + i];
                    AuxROMs[0x0A30 + i] = AuxROMs[0x6118 + i];
                    AuxROMs[0x0BD0 + i] = AuxROMs[0x60D8 + i];
                    AuxROMs[0x0C20 + i] = AuxROMs[0x6120 + i];
                    AuxROMs[0x0E58 + i] = AuxROMs[0x6168 + i];
                    AuxROMs[0x0EA8 + i] = AuxROMs[0x6198 + i];

                    AuxROMs[0x1000 + i] = AuxROMs[0x6020 + i];
                    AuxROMs[0x1008 + i] = AuxROMs[0x6010 + i];
                    AuxROMs[0x1288 + i] = AuxROMs[0x6098 + i];
                    AuxROMs[0x1348 + i] = AuxROMs[0x6048 + i];
                    AuxROMs[0x1688 + i] = AuxROMs[0x6088 + i];
                    AuxROMs[0x16B0 + i] = AuxROMs[0x6188 + i];
                    AuxROMs[0x16D8 + i] = AuxROMs[0x60C8 + i];
                    AuxROMs[0x16F8 + i] = AuxROMs[0x61C8 + i];
                    AuxROMs[0x19A8 + i] = AuxROMs[0x60A8 + i];
                    AuxROMs[0x19B8 + i] = AuxROMs[0x61A8 + i];

                    AuxROMs[0x2060 + i] = AuxROMs[0x6148 + i];
                    AuxROMs[0x2108 + i] = AuxROMs[0x6018 + i];
                    AuxROMs[0x21A0 + i] = AuxROMs[0x61A0 + i];
                    AuxROMs[0x2298 + i] = AuxROMs[0x60A0 + i];
                    AuxROMs[0x23E0 + i] = AuxROMs[0x60E8 + i];
                    AuxROMs[0x2418 + i] = AuxROMs[0x6000 + i];
                    AuxROMs[0x2448 + i] = AuxROMs[0x6058 + i];
                    AuxROMs[0x2470 + i] = AuxROMs[0x6140 + i];
                    AuxROMs[0x2488 + i] = AuxROMs[0x6080 + i];
                    AuxROMs[0x24B0 + i] = AuxROMs[0x6180 + i];
                    AuxROMs[0x24D8 + i] = AuxROMs[0x60C0 + i];
                    AuxROMs[0x24F8 + i] = AuxROMs[0x61C0 + i];
                    AuxROMs[0x2748 + i] = AuxROMs[0x6050 + i];
                    AuxROMs[0x2780 + i] = AuxROMs[0x6090 + i];
                    AuxROMs[0x27B8 + i] = AuxROMs[0x6190 + i];
                    AuxROMs[0x2800 + i] = AuxROMs[0x6028 + i];
                    AuxROMs[0x2B20 + i] = AuxROMs[0x6100 + i];
                    AuxROMs[0x2B30 + i] = AuxROMs[0x6110 + i];
                    AuxROMs[0x2BF0 + i] = AuxROMs[0x61D0 + i];
                    AuxROMs[0x2CC0 + i] = AuxROMs[0x60D0 + i];
                    AuxROMs[0x2CD8 + i] = AuxROMs[0x60E0 + i];
                    AuxROMs[0x2CF0 + i] = AuxROMs[0x61E0 + i];
                    AuxROMs[0x2D60 + i] = AuxROMs[0x6160 + i];
                }

                break;
            }
        }
    }

    private void LoadPacmanNeonHack()
    {
        Array.Copy(LoadSpriteROM("roms/hacks/neon/pacman/pacman.5f"),spriteMemory,4096);
        Array.Copy(LoadCharROM("roms/hacks/neon/pacman/pacman.5e"),charMemory,4096);
    }
    
    private static byte[] LoadCharROM(string path)
    {
        byte[] charROM = File.ReadAllBytes(path);

        if (charROM.Length != 4096)
            throw new Exception($"Unexpected ROM size: {charROM.Length} bytes");

        return charROM;
    }

    private static byte[] LoadSpriteROM(string path)
    {
        byte[] spriteROM = File.ReadAllBytes(path);

        if (spriteROM.Length != 4096)
            throw new Exception($"Unexpected ROM size: {spriteROM.Length} bytes");

        return spriteROM;
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
    
    // Method for decrypting Pac-Man Plus
    private static uint pacPlusDecrypt(int addr, byte e)
    {
        byte[][] swapXorTable = new byte[6][]
        {
            [ 7,6,5,4,3,2,1,0, 0x00],
            [ 7,6,5,4,3,2,1,0, 0x28],
            [ 6,1,3,2,5,7,0,4, 0x96],
            [ 6,1,5,2,3,7,0,4, 0xBE],
            [ 0,3,7,6,4,2,1,5, 0xD5],
            [ 0,3,4,6,7,2,1,5, 0xDD]
        };
        int[] pickTable = new int[32]
        {
            0,2,4,2,4,0,4,2,2,0,2,2,4,0,4,2,
            2,2,4,0,4,2,4,0,0,4,0,4,4,2,4,2
        };

        uint method = (uint)pickTable[
            (addr & 0x001) |
            ((addr & 0x004) >> 1) |
            ((addr & 0x020) >> 3) |
            ((addr & 0x080) >> 4) |
            ((addr & 0x200) >> 5)];

        if ((addr & 0x800) == 0x800)
            method ^= 1;

        byte[] tbl = swapXorTable[method];
        int res = 0;
        for (int i = 0; i < 8; i++)
        {
            // If the bit at the position defined by the table is set...
            if ((e & (1 << tbl[i])) != 0)
            {
                // ...set the current bit in our result
                res |= (1 << i);
            }
        }

        // Final XOR step
        return (byte)(res ^ tbl[8]);
    }
    
    private static byte PacPlusDecryptGraphics(byte data)
    {
        // Pac-Man Plus graphics use a constant bit-swap encryption
        int res = 0;
        if ((data & 0x01) != 0) res |= 0x01;
        if ((data & 0x02) != 0) res |= 0x10;
        if ((data & 0x04) != 0) res |= 0x02;
        if ((data & 0x08) != 0) res |= 0x20;
        if ((data & 0x10) != 0) res |= 0x04;
        if ((data & 0x20) != 0) res |= 0x40;
        if ((data & 0x40) != 0) res |= 0x08;
        if ((data & 0x80) != 0) res |= 0x80;
        return (byte)res;
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

    private void PrepareTileTextures(GraphicsDevice graphicsDevice)
    {
        for (int tileIndex = 0; tileIndex < 256; tileIndex++)
        {
            int[,] rawTile = ExtractRawTileData(tileIndex);
            for (int paletteIndex = 0; paletteIndex < 32; paletteIndex++)
            {
                Texture2D tileTexture = new (graphicsDevice, 8, 8);
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

    private void PrepareSpriteTextures(GraphicsDevice graphicsDevice)
    {
        for (int spriteIndex = 0; spriteIndex < 64; spriteIndex++)
        {
            int[,] rawSprite = ExtractRawSpriteData(spriteIndex);
            for (int paletteIndex = 0; paletteIndex < 32; paletteIndex++)
            {
                Texture2D spriteTexture = new (graphicsDevice, 16, 16);
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

    static int GetPixelValue(byte pixelData, int pixelIndex)
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