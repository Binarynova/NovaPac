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
    
    private byte[] _sharedRam = new byte[0x10000];

    public int mode { get; set; }
    public List<int> subOptionIndices { get; set; }
    public bool secondPlayerFlip { get; }
    private static bool steamDeckTwoPlayerMode = false;
    public int graphicsViewerMode { get; set; }
    public int tileViewerPaletteIndex { get; set; }
    List<Color> colors = [];
    List<List<Color>> palettes = [];
    
    public void DrawDebugUI(ImGuiRenderer renderer)
    {
        ImGui.Begin("Graphics Viewer");
        ImGui.SetWindowSize(new System.Numerics.Vector2(300, 340));
        DrawMemoryViewer(renderer);
        ImGui.End();
    }

    public void DrawMemoryViewer(ImGuiRenderer renderer)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0, 0));
        ImGuiTableFlags flags = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoHostExtendX;
        
        ImGui.PopStyleVar();
    }

    public GalagaPCB(string romFileName, bool twoPlayerScreenFlip)
    {
        var cpu1Bus = new CPU1MemoryBus(MainCPUMemory, _sharedRam);
        var cpu2Bus = new CPU2MemoryBus(SubCPUMemory, _sharedRam);
        var cpu3Bus = new CPU3MemoryBus(Sub2CPUMemory, _sharedRam);
        
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
                { "gg1_5b.3f", 0x0000 },

            };
            var romMap3 = new Dictionary<string, int>
            {
                { "gg1_7b.2c", 0x0000 }
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
            new Color(0x21, 0x97, 0x00),       //(not used in palettes)
            new Color(0x00, 0x68, 0xDE),      
            new Color(0x97, 0x00, 0xDE),      
            new Color(0x00, 0x00, 0xDE),      
            new Color(0x00, 0x97, 0x97),      
            new Color(0x00, 0x00, 0x00)
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