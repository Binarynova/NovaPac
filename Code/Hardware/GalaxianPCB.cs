using System.IO.Compression;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.ImGuiNet;

public class GalaxianPCB : IArcadeMachine
{
    public List<int> subOptionIndices { get; set; }
    public bool secondPlayerFlip { get; }

    private byte[] _mainMemory = new byte[0x10000];
    private byte[] _gfx1 = new byte[0x1000];
    private byte[] _proms = new byte[0x0020];
    private Z80Cpu cpu;
    private GalaxianMemoryBus _memoryBus;
    GraphicsDevice _graphicsDevice;
    private List<DrawRequest> requests = new ();
    public int mode { get; set; } = 0;
    private int graphicsViewerMode { get; set; } = 0;

    Texture2D[] TileTextures = new Texture2D[256];
    List<Color> colors = [];
    
    public GalaxianPCB(string romFileName,  bool verticalScreenMode)
    {
        LoadRom(romFileName);
        _memoryBus = new GalaxianMemoryBus(_mainMemory);
        cpu = new Z80Cpu(_memoryBus);
    }

    private void LoadRom(string romFileName)
    {
        switch (romFileName)
        {
            case "roms/galaxian.zip":
            {
                using ZipArchive archive = ZipFile.OpenRead(romFileName);
                var romMap = new Dictionary<string, int>
                {
                    { "galmidw.u", 0x0000 },
                    { "galmidw.v", 0x0800 },
                    { "galmidw.w", 0x1000 },
                    { "galmidw.y", 0x1800 },
                    { "7l", 0x2000 }
                };
                var gfx1Map = new Dictionary<string, int>()
                {
                    { "1h", 0x0000 },
                    { "1k", 0x0800 }
                };
                var promsMap = new Dictionary<string, int>()
                {
                    { "galaxian.clr", 0x0000 }
                };

                foreach (var entry in romMap)
                {
                    ZipArchiveEntry romEntry = archive.GetEntry(entry.Key);

                    if (romEntry != null)
                    {
                        using Stream s = romEntry.Open();
                        byte[] buffer = new byte[romEntry.Length];
                        s.ReadExactly(buffer, 0, buffer.Length);

                        Buffer.BlockCopy(buffer, 0, _mainMemory, entry.Value, buffer.Length);
                    }

                    else
                    {
                        throw new FileNotFoundException($"Required ROM file {entry.Key} not found in zip!");
                    }
                }
                
                foreach (var entry in gfx1Map)
                {
                    ZipArchiveEntry romEntry = archive.GetEntry(entry.Key);

                    if (romEntry != null)
                    {
                        using Stream s = romEntry.Open();
                        byte[] buffer = new byte[romEntry.Length];
                        s.ReadExactly(buffer, 0, buffer.Length);

                        Buffer.BlockCopy(buffer, 0, _gfx1, entry.Value, buffer.Length);
                    }

                    else
                    {
                        throw new FileNotFoundException($"Required ROM file {entry.Key} not found in zip!");
                    }
                }
                
                foreach (var entry in promsMap)
                {
                    ZipArchiveEntry romEntry = archive.GetEntry(entry.Key);

                    if (romEntry != null)
                    {
                        using Stream s = romEntry.Open();
                        byte[] buffer = new byte[romEntry.Length];
                        s.ReadExactly(buffer, 0, buffer.Length);

                        Buffer.BlockCopy(buffer, 0, _proms, entry.Value, buffer.Length);
                    }

                    else
                    {
                        throw new FileNotFoundException($"Required ROM file {entry.Key} not found in zip!");
                    }
                }
                Console.WriteLine("Galaxian roms loaded.");
                break;
            }
        }
    }

    public short[] GetAudioSamples()
    {
        return null;
    }

    public void InitializeGraphics(GraphicsDevice device)
    {
        _graphicsDevice = device;
        PrepareColors();
        PrepareTileTextures(_graphicsDevice);
    }

    public int Step(bool steppingThrough)
    {
        int cycles = cpu.Step(steppingThrough);

        return cycles;
    }

    public List<DrawRequest> GetDrawRequests(bool secondPlayFlip)
    {
        requests.Clear();
        DrawGameTiles();

        return requests;
    }

    public void TriggerVBlankInterrupt()
    {
        if (_memoryBus.NmiEnabled)
        {
            cpu.TriggerNmi();
        }
    }

