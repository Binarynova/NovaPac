using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq.Expressions;

namespace pacman;

public class NamcoWSG
{
    public double _accumulator = 0;
    private long _totalGenerated = 0;
    private const double CyclesPerSample = 3072000.0 / 44100.0;
    
    private Queue<short> _sampleBuffer = new Queue<short>();
    bool voice1Enabled = true;
    bool voice2Enabled = true;
    bool voice3Enabled = true;
    
    private byte[] waveformROM = new byte[512];

    private int Voice1Frequency = 0;
    public byte Voice1Volume = 0x00;
    public byte Voice1Waveform = 0x00;
    private int Voice1Accumulator = 0;
    
    public int Voice2Frequency = 0;
    public byte Voice2Volume = 0x00;
    public byte Voice2Waveform = 0x00;
    private int Voice2Accumulator = 0;
    
    public int Voice3Frequency = 0;
    public byte Voice3Volume = 0x00;
    public byte Voice3Waveform = 0x00;
    private int Voice3Accumulator = 0;

    public NamcoWSG(string romFileName)
    {
        LoadRom(romFileName);
    }

    public void Update(int cycles)
    {
        _accumulator += cycles;
        
        while(_accumulator >= CyclesPerSample)
        {
            short sample = GenerateCurrentSample();
            _totalGenerated++;
            _sampleBuffer.Enqueue(sample);
            _accumulator -= CyclesPerSample;
        }
    }

    public short[] DumpSamples()
    {
        short[] samples = _sampleBuffer.ToArray();
        _sampleBuffer.Clear();
        return samples;
    }
    
    private short GenerateCurrentSample()
    {
        int combinedOutput = 0;

        Voice1Accumulator = (Voice1Accumulator + Voice1Frequency) & 0xFFFFF;
        int waveformIndex = (int)((Voice1Accumulator >> 15) & 0x1F);
        int romAddress = (Voice1Waveform * 32) + waveformIndex;
        int rawSample = waveformROM[romAddress];
        if(voice1Enabled) combinedOutput += (rawSample - 8) * Voice1Volume;

        Voice2Accumulator = (Voice2Accumulator + Voice2Frequency) & 0xFFFF;
        waveformIndex = (int)((Voice2Accumulator >> 11) & 0x1F);
        romAddress = (Voice2Waveform * 32) + waveformIndex;
        rawSample = waveformROM[romAddress];
        if(voice2Enabled) combinedOutput += (rawSample - 8) * Voice2Volume;
        
        Voice3Accumulator = (Voice3Accumulator + Voice3Frequency) & 0xFFFF;
        waveformIndex = (int)((Voice3Accumulator >> 11) & 0x1F);
        romAddress = (Voice3Waveform * 32) + waveformIndex;
        rawSample = waveformROM[romAddress];
        if(voice3Enabled) combinedOutput += (rawSample - 8) * Voice3Volume;

        return (short)(combinedOutput * 45);
    }

    public void UpdateRegister(ushort address, byte value)
    {
            switch (address)
            {
                // --- WAVEFORM SELECT (Group A) ---
                case 0x5045: Voice1Waveform = (byte)(value & 0x07); break;
                case 0x504A: Voice2Waveform = (byte)(value & 0x07); break;
                case 0x504F: Voice3Waveform = (byte)(value & 0x07); break;

                // --- VOICE 1 (Group B) ---
                case 0x5050: Voice1Frequency = (Voice1Frequency & ~0x0000F) | (value & 0x0F); break;
                case 0x5051: Voice1Frequency = (Voice1Frequency & ~0x000F0) | ((value & 0x0F) << 4); break;
                case 0x5052: Voice1Frequency = (Voice1Frequency & ~0x00F00) | ((value & 0x0F) << 8); break;
                case 0x5053: Voice1Frequency = (Voice1Frequency & ~0x0F000) | ((value & 0x0F) << 12); break;
                case 0x5054: Voice1Frequency = (Voice1Frequency & ~0xF0000) | ((value & 0x0F) << 16); break;
                case 0x5055: Voice1Volume = (byte)(value & 0x0F); break;

                // --- VOICE 2 (Group B) ---
                case 0x5056: Voice2Frequency = (Voice2Frequency & ~0x0000F) | (value & 0x0F); break;
                case 0x5057: Voice2Frequency = (Voice2Frequency & ~0x000F0) | ((value & 0x0F) << 4); break;
                case 0x5058: Voice2Frequency = (Voice2Frequency & ~0x00F00) | ((value & 0x0F) << 8); break;
                case 0x5059: Voice2Frequency = (Voice2Frequency & ~0x0F000) | ((value & 0x0F) << 12); break;
                // In Pac-Man, V2 Frequency is only 16-bit + 1 nibble volume!
                case 0x505A: Voice2Volume = (byte)(value & 0x0F); break;

                // --- VOICE 3 (Group B) ---
                case 0x505B: Voice3Frequency = (Voice3Frequency & ~0x0000F) | (value & 0x0F); break;
                case 0x505C: Voice3Frequency = (Voice3Frequency & ~0x000F0) | ((value & 0x0F) << 4); break;
                case 0x505D: Voice3Frequency = (Voice3Frequency & ~0x00F00) | ((value & 0x0F) << 8); break;
                case 0x505E: Voice3Frequency = (Voice3Frequency & ~0x0F000) | ((value & 0x0F) << 12); break;
                case 0x505F: Voice3Volume = (byte)(value & 0x0F); break;
        }
            if (address >= 0x5050) Console.WriteLine($"V1:{Voice1Volume} V2:{Voice2Volume} V3:{Voice3Volume}");
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