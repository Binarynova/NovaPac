using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.Xna.Framework.Graphics;

public class GalagaPCB : IArcadeMachine
{
    private Z80Cpu mainCpu, subCpu, subCpu2;
    private NamcoWSG soundChip;
    GraphicsDevice _graphicsDevice;

    private byte[] MainCPUMemory = new byte[0x10000];
    private byte[] SubCPUMemory =  new byte[0x10000];
    private byte[] Sub2CPUMemory = new byte[0x10000];
    
    private byte[] _sharedRam = new byte[0x10000];

    public void DrawDebugUI()
    {
        throw new NotImplementedException();
    }

    public int mode { get; set; }
    public List<int> subOptionIndices { get; set; }
    public bool secondPlayerFlip { get; }
    private static bool steamDeckTwoPlayerMode = false;
    public int graphicsViewerMode { get; set; }
    public int tileViewerPaletteIndex { get; set; }

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
        throw new NotImplementedException();
    }

    public void InitializeGraphics(GraphicsDevice device)
    {
        _graphicsDevice = device;
    }

    public List<PacManPCB.DrawRequest> GetDrawRequests(bool secondPlayFlip)
    {
        throw new NotImplementedException();
    }

    public void TriggerVBlankInterrupt()
    {
        throw new NotImplementedException();
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
}