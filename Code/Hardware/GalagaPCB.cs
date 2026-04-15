using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.ImGuiNet;

public class GalagaPCB : IArcadeMachine
{
    private Z80Cpu mainCpu, subCpu, subCpu2;
    private NamcoWSG soundChip;
    GraphicsDevice _graphicsDevice;

    // 64K of ram for each CPU, ROM separate from RAM
    private byte[] MainCPUMemory = new byte[0x10000]; // first 0x4000 contains ROM
    private byte[] SubCPUMemory =  new byte[0x10000]; // first 0x1000 contains ROM
    private byte[] Sub2CPUMemory = new byte[0x10000]; // first 0x1000 contains ROM

    private byte[] gfx1 = new byte[0x1000];
    private byte[] gfx2 = new byte[0x2000];
    private byte[] proms = new byte[0x220];
    
    CPU1MemoryBus cpu1Bus;
    CPU2MemoryBus cpu2Bus;
    CPU3MemoryBus cpu3Bus;
    
    private byte[] _sharedRam = new byte[0x10000];

    public int mode { get; set; }
    public List<int> subOptionIndices { get; set; }
    public bool secondPlayerFlip { get; set; }
    private static bool steamDeckTwoPlayerMode = false;
    public int graphicsViewerMode { get; set; }
    public int tileViewerPaletteIndex { get; set; }
    List<Color> colors = [];
    List<List<Color>> palettes = [];
    int item_selected_idx = 0; // Here we store our selection data as an index.
    private string previewValue = "maincpu";
    
    public void DrawDebugUI(ImGuiRenderer renderer)
    {
        ImGui.Begin("Memory Viewer");
        ImGui.SetWindowSize(new System.Numerics.Vector2(460, 300));
        DrawMemoryViewer();
        ImGui.End();
    }

