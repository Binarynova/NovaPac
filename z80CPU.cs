using System;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using Reg = Registers;

public class z80Cpu(Machine machine)
{
    private readonly bool[] _parity = new bool[256];
    private bool _iff1;
    private bool _halted;
    int traceCount = 0;
    int traceLimit = 1000000;

    private StreamWriter trace;

    private delegate int OpcodeHandler();
    private readonly OpcodeHandler[] _mainOpcodes = new OpcodeHandler[256];
    private readonly OpcodeHandler[] _ddOpcodes = new OpcodeHandler[256];
    private readonly OpcodeHandler[] _edOpcodes = new OpcodeHandler[256];
    private readonly OpcodeHandler[] _fdOpcodes = new OpcodeHandler[256];
    private readonly OpcodeHandler[] _cbOpcodes = new OpcodeHandler[256];

    // Interrupts
    private int _interruptMode;
    public bool InterruptPending;
    public bool EI_Pending;
    private byte interruptVectorLow;

    [Flags]
    private enum Flags : byte
    {
        C = 1 << 0, N = 1 << 1, P = 1 << 2, F3 = 1 << 3, H = 1 << 4, F5 = 1 << 5, Z = 1 << 6, S = 1 << 7
    }

    public void SetTraceWriter(StreamWriter writer)
    {
        trace = writer;
    }
    private void IncrementRegisterR()
    {
        Reg.R = (byte)((Reg.R & 0x80) | (((Reg.R & 0x7f) + 1) & 0x7f));
    }

    public void RequestInterrupt()
    {
        InterruptPending = true;
    }

    public void HandleInterrupt()
    {
        PushWord(Reg.PC);
        InterruptPending = _halted = false;
        _iff1 = false;

        switch(_interruptMode)
        {
            case 0:
                break;
            case 1:
                Reg.PC = 0x38;
                break;
            case 2:
                trace.WriteLine();
                trace.WriteLine($"   (interrupted at {Reg.PC+1:X4}, IRQ 0)");
                trace.WriteLine();
                ushort vectorAddress = (ushort)((Reg.I << 8) | interruptVectorLow);
                byte low = machine.ReadByte(vectorAddress);
                byte high = machine.ReadByte(((ushort)(vectorAddress + 1)));
                Reg.PC = (ushort)((high << 8) | low);
                break;
        }
    }
    
    public int Step(bool SteppingThrough)
    {
        if(InterruptPending && _iff1) // InterruptPending turned on in Game1.cs at the end of a frame, mimicking VBLANK
        {
            HandleInterrupt();
        }

        if (_halted)
        {
            if (InterruptPending)
            {
                _halted = false;       // resume from HALT
            }
            else
            {
                return 4;              // stay halted, do not fetch opcode
            }
        }

        if (traceCount == 104895)
        {
            Console.Clear();
        }
        byte opcode = machine.ReadByte(Reg.PC);
        
        if(SteppingThrough)
        {
            Console.Clear();
            Console.WriteLine($"Flags: {GetFlagDebugValue(Flags.S)}{GetFlagDebugValue(Flags.Z)}.{GetFlagDebugValue(Flags.H)}.{GetFlagDebugValue(Flags.P)}{GetFlagDebugValue(Flags.N)}{GetFlagDebugValue(Flags.C)}");
            Console.WriteLine();
            Console.WriteLine($"   PC: {Reg.PC:X4}");
            Console.WriteLine($"   SP: {Reg.SP:X4}");
            Console.WriteLine();
            Console.WriteLine($"   AF: {Reg.AF:X4}");
            Console.WriteLine($"   BC: {Reg.BC:X4}");
            Console.WriteLine($"   DE: {Reg.DE:X4}");
            Console.WriteLine($"   HL: {Reg.HL:X4}");
            Console.WriteLine($"   IX: {Reg.IX:X4}");
            Console.WriteLine($"   IY: {Reg.IY:X4}");
            Console.WriteLine($"    R: {Reg.R:X2}");
            Console.WriteLine($"    I: {Reg.I:X2}");
            Console.WriteLine($" IFF1: {_iff1}");
            Console.WriteLine($" HALT: {_halted : 1 ? 0}");
            Console.WriteLine();
            Console.WriteLine($"Next Opcode: {opcode:X2}");
            switch (Console.ReadKey().Key)
            {
                case ConsoleKey.Escape:
                    Environment.Exit(0);
                    break;
            }
        }
        else
            Console.WriteLine($"0x{Reg.PC:X4} - {opcode:X2}");

        IncrementRegisterR();
        if (trace != null && traceCount <= traceLimit && Reg.PC != 0)
        {
            trace.WriteLine($"{Reg.PC:X4}");
            traceCount++;
        }
        int cycles = _mainOpcodes[opcode]();

        if (!EI_Pending) return cycles;
        if (trace != null && traceCount <= traceLimit && Reg.PC != 0)
        {
            trace.WriteLine($"{Reg.PC:X4}");
            traceCount++;
        }
        EI_Pending = false;
        _iff1 = true;

        return cycles;
    }

    private static ushort LEWord(byte highByte, byte lowByte)
    {
        return (ushort)((highByte << 8) | lowByte);
    }

    private void PushWord(ushort value)
    {
        Reg.SP--;                     // decrement stack pointer
        machine.WriteByte(Reg.SP, Reg.HighByte(value)); // write high byte first
        Reg.SP--;
        machine.WriteByte(Reg.SP, Reg.LowByte(value));  // then low byte
    }
    private ushort PopWord()
    {
        byte low = machine.ReadByte(Reg.SP);
        Reg.SP++;
        byte high = machine.ReadByte(Reg.SP);
        Reg.SP++;
        
        ushort value = LEWord(high, low);
        return value;
    }

    private void WritePort(ushort port, byte value)
    {
        switch (port & 0xFF)
        {
            case 0x00:
                interruptVectorLow = value;
                break;
            case 0x01:
                // sound
                break;
            case 0x02:
                // watchdog
                break;
        }
    }

