using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework.Graphics;

public class SSTests : IArcadeMachine
{
    Z80Cpu cpu;
    private byte[] Memory = new byte[0x10000];

    public byte ReadByte(ushort address) => Memory[address];

    public void WriteByte(ushort address, byte value)
    {
        Memory[address] = value;
    }

    public void ClearRAM()
    {
        Array.Clear(Memory, 0, Memory.Length);
    }

    public short[] GetAudioSamples()
    {
        throw new NotImplementedException();
    }

    public void InitializeGraphics(GraphicsDevice device)
    {
        throw new NotImplementedException();
    }

    public int Step(bool steppingThrough)
    {
        throw new NotImplementedException();
    }

    public List<PacManPCB.DrawRequest> GetDrawRequests(bool secondPlayFlip)
    {
        throw new NotImplementedException();
    }

    public void TriggerVBlankInterrupt()
    {
        throw new NotImplementedException();
    }

    public int mode { get; set; }
    public List<int> subOptionIndices { get; set; }
    public bool secondPlayerFlip { get; }
    public int graphicsViewerMode { get; set; }
    public int tileViewerPaletteIndex { get; set; }

    public SSTests()
    {
        cpu = new Z80Cpu(this);
    }
    

    public void Run()
    {
        // Load
        IArcadeMachine sstMachine = new SSTests();
        cpu = new Z80Cpu(sstMachine);
        int hexCode = 0;
        int passedCount;
        
        StreamWriter sw = new("testlog.txt");
        
        Console.WriteLine($"\nMain Instructions:");
        while (hexCode <= 0xFF)
        {
            if (hexCode is 0xCB or 0xDD or 0xED or 0xFD)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"  {hexCode:X2}");
                hexCode++;
                continue;
            }
            
            string testFile = $"tests/{hexCode:x2}.json";
            
        
            string json = File.ReadAllText(testFile);
            var tests = JsonSerializer.Deserialize<List<Z80SingleStepTest>>(json);
        
            passedCount = 0;
        
            foreach (Z80SingleStepTest test in tests)
            {
                cpu.Reset();
                cpu.SetInitialCPUState(test);
                cpu.Step();
                string error = cpu.CheckFinalCPUState(test, sw);
                if (error == "")
                    passedCount++;
            }

