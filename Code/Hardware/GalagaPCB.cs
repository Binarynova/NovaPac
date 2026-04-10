using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

public class GalagaPCB : IMemoryProvider
{
    private Z80Cpu maincpu, sub, sub2;
    private NamcoWSG soundChip;

    private byte[] MainCPUMemory = new byte[0x10000];
    private byte[] SubCPUMemory = new byte[0x10000];
    private byte[] Sub2CPUMemory = new byte[0x10000];

    public GalagaPCB(string romFileName, bool twoPlayerScreenFlip)
    {
        maincpu = new Z80Cpu(this);
        sub = new Z80Cpu(this);
        sub2 = new Z80Cpu(this);
        soundChip = new NamcoWSG(romFileName);

        ClearRAM();
        LoadROM(romFileName);
    }
    
    public byte ReadByte(ushort address)
    {
        throw new System.NotImplementedException();
    }

    public void WriteByte(ushort address, byte value)
    {
        throw new System.NotImplementedException();
    }

    public void ClearRAM()
    {
        Array.Clear(MainCPUMemory, 0, MainCPUMemory.Length);
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