    private void BuildOpcodeTable()
    {
        for (int i = 0; i < 256; i++)
        {
            _mainOpcodes[i] = Op_UNK;
            _ddOpcodes[i] = Op_UNK;
            _edOpcodes[i] = Op_UNK;
            _fdOpcodes[i] = Op_UNK;
        }

        _mainOpcodes[0x00] = Op_NOP;
        _mainOpcodes[0x01] = Op_LD_BC_nn;
        _mainOpcodes[0x03] = Op_INC_BC;
        _mainOpcodes[0x04] = Op_INC_B;
        _mainOpcodes[0x05] = Op_DEC_B;
        _mainOpcodes[0x06] = Op_LD_B_n;
        _mainOpcodes[0x07] = Op_RLCA;
        _mainOpcodes[0x0B] = Op_DEC_BC;
        _mainOpcodes[0x0C] = Op_INC_C;
        _mainOpcodes[0x0D] = Op_DEC_C;
        _mainOpcodes[0x0E] = Op_LD_C_n;
        _mainOpcodes[0x0F] = Op_RRCA;

        _mainOpcodes[0x10] = Op_DJNZ_e;
        _mainOpcodes[0x11] = Op_LD_DE_nn;
        _mainOpcodes[0x12] = Op_LD_DE_A;
        _mainOpcodes[0x13] = Op_INC_DE;
        _mainOpcodes[0x14] = Op_INC_D;
        _mainOpcodes[0x15] = Op_DEC_D;
        _mainOpcodes[0x16] = Op_LD_D_n;
        _mainOpcodes[0x18] = Op_JR_e;
        _mainOpcodes[0x19] = Op_ADD_HL_DE;
        _mainOpcodes[0x1A] = Op_LD_A_ptrDE;
        _mainOpcodes[0x1C] = Op_INC_E;
        _mainOpcodes[0x1D] = Op_DEC_E;
        _mainOpcodes[0x1E] = Op_LD_E_n;

        _mainOpcodes[0x20] = Op_JR_NZ_e;
        _mainOpcodes[0x21] = Op_LD_HL_nn;
        _mainOpcodes[0x22] = Op_LD_ptrnn_HL;
        _mainOpcodes[0x23] = Op_INC_HL;
        _mainOpcodes[0x24] = Op_INC_H;
        _mainOpcodes[0x25] = Op_DEC_H;
        _mainOpcodes[0x26] = Op_LD_H_n;
        _mainOpcodes[0x28] = Op_JR_Z_e;
        _mainOpcodes[0x2A] = Op_LD_HL_ptrnn;
        _mainOpcodes[0x2C] = Op_INC_L;
        _mainOpcodes[0x2D] = Op_DEC_L;
        _mainOpcodes[0x2E] = Op_LD_L_n;

        _mainOpcodes[0x30] = Op_JR_NC_e;
        _mainOpcodes[0x31] = Op_LD_SP_nn;
        _mainOpcodes[0x32] = Op_LD_ptrnn_A;
        _mainOpcodes[0x33] = Op_INC_SP;
        _mainOpcodes[0x34] = Op_INC_ptrHL;
        _mainOpcodes[0x35] = Op_DEC_ptrHL;
        _mainOpcodes[0x36] = Op_LD_ptrHL_n;
        _mainOpcodes[0x38] = Op_JR_C_e;
        _mainOpcodes[0x3A] = Op_LD_A_ptrNN;
        _mainOpcodes[0x3B] = Op_DEC_SP;
        _mainOpcodes[0x3C] = Op_INC_A;
        _mainOpcodes[0x3D] = Op_DEC_A;
        _mainOpcodes[0x3E] = Op_LD_A_n;
        _mainOpcodes[0x3F] = Op_CCF;

        _mainOpcodes[0x40] = Op_LD_B_B;
        _mainOpcodes[0x41] = Op_LD_B_C;
        _mainOpcodes[0x42] = Op_LD_B_D;
        _mainOpcodes[0x43] = Op_LD_B_E;
        _mainOpcodes[0x44] = Op_LD_B_H;
        _mainOpcodes[0x45] = Op_LD_B_L;
        _mainOpcodes[0x46] = Op_LD_B_ptrHL;
        _mainOpcodes[0x47] = Op_LD_B_A;
        _mainOpcodes[0x48] = Op_LD_C_B;
        _mainOpcodes[0x49] = Op_LD_C_C;
        _mainOpcodes[0x4A] = Op_LD_C_D;
        _mainOpcodes[0x4B] = Op_LD_C_E;
        _mainOpcodes[0x4C] = Op_LD_C_H;
        _mainOpcodes[0x4D] = Op_LD_C_L;
        _mainOpcodes[0x4E] = Op_LD_C_ptrHL;
        _mainOpcodes[0x4F] = Op_LD_C_A;

        _mainOpcodes[0x50] = Op_LD_D_B;
        _mainOpcodes[0x51] = Op_LD_D_C;
        _mainOpcodes[0x52] = Op_LD_D_D;
        _mainOpcodes[0x53] = Op_LD_D_E;
        _mainOpcodes[0x54] = Op_LD_D_H;
        _mainOpcodes[0x55] = Op_LD_D_L;
        _mainOpcodes[0x56] = Op_LD_D_ptrHL;
        _mainOpcodes[0x57] = Op_LD_D_A;
        _mainOpcodes[0x58] = Op_LD_E_B;
        _mainOpcodes[0x59] = Op_LD_E_C;
        _mainOpcodes[0x5A] = Op_LD_E_D;
        _mainOpcodes[0x5B] = Op_LD_E_E;
        _mainOpcodes[0x5C] = Op_LD_E_H;
        _mainOpcodes[0x5D] = Op_LD_E_L;
        _mainOpcodes[0x5E] = Op_LD_E_ptrHL;
        _mainOpcodes[0x5F] = Op_LD_E_A;

        _mainOpcodes[0x60] = Op_LD_H_B;
        _mainOpcodes[0x61] = Op_LD_H_C;
        _mainOpcodes[0x62] = Op_LD_H_D;
        _mainOpcodes[0x63] = Op_LD_H_E;
        _mainOpcodes[0x64] = Op_LD_H_H;
        _mainOpcodes[0x65] = Op_LD_H_L;
        _mainOpcodes[0x66] = Op_LD_H_ptrHL;
        _mainOpcodes[0x67] = Op_LD_H_A;
        _mainOpcodes[0x68] = Op_LD_L_B;
        _mainOpcodes[0x69] = Op_LD_L_C;
        _mainOpcodes[0x6A] = Op_LD_L_D;
        _mainOpcodes[0x6B] = Op_LD_L_E;
        _mainOpcodes[0x6C] = Op_LD_L_H;
        _mainOpcodes[0x6D] = Op_LD_L_L;
        _mainOpcodes[0x6E] = Op_LD_L_ptrHL;
        _mainOpcodes[0x6F] = Op_LD_L_A;

        _mainOpcodes[0x70] = Op_LD_ptrHL_B;
        _mainOpcodes[0x71] = Op_LD_ptrHL_C;
        _mainOpcodes[0x72] = Op_LD_ptrHL_D;
        _mainOpcodes[0x73] = Op_LD_ptrHL_E;
        _mainOpcodes[0x74] = Op_LD_ptrHL_H;
        _mainOpcodes[0x75] = Op_LD_ptrHL_L;
        _mainOpcodes[0x76] = Op_HALT;
        _mainOpcodes[0x77] = Op_LD_ptrHL_A;
        _mainOpcodes[0x78] = Op_LD_A_B;
        _mainOpcodes[0x79] = Op_LD_A_C;
        _mainOpcodes[0x7A] = Op_LD_A_D;
        _mainOpcodes[0x7B] = Op_LD_A_E;
        _mainOpcodes[0x7C] = Op_LD_A_H;
        _mainOpcodes[0x7D] = Op_LD_A_L;
        _mainOpcodes[0x7E] = Op_LD_A_ptrHL;
        _mainOpcodes[0x7F] = Op_LD_A_A;

        _mainOpcodes[0x80] = Op_ADD_A_B;
        _mainOpcodes[0x81] = Op_ADD_A_C;
        _mainOpcodes[0x82] = Op_ADD_A_D;
        _mainOpcodes[0x83] = Op_ADD_A_E;
        _mainOpcodes[0x84] = Op_ADD_A_H;
        _mainOpcodes[0x85] = Op_ADD_A_L;
        _mainOpcodes[0x86] = Op_ADD_A_ptrHL;
        _mainOpcodes[0x87] = Op_ADD_A_A;

        _mainOpcodes[0x88] = Op_ADC_A_B;
        _mainOpcodes[0x89] = Op_ADC_A_C;
        _mainOpcodes[0x8A] = Op_ADC_A_D;
        _mainOpcodes[0x8B] = Op_ADC_A_E;
        _mainOpcodes[0x8C] = Op_ADC_A_H;
        _mainOpcodes[0x8D] = Op_ADC_A_L;
        _mainOpcodes[0x8E] = Op_ADC_A_ptrHL;
        _mainOpcodes[0x8F] = Op_ADC_A_A;

        _mainOpcodes[0x90] = Op_SUB_A_B;
        _mainOpcodes[0x91] = Op_SUB_A_C;
        _mainOpcodes[0x92] = Op_SUB_A_D;
        _mainOpcodes[0x93] = Op_SUB_A_E;
        _mainOpcodes[0x94] = Op_SUB_A_H;
        _mainOpcodes[0x95] = Op_SUB_A_L;
        _mainOpcodes[0x96] = Op_SUB_A_ptrHL;
        _mainOpcodes[0x97] = Op_SUB_A_A;
        _mainOpcodes[0x98] = Op_SBC_A_B;
        _mainOpcodes[0x99] = Op_SBC_A_C;
        _mainOpcodes[0x9A] = Op_SBC_A_D;
        _mainOpcodes[0x9B] = Op_SBC_A_E;
        _mainOpcodes[0x9C] = Op_SBC_A_H;
        _mainOpcodes[0x9D] = Op_SBC_A_L;
        _mainOpcodes[0x9E] = Op_SBC_A_ptrHL;
        _mainOpcodes[0x9F] = Op_SBC_A_A;

        _mainOpcodes[0xA0] = Op_AND_B;
        _mainOpcodes[0xA1] = Op_AND_C;
        _mainOpcodes[0xA2] = Op_AND_D;
        _mainOpcodes[0xA3] = Op_AND_E;
        _mainOpcodes[0xA4] = Op_AND_H;
        _mainOpcodes[0xA5] = Op_AND_L;
        _mainOpcodes[0xA6] = Op_AND_ptrHL;
        _mainOpcodes[0xA7] = Op_AND_A;
        _mainOpcodes[0xAF] = Op_XOR_A_A;

        _mainOpcodes[0xB0] = Op_OR_B;
        _mainOpcodes[0xB1] = Op_OR_C;
        _mainOpcodes[0xB2] = Op_OR_D;
        _mainOpcodes[0xB3] = Op_OR_E;
        _mainOpcodes[0xB4] = Op_OR_H;
        _mainOpcodes[0xB5] = Op_OR_L;
        _mainOpcodes[0xB6] = Op_OR_ptrHL;
        _mainOpcodes[0xB7] = Op_OR_A;
        _mainOpcodes[0xB9] = Op_CP_C;
        _mainOpcodes[0xBE] = Op_CP_ptrHL;

        _mainOpcodes[0xC0] = Op_RET_NZ;
        _mainOpcodes[0xC1] = Op_POP_BC;
        _mainOpcodes[0xC2] = Op_JP_NZ_nn;
        _mainOpcodes[0xC3] = Op_JP_nn;
        _mainOpcodes[0xC5] = Op_PUSH_BC;
        _mainOpcodes[0xC6] = Op_ADD_A_n;
        _mainOpcodes[0xC7] = () => Op_RST(0x00);
        _mainOpcodes[0xC8] = Op_RET_Z;
        _mainOpcodes[0xC9] = Op_RET;
        _mainOpcodes[0xCA] = Op_JP_Z_nn;
        _mainOpcodes[0xCB] = Op_CB;
        _mainOpcodes[0xCD] = Op_CALL_nn;
        _mainOpcodes[0xCF] = () => Op_RST(0x08);

        _mainOpcodes[0xD0] = Op_RET_NC;
        _mainOpcodes[0xD1] = Op_POP_DE;
        _mainOpcodes[0xD2] = Op_JP_NC_nn;
        _mainOpcodes[0xD3] = Op_OUT_ptrn_A;
        _mainOpcodes[0xD5] = Op_PUSH_DE;
        _mainOpcodes[0xD6] = Op_SUB_n;
        _mainOpcodes[0xD7] = () => Op_RST(0x10);
        _mainOpcodes[0xD9] = Op_EXX;
        _mainOpcodes[0xDA] = Op_JP_C_nn;
        _mainOpcodes[0xDD] = Op_DD;
        _mainOpcodes[0xDF] = () => Op_RST(0x18);

        _mainOpcodes[0xE1] = Op_POP_HL;
        _mainOpcodes[0xE5] = Op_PUSH_HL;
        _mainOpcodes[0xE6] = Op_AND_n;
        _mainOpcodes[0xE7] = () => Op_RST(0x20);
        _mainOpcodes[0xE9] = Op_JP_ptrHL;
        _mainOpcodes[0xEB] = Op_EX_DE_HL;
        _mainOpcodes[0xED] = Op_ED;
        _mainOpcodes[0xEE] = Op_XOR_A_n;
        _mainOpcodes[0xEF] = () => Op_RST(0x28);

        _mainOpcodes[0xF0] = Op_RET_P;
        _mainOpcodes[0xF1] = Op_POP_AF;
        _mainOpcodes[0xF3] = Op_DI;
        _mainOpcodes[0xF5] = Op_PUSH_AF;
        _mainOpcodes[0xF7] = () => Op_RST(0x30);
        _mainOpcodes[0xF8] = Op_RET_M;
        _mainOpcodes[0xFA] = Op_JP_M_nn;
        _mainOpcodes[0xFB] = Op_EI;
        _mainOpcodes[0xFC] = Op_CALL_M_nn;
        _mainOpcodes[0xFD] = Op_FD;
        _mainOpcodes[0xFE] = Op_CP_n;
        _mainOpcodes[0xFF] = () => Op_RST(0x38);

        _ddOpcodes[0x19] = Op_ADD_IX_DE;
        _ddOpcodes[0x21] = Op_LD_IX_nn;
        _ddOpcodes[0x70] = Op_LD_ptrIXd_B;
        _ddOpcodes[0x71] = Op_LD_ptrIXd_C;
        _ddOpcodes[0x72] = Op_LD_ptrIXd_D;
        _ddOpcodes[0x73] = Op_LD_ptrIXd_E;
        _ddOpcodes[0x74] = Op_LD_ptrIXd_H;
        _ddOpcodes[0x75] = Op_LD_ptrIXd_L;
        _ddOpcodes[0x77] = Op_LD_ptrIXd_A;
        _ddOpcodes[0x7E] = Op_LD_A_ptrIXd;
        _ddOpcodes[0xE1] = Op_POP_IX;
        _ddOpcodes[0xE5] = Op_PUSH_IX;

        _edOpcodes[0x42] = Op_SBC_HL_BC;
        _edOpcodes[0x46] = Op_IM_0;
        _edOpcodes[0x47] = Op_LD_I_A;
        _edOpcodes[0x52] = Op_SBC_HL_DE;
        _edOpcodes[0x56] = Op_IM_1;
        _edOpcodes[0x5E] = Op_IM_2;
        _edOpcodes[0x62] = Op_SBC_HL_HL;
        _edOpcodes[0x72] = Op_SBC_HL_SP;
        _edOpcodes[0xB0] = Op_LDIR;

        _fdOpcodes[0x21] = Op_LD_IY_nn;
        _fdOpcodes[0x6E] = Op_LD_L_ptrIYd;
        _fdOpcodes[0xE1] = Op_POP_IY;

        _cbOpcodes[0x7E] = Op_BIT_7_ptrHL;
    }