            Console.ForegroundColor = passedCount == 1000 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write($"  {hexCode:X2}");
            if ((hexCode & 0x0F) == 0x0F)
                Console.WriteLine();
            hexCode++;
        }

        hexCode = 0x00;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"\nED Instructions:");
        while (hexCode <= 0xBF)
        {
            if (cpu._edOpcodes[hexCode].Method.Name != "Op_UNK")
            {
                string testFile = $"tests/ed {hexCode:x2}.json";
                string json = File.ReadAllText(testFile);
                var tests = JsonSerializer.Deserialize<List<Z80SingleStepTest>>(json);
            
                passedCount = 0;
            
                foreach (Z80SingleStepTest test in tests)
                {
                    cpu.Reset();
                    cpu.SetInitialCPUState(test);
                    cpu.Step();
                    string error = cpu.CheckFinalCPUState(test, sw);
                    if (error == "")
                        passedCount++;
                }

                Console.ForegroundColor = passedCount == 1000 ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write($"  {hexCode:X2}");
                if ((hexCode & 0x0F) == 0x0F)
                    Console.WriteLine();
                hexCode++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"  {hexCode:X2}");
                if ((hexCode & 0x0F) == 0x0F)
                    Console.WriteLine();
                hexCode++;
            }
        }
        
        hexCode = 0x00;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"\nDD Instructions:");
        while (hexCode <= 0xFF)
        {
            if (cpu._ddOpcodes[hexCode].Method.Name != "Op_UNK")
            {
                string testFile = $"tests/dd {hexCode:x2}.json";
                string json = File.ReadAllText(testFile);
                var tests = JsonSerializer.Deserialize<List<Z80SingleStepTest>>(json);
            
                passedCount = 0;
            
                foreach (Z80SingleStepTest test in tests)
                {
                    cpu.Reset();
                    cpu.SetInitialCPUState(test);
                    cpu.Step();
                    string error = cpu.CheckFinalCPUState(test, sw);
                    if (error == "")
                        passedCount++;
                }

                Console.ForegroundColor = passedCount == 1000 ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write($"  {hexCode:X2}");
                if ((hexCode & 0x0F) == 0x0F)
                    Console.WriteLine();
                hexCode++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"  {hexCode:X2}");
                if ((hexCode & 0x0F) == 0x0F)
                    Console.WriteLine();
                hexCode++;
            }
        }
        
        hexCode = 0x00;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"\nFD Instructions:");
        while (hexCode <= 0xFF)
        {
            if (cpu._fdOpcodes[hexCode].Method.Name != "Op_UNK")
            {
                string testFile = $"tests/fd {hexCode:x2}.json";
                string json = File.ReadAllText(testFile);
                var tests = JsonSerializer.Deserialize<List<Z80SingleStepTest>>(json);
            
                passedCount = 0;
            
                foreach (Z80SingleStepTest test in tests)
                {
                    cpu.Reset();
                    cpu.SetInitialCPUState(test);
                    cpu.Step();
                    string error = cpu.CheckFinalCPUState(test, sw);
                    if (error == "")
                        passedCount++;
                }

                Console.ForegroundColor = passedCount == 1000 ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write($"  {hexCode:X2}");
                if ((hexCode & 0x0F) == 0x0F)
                    Console.WriteLine();
                hexCode++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"  {hexCode:X2}");
                if ((hexCode & 0x0F) == 0x0F)
                    Console.WriteLine();
                hexCode++;
            }
        }
        
        hexCode = 0x00;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"\nCB Instructions:");
        while (hexCode <= 0xFF)
        {
            if (cpu._cbOpcodes[hexCode].Method.Name != "Op_UNK" || hexCode >= 0x40)
            {
                string testFile = $"tests/cb {hexCode:x2}.json";
                string json = File.ReadAllText(testFile);
                var tests = JsonSerializer.Deserialize<List<Z80SingleStepTest>>(json);
            
                passedCount = 0;
            
                foreach (Z80SingleStepTest test in tests)
                {
                    cpu.Reset();
                    cpu.SetInitialCPUState(test);
                    cpu.Step();
                    string error = cpu.CheckFinalCPUState(test, sw);
                    if (error == "")
                        passedCount++;
                }

                Console.ForegroundColor = passedCount == 1000 ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write($"  {hexCode:X2}");
                if ((hexCode & 0x0F) == 0x0F)
                    Console.WriteLine();
                hexCode++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"  {hexCode:X2}");
                if ((hexCode & 0x0F) == 0x0F)
                    Console.WriteLine();
                hexCode++;
            }
        }
        
        hexCode = 0x00;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"\nDD CB Instructions:");
        while (hexCode <= 0xFF)
        {
            string testFile = $"tests/dd cb __ {hexCode:x2}.json";
            string json = File.ReadAllText(testFile);
            var tests = JsonSerializer.Deserialize<List<Z80SingleStepTest>>(json);
        
            passedCount = 0;
        
            foreach (Z80SingleStepTest test in tests)
            {
                cpu.Reset();
                cpu.SetInitialCPUState(test);
                cpu.Step();
                string error = cpu.CheckFinalCPUState(test, sw);
                if (error == "")
                    passedCount++;
            }

            Console.ForegroundColor = passedCount == 1000 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write($"  {hexCode:X2}");
            if ((hexCode & 0x0F) == 0x0F)
                Console.WriteLine();
            hexCode++;
        }
        
        hexCode = 0x00;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"\nFD CB Instructions:");
        while (hexCode <= 0xFF)
        {
            string testFile = $"tests/fd cb __ {hexCode:x2}.json";
            string json = File.ReadAllText(testFile);
            var tests = JsonSerializer.Deserialize<List<Z80SingleStepTest>>(json);
        
            passedCount = 0;
        
            foreach (Z80SingleStepTest test in tests)
            {
                cpu.Reset();
                cpu.SetInitialCPUState(test);
                cpu.Step();
                string error = cpu.CheckFinalCPUState(test, sw);
                if (error == "")
                    passedCount++;
            }

            Console.ForegroundColor = passedCount == 1000 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write($"  {hexCode:X2}");
            if ((hexCode & 0x0F) == 0x0F)
                Console.WriteLine();
            hexCode++;
        }

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("\nTests complete. Errors written to: testlog.txt");
        sw.Close();
        Environment.Exit(0);
    }
}