    public void DrawDebugUI(ImGuiRenderer renderer)
    {
        ImGui.Begin("Graphics Viewer");
        ImGui.SetWindowSize(new System.Numerics.Vector2(300, 340));
        DrawGraphicsViewer(renderer);
        ImGui.End();
        
        ImGui.Begin("Memory Viewer");
        ImGui.SetWindowSize(new System.Numerics.Vector2(460, 350));
        DrawMemoryViewer();
        ImGui.End();
    }
    
    public void DrawMemoryViewer()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0, 0));
        ImGuiTableFlags flags = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoHostExtendX;
        
        if (ImGui.BeginTable("Memory Viewer", 18, flags))
        {
            for (int j = 0; j < 4096; j++)
            {
                ImGui.TableNextColumn();
                ImGui.Text($"{(j * 16):X8}");
                ImGui.TableNextColumn();
                ImGui.Text("|");
                for (int i = 0; i < 16; i++)
                {
                    ImGui.TableNextColumn();
                    ImGui.Text($"{_memoryBus.ReadByte((ushort)(i + 16 * j)):X2}");
                }
            }
            ImGui.EndTable();
        }
        
        ImGui.PopStyleVar();
    }
    
    int GetPixelValue(int tileIndex, int rowIndex, int pixelIndex)
    {
        byte pixelData1 = _gfx1[tileIndex * 8 + rowIndex];
        byte pixelData2 = _gfx1[tileIndex * 8 + 0x800 + rowIndex];
        
        int pixel1 = (pixelData1 & (int)Math.Pow(2,pixelIndex)) != 0 ? 1 : 0;  // this returns the 1 or 0 for a given pixel from the first byte
        int pixel2 = (pixelData2 & (int)Math.Pow(2,pixelIndex)) != 0 ? 1 : 0; // same but for the other byte

        int paletteValue = pixel1 << 1 | pixel2;
        return paletteValue;    // this now returns the 0-3 (0x00 to 0x11) value for the pixel in the tile
    }
    
    private int[,] ExtractRawTileData(int tileIndex)
    {
        int[,] tile = new int[8,8];

        for (int r = 0; r < 8; r++)
        {
            for (int b = 0; b < 8; b++)
            {
                tile[7-r, 7-b] = GetPixelValue(tileIndex, r, b);
            }
        }
        
        return tile;
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
                        colorData[y * 8 + x] = colors[colorId];
                    }
                }
                
                tileTexture.SetData(colorData);
                TileTextures[tileIndex] = tileTexture;
            }
        }
    }
    
    void PrepareColors()
    {
        for (int i = 0; i < 8; i++)
        {
            colors.Add(new Color((int)_proms[4*i+1],  _proms[4*i+2], _proms[4*i+3], 255));
        }
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
                        IntPtr texturePtr = renderer.BindTexture(TileTextures[i]);
                        ImGui.Image(texturePtr, new System.Numerics.Vector2(16, 16));
                    }
                    ImGui.EndTable();
                }
                break;
        }
        
        ImGui.PopStyleVar();
    }
    
    private void DrawGameTiles()
    {
        // Row 1 and 2
        for (int i = 0x3DF; i >= 0x3C0; i--)
        {
            for (int row = 0; row < 2; row++)
            {
                int tileAddress = 0x4000 + i + 0x20 * row;
            
                byte tileIndex = _memoryBus.ReadByte((ushort)tileAddress);

                int yPos = 0 + 8 * row;
                int xPos = 232 - (i - 0x3C0) * 8;
            
                requests.Add(new DrawRequest {
                    Texture = TileTextures[tileIndex],
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
            
                byte tileIndex = _memoryBus.ReadByte((ushort)tileAddress);
        
                int yPos = 8 * (i - 0x040) + 16;
                int xPos = 216 - (col) * 8;
            
                requests.Add(new DrawRequest {
                    Texture = TileTextures[tileIndex],
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
            
                byte tileIndex = _memoryBus.ReadByte((ushort)tileAddress);
        
                int yPos = 272 + 8 * row;
                int xPos = 232 - (i - 0x000) * 8;
            
                requests.Add(new DrawRequest {
                    Texture = TileTextures[tileIndex],
                    Position = new Vector2(xPos, yPos)
                });
            }
        }
    }
}