    private void InitParity()
    {
        for (int i = 0; i < 256; i++)
        {
            int count = 0;
            for (int b = 0; b < 8; b++)
                if ((i & (1 << b)) != 0) count++;

            _parity[i] = (count % 2) == 0;
        }
    }
    private void SetParity(byte value)
    {
        //flags.ParityOverflow = parity[value];
        WriteFlag(Flags.P, _parity[value]);
    }
    private static void CheckINCOverflow(byte value)
    {
        if (value == 0x7F)
            SetFlag(Flags.P);
        else
            ClearFlag(Flags.P);
    }
    private static void CheckDECOverflow(byte value)
    {
        if (value == 0x7F)
            SetFlag(Flags.P);
        else
            ClearFlag(Flags.P);
    }

    public void Reset()
    {
        // reset CPU values
        machine.ClearRAM();
        Reg.PC = 0x0000;
        Reg.A = Reg.B = Reg.C = Reg.D = Reg.E = 0;
        Reg.HL = 0x0000;
        Reg.IX = Reg.IY = 0xFFFF;
        Reg.F = 0x40;
        Reg.SP = 0x0000;
        _iff1 = false;
        _halted = false;
        machine.ReadROMsIntoMemory();
        EI_Pending = false;
        InterruptPending = false;
        BuildOpcodeTable();
        InitParity();
    }    

    private static void SetFlag(Flags f)
    {
        Reg.F |= (byte)f;
    }
    private static void ClearFlag(Flags f)
    {
        Reg.F &= (byte)~f;
    }
    private static bool GetFlag(Flags f)
    {
        return (Reg.F & (byte)f) != 0;
    }

    private static string GetFlagDebugValue(Flags f)
    {
        return (Reg.F & (byte)f) != 0 ? f.ToString() : ".";
    }
    private static void ToggleFlag(Flags f)
    {
        Reg.F = (byte)(Reg.F ^ (byte)f);
    }
    private static void WriteFlag(Flags f, bool value)
    {
        if (value) SetFlag(f);
        else ClearFlag(f);
    }

    private static void SetSZFlags(byte value)
    {
        WriteFlag(Flags.Z, value == 0);
        WriteFlag(Flags.S, (value & 0x80) != 0);
        WriteFlag(Flags.F5, (value & 0x20) != 0);
        WriteFlag(Flags.F3, (value & 0x08) != 0);
    }
    