    public void DrawMemoryViewer()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0, 0));
        string[] items = new[] { "maincpu", "subcpu", "sub2cpu", "gfx1", "gfx2", "proms" };
        if (ImGui.BeginCombo("Viewing", previewValue))
        {
            for (int n = 0; n < items.Length; n++)
            {
                bool is_selected = item_selected_idx == n;
                if (ImGui.Selectable(items[n], is_selected))
                {
                    item_selected_idx = n;
                    previewValue = items[n];
                }

                // Set the initial focus when opening the combo (scrolling + keyboard navigation focus)
                if (is_selected)
                    ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }
        
        ImGuiTableFlags flags = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoHostExtendX;

        switch (item_selected_idx)
        {
            case 0:
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
                            ImGui.Text($"{cpu1Bus.ReadByte((ushort)(i + 16 * j)):X2}");
                        }
                    }
                    ImGui.EndTable();
                }
                break;
            case 1:
                if (ImGui.BeginTable("Memory Viewer", 18, flags))
                {
                    for (int j = 0; j < 64; j++)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text($"{(j * 16):X8}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"|");
                        for (int i = 0; i < 16; i++)
                        {
                            ImGui.TableNextColumn();
                            ImGui.Text($"{cpu2Bus.ReadByte((ushort)(i + 16 * j)):X2}");
                        }
                    }
                    ImGui.EndTable();
                }
                break;
            case 2:
                if (ImGui.BeginTable("Memory Viewer", 18, flags))
                {
                    for (int j = 0; j < 64; j++)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text($"{(j * 16):X8}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"|");
                        for (int i = 0; i < 16; i++)
                        {
                            ImGui.TableNextColumn();
                            ImGui.Text($"{cpu3Bus.ReadByte((ushort)(i + 16 * j)):X2}");
                        }
                    }
                    ImGui.EndTable();
                }
                break;
            case 3:
                if (ImGui.BeginTable("Memory Viewer", 18, flags))
                {
                    for (int j = 0; j < 64; j++)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text($"{(j * 16):X8}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"|");
                        for (int i = 0; i < 16; i++)
                        {
                            ImGui.TableNextColumn();
                            ImGui.Text($"{gfx1[(ushort)(i + 16 * j)]:X2}");
                        }
                    }
                    ImGui.EndTable();
                }
                break;
            case 4:
                if (ImGui.BeginTable("Memory Viewer", 18, flags))
                {
                    for (int j = 0; j < 64; j++)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text($"{(j * 16):X8}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"|");
                        for (int i = 0; i < 16; i++)
                        {
                            ImGui.TableNextColumn();
                            ImGui.Text($"{gfx2[(ushort)(i + 16 * j)]:X2}");
                        }
                    }
                    ImGui.EndTable();
                }
                break;
            case 5:
                if (ImGui.BeginTable("Memory Viewer", 18, flags))
                {
                    for (int j = 0; j < 18; j++)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text($"{(j * 16):X8}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"|");
                        for (int i = 0; i < 16; i++)
                        {
                            ImGui.TableNextColumn();
                            ImGui.Text($"{proms[(ushort)(i + 16 * j)]:X2}");
                        }
                    }
                    ImGui.EndTable();
                }
                break;
        }
        
        
        ImGui.PopStyleVar();
    }

    public GalagaPCB(string romFileName, bool twoPlayerScreenFlip)
    {
        cpu1Bus = new CPU1MemoryBus(MainCPUMemory, _sharedRam);
        cpu2Bus = new CPU2MemoryBus(SubCPUMemory, _sharedRam); 
        cpu3Bus = new CPU3MemoryBus(Sub2CPUMemory, _sharedRam);
        
        mainCpu = new Z80Cpu(cpu1Bus);
        subCpu = new Z80Cpu(cpu2Bus);
        subCpu2 = new Z80Cpu(cpu3Bus);
        soundChip = new NamcoWSG(romFileName);
        steamDeckTwoPlayerMode = twoPlayerScreenFlip;

        ClearRAM();
        LoadROM(romFileName);
    }

    public int Step(bool steppingThrough)
    {
        int cycles = mainCpu.Step(steppingThrough);
        int subCycles = 0;
        int sub2Cycles = 0;

        while(subCycles < cycles)
        {
            subCycles += subCpu.Step(steppingThrough);
        }

        while (sub2Cycles < cycles)
        {
            sub2Cycles += subCpu2.Step(steppingThrough);
        }
        
        soundChip.Update(cycles);

        return cycles;
    }

    public void ClearRAM()
    {
        Array.Clear(MainCPUMemory, 0, MainCPUMemory.Length);
    }

    public short[] GetAudioSamples()
    {
        return soundChip.DumpSamples();
    }

    public void InitializeGraphics(GraphicsDevice device)
    {
        _graphicsDevice = device;
    }

    public List<PacManPCB.DrawRequest> GetDrawRequests(bool secondPlayFlip)
    {
        return new List<PacManPCB.DrawRequest>();
    }

    public void TriggerVBlankInterrupt()
    {
        mainCpu.RequestInterrupt();
    }

    private void LoadROM(string romFileName)
    {
        if (romFileName == "roms/galaga.zip")
        {
            using ZipArchive archive = ZipFile.OpenRead(romFileName);
            var romMap = new Dictionary<string, int>
            {
                { "gg1_1b.3p", 0x0000 },
                { "gg1_2b.3m", 0x1000 },
                { "gg1_3.2m", 0x2000 },
                { "gg1_4b.2l", 0x3000 }
            };
            var romMap2 = new Dictionary<string, int>
            {
                { "gg1_5b.3f", 0x0000 }
            };
            var romMap3 = new Dictionary<string, int>
            {
                { "gg1_7b.2c", 0x0000 }
            };
            var gfxRomMap1 = new Dictionary<string, int>
            {
                { "gg1_9.4l", 0x0000 }
            };
            var gfxRomMap2 = new Dictionary<string, int>
            {
                { "gg1_11.4d", 0x0000 },
                { "gg1_10.4f", 0x1000 }
            };
            var promsRomMap = new Dictionary<string, int>
            {
                { "prom-5.5n", 0x000 },
                { "prom-4.2n", 0x020 },
                { "prom-3.1c", 0x120 }
            };

            foreach (var entry in romMap)
            {
                ZipArchiveEntry romEntry = archive.GetEntry(entry.Key);

                if (romEntry != null)
                {
                    using Stream s = romEntry.Open();
                    byte[] buffer = new byte[romEntry.Length];
                    s.ReadExactly(buffer, 0, buffer.Length);
                    
                    Buffer.BlockCopy(buffer, 0, MainCPUMemory, entry.Value, buffer.Length);
                }

                else
                {
                    throw new FileNotFoundException($"Required ROM file {entry.Key} not found in zip.");
                }
            }
            foreach (var entry in romMap2)
            {
                ZipArchiveEntry romEntry = archive.GetEntry(entry.Key);

                if (romEntry != null)
                {
                    using Stream s = romEntry.Open();
                    byte[] buffer = new byte[romEntry.Length];
                    s.ReadExactly(buffer, 0, buffer.Length);
                    
                    Buffer.BlockCopy(buffer, 0, SubCPUMemory, entry.Value, buffer.Length);
                }

                else
                {
                    throw new FileNotFoundException($"Required ROM file {entry.Key} not found in zip.");
                }
            }
            
            foreach (var entry in romMap3)
            {
                ZipArchiveEntry romEntry = archive.GetEntry(entry.Key);

                if (romEntry != null)
                {
                    using Stream s = romEntry.Open();
                    byte[] buffer = new byte[romEntry.Length];
                    s.ReadExactly(buffer, 0, buffer.Length);
                    
                    Buffer.BlockCopy(buffer, 0, Sub2CPUMemory, entry.Value, buffer.Length);
                }

                else
                {
                    throw new FileNotFoundException($"Required ROM file {entry.Key} not found in zip.");
                }
            }
            Console.WriteLine("CPU ROMs read into Memory.");
            
            foreach (var entry in gfxRomMap1)
            {
                ZipArchiveEntry romEntry = archive.GetEntry(entry.Key);

                if (romEntry != null)
                {
                    using Stream s = romEntry.Open();
                    byte[] buffer = new byte[romEntry.Length];
                    s.ReadExactly(buffer, 0, buffer.Length);
                    
                    Buffer.BlockCopy(buffer, 0, gfx1, entry.Value, buffer.Length);
                }

                else
                {
                    throw new FileNotFoundException($"Required ROM file {entry.Key} not found in zip.");
                }
            }
            
            foreach (var entry in gfxRomMap2)
            {
                ZipArchiveEntry romEntry = archive.GetEntry(entry.Key);

                if (romEntry != null)
                {
                    using Stream s = romEntry.Open();
                    byte[] buffer = new byte[romEntry.Length];
                    s.ReadExactly(buffer, 0, buffer.Length);
                    
                    Buffer.BlockCopy(buffer, 0, gfx2, entry.Value, buffer.Length);
                }

                else
                {
                    throw new FileNotFoundException($"Required ROM file {entry.Key} not found in zip.");
                }
            }
            Console.WriteLine("GFX ROMs read into Memory.");
            
            foreach (var entry in promsRomMap)
            {
                ZipArchiveEntry romEntry = archive.GetEntry(entry.Key);

                if (romEntry != null)
                {
                    using Stream s = romEntry.Open();
                    byte[] buffer = new byte[romEntry.Length];
                    s.ReadExactly(buffer, 0, buffer.Length);
                    
                    Buffer.BlockCopy(buffer, 0, proms, entry.Value, buffer.Length);
                }

                else
                {
                    throw new FileNotFoundException($"Required ROM file {entry.Key} not found in zip.");
                }
            }
            Console.WriteLine("PROMS read into Memory.");
        }
    }
    void PrepareColors()
    {
        // hard-coded because the ROM stores them as intensities of output on hardware, not as color
        colors =
        [
            new Color(0xDE, 0xDE, 0xDE),      
            new Color(0xFF, 0x00, 0x00),      
            new Color(0xFF, 0xFF, 0x00),      
            new Color(0xFF, 0x97, 0x00),      
            new Color(0xFF, 0xB8, 0x00),      
            new Color(0xFF, 0x00, 0xDE),      
            new Color(0x00, 0xFF, 0xDE),      
            new Color(0xB8, 0xB8, 0xDE),      
            new Color(0xDE, 0x47, 0x00),      
            new Color(0x00, 0xFF, 0x00),      
            new Color(0x21, 0x97, 0x00), // unused
            new Color(0x00, 0x68, 0xDE),      
            new Color(0x97, 0x00, 0xDE),      
            new Color(0x00, 0x00, 0xDE),      
            new Color(0x00, 0x97, 0x97),      
            new Color(0x00, 0x00, 0x00)
        ];
    }

    private int[,] ExtractRawTileData(int tileIndex)
    {
        int[,] tile = new int[8, 8];
        // first 8 bytes of a tile
        for (int i = 0; i < 8; i++)
        {
            byte pixelQuad = gfx1[i + tileIndex * 16];
            for (int c = 4; c < 8; c++)
            {
                tile[c, 7-i] = GetPixelValue(pixelQuad, c);
            }
        }
        // second 8 bytes of tile
        for (int i = 8; i < 16; i++)
        {
            byte pixelQuad = gfx1[i + tileIndex * 16];
            for (int c = 0; c < 4; c++)
            {
                tile[c, 15-i] = GetPixelValue(pixelQuad, c);
            }
        }

        return tile;
    }
    
    static int GetPixelValue(byte pixelData, int pixelIndex)
    {
        // pixelIndex is a value from 1-4 referring to which column of the tile
        
        int paletteValue = 0;
        int highBit = 0, lowBit = 0;
        // returns the palette index of a pixel 0 - 3
        
        // bits 0 through 3 are the high bit of each pair
        // bits 4 through 7 are the low bit of each pair
        // i.e. (bit 7 | bit 3 < 1) gives a 2-bit value (0-3)

        switch (pixelIndex)
        {
            case 0 or 4:
                highBit = (pixelData & 0x08) >> 3;
                lowBit = (pixelData & 0x80) >> 7;
                break;
            
            case 1 or 5:
                highBit = (pixelData & 0x04) >> 2;
                lowBit = (pixelData & 0x40) >> 6;
                break;
            
            case 2 or 6:
                highBit = (pixelData & 0x02) >> 1;
                lowBit = (pixelData & 0x20) >> 5;
                break;
            
            case 3 or 7:
                highBit = (pixelData & 0x01);
                lowBit = (pixelData & 0x10) >> 4;
                break;
        }
        paletteValue = lowBit | (highBit << 1);
        Console.WriteLine($"{paletteValue}");

        return paletteValue;
    }
}