using System.IO.Compression;
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.ImGuiNet;

public class GalaxianPCB : IArcadeMachine
{
    public int mode { get; set; }
    public List<int> subOptionIndices { get; set; }
    public bool secondPlayerFlip { get; }

    private byte[] _mainMemory = new byte[0x10000];
    private byte[] _gfx1 = new byte[0x1000];
    private byte[] _proms = new byte[0x0020];
    private Z80Cpu cpu;
    private GalaxianMemoryBus _memoryBus;
    GraphicsDevice _graphicsDevice;
    private List<DrawRequest> requests = new ();

    int[,] testTile = new int[8,8];
    
    public GalaxianPCB(string romFileName,  bool verticalScreenMode)
    {
        LoadRom(romFileName);
        _memoryBus = new GalaxianMemoryBus(_mainMemory);
        cpu = new Z80Cpu(_memoryBus);

        testTile = ExtractRawTileData(0x00);
        Console.WriteLine("stop");
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
    }

    public int Step(bool steppingThrough)
    {
        int cycles = cpu.Step(steppingThrough);

        return cycles;
    }

    public List<DrawRequest> GetDrawRequests(bool secondPlayFlip)
    {
        requests.Clear();

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
    
    int GetPixelValue(byte tileIndex, int rowIndex, int pixelIndex)
    {
        byte pixelData1 = _gfx1[tileIndex + rowIndex];
        byte pixelData2 = _gfx1[tileIndex + 0x800 + rowIndex];
        
        int pixel1 = (pixelData1 & (int)Math.Pow(2,pixelIndex)) != 0 ? 1 : 0;  // this returns the 1 or 0 for a given pixel from the first byte
        int pixel2 = (pixelData2 & (int)Math.Pow(2,pixelIndex)) != 0 ? 1 : 0; // same but for the other byte

        int paletteValue = pixel1 << 1 | pixel2;
        return paletteValue;    // this now returns the 0-3 (0x00 to 0x11) value for the pixel in the tile
    }
    
    private int[,] ExtractRawTileData(byte tileIndex)
    {
        int[,] tile = new int[8,8];

        for (int r = 0; r < 8; r++)
        {
            for (int b = 0; b < 8; b++)
            {
                tile[7-b, 7-r] = GetPixelValue(tileIndex, r, b);
            }
        }
        
        return tile;
    }
}