    private byte ImmediateValue() => machine.ReadByte((ushort)(Reg.PC + 1));
    private static ushort ImmediateExtendedValue(Machine machine, ushort addr)
    {
        // read word (little-endian direction)
        byte low = machine.ReadByte(addr);
        byte high = machine.ReadByte((ushort)(addr + 1));
        return LEWord(high, low);
    }

    private sbyte ReadSignedOffset() => (sbyte)ImmediateValue(); // needs to be sbyte to properly handled sign
    
    // OPCODES AND HELPERS
    private int Op_UNK()
    {
        throw new NotImplementedException(
            $"Unhandled opcode {machine.ReadByte(Reg.PC):X2} at {Reg.PC:X4}"
        );
    }
    private int Op_DD()
    {
        byte opcode = ImmediateValue();
        // prefix opcodes increment Reg.PC by 1, remaining bytes should be incremented in suffix opcode
        Reg.PC++;
        IncrementRegisterR();
        return _ddOpcodes[opcode]();
    }
    private int Op_ED()
    {
        byte opcode = ImmediateValue();
        // prefix opcodes increment Reg.PC by 1, remaining bytes should be incremented in suffix opcode
        Reg.PC++;
        IncrementRegisterR();
        return _edOpcodes[opcode]();
    }
    private int Op_FD()
    {
        byte opcode = ImmediateValue();
        // prefix opcodes increment Reg.PC by 1, remaining bytes should be incremented in suffix opcode
        Reg.PC++;
        IncrementRegisterR();
        return _fdOpcodes[opcode]();
    }
    private int Op_CB()
    {
        byte opcode = ImmediateValue();
        // prefix opcodes increment Reg.PC by 1, remaining bytes should be incremented in suffix opcode
        Reg.PC++;
        IncrementRegisterR();
        return _cbOpcodes[opcode]();
    }
        
    private int AND(byte value, int cycles)
    {
        Reg.A &= value;

        ClearFlag(Flags.C | Flags.N);
        SetFlag(Flags.H);       
        SetSZFlags(Reg.A);
        SetParity(Reg.A);

        Reg.PC += 1;        
        return cycles;
    }
    private int OR(byte value, int cycles)
    {
        Reg.A |= value;

        ClearFlag(Flags.C | Flags.N | Flags.H);
        SetSZFlags(Reg.A);
        SetParity(Reg.A);

        Reg.PC += 1;
        return cycles;
    }
    private static byte ADD(byte acc, byte value)
    {
        ushort sum = (ushort)(acc + value);
        byte result = (byte)sum;

        WriteFlag(Flags.C, sum > 0xFF);
        WriteFlag(Flags.H, ((acc & 0x0F) + (value & 0x0F)) > 0x0F);                
        ClearFlag(Flags.N);        
        SetSZFlags(result);
        WriteFlag(Flags.P, ((acc ^ result) & (value ^ result) & 0x80) != 0);

        return (byte)sum;
    }
    private static ushort ADDWord(ushort acc, ushort value)
    {
        uint sum = (uint)(acc + value);

        WriteFlag(Flags.C, sum > 0xFFFF);
        WriteFlag(Flags.H, ((acc & 0x0FFF) + (value & 0x0FFF)) > 0x0FFF);                
        ClearFlag(Flags.N);

        return (ushort)sum;
    }
    private static byte SUB(byte acc, byte value)
    {
        byte result = (byte)(acc - value);

        WriteFlag(Flags.C, acc < value);
        WriteFlag(Flags.H, ((acc ^ value ^ result) & 0x10) != 0);
        SetFlag(Flags.N); 
        SetSZFlags(result);
        WriteFlag(Flags.P, ((acc ^ value) & (acc ^ result) & 0x80) != 0);

        return result;
    }
    private static byte ADC(byte acc, byte value)
    {
        int carry = GetFlag(Flags.C) ? 1 : 0;
        ushort sum = (ushort)(acc + value + carry);
        byte result = (byte)sum;

        WriteFlag(Flags.C, sum > 0xFF);
        WriteFlag(Flags.H, ((acc & 0x0F) + (value & 0x0F) + carry) > 0x0F);                
        ClearFlag(Flags.N);        
        SetSZFlags(result);
        WriteFlag(Flags.P, ((acc ^ result) & (value ^ result) & 0x80) != 0);

        return (byte)sum;        
    }
    private static byte SBC(byte acc, byte value)
    {
        int carry = GetFlag(Flags.C) ? 1 : 0;
        byte result = (byte)(acc - value - carry);

        WriteFlag(Flags.C, acc < (value + carry));
        WriteFlag(Flags.H, (acc & 0x0F) < ((value & 0x0F) + carry));
        SetFlag(Flags.N); 
        SetSZFlags(result);
        WriteFlag(Flags.P, ((acc ^ value) & (acc ^ result) & 0x80) != 0);

        return result;
    }
    private static ushort SBCWord(ushort acc, ushort value)
    {
        int carry = GetFlag(Flags.C) ? 1 : 0;
        ushort result = (ushort)(acc - value - carry);

        WriteFlag(Flags.C, acc < (value + carry));
        WriteFlag(Flags.H, (acc & 0x0FFF) < ((value & 0x0FFF) + carry));
        SetFlag(Flags.N);

        // 16-bit signed overflow detection
        bool overflow = ((acc ^ value) & (acc ^ result) & 0x8000) != 0;
        WriteFlag(Flags.P, overflow);

        // S/Z for 16-bit
        WriteFlag(Flags.S, (result & 0x8000) != 0);
        WriteFlag(Flags.Z, result == 0);

        return result;
    }
    private int BIT(byte n, byte value, bool isMemory = false)
    {
        // Test the bit
        bool bitSet = (value & (1 << n)) != 0;

        // Flags
        ClearFlag(Flags.N);            // N always cleared
        WriteFlag(Flags.H, true);              // H always set
        WriteFlag(Flags.S, n == 7 && bitSet); // S only set for bit 7
        WriteFlag(Flags.Z, !bitSet);         // Z set if bit is 0
        WriteFlag(Flags.P, !bitSet);       // PV mirrors Z
        WriteFlag(Flags.F5, (value & 0x20) != 0); // undocumented F5
        WriteFlag(Flags.F3, (value & 0x08) != 0); // undocumented F3

        // Return cycles
        return isMemory ? 12 : 8; // 12 cycles if operand is (HL), else 8
    }
    
    private void LOAD_ptrIXd(byte reg)
    {
        sbyte d = (sbyte)machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        machine.WriteByte(addr, reg);
    }
 
    private int Op_LDIR()
    {        
        ClearFlag(Flags.N | Flags.H);
        if(Reg.BC == 0)
        {
            Reg.PC += 1;
            return 16;
        }

        machine.WriteByte(Reg.DE, machine.ReadByte(Reg.HL));
        Reg.HL += 1;
        Reg.DE += 1;
        Reg.BC -= 1;
        if(Reg.BC != 0)
        {
            SetFlag(Flags.P);
        }
        else
        {
            ClearFlag(Flags.P);
            Reg.PC += 1;
        }

        return (Reg.BC != 0) ? 21 : 16;
    }
    private int Op_LD_IX_nn()
    {
        Reg.IX = ImmediateExtendedValue(machine, (ushort)(Reg.PC + 1));
        Reg.PC += 3;
        return 14;
    }
    
    private int Op_LD_ptrIXd_A() { LOAD_ptrIXd(Reg.A); Reg.PC += 2; return 19; }
    private int Op_LD_ptrIXd_B() { LOAD_ptrIXd(Reg.B); Reg.PC += 2; return 19; }
    private int Op_LD_ptrIXd_C() { LOAD_ptrIXd(Reg.C); Reg.PC += 2; return 19; }
    private int Op_LD_ptrIXd_D() { LOAD_ptrIXd(Reg.D); Reg.PC += 2; return 19; }
    private int Op_LD_ptrIXd_E() { LOAD_ptrIXd(Reg.E); Reg.PC += 2; return 19; }
    private int Op_LD_ptrIXd_H() { LOAD_ptrIXd(Reg.H); Reg.PC += 2; return 19; }
    private int Op_LD_ptrIXd_L() { LOAD_ptrIXd(Reg.L); Reg.PC += 2; return 19; }
    private int Op_LD_A_ptrIXd()
    {
        sbyte d = (sbyte)machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        Reg.A = machine.ReadByte(addr);
        
        Reg.PC += 2;
        return 19;
    }
    
