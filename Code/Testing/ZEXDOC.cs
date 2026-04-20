using Microsoft.Xna.Framework.Graphics;
using MonoGame.ImGuiNet;

public class ZEXDOC : IArcadeMachine
{
    Registers Reg;
    private byte[] Memory = new byte[0x10000];
    private Z80Cpu _cpu;
    private PacManMemoryBus _memoryMap = null;

    public ZEXDOC()
    {
        Reg = new Registers();
        _cpu = new Z80Cpu(_memoryMap);
        LoadRom();
    }
    
    public byte ReadByte(ushort address) => Memory[address];
    public void WriteByte(ushort address, byte value)
    {
        Memory[address] = value;
    }

    private void LoadRom()
    {
        using FileStream fs = File.OpenRead("roms/zexdoc.com");
        fs.ReadExactly(Memory, 0x0100, (int)fs.Length);
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

    public int Step()
    {
        throw new NotImplementedException();
    }

    public List<DrawRequest> GetDrawRequests(bool secondPlayFlip)
    {
        throw new NotImplementedException();
    }

    public void TriggerVBlankInterrupt()
    {
        throw new NotImplementedException();
    }

    public void DrawDebugUI(ImGuiRenderer renderer)
    {
        throw new NotImplementedException();
    }

    public int mode { get; set; }
    public List<int> subOptionIndices { get; set; }
    public bool secondPlayerFlip { get; }
    public int graphicsViewerMode { get; set; }
    public int tileViewerPaletteIndex { get; set; }

    public void Run()
    {
        // CP/M initialization
        Reg.SP = 0xF000;
        Reg.PC = 0x0100;

        while (true)
        {
            // Intercept Call 5
            if (Reg.PC == 0x0005)
            {
                HandleCpmCall();
                continue; 
            }

            if (Reg.PC == 0x0000) break;

            byte opcode = ReadByte(Reg.PC);
            _cpu._mainOpcodes[opcode]();
        }
    
        Console.WriteLine("\nTests Finished.");
    }

    private void HandleCpmCall()
    {
        if (Reg.C == 2) // Output character
        {
            Console.Write((char)Reg.E);
        }
        else if (Reg.C == 9) // Output string
        {
            ushort addr = Reg.DE;
            byte current;
            while ((current = ReadByte(addr++)) != (byte)'$')
            {
                Console.Write((char)current);
            }
        }

        _cpu.Op_RET(); 
    }
}