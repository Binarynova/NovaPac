using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace pacman;

public class WSG
{
    public int cycleCounter;
    
    private byte[] waveformROM = new byte[512];

    private int Voice1Frequency;
    public byte Voice1Volume = 0x00;
    public byte Voice1Waveform = 0x00;
    private int Voice1Accumulator = 0;
    
    public int Voice2Frequency;
    public byte Voice2Volume = 0x00;
    public byte Voice2Waveform = 0x00;
    private int Voice2Accumulator = 0;
    
    public int Voice3Frequency;
    public byte Voice3Volume = 0x00;
    public byte Voice3Waveform = 0x00;
    private int Voice3Accumulator = 0;

    public WSG(string romFileName)
    {
        LoadRom(romFileName);
    }

    public void Update(int cycles)
    {
        
    }

    public byte UpdateRegister(ushort address, byte value)
    {
        switch (address)
        {
            // Voice 1
            case 0x5045:
                Voice1Waveform = (byte)(value & 0x07);
                break;
            case >= 0x5050 and <= 0x5054:
                Voice1Frequency &= ~(0x0000F << (4 * (address - 0x5050)));
                Voice1Frequency |= (value & 0x0F) << (4 * (address - 0x5050));
                break;
            case 0x5055:
                Voice1Volume = (byte)(value & 0x0F);
                break;
            case >= 0x5040 and <= 0x5044:
                Voice1Accumulator &= ~(0x0000F << (4 * (address - 0x5040)));
                Voice1Accumulator |= (value & 0x0F) << (4 * (address - 0x5040));
                break;
            
            // Voice 2
            case 0x504A:
                Voice2Waveform = (byte)(value & 0x07);
                break;
            case >= 0x5056 and <= 0x5059:
                Voice2Frequency &= ~(0x0000F << (4 * (address - 0x5056)));
                Voice2Frequency |= (value & 0x0F) << (4 * (address - 0x5056));
                break;
            case 0x505A:
                Voice2Volume = (byte)(value & 0x0F);
                break;
            case >= 0x5046 and <= 0x5049:
                Voice2Accumulator &= ~(0x0000F << (4 * (address - 0x5046)));
                Voice2Accumulator |= (value & 0x0F) << (4 * (address - 0x5046));
                break;
            
            // Voice 3
            case 0x504F:
                Voice3Waveform = (byte)(value & 0x07);
                break;
            case >= 0x505B and <= 0x505E:
                Voice3Frequency &= ~(0x0000F << (4 * (address - 0x505B)));
                Voice3Frequency |= (value & 0x0F) << (4 * (address - 0x505B));
                break;
            case 0x505F:
                Voice3Volume = (byte)(value & 0x0F);
                break;
            case >= 0x504B and <= 0x504E:
                Voice3Accumulator &= ~(0x0000F << (4 * (address - 0x504B)));
                Voice3Accumulator |= (value & 0x0F) << (4 * (address - 0x504B));
                break;
        }

        return 0;
    }
    
    private void LoadRom(string romFileName)
    {
        if (romFileName == "roms/pacman.zip" || romFileName == "roms/matrix.zip" || romFileName == "roms/newpuckx.zip")
        {
            using ZipArchive archive = ZipFile.OpenRead(romFileName);
            
            var soundRom1 = ExtractRom(archive, "82s126.1m");
            var soundRom2 = ExtractRom(archive, "82s126.3m");
            Array.Copy(soundRom1, 0, waveformROM, 0, 256);
            Array.Copy(soundRom2, 0, waveformROM, 256, 256);
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
}