    private int Op_CP_n()
    {
        byte n = ImmediateValue();
        byte result = (byte)(Reg.A - n);        

        WriteFlag(Flags.C, Reg.A < n);
        WriteFlag(Flags.H, (Reg.A & 0x0F) < (n & 0x0F));
        SetFlag(Flags.N);
        SetSZFlags(result);
        WriteFlag(Flags.P, ((Reg.A ^ n) & (Reg.A ^ result) & 0x80) != 0);

        Reg.PC += 2;
        return 7;
    }
    private static int Op_CP_C()
    {        
        byte result = (byte)(Reg.A - Reg.C);        

        WriteFlag(Flags.C, Reg.A < Reg.C);
        WriteFlag(Flags.H, (Reg.A & 0x0F) < (Reg.C & 0x0F));
        SetFlag(Flags.N);
        SetSZFlags(result);
        WriteFlag(Flags.P, ((Reg.A ^ Reg.C) & (Reg.A ^ result) & 0x80) != 0);

        Reg.PC += 1;
        return 4;
    }
    private int Op_CP_ptrHL()
    {
        byte value = machine.ReadByte(Reg.HL);
        byte result = (byte)(Reg.A - value);        

        WriteFlag(Flags.C, Reg.A < value);
        WriteFlag(Flags.H, (Reg.A & 0x0F) < (value & 0x0F));
        SetFlag(Flags.N);
        SetSZFlags(result);
        WriteFlag(Flags.P, ((Reg.A ^ value) & (Reg.A ^ result) & 0x80) != 0);

        Reg.PC += 1;
        return 7;
    }

    private int Op_ADD_A_n()
    {
        Reg.A = ADD(Reg.A, ImmediateValue());
        Reg.PC += 2;
        return 7;
    }

    private static int Op_ADD_A_A() { Reg.A = ADD(Reg.A, Reg.A); Reg.PC += 1; return 4; }
    private static int Op_ADD_A_B() { Reg.A = ADD(Reg.A, Reg.B); Reg.PC += 1; return 4; }
    private static int Op_ADD_A_C() { Reg.A = ADD(Reg.A, Reg.C); Reg.PC += 1; return 4; }
    private static int Op_ADD_A_D() { Reg.A = ADD(Reg.A, Reg.D); Reg.PC += 1; return 4; }
    private static int Op_ADD_A_E() { Reg.A = ADD(Reg.A, Reg.E); Reg.PC += 1; return 4; }
    private static int Op_ADD_A_H() { Reg.A = ADD(Reg.A, Reg.H); Reg.PC += 1; return 4; }
    private static int Op_ADD_A_L() { Reg.A = ADD(Reg.A, Reg.L); Reg.PC += 1; return 4; }
    private int Op_ADD_A_ptrHL() { Reg.A = ADD(Reg.A, machine.ReadByte(Reg.HL)); Reg.PC += 1; return 7; }
    private static int Op_ADD_IX_DE() { Reg.IX = ADDWord(Reg.IX, Reg.DE); Reg.PC += 1; return 15; }
    private static int Op_ADD_HL_DE() { Reg.HL = ADDWord(Reg.HL, Reg.DE); Reg.PC += 1; return 11; }

    private static int Op_ADC_A_A() { Reg.A = ADC(Reg.A, Reg.A); Reg.PC += 1; return 4; }
    private static int Op_ADC_A_B() { Reg.A = ADC(Reg.A, Reg.B); Reg.PC += 1; return 4; }
    private static int Op_ADC_A_C() { Reg.A = ADC(Reg.A, Reg.C); Reg.PC += 1; return 4; }
    private static int Op_ADC_A_D() { Reg.A = ADC(Reg.A, Reg.D); Reg.PC += 1; return 4; }
    private static int Op_ADC_A_E() { Reg.A = ADC(Reg.A, Reg.E); Reg.PC += 1; return 4; }
    private static int Op_ADC_A_H() { Reg.A = ADC(Reg.A, Reg.H); Reg.PC += 1; return 4; }
    private static int Op_ADC_A_L() { Reg.A = ADC(Reg.A, Reg.L); Reg.PC += 1; return 4; }
    private int Op_ADC_A_ptrHL() { Reg.A = ADC(Reg.A, machine.ReadByte(Reg.HL)); Reg.PC += 1; return 7; }

    private static int Op_SUB_A_A() { Reg.A = SUB(Reg.A, Reg.A); Reg.PC += 1; return 4; }
    private static int Op_SUB_A_B() { Reg.A = SUB(Reg.A, Reg.B); Reg.PC += 1; return 4; }
    private static int Op_SUB_A_C() { Reg.A = SUB(Reg.A, Reg.C); Reg.PC += 1; return 4; }
    private static int Op_SUB_A_D() { Reg.A = SUB(Reg.A, Reg.D); Reg.PC += 1; return 4; }
    private static int Op_SUB_A_E() { Reg.A = SUB(Reg.A, Reg.E); Reg.PC += 1; return 4; }
    private static int Op_SUB_A_H() { Reg.A = SUB(Reg.A, Reg.H); Reg.PC += 1; return 4; }
    private static int Op_SUB_A_L() { Reg.A = SUB(Reg.A, Reg.L); Reg.PC += 1; return 4; }
    private int Op_SUB_A_ptrHL() { Reg.A = SUB(Reg.A, machine.ReadByte(Reg.HL)); Reg.PC += 1; return 7; }
    private int Op_SUB_n()
    {
        byte value = ImmediateValue();
        Reg.A = SUB(Reg.A, value);
        Reg.PC += 2;
        return 7;
    }
    private static int Op_SBC_A_A() { Reg.A = SBC(Reg.A, Reg.A); Reg.PC += 1; return 4; }
    private static int Op_SBC_A_B() { Reg.A = SBC(Reg.A, Reg.B); Reg.PC += 1; return 4; }
    private static int Op_SBC_A_C() { Reg.A = SBC(Reg.A, Reg.C); Reg.PC += 1; return 4; }
    private static int Op_SBC_A_D() { Reg.A = SBC(Reg.A, Reg.D); Reg.PC += 1; return 4; }
    private static int Op_SBC_A_E() { Reg.A = SBC(Reg.A, Reg.E); Reg.PC += 1; return 4; }
    private static int Op_SBC_A_H() { Reg.A = SBC(Reg.A, Reg.H); Reg.PC += 1; return 4; }
    private static int Op_SBC_A_L() { Reg.A = SBC(Reg.A, Reg.L); Reg.PC += 1; return 4; }
    private int Op_SBC_A_ptrHL() { Reg.A = SBC(Reg.A, machine.ReadByte(Reg.HL)); Reg.PC += 1; return 7; }

    private static int Op_SBC_HL_BC() { Reg.HL = SBCWord(Reg.HL, Reg.BC); Reg.PC += 1; return 15; }
    private static int Op_SBC_HL_DE() { Reg.HL = SBCWord(Reg.HL, Reg.DE); Reg.PC += 1; return 15; }
    private static int Op_SBC_HL_HL() { Reg.HL = SBCWord(Reg.HL, Reg.HL); Reg.PC += 1; return 15; }
    private static int Op_SBC_HL_SP() { Reg.HL = SBCWord(Reg.HL, Reg.SP); Reg.PC += 1; return 15; }

    private static int Op_NOP() { Reg.PC += 1; return 4; }

    private static int Op_LD_A_A() { Reg.PC += 1; return 4; }
    private static int Op_LD_A_B() { Reg.A = Reg.B; Reg.PC += 1; return 4; }
    private static int Op_LD_A_C() { Reg.A = Reg.C; Reg.PC += 1; return 4; }
    private static int Op_LD_A_D() { Reg.A = Reg.D; Reg.PC += 1; return 4; }
    private static int Op_LD_A_E() { Reg.A = Reg.E; Reg.PC += 1; return 4; }
    private static int Op_LD_A_H() { Reg.A = Reg.H; Reg.PC += 1; return 4; }
    private static int Op_LD_A_L() { Reg.A = Reg.L; Reg.PC += 1; return 4; }
    private int Op_LD_A_n() { Reg.A = ImmediateValue(); Reg.PC += 2; return 7; }