public class CPUState
{
    // 8-bit registers
    public byte A { get; set; }
    public byte B { get; set; }
    public byte C { get; set; }
    public byte D { get; set; }
    public byte E { get; set; }
    public byte F { get; set; }
    public byte H { get; set; }
    public byte L { get; set; }
    
    // special registers
    public byte I { get; set; }
    public byte R { get; set; }
    
    //16-bit registers
    public ushort PC { get; set; }
    public ushort SP { get; set; }
    public ushort IX { get; set; }
    public ushort IY { get; set; }
    public ushort WZ { get; set; }
    
    // alternate registers
    public ushort AF_ { get; set; }
    public ushort BC_ { get; set; }
    public ushort DE_ { get; set; }
    public ushort HL_ { get; set; }
    
    // interrupt state
    public byte IM { get; set; }
    public byte EI { get; set; }
    public byte IFF1 { get; set; }
    public byte IFF2 { get; set; }
    
    // undocumented flags
    public byte P { get; set; }
    public byte Q { get; set; }
    
    // RAM
    public List<List<int>> RAM { get; set; }
}

public class Z80SingleStepTest
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("initial")]
    public TestState Initial { get; set; }
    
    [JsonPropertyName("final")]
    public TestState Final { get; set; }
    
    public class TestState
    {
        [JsonPropertyName("pc")]
        public ushort PC { get; set; }
        [JsonPropertyName("sp")]
        public ushort SP { get; set; }
        [JsonPropertyName("a")]
        public byte A { get; set; }
        [JsonPropertyName("b")]
        public byte B { get; set; }
        [JsonPropertyName("c")]
        public byte C { get; set; }
        [JsonPropertyName("d")]
        public byte D { get; set; }
        [JsonPropertyName("e")]
        public byte E { get; set; }
        [JsonPropertyName("f")]
        public byte F { get; set; }
        [JsonPropertyName("h")]
        public byte H { get; set; }
        [JsonPropertyName("l")]
        public byte L { get; set; }
        [JsonPropertyName("i")]
        public byte I { get; set; }
        [JsonPropertyName("r")]
        public byte R { get; set; }
        [JsonPropertyName("p")]
        public byte P { get; set; }
        [JsonPropertyName("q")]
        public byte Q { get; set; }
        [JsonPropertyName("ix")]
        public ushort IX { get; set; }
        [JsonPropertyName("iy")]
        public ushort IY { get; set; }
        [JsonPropertyName("af_")]
        public ushort AF_ { get; set; }
        [JsonPropertyName("bc_")]
        public ushort BC_ { get; set; }
        [JsonPropertyName("de_")]
        public ushort DE_ { get; set; }
        [JsonPropertyName("hl_")]
        public ushort HL_ { get; set; }
        [JsonPropertyName("im")]
        public byte IM { get; set; }
        [JsonPropertyName("iff1")]
        public byte IFF1 { get; set; }
        [JsonPropertyName("iff2")]
        public byte IFF2 { get; set; }
        [JsonPropertyName("wz")]
        public ushort WZ { get; set; }

        [JsonPropertyName("ram")]
        public List<List<int>> RAM { get; set; } = new();
    }
}