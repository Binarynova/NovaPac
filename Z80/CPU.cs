using System;
using System.IO;
using Reg = Registers;

public partial class Z80(Machine machine)
{
    private readonly bool[] _parity = new bool[256];
    private bool _iff1;
    private bool _halted;
    int traceCount = 0;
    int traceLimit = 2700000;

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

        if (traceCount > 1870700)
        {
            if(Reg.PC == 0x3055)
            {
                Console.WriteLine($"E={Reg.E:X2} C={Reg.C:X2} HL={Reg.HL:X4}");
            }
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
        _mainOpcodes[0x02] = Op_LD_ptrBC_A;
        _mainOpcodes[0x03] = Op_INC_BC;
        _mainOpcodes[0x04] = Op_INC_B;
        _mainOpcodes[0x05] = () => Op_DEC_Reg(ref Reg.B);
        _mainOpcodes[0x06] = Op_LD_B_n;
        _mainOpcodes[0x07] = Op_RLCA;
        _mainOpcodes[0x08] = Op_EX_AF_AF2;
        _mainOpcodes[0x09] = Op_ADD_HL_BC;
        _mainOpcodes[0x0A] = Op_LD_A_ptrBC;
        _mainOpcodes[0x0B] = Op_DEC_BC;
        _mainOpcodes[0x0C] = Op_INC_C;
        _mainOpcodes[0x0D] = () => Op_DEC_Reg(ref Reg.C);
        _mainOpcodes[0x0E] = Op_LD_C_n;
        _mainOpcodes[0x0F] = Op_RRCA;

        _mainOpcodes[0x10] = Op_DJNZ_e;
        _mainOpcodes[0x11] = Op_LD_DE_nn;
        _mainOpcodes[0x12] = Op_LD_ptrDE_A;
        _mainOpcodes[0x13] = Op_INC_DE;
        _mainOpcodes[0x14] = Op_INC_D;
        _mainOpcodes[0x15] = () => Op_DEC_Reg(ref Reg.D);
        _mainOpcodes[0x16] = Op_LD_D_n;
        _mainOpcodes[0x17] = Op_RLA;
        _mainOpcodes[0x18] = Op_JR_e;
        _mainOpcodes[0x19] = Op_ADD_HL_DE;
        _mainOpcodes[0x1A] = Op_LD_A_ptrDE;
        _mainOpcodes[0x1B] = Op_DEC_DE;
        _mainOpcodes[0x1C] = Op_INC_E;
        _mainOpcodes[0x1D] = () => Op_DEC_Reg(ref Reg.E);
        _mainOpcodes[0x1E] = Op_LD_E_n;
        _mainOpcodes[0x1F] = Op_RRA;

        _mainOpcodes[0x20] = Op_JR_NZ_e;
        _mainOpcodes[0x21] = Op_LD_HL_nn;
        _mainOpcodes[0x22] = Op_LD_ptrnn_HL;
        _mainOpcodes[0x23] = Op_INC_HL;
        _mainOpcodes[0x24] = Op_INC_H;
        _mainOpcodes[0x25] = () => Op_DEC_Reg(ref Reg.H);
        _mainOpcodes[0x26] = Op_LD_H_n;
        _mainOpcodes[0x27] = Op_DAA;
        _mainOpcodes[0x28] = Op_JR_Z_e;
        _mainOpcodes[0x29] = Op_ADD_HL_HL;
        _mainOpcodes[0x2A] = Op_LD_HL_ptrnn;
        _mainOpcodes[0x2B] = Op_DEC_HL;
        _mainOpcodes[0x2C] = Op_INC_L;
        _mainOpcodes[0x2D] = () => Op_DEC_Reg(ref Reg.L);
        _mainOpcodes[0x2E] = Op_LD_L_n;
        _mainOpcodes[0x2F] = Op_CPL;

        _mainOpcodes[0x30] = Op_JR_NC_e;
        _mainOpcodes[0x31] = Op_LD_SP_nn;
        _mainOpcodes[0x32] = Op_LD_ptrnn_A;
        _mainOpcodes[0x33] = Op_INC_SP;
        _mainOpcodes[0x34] = Op_INC_ptrHL;
        _mainOpcodes[0x35] = Op_DEC_ptrHL;
        _mainOpcodes[0x36] = Op_LD_ptrHL_n;
        _mainOpcodes[0x37] = Op_SCF;
        _mainOpcodes[0x38] = Op_JR_C_e;
        _mainOpcodes[0x39] = Op_ADD_HL_SP;
        _mainOpcodes[0x3A] = Op_LD_A_ptrNN;
        _mainOpcodes[0x3B] = Op_DEC_SP;
        _mainOpcodes[0x3C] = Op_INC_A;
        _mainOpcodes[0x3D] = () => Op_DEC_Reg(ref Reg.A);
        _mainOpcodes[0x3E] = Op_LD_A_n;
        _mainOpcodes[0x3F] = Op_CCF;

        _mainOpcodes[0x40] = () => Op_LD_B(Reg.B);
        _mainOpcodes[0x41] = () => Op_LD_B(Reg.C);
        _mainOpcodes[0x42] = () => Op_LD_B(Reg.D);
        _mainOpcodes[0x43] = () => Op_LD_B(Reg.E);
        _mainOpcodes[0x44] = () => Op_LD_B(Reg.H);
        _mainOpcodes[0x45] = () => Op_LD_B(Reg.L);
        _mainOpcodes[0x46] = Op_LD_B_ptrHL;
        _mainOpcodes[0x47] = () => Op_LD_B(Reg.A);
        _mainOpcodes[0x48] = () => Op_LD_C(Reg.B);
        _mainOpcodes[0x49] = () => Op_LD_C(Reg.C);
        _mainOpcodes[0x4A] = () => Op_LD_C(Reg.D);
        _mainOpcodes[0x4B] = () => Op_LD_C(Reg.E);
        _mainOpcodes[0x4C] = () => Op_LD_C(Reg.H);
        _mainOpcodes[0x4D] = () => Op_LD_C(Reg.L);
        _mainOpcodes[0x4E] = Op_LD_C_ptrHL;
        _mainOpcodes[0x4F] = () => Op_LD_C(Reg.A);

        _mainOpcodes[0x50] = () => Op_LD_D(Reg.B);
        _mainOpcodes[0x51] = () => Op_LD_D(Reg.C);
        _mainOpcodes[0x52] = () => Op_LD_D(Reg.D);
        _mainOpcodes[0x53] = () => Op_LD_D(Reg.E);
        _mainOpcodes[0x54] = () => Op_LD_D(Reg.H);
        _mainOpcodes[0x55] = () => Op_LD_D(Reg.L);
        _mainOpcodes[0x56] = Op_LD_D_ptrHL;
        _mainOpcodes[0x57] = () => Op_LD_D(Reg.A);
        _mainOpcodes[0x58] = () => Op_LD_E(Reg.B);
        _mainOpcodes[0x59] = () => Op_LD_E(Reg.C);
        _mainOpcodes[0x5A] = () => Op_LD_E(Reg.D);
        _mainOpcodes[0x5B] = () => Op_LD_E(Reg.E);
        _mainOpcodes[0x5C] = () => Op_LD_E(Reg.H);
        _mainOpcodes[0x5D] = () => Op_LD_E(Reg.L);
        _mainOpcodes[0x5E] = Op_LD_E_ptrHL;
        _mainOpcodes[0x5F] = () => Op_LD_E(Reg.A);

        _mainOpcodes[0x60] = () => Op_LD_H(Reg.B);
        _mainOpcodes[0x61] = () => Op_LD_H(Reg.C);
        _mainOpcodes[0x62] = () => Op_LD_H(Reg.D);
        _mainOpcodes[0x63] = () => Op_LD_H(Reg.E);
        _mainOpcodes[0x64] = () => Op_LD_H(Reg.H);
        _mainOpcodes[0x65] = () => Op_LD_H(Reg.L);
        _mainOpcodes[0x66] = Op_LD_H_ptrHL;
        _mainOpcodes[0x67] = () => Op_LD_H(Reg.A);
        _mainOpcodes[0x68] = () => Op_LD_L(Reg.B);
        _mainOpcodes[0x69] = () => Op_LD_L(Reg.C);
        _mainOpcodes[0x6A] = () => Op_LD_L(Reg.D);
        _mainOpcodes[0x6B] = () => Op_LD_L(Reg.E);
        _mainOpcodes[0x6C] = () => Op_LD_L(Reg.H);
        _mainOpcodes[0x6D] = () => Op_LD_L(Reg.L);
        _mainOpcodes[0x6E] = Op_LD_L_ptrHL;
        _mainOpcodes[0x6F] = () => Op_LD_L(Reg.A);

        _mainOpcodes[0x70] = () => Op_LD_ptrHL(Reg.B);
        _mainOpcodes[0x71] = () => Op_LD_ptrHL(Reg.C);
        _mainOpcodes[0x72] = () => Op_LD_ptrHL(Reg.D);
        _mainOpcodes[0x73] = () => Op_LD_ptrHL(Reg.E);
        _mainOpcodes[0x74] = () => Op_LD_ptrHL(Reg.H);
        _mainOpcodes[0x75] = () => Op_LD_ptrHL(Reg.L);
        _mainOpcodes[0x76] = Op_HALT;
        _mainOpcodes[0x77] = () => Op_LD_ptrHL(Reg.A);
        _mainOpcodes[0x78] = () => Op_LD_A(Reg.B);
        _mainOpcodes[0x79] = () => Op_LD_A(Reg.C);
        _mainOpcodes[0x7A] = () => Op_LD_A(Reg.D);
        _mainOpcodes[0x7B] = () => Op_LD_A(Reg.E);
        _mainOpcodes[0x7C] = () => Op_LD_A(Reg.H);
        _mainOpcodes[0x7D] = () => Op_LD_A(Reg.L);
        _mainOpcodes[0x7E] = Op_LD_A_ptrHL;
        _mainOpcodes[0x7F] = () => Op_LD_A(Reg.A);

        _mainOpcodes[0x80] = () => Op_ADD_A(Reg.B);
        _mainOpcodes[0x81] = () => Op_ADD_A(Reg.C);
        _mainOpcodes[0x82] = () => Op_ADD_A(Reg.D);
        _mainOpcodes[0x83] = () => Op_ADD_A(Reg.E);
        _mainOpcodes[0x84] = () => Op_ADD_A(Reg.H);
        _mainOpcodes[0x85] = () => Op_ADD_A(Reg.L);
        _mainOpcodes[0x86] = Op_ADD_A_ptrHL;
        _mainOpcodes[0x87] = () => Op_ADD_A(Reg.A);
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
        _mainOpcodes[0xA8] = () => Op_XOR_A(Reg.B);
        _mainOpcodes[0xA9] = () => Op_XOR_A(Reg.C);
        _mainOpcodes[0xAA] = () => Op_XOR_A(Reg.D);
        _mainOpcodes[0xAB] = () => Op_XOR_A(Reg.E);
        _mainOpcodes[0xAC] = () => Op_XOR_A(Reg.H);
        _mainOpcodes[0xAD] = () => Op_XOR_A(Reg.L);
        _mainOpcodes[0xAD] = Op_XOR_A_ptrHL;
        _mainOpcodes[0xAF] = () => Op_XOR_A(Reg.A);

        _mainOpcodes[0xB0] = Op_OR_B;
        _mainOpcodes[0xB1] = Op_OR_C;
        _mainOpcodes[0xB2] = Op_OR_D;
        _mainOpcodes[0xB3] = Op_OR_E;
        _mainOpcodes[0xB4] = Op_OR_H;
        _mainOpcodes[0xB5] = Op_OR_L;
        _mainOpcodes[0xB6] = Op_OR_ptrHL;
        _mainOpcodes[0xB7] = Op_OR_A;
        _mainOpcodes[0xB8] = () => Op_CP(Reg.B);
        _mainOpcodes[0xB9] = () => Op_CP(Reg.C);
        _mainOpcodes[0xBA] = () => Op_CP(Reg.D);
        _mainOpcodes[0xBB] = () => Op_CP(Reg.E);
        _mainOpcodes[0xBC] = () => Op_CP(Reg.H);
        _mainOpcodes[0xBD] = () => Op_CP(Reg.L);
        _mainOpcodes[0xBE] = Op_CP_ptrHL;
        _mainOpcodes[0xBF] = () => Op_CP(Reg.A);

        _mainOpcodes[0xC0] = Op_RET_NZ;
        _mainOpcodes[0xC1] = Op_POP_BC;
        _mainOpcodes[0xC2] = Op_JP_NZ_nn;
        _mainOpcodes[0xC3] = Op_JP_nn;
        _mainOpcodes[0xC4] = null;
        _mainOpcodes[0xC5] = Op_PUSH_BC;
        _mainOpcodes[0xC6] = Op_ADD_A_n;
        _mainOpcodes[0xC7] = () => Op_RST(0x00);
        _mainOpcodes[0xC8] = Op_RET_Z;
        _mainOpcodes[0xC9] = Op_RET;
        _mainOpcodes[0xCA] = Op_JP_Z_nn;
        _mainOpcodes[0xCB] = Op_CB;
        _mainOpcodes[0xCC] = null;
        _mainOpcodes[0xCD] = Op_CALL_nn;
        _mainOpcodes[0xCE] = null;
        _mainOpcodes[0xCF] = () => Op_RST(0x08);

        _mainOpcodes[0xD0] = Op_RET_NC;
        _mainOpcodes[0xD1] = Op_POP_DE;
        _mainOpcodes[0xD2] = Op_JP_NC_nn;
        _mainOpcodes[0xD3] = Op_OUT_ptrn_A;
        _mainOpcodes[0xD4] = null;
        _mainOpcodes[0xD5] = Op_PUSH_DE;
        _mainOpcodes[0xD6] = Op_SUB_n;
        _mainOpcodes[0xD7] = () => Op_RST(0x10);
        _mainOpcodes[0xD8] = null;
        _mainOpcodes[0xD9] = Op_EXX;
        _mainOpcodes[0xDA] = Op_JP_C_nn;
        _mainOpcodes[0xDB] = null;
        _mainOpcodes[0xDC] = null;
        _mainOpcodes[0xDD] = Op_DD;
        _mainOpcodes[0xDE] = null;
        _mainOpcodes[0xDF] = () => Op_RST(0x18);

        _mainOpcodes[0xE0] = null;
        _mainOpcodes[0xE1] = Op_POP_HL;
        _mainOpcodes[0xE2] = null;
        _mainOpcodes[0xE3] = null;
        _mainOpcodes[0xE4] = null;
        _mainOpcodes[0xE5] = Op_PUSH_HL;
        _mainOpcodes[0xE6] = Op_AND_n;
        _mainOpcodes[0xE7] = () => Op_RST(0x20);
        _mainOpcodes[0xE8] = null;
        _mainOpcodes[0xE9] = Op_JP_ptrHL;
        _mainOpcodes[0xEA] = null;
        _mainOpcodes[0xEB] = Op_EX_DE_HL;
        _mainOpcodes[0xEC] = null;
        _mainOpcodes[0xED] = Op_ED;
        _mainOpcodes[0xEE] = Op_XOR_A_n;
        _mainOpcodes[0xEF] = () => Op_RST(0x28);

        _mainOpcodes[0xF0] = Op_RET_P;
        _mainOpcodes[0xF1] = Op_POP_AF;
        _mainOpcodes[0xF2] = null;
        _mainOpcodes[0xF3] = Op_DI;
        _mainOpcodes[0xF4] = null;
        _mainOpcodes[0xF5] = Op_PUSH_AF;
        _mainOpcodes[0xF6] = null;
        _mainOpcodes[0xF7] = () => Op_RST(0x30);
        _mainOpcodes[0xF8] = Op_RET_M;
        _mainOpcodes[0xF9] = null;
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
        _fdOpcodes[0xE5] = Op_PUSH_IY;

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
        WriteFlag(Flags.P, _parity[value]);
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
        if (value) Reg.F |= (byte)f;
        else Reg.F &= (byte)~f;
    }

    private static void SetSZFlags(byte value)
    {
        WriteFlag(Flags.Z, value == 0);
        WriteFlag(Flags.S, (value & 0x80) != 0);
        WriteFlag(Flags.F5, (value & 0x20) != 0);
        WriteFlag(Flags.F3, (value & 0x08) != 0);
    }
    
    byte ReadImmediateByte()
    {
        return machine.ReadByte((ushort)(Reg.PC + 1));
    }
    ushort ReadImmediateWord()
    {
        return (ushort)(machine.ReadByte((ushort)(Reg.PC + 1)) | 
                       (machine.ReadByte((ushort)(Reg.PC + 2)) << 8));
    }

    private byte PeekNextByte() => ReadImmediateByte();
    private sbyte ReadSignedOffset() => (sbyte)ReadImmediateByte(); // needs to be sbyte to properly handled sign
    
    private int Op_UNK()
    {
        throw new NotImplementedException(
            $"Unhandled opcode {machine.ReadByte(Reg.PC):X2} at {Reg.PC:X4}"
        );
    }
}