    private static int Op_LD_B_A() { Reg.B = Reg.A; Reg.PC += 1; return 4; }
    private static int Op_LD_B_B() { Reg.PC += 1; return 4; }
    private static int Op_LD_B_C() { Reg.B = Reg.C; Reg.PC += 1; return 4; }
    private static int Op_LD_B_D() { Reg.B = Reg.D; Reg.PC += 1; return 4; }
    private static int Op_LD_B_E() { Reg.B = Reg.E; Reg.PC += 1; return 4; }
    private static int Op_LD_B_H() { Reg.B = Reg.H; Reg.PC += 1; return 4; }
    private static int Op_LD_B_L() { Reg.B = Reg.L; Reg.PC += 1; return 4; }
    private int Op_LD_B_n() { Reg.B = ImmediateValue(); Reg.PC += 2; return 7; }

    private static int Op_LD_C_A() { Reg.C = Reg.A; Reg.PC += 1; return 4; }
    private static int Op_LD_C_B() { Reg.C = Reg.B; Reg.PC += 1; return 4; }
    private static int Op_LD_C_C() { Reg.PC += 1; return 4; }
    private static int Op_LD_C_D() { Reg.C = Reg.D; Reg.PC += 1; return 4; }
    private static int Op_LD_C_E() { Reg.C = Reg.E; Reg.PC += 1; return 4; }
    private static int Op_LD_C_H() { Reg.C = Reg.H; Reg.PC += 1; return 4; }
    private static int Op_LD_C_L() { Reg.C = Reg.L; Reg.PC += 1; return 4; }
    private int Op_LD_C_n() { Reg.C = ImmediateValue(); Reg.PC += 2; return 7; }

    private static int Op_LD_D_A() { Reg.D = Reg.A; Reg.PC += 1; return 4; }
    private static int Op_LD_D_B() { Reg.D = Reg.B; Reg.PC += 1; return 4; }
    private static int Op_LD_D_C() { Reg.D = Reg.C; Reg.PC += 1; return 4; }
    private static int Op_LD_D_D() { Reg.PC += 1; return 4; }
    private static int Op_LD_D_E() { Reg.D = Reg.E; Reg.PC += 1; return 4; }
    private static int Op_LD_D_H() { Reg.D = Reg.H; Reg.PC += 1; return 4; }
    private static int Op_LD_D_L() { Reg.D = Reg.L; Reg.PC += 1; return 4; }
    private int Op_LD_D_n() { Reg.D = ImmediateValue(); Reg.PC += 2; return 7; }

    private static int Op_LD_E_A() { Reg.E = Reg.A; Reg.PC += 1; return 4; }
    private static int Op_LD_E_B() { Reg.E = Reg.B; Reg.PC += 1; return 4; }
    private static int Op_LD_E_C() { Reg.E = Reg.C; Reg.PC += 1; return 4; }
    private static int Op_LD_E_D() { Reg.E = Reg.D; Reg.PC += 1; return 4; }
    private static int Op_LD_E_E() { Reg.PC += 1; return 4; }
    private static int Op_LD_E_H() { Reg.E = Reg.H; Reg.PC += 1; return 4; }
    private static int Op_LD_E_L() { Reg.E = Reg.L; Reg.PC += 1; return 4; }
    private int Op_LD_E_n() { Reg.E = ImmediateValue(); Reg.PC += 2; return 7; }

    private static int Op_LD_H_A() { Reg.H = Reg.A; Reg.PC += 1; return 4; }
    private static int Op_LD_H_B() { Reg.H = Reg.B; Reg.PC += 1; return 4; }
    private static int Op_LD_H_C() { Reg.H = Reg.C; Reg.PC += 1; return 4; }
    private static int Op_LD_H_D() { Reg.H = Reg.D; Reg.PC += 1; return 4; }
    private static int Op_LD_H_E() { Reg.H = Reg.E; Reg.PC += 1; return 4; }
    private static int Op_LD_H_H() { Reg.PC += 1; return 4; }
    private static int Op_LD_H_L() { Reg.H = Reg.L; Reg.PC += 1; return 4; }
    private int Op_LD_H_n() { Reg.H = ImmediateValue(); Reg.PC += 2; return 7; }

    private static int Op_LD_L_A() { Reg.L = Reg.A; Reg.PC += 1; return 4; }
    private static int Op_LD_L_B() { Reg.L = Reg.B; Reg.PC += 1; return 4; }
    private static int Op_LD_L_C() { Reg.L = Reg.C; Reg.PC += 1; return 4; }
    private static int Op_LD_L_D() { Reg.L = Reg.D; Reg.PC += 1; return 4; }
    private static int Op_LD_L_E() { Reg.L = Reg.E; Reg.PC += 1; return 4; }
    private static int Op_LD_L_H() { Reg.L = Reg.H; Reg.PC += 1; return 4; }
    private static int Op_LD_L_L() { Reg.PC += 1; return 4; }
    private int Op_LD_L_n() { Reg.L = ImmediateValue(); Reg.PC += 2; return 7; }

    private static int Op_LD_I_A() { Reg.I = Reg.A; Reg.PC += 1; return 9; }    

    private int Op_LD_ptrHL_A() { machine.WriteByte(Reg.HL, Reg.A); Reg.PC += 1; return 7; }
    private int Op_LD_ptrHL_B() { machine.WriteByte(Reg.HL, Reg.B); Reg.PC += 1; return 7; }
    private int Op_LD_ptrHL_C() { machine.WriteByte(Reg.HL, Reg.C); Reg.PC += 1; return 7; }
    private int Op_LD_ptrHL_D() { machine.WriteByte(Reg.HL, Reg.D); Reg.PC += 1; return 7; }
    private int Op_LD_ptrHL_E() { machine.WriteByte(Reg.HL, Reg.E); Reg.PC += 1; return 7; }    
    private int Op_LD_ptrHL_H() { machine.WriteByte(Reg.HL, Reg.H); Reg.PC += 1; return 7; }
    private int Op_LD_ptrHL_L() { machine.WriteByte(Reg.HL, Reg.L); Reg.PC += 1; return 7; }

    private int Op_LD_A_ptrHL() { Reg.A = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }
    private int Op_LD_B_ptrHL() { Reg.B = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }
    private int Op_LD_C_ptrHL() { Reg.C = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }
    private int Op_LD_D_ptrHL() { Reg.D = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }
    private int Op_LD_E_ptrHL() { Reg.E = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }
    private int Op_LD_H_ptrHL() { Reg.H = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }
    private int Op_LD_L_ptrHL() { Reg.L = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }

    private int Op_AND_A() => AND(Reg.A, 4);
    private int Op_AND_B() => AND(Reg.B, 4);
    private int Op_AND_C() => AND(Reg.C, 4);
    private int Op_AND_D() => AND(Reg.D, 4);
    private int Op_AND_E() => AND(Reg.E, 4);
    private int Op_AND_H() => AND(Reg.H, 4);
    private int Op_AND_L() => AND(Reg.L, 4);
    private int Op_AND_ptrHL() => AND(machine.ReadByte(Reg.HL), 7);
    private int Op_AND_n()
    {
        byte value = ImmediateValue();
        Reg.PC += 1;
        return AND(value, 7);
    }

    private int Op_OR_A() => OR(Reg.A, 4);
    private int Op_OR_B() => OR(Reg.B, 4);
    private int Op_OR_C() => OR(Reg.C, 4);
    private int Op_OR_D() => OR(Reg.D, 4);
    private int Op_OR_E() => OR(Reg.E, 4);
    private int Op_OR_H() => OR(Reg.H, 4);
    private int Op_OR_L() => OR(Reg.L, 4);
    private int Op_OR_ptrHL() => OR(machine.ReadByte(Reg.HL), 7);

    private int Op_BIT_7_ptrHL()
    {
        byte value = machine.ReadByte(Reg.HL);
        return BIT(7, value, isMemory: true);
    }
    
