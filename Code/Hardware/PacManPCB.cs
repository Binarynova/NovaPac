using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.ImGuiNet;

public class PacManPCB : IArcadeMachine
{
    GraphicsDevice _graphicsDevice;
    private PacManMemoryBus memoryBus;
    private NamcoWSG wsg;
    private Z80Cpu cpu;
    
    private byte[] _maincpu;
    private byte[] _gfx1;
    private byte[] _proms;
    private byte[] _namco;
    
    private byte[] spriteram = new byte[0x10];
    private byte[] spriteram2 = new byte[0x10];
    byte[] _decryptedRom = null;
    string buttonText = "Tiles";

    Texture2D[,] TileTextures = new Texture2D[256, 32];
    Texture2D[,] SpriteTextures = new Texture2D[64, 32];
    List<Color> colors = [];
    List<List<Color>> palettes = [];

    public int mode { get; set; } = 0;
    private int graphicsViewerMode { get; set; } = 0;
    const int tileWidth = 8;
    const int spriteWidth = 16;
    List<int> tileViewerPalettes = [1, 3, 5, 7, 9, 14, 15, 16, 17, 18, 20, 21, 22, 23, 24, 25, 26, 27, 29, 30, 31];
    private int tileViewerPaletteIndex { get; set; } = 0;
    int pIndex = 0;
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
    
    public PacManPCB(string romFileNameandPath, List<int> indices)
    {
        string romFileName = Path.GetFileName(romFileNameandPath);
        LoadRom(romFileNameandPath);
        wsg = new NamcoWSG(_namco);
        memoryBus = new PacManMemoryBus(_decryptedRom, spriteram, spriteram2, wsg, _maincpu);
        memoryBus.PlayingMsPacMan = (romFileName is "mspacman" or "mspacmnf");
        memoryBus.SecondPlayerFlip = indices[0] == 1;
        memoryBus.SteamDeckTwoPlayerMode = indices[0] == 1;
        memoryBus.SubOptionIndices = new List<int>(indices);
        cpu = new Z80Cpu(memoryBus);
        
        mode = 0;
    }

    public int Step()
    {
        int cycles = cpu.Step();
        wsg.Update(cycles);

        return cycles;
    }

    public void InitializeGraphics(GraphicsDevice device)
    {
        _graphicsDevice = device;
        
        PrepareColors();
        PreparePalettes();
        PrepareTileTextures(_graphicsDevice);
        PrepareSpriteTextures(_graphicsDevice);
    }

    public void DrawSpriteRamViewer(ImGuiRenderer renderer)
    {
        if (ImGui.BeginTable("SpriteRam", 8))
        {
            for (int i = 0; i < 8; i++)
            {
                int offset = i * 2;
                int attr = GetSpriteRam(offset);
                int paletteIndex = GetSpriteRam(offset + 1) & 0x1F;
                
                int spriteIndex = (attr & 0xFC) >> 2;
                ImGui.TableNextColumn();
                IntPtr texturePtr = renderer.BindTexture(SpriteTextures[spriteIndex, paletteIndex]);
                ImGui.Image(texturePtr, new System.Numerics.Vector2(32, 32));
            }
            ImGui.EndTable();
        }
    }