    private int Op_XOR_A_A()
    {
        // xor the accumulator with itself
        Reg.A = (byte)(Reg.A ^ Reg.A);
        Reg.PC += 1;

        SetSZFlags(Reg.A);
        ClearFlag(Flags.C | Flags.H | Flags.N);
        SetParity(Reg.A);

        return 4;
    }
    private int Op_XOR_A_n()
    {
        // xor the accumulator with itself
        Reg.A = (byte)(Reg.A ^ ImmediateValue());
        Reg.PC += 2;

        SetSZFlags(Reg.A);
        ClearFlag(Flags.C | Flags.H | Flags.N);
        SetParity(Reg.A);

        return 7;
    }
    
    private static int Op_INC_BC() { Reg.BC++; Reg.PC += 1; return 6; }
    private static int Op_INC_DE() { Reg.DE++; Reg.PC += 1; return 6; }
    private static int Op_INC_HL() { Reg.HL++; Reg.PC += 1; return 6; }
    private static int Op_INC_SP() { Reg.SP++; Reg.PC += 1; return 6; }

    private int Op_POP_BC() { Reg.BC = PopWord(); Reg.PC += 1; return 10; }
    private int Op_POP_DE() { Reg.DE = PopWord(); Reg.PC += 1; return 10; }
    private int Op_POP_HL() { Reg.HL = PopWord(); Reg.PC += 1; return 10; }
    private int Op_POP_AF() { Reg.AF = PopWord(); Reg.F &= 0xD7; Reg.PC += 1; return 10; }
    private int Op_POP_IX() { Reg.IX = PopWord(); Reg.PC += 1; return 14; }
    private int Op_POP_IY() { Reg.IY = PopWord(); Reg.PC += 1; return 14; }

    private int Op_PUSH_BC() { PushWord(Reg.BC); Reg.PC += 1; return 11; }
    private int Op_PUSH_DE() { PushWord(Reg.DE); Reg.PC += 1; return 11; }
    private int Op_PUSH_HL() { PushWord(Reg.HL); Reg.PC += 1; return 11; }
    private int Op_PUSH_AF() { PushWord(Reg.AF); Reg.PC += 1; return 11; }

    private int Op_PUSH_IX() { PushWord(Reg.IX); Reg.PC += 2; return 15; }

    private int Op_DI() { _iff1 = false; Reg.PC += 1; return 4; }
    private int Op_EI() { EI_Pending = true; Reg.PC += 1; return 4; }

    private int Op_IM_0() { _interruptMode = 0; Reg.PC += 1; return 8; }
    private int Op_IM_1() { _interruptMode = 1; Reg.PC += 1; return 8; }
    private int Op_IM_2() { _interruptMode = 2; Reg.PC += 1; return 8; }
    
    private int JR_Cond(Func<bool> condition)
    {
        sbyte offset = (sbyte)ImmediateValue();
        ushort nextPC = (ushort)(Reg.PC + 2);

        if (condition())
        {
            Reg.PC = (ushort)(nextPC + offset);
            return 12;
        }
        else
        {
            Reg.PC = nextPC;
            return 7;
        }
    }
    private int Op_JR_e()
    {
        sbyte offset = (sbyte)ImmediateValue();
        ushort nextPC = (ushort)(Reg.PC + 2);

        Reg.PC = (ushort)(nextPC + offset);
        return 12;
    }
    private int Op_JR_Z_e() => JR_Cond(() => GetFlag(Flags.Z));
    private int Op_JR_NZ_e() => JR_Cond(() => !GetFlag(Flags.Z));
    private int Op_JR_C_e() => JR_Cond(() => GetFlag(Flags.C));
    private int Op_JR_NC_e() => JR_Cond(() => !GetFlag(Flags.C));
    
    private static void SetIncFlags(byte target)
    {
        if ((target & 0x0F) == 0x0F)  // if lower nibble is F, half-carry will occur when adding 1
            SetFlag(Flags.H);
        else
            ClearFlag(Flags.H);
        SetSZFlags((byte)(target+1)); // adding one because this needs to be checked AFTER the addition
        ClearFlag(Flags.N);
        CheckINCOverflow(target);
    }
    private static int Op_INC_A() { SetIncFlags(Reg.A); Reg.A += 1; Reg.PC += 1; return 4; }
    private static int Op_INC_B() { SetIncFlags(Reg.B); Reg.B += 1; Reg.PC += 1; return 4; }
    private static int Op_INC_C() { SetIncFlags(Reg.C); Reg.C += 1; Reg.PC += 1; return 4; }
    private static int Op_INC_D() { SetIncFlags(Reg.D); Reg.D += 1; Reg.PC += 1; return 4; }
    private static int Op_INC_E() { SetIncFlags(Reg.E); Reg.E += 1; Reg.PC += 1; return 4; }
    private static int Op_INC_H() { SetIncFlags(Reg.H); Reg.H += 1; Reg.PC += 1; return 4; }
    private static int Op_INC_L() { SetIncFlags(Reg.L); Reg.L += 1; Reg.PC += 1; return 4; }
    private int Op_INC_ptrHL()
    {
        byte target = machine.ReadByte(Reg.HL);
        if ((target & 0x0F) == 0x0F)  // if lower nibble is F, half-carry will occur when adding 1
            SetFlag(Flags.H);
        else
            ClearFlag(Flags.H);

        target += 1;

        SetSZFlags(target);
        ClearFlag(Flags.N);
        CheckINCOverflow(target);
        Reg.PC += 1;

        machine.WriteByte(Reg.HL, target);
        return 11;
    }
    
    private static void SetDecFlags(byte target)
    {
        byte result = (byte)(target - 1);

        if ((target & 0x0F) == 0x00)
            SetFlag(Flags.H);
        else
            ClearFlag(Flags.H);

        SetSZFlags(result);

        SetFlag(Flags.N);

        CheckDECOverflow(target);

        WriteFlag(Flags.F5, (result & 0x20) != 0);
        WriteFlag(Flags.F3, (result & 0x08) != 0);
    }
    private static int Op_DEC_A() { SetDecFlags(Reg.A); Reg.A--; Reg.PC += 1; return 4; }
    private static int Op_DEC_B() { SetDecFlags(Reg.B); Reg.B--; Reg.PC += 1; return 4; }
    private static int Op_DEC_C() { SetDecFlags(Reg.C); Reg.C--; Reg.PC += 1; return 4; }
    private static int Op_DEC_D() { SetDecFlags(Reg.D); Reg.D--; Reg.PC += 1; return 4; }
    private static int Op_DEC_E() { SetDecFlags(Reg.E); Reg.E--; Reg.PC += 1; return 4; }
    private static int Op_DEC_H() { SetDecFlags(Reg.H); Reg.H--; Reg.PC += 1; return 4; }
    private static int Op_DEC_L() { SetDecFlags(Reg.L); Reg.L--; Reg.PC += 1; return 4; }

    private static int Op_DEC_SP() { Reg.SP--; Reg.PC += 1; return 6; }

    private int Op_DEC_ptrHL()
    {
        byte value = machine.ReadByte(Reg.HL);
        WriteFlag(Flags.H, (value & 0x0F) == 0);       
        
        value--;
        machine.WriteByte(Reg.HL, value);

        CheckDECOverflow(value);
        SetFlag(Flags.N);
        SetSZFlags(value);

        Reg.PC += 1;
        return 11;
    }
    
    private int Op_JP_nn()
    {
        // jump to address nn
        ushort address = ImmediateExtendedValue(machine, (ushort)(Reg.PC + 1));
        Reg.PC = address;
        return 10;
    }
    private int Op_JP_NC_nn()
    {
        if(!GetFlag(Flags.C))
        {
            // jump to address nn
            ushort address = ImmediateExtendedValue(machine, (ushort)(Reg.PC + 1));
            Reg.PC = address;
            return 10;            
        }

        Reg.PC += 3;
        return 10;
    }
    private int Op_JP_NZ_nn()
    {
        if(!GetFlag(Flags.Z))
        {
            // jump to address nn
            ushort address = ImmediateExtendedValue(machine, (ushort)(Reg.PC + 1));
            Reg.PC = address;
            return 10;            
        }

        Reg.PC += 3;
        return 10;
    }
    private int Op_JP_M_nn()
    {
        if(GetFlag(Flags.S))
        {
            Reg.PC = ImmediateExtendedValue(machine, (ushort)(Reg.PC + 1));
        }
        else
        {
            Reg.PC += 3;
        }
        return 10;
    }
    private int Op_JP_C_nn()
    {
        if(GetFlag(Flags.C))
        {
            Reg.PC = ImmediateExtendedValue(machine, (ushort)(Reg.PC + 1));
        }
        else
        {
            Reg.PC += 3;            
        }
        return 10;
    }
    private int Op_JP_Z_nn()
    {
        if(GetFlag(Flags.Z))
        {
            Reg.PC = ImmediateExtendedValue(machine, (ushort)(Reg.PC + 1));
        }
        else
        {
            Reg.PC += 3;            
        }
        return 10;
    }
    private static int Op_JP_ptrHL()
    {
        // jump to address at location stored HL
        Reg.PC = Reg.HL;
        return 4;
    }
    
    private int Op_LD_A_ptrDE()
    {
        Reg.A = machine.ReadByte(Reg.DE);
        Reg.PC += 1;
        return 7;
    }
    private int Op_LD_A_ptrNN()
    {
        ushort addr = ImmediateExtendedValue(machine, (ushort)(Reg.PC + 1));
        Reg.A = machine.ReadByte(addr);
        Reg.PC += 3;
        return 13;
    }
    private int Op_LD_HL_nn()
    {
        // load nn into HL
        Reg.HL = ImmediateExtendedValue(machine, (ushort)(Reg.PC + 1));
        Reg.PC += 3;
        return 10;
    }    
    private int Op_LD_SP_nn()
    {
        // load nn into SP
        Reg.SP = ImmediateExtendedValue(machine, (ushort)(Reg.PC + 1));
        Reg.PC += 3;
        return 10;
    }
    private int Op_LD_BC_nn()
    {
        // load nn into BC
        Reg.BC = ImmediateExtendedValue(machine, (ushort)(Reg.PC + 1));
        Reg.PC += 3;
        return 10;
    }
    private int Op_LD_ptrHL_n()
    {
        machine.WriteByte(Reg.HL, ImmediateValue());
        Reg.PC += 2;
        return 10;
    }
    private int Op_LD_DE_nn()
    {
        Reg.DE = ImmediateExtendedValue(machine, (ushort)(Reg.PC + 1));
        Reg.PC += 3;
        return 10;
    }
    private int Op_LD_ptrnn_A()
    {
        // store value of Accumulator in memory at the location nn
        ushort address = ImmediateExtendedValue(machine, (ushort)(Reg.PC + 1));
        machine.WriteByte(address, Reg.A);
        Reg.PC += 3;
        return 13;
    }
    private int Op_LD_ptrnn_HL()
    {
        // store value of HL in memory at the location nn
        ushort address = ImmediateExtendedValue(machine, (ushort)(Reg.PC + 1));
        machine.WriteByte(address, Reg.L);
        machine.WriteByte((ushort)(address + 1), Reg.H);
        Reg.PC += 3;
        return 16;
    }
    private int Op_LD_IY_nn()
    {
        Reg.IY = ImmediateExtendedValue(machine, (ushort)(Reg.PC + 1));
        Reg.PC += 3;
        return 14;
    }
    private int Op_LD_HL_ptrnn()
    {
        Reg.HL = machine.ReadByte(ImmediateExtendedValue(machine, (ushort)(Reg.PC + 1)));
        Reg.PC += 3;
        return 16;
    }
    private int Op_LD_L_ptrIYd()
    {
        sbyte d = (sbyte)machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        Reg.L = machine.ReadByte(addr);
        
        Reg.PC += 2;
        return 19;
    }
    private int Op_LD_DE_A()
    {
        // stores the value of A into RAM at address DE
        machine.WriteByte(Reg.DE, Reg.A);
        Reg.PC += 1;
        return 7;
    }
    
    private int Op_DJNZ_e()
    {
        Reg.B--;
        if (Reg.B != 0x00)
        {
            sbyte offset = ReadSignedOffset();
            Reg.PC = (ushort)(Reg.PC + 2 + offset);
            return 13;
        }
        else
        {
            Reg.PC += 2;
            return 8;
        }
    }
    private int Op_OUT_ptrn_A()
    {
        byte n = ImmediateValue();
        ushort port = LEWord(Reg.I, n);
        WritePort(port, Reg.A);
        Reg.PC += 2;
        return 11;
    }
    private int Op_HALT()
    {
        _halted = true;
        return 4;
    }
    private static int Op_DEC_BC()
    {
        Reg.BC--;
        Reg.PC += 1;
        return 6;
    }
    private static int Op_CCF()
    {
        WriteFlag(Flags.H, GetFlag(Flags.C));
        ClearFlag(Flags.N);
        ToggleFlag(Flags.C);

        Reg.PC += 1;
        return 4;
    }
    
    private int Op_RET()
    {
        Reg.PC = PopWord();
        return 10;
    }
    private int Op_RET_NZ()
    {
        if(!GetFlag(Flags.Z))
        {            
            Reg.PC = PopWord();
            return 11;
        }       
        Reg.PC += 1; 
        return 5;
    }
    private int Op_RET_Z()
    {
        if(GetFlag(Flags.Z))
        {            
            Reg.PC = PopWord();
            return 11;
        }       
        Reg.PC += 1; 
        return 5;
    }
    private int Op_RET_M()
    {
        if(GetFlag(Flags.S))
        {            
            Reg.PC = PopWord();
            return 11;
        }       
        Reg.PC += 1; 
        return 5;
    }
    private int Op_RET_P()
    {
        if(!GetFlag(Flags.S))
        {            
            Reg.PC = PopWord();
            return 11;
        }       
        Reg.PC += 1; 
        return 5;
    }
    private int Op_RET_NC()
    {
        if(!GetFlag(Flags.C))
        {            
            Reg.PC = PopWord();
            return 11;
        }       
        Reg.PC += 1; 
        return 5;
    }
    
    private int Op_RST(ushort vector)
    {
        // vector can be 00, 08, 10, 18, 20, 28, 30, 38
        PushWord((ushort)(Reg.PC + 1));
        Reg.PC = vector;
        return 11;
    }
    private int Op_CALL_nn()
    {
        PushWord((ushort)(Reg.PC + 3));
        Reg.PC = ImmediateExtendedValue(machine, (ushort)(Reg.PC + 1));
        return 17;
    }
    private int Op_CALL_M_nn()
    {
        if(GetFlag(Flags.S))
        {
            PushWord((ushort)(Reg.PC + 3));
            Reg.PC = ImmediateExtendedValue(machine, (ushort)(Reg.PC + 1));
            return 17;
        }
        else
        {
            Reg.PC += 3;
            return 10;
        }
    }
    private static int Op_EX_DE_HL()
    {
        (Reg.DE, Reg.HL) = (Reg.HL, Reg.DE); // tuples from .NET 7 allow swapping values without a temp var

        Reg.PC += 1;
        return 4;
    }
    private static int Op_RLCA()
    {
        if((Reg.A & 0x80) != 0)
        {
            Reg.A = (byte)((Reg.A << 1) | 1);
            SetFlag(Flags.C);
        }
        else
        {
            Reg.A = (byte)(Reg.A << 1);
            ClearFlag(Flags.C);
        }        
        
        ClearFlag(Flags.N | Flags.H);
        Reg.PC += 1;
        return 4;
    }
    private static int Op_RRCA()
    {
        if((Reg.A & 0x01) != 0)
        {
            Reg.A = (byte)((Reg.A >> 1) | 0x80);
            SetFlag(Flags.C);
        }
        else
        {
            Reg.A = (byte)(Reg.A >> 1);
            ClearFlag(Flags.C);
        }        
        
        ClearFlag(Flags.N | Flags.H);
        Reg.PC += 1;
        return 4;
    }

    private static int Op_EXX()
    {
        Reg.BC = Reg.BC2;
        Reg.DE = Reg.DE2;
        Reg.HL = Reg.HL2;
        Reg.PC += 1;
        return 4;
    }
}