    public void DrawMemoryViewer()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0, 0));
        ImGuiTableFlags flags = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoHostExtendX;
        
        if (ImGui.BeginTable("Memory Viewer", 18, flags))
        {
            for (int j = 0; j < 256; j++)
            {
                ImGui.TableNextColumn();
                ImGui.Text($"{(j * 16):X8}");
                ImGui.TableNextColumn();
                ImGui.Text($"|");
                for (int i = 0; i < 16; i++)
                {
                    ImGui.TableNextColumn();
                    ImGui.Text($"{memoryBus.ReadByte((ushort)(i + 16 * j)):X2}");
                }
            }
            ImGui.EndTable();
        }
        
        ImGui.PopStyleVar();
    }
    
    public void DrawGraphicsViewer(ImGuiRenderer renderer)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new System.Numerics.Vector2(0, 0));
        ImGuiTableFlags flags = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoHostExtendX;
        switch (graphicsViewerMode)
        {
            case 0:
                if (ImGui.BeginTable("Tiles", 16, flags))
                {
                    for (int i = 0; i < 256; i++)
                    {
                        ImGui.TableNextColumn();
                        IntPtr texturePtr = renderer.BindTexture(TileTextures[i, tileViewerPalettes[tileViewerPaletteIndex]]);
                        ImGui.Image(texturePtr, new System.Numerics.Vector2(16, 16));
                    }
                    ImGui.EndTable();
                }
                break;
            case 1:
                if (ImGui.BeginTable("Sprites", 8, flags))
                {
                    for (int i = 0; i < 64; i++)
                    {
                        ImGui.TableNextColumn();
                        IntPtr texturePtr = renderer.BindTexture(SpriteTextures[i, tileViewerPalettes[tileViewerPaletteIndex]]);
                        ImGui.Image(texturePtr, new System.Numerics.Vector2(32, 32));
                    }
                    ImGui.EndTable();
                }
                break;
        }
        
        ImGui.PopStyleVar();

        if (ImGui.Button(buttonText))
        {
            switch (graphicsViewerMode)
            {
                case 0:
                    graphicsViewerMode = 1;
                    buttonText = "Sprites";
                    break;
                case 1:
                    graphicsViewerMode = 0;
                    buttonText = "Tiles";
                    break;
            }
        }

        if (ImGui.InputInt("Palette", ref pIndex))
        {
            if (pIndex < 0)
                pIndex = 0;
            if (pIndex >= tileViewerPalettes.Count)
                pIndex = tileViewerPalettes.Count - 1;
            
            tileViewerPaletteIndex = pIndex;
        }
    }
    public void DrawDebugUI(ImGuiRenderer renderer)
    {
        ImGui.Begin("Graphics Viewer");
        ImGui.SetWindowSize(new System.Numerics.Vector2(300, 340));
        DrawGraphicsViewer(renderer);
        ImGui.End();

        ImGui.Begin("Sprite RAM Viewer");
        ImGui.SetWindowSize(new System.Numerics.Vector2(340, 80));
        DrawSpriteRamViewer(renderer);
        ImGui.End();
        
        ImGui.Begin("Memory Viewer");
        ImGui.SetWindowSize(new System.Numerics.Vector2(460, 200));
        DrawMemoryViewer();
        ImGui.End();
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
    
    private static byte[] ExtractRom(ZipArchive archive, string fileName)
    {
        ZipArchiveEntry entry = archive.GetEntry(fileName);
        if (entry == null) throw new FileNotFoundException($"Missing {fileName}");

        using Stream s = entry.Open();
        byte[] data = new byte[entry.Length];
        s.ReadExactly(data, 0, data.Length);
        return data;
    }

    private void LoadRegionIntoMemory(RomSets.RomRegion region, ZipArchive archive, byte[] destination)
    {
        foreach (var file in region.Files)
        {
            var entry = archive.GetEntry(file.Filename);
            if (entry == null) continue;
            
            using Stream s = entry.Open();

            Span<byte> targetSlice = destination.AsSpan((int)file.Offset, (int)file.Length);
            
            s.ReadExactly(targetSlice);
        }
    }

    private void LoadMergedRomsIntoMemory(ZipArchive archive)
    {
        byte[] prom9e = ExtractRom(archive, "jr.pac-man_9e_11-9-83.9e"); // Low nibble
        byte[] prom9f = ExtractRom(archive, "jr.pac-man_9f_11-9-83.9f"); // High nibble
        byte[] prom9p = ExtractRom(archive, "jr.pac-man_9p_11-9-83.9p"); // Lookup table

        for (int i = 0; i < 32; i++)
        {
            // 9f (High Nibble) << 4 | 9e (Low Nibble)
            _proms[i] = (byte)((prom9f[i] << 4) | (prom9e[i] & 0x0F));
        }

        Array.Copy(prom9p, 0, _proms, 0x20, 256);
    }
    
    private void LoadRom(string romFileNameandPath)
    {
        string romfile = Path.GetFileName(romFileNameandPath);
        _maincpu = new byte[RomSets.Get(romfile)["maincpu"].Size];
        _gfx1 = new byte[RomSets.Get(romfile)["gfx1"].Size];
        _proms = new byte[RomSets.Get(romfile)["proms"].Size];
        _namco = new byte[RomSets.Get(romfile)["namco"].Size];
        
        using ZipArchive archive = ZipFile.OpenRead(romFileNameandPath + ".zip");
        
        LoadRegionIntoMemory(RomSets.Get(romfile)["maincpu"], archive, _maincpu);
        LoadRegionIntoMemory(RomSets.Get(romfile)["gfx1"], archive, _gfx1);
        if (romfile != "jrpacman")
        {
            LoadRegionIntoMemory(RomSets.Get(romfile)["proms"], archive, _proms);
        }
        else
        {
            LoadMergedRomsIntoMemory(archive);
        }
        LoadRegionIntoMemory(RomSets.Get(romfile)["namco"], archive, _namco);
        
        if(romfile is "mspacman" or "mspacmnf")
        {
            var daughterBoard = new MsPacManDaughterBoard(_maincpu);
            daughterBoard.Initialize();

            _decryptedRom = daughterBoard.DecryptedMemory;
        }

        if (romfile is "pacplus")
        {
            var daughterBoard = new PacManPlusDaughterBoard(_maincpu);
            daughterBoard.Initialize();

            Array.Copy(daughterBoard.DecryptedMemory, _maincpu, 0x4000);
        }
    }

    private void PreparePalettes()
    {
        for (int i = 0; i < 32; i++)
        {
            palettes.Add([
                colors[_proms[4*i + 0 + 0x20]],
                colors[_proms[4*i + 1 + 0x20]],
                colors[_proms[4*i + 2 + 0x20]],
                colors[_proms[4*i + 3 + 0x20]]
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

    void PrepareColors()
    {
        colors.Clear();
        
        for (int i = 0; i < 32; i++)
        {
            byte data = _proms[i];

            // Red: Bits 0, 1, 2 (Weights: 0x21, 0x47, 0x97)
            int r = 0x21 * ((data >> 0) & 1) + 0x47 * ((data >> 1) & 1) + 0x97 * ((data >> 2) & 1);
        
            // Green: Bits 3, 4, 5 (Weights: 0x21, 0x47, 0x97)
            int g = 0x21 * ((data >> 3) & 1) + 0x47 * ((data >> 4) & 1) + 0x97 * ((data >> 5) & 1);
        
            // Blue: Bits 6, 7 (Weights: 0x51, 0xAE)
            int b = 0x51 * ((data >> 6) & 1) + 0xAE * ((data >> 7) & 1);

            int a = (r == 0 && g == 0 && b == 0) ? 0 : 255;

            colors.Add(new Color(r, g, b, a));
        }
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
    
    private int[,] ExtractRawTileData(int tileIndex)
    {
        int[,] tile = new int[8,8];
        for(int i = 0; i < 8; i++) // first 8 bytes of tile
        {
            byte pixelQuad = _gfx1[i + (tileIndex * 16)];
            for(int r = 4; r < 8; r++)
            {
                tile[7-i,r] = GetPixelValue(pixelQuad,r);
            }
        }
        for(int i = 8; i < 16; i++) // second 8 bytes of tile
        {
            byte pixelQuad = _gfx1[i + (tileIndex * 16)];
            for(int r = 0; r < 4; r++)
            {
                tile[15-i,r] = GetPixelValue(pixelQuad, r);
            }
        }
        
        return tile;
    }
    
    private int[,] ExtractRawSpriteData(int spriteIndex)
    {
        int[,] sprite = new int[16,16];
        for(int i = 0; i < 8; i++) // bottom right
        {
            byte spriteQuad = _gfx1[(0x00 + i) + (0x40 * spriteIndex) + 0x1000];
            for (int r = 0; r < 4; r++)
            {
                sprite[15-i,12+r] = GetPixelValue(spriteQuad, r);
            }
        }

        for(int i = 0; i < 8; i++) // top right
        {
            byte spriteQuad = _gfx1[(0x08 + i) + (0x40 * spriteIndex) + 0x1000];
            for (int r = 0; r < 4; r++)
            {
                sprite[15-i,r] = GetPixelValue(spriteQuad, r);
            }
        }

        for(int i = 0; i < 8; i++) // top right 2
        {
            byte spriteQuad = _gfx1[(0x10 + i) + (0x40 * spriteIndex) + 0x1000];
            for (int r = 0; r < 4; r++)
            {
                sprite[15-i,4+r] = GetPixelValue(spriteQuad, r);
            }
        }

        for(int i = 0; i < 8; i++) // top right 3
        {
            byte spriteQuad = _gfx1[(0x18 + i) + (0x40 * spriteIndex) + 0x1000];
            for (int r = 0; r < 4; r++)
            {
                sprite[15-i,8+r] = GetPixelValue(spriteQuad, r);
            }
        }

        for(int i = 0; i < 8; i++) // bottom right
        {
            byte spriteQuad = _gfx1[(0x20 + i) + (0x40 * spriteIndex) + 0x1000];
            for (int r = 0; r < 4; r++)
            {
                sprite[7-i,12+r] = GetPixelValue(spriteQuad, r);
            }
        }

        for(int i = 0; i < 8; i++) // top right
        {
            byte spriteQuad = _gfx1[(0x28 + i) + (0x40 * spriteIndex) + 0x1000];
            for (int r = 0; r < 4; r++)
            {
                sprite[7-i,r] = GetPixelValue(spriteQuad, r);
            }
        }

        for(int i = 0; i < 8; i++) // top right 2
        {
            byte spriteQuad = _gfx1[(0x30 + i) + (0x40 * spriteIndex) + 0x1000];
            for (int r = 0; r < 4; r++)
            {
                sprite[7-i,4+r] = GetPixelValue(spriteQuad, r);
            }
        }

        for(int i = 0; i < 8; i++) // top right 3
        {
            byte spriteQuad = _gfx1[(0x38 + i) + (0x40 * spriteIndex) + 0x1000];
            for (int r = 0; r < 4; r++)
            {
                sprite[7-i,8+r] = GetPixelValue(spriteQuad, r);
            }
        }

        return sprite;
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