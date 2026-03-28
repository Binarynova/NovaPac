using System;
using System.Collections.Generic;
using System.IO;
using Reg = Registers;

public partial class Z80
{
    private IMemoryProvider _machine;
    private readonly bool[] _parity = new bool[256];
    private bool _iff1;
    private bool _iff2;
    private bool _halted;

    public delegate int OpcodeHandler();
    public readonly OpcodeHandler[] _mainOpcodes = new OpcodeHandler[256];
    public readonly OpcodeHandler[] _ddOpcodes = new OpcodeHandler[256];
    public readonly OpcodeHandler[] _edOpcodes = new OpcodeHandler[256];
    public readonly OpcodeHandler[] _fdOpcodes = new OpcodeHandler[256];
    public readonly OpcodeHandler[] _cbOpcodes = new OpcodeHandler[256];

    // Interrupts
    private int _interruptMode;
    public bool InterruptPending;
    public bool EI_Pending;
    public bool EI_EnableAfterInstruction;
    public byte interruptVectorLow;

    [Flags]
    private enum Flags : byte
    {
        C = 1 << 0, N = 1 << 1, P = 1 << 2, F3 = 1 << 3, H = 1 << 4, F5 = 1 << 5, Z = 1 << 6, S = 1 << 7
    }
    
    public Z80(IMemoryProvider machine)
    {
        _machine = machine;

        BuildOpcodeTable();
        InitParity();
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
        _iff2 = _iff1;
        _iff1 = false;

        switch(_interruptMode)
        {
            case 0:
                Reg.PC = 0x38;
                break;
            case 1:
                Reg.PC = 0x38;
                break;
            case 2:
                ushort vectorAddress = (ushort)((Reg.I << 8) | interruptVectorLow);
                byte low = _machine.ReadByte(vectorAddress);
                byte high = _machine.ReadByte((ushort)(vectorAddress + 1));
                Reg.PC = (ushort)((high << 8) | low);
                break;
        }
    }
    
    public int Step(bool SteppingThrough = false)
    {
        if(InterruptPending && _iff1) // InterruptPending turned on in Game1.cs at the end of a frame, mimicking VBLANK
            HandleInterrupt();

        if (_halted)
        {
            if (InterruptPending)
                _halted = false;       // resume from HALT
            else
                return 4;              // stay halted, do not fetch opcode
        }
        
        byte opcode = _machine.ReadByte(Reg.PC);
        
        if (SteppingThrough)
            PrintStepThroughDebug(opcode);
        
        IncrementRegisterR();
        int cycles = _mainOpcodes[opcode]();

        if (EI_EnableAfterInstruction)
        {
            _iff1 = true;
            _iff2 = true;
            EI_EnableAfterInstruction = false;
        }
        else if (EI_Pending)
        {
            EI_Pending = false;
            EI_EnableAfterInstruction = true;
        }

        return cycles;
    }

    public void SetInterruptVectorLow(byte value)
    {
        interruptVectorLow = value;
    }

    private static byte ReadPort(ushort port)
    {
        return (port & 0xFF) switch
        {
            0x00 => 0xBF, // IN0
            0x01 => 0xFF, // IN1
            0x02 => 0xC9, // dip switches
            _ => 0xFF
        };
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
            _cbOpcodes[i] = Op_UNK;
        }

        _mainOpcodes[0x00] = Op_NOP;
        _mainOpcodes[0x01] = Op_LD_BC_nn;
        _mainOpcodes[0x02] = Op_LD_ptrBC_A;
        _mainOpcodes[0x03] = Op_INC_BC;
        _mainOpcodes[0x04] = () => Op_INC_r(ref Reg.B);
        _mainOpcodes[0x05] = () => Op_DEC_r(ref Reg.B);
        _mainOpcodes[0x06] = () => Op_LD_r_n(ref Reg.B);
        _mainOpcodes[0x07] = Op_RLCA;
        _mainOpcodes[0x08] = Op_EX_AF_AF2;
        _mainOpcodes[0x09] = () => Op_ADD_HL(Reg.BC);
        _mainOpcodes[0x0A] = Op_LD_A_ptrBC;
        _mainOpcodes[0x0B] = Op_DEC_BC;
        _mainOpcodes[0x0C] = () => Op_INC_r(ref Reg.C);
        _mainOpcodes[0x0D] = () => Op_DEC_r(ref Reg.C);
        _mainOpcodes[0x0E] = () => Op_LD_r_n(ref Reg.C);
        _mainOpcodes[0x0F] = Op_RRCA;

        _mainOpcodes[0x10] = Op_DJNZ_e;
        _mainOpcodes[0x11] = Op_LD_DE_nn;
        _mainOpcodes[0x12] = Op_LD_ptrDE_A;
        _mainOpcodes[0x13] = Op_INC_DE;
        _mainOpcodes[0x14] = () => Op_INC_r(ref Reg.D);
        _mainOpcodes[0x15] = () => Op_DEC_r(ref Reg.D);
        _mainOpcodes[0x16] = () => Op_LD_r_n(ref Reg.D);
        _mainOpcodes[0x17] = Op_RLA;
        _mainOpcodes[0x18] = Op_JR_e;
        _mainOpcodes[0x19] = () => Op_ADD_HL(Reg.DE);
        _mainOpcodes[0x1A] = Op_LD_A_ptrDE;
        _mainOpcodes[0x1B] = Op_DEC_DE;
        _mainOpcodes[0x1C] = () => Op_INC_r(ref Reg.E);
        _mainOpcodes[0x1D] = () => Op_DEC_r(ref Reg.E);
        _mainOpcodes[0x1E] = () => Op_LD_r_n(ref Reg.E);
        _mainOpcodes[0x1F] = Op_RRA;

        _mainOpcodes[0x20] = Op_JR_NZ_e;
        _mainOpcodes[0x21] = Op_LD_HL_nn;
        _mainOpcodes[0x22] = Op_LD_ptrNN_HL;
        _mainOpcodes[0x23] = Op_INC_HL;
        _mainOpcodes[0x24] = () => Op_INC_r(ref Reg.H);
        _mainOpcodes[0x25] = () => Op_DEC_r(ref Reg.H);
        _mainOpcodes[0x26] = () => Op_LD_r_n(ref Reg.H);
        _mainOpcodes[0x27] = Op_DAA;
        _mainOpcodes[0x28] = Op_JR_Z_e;
        _mainOpcodes[0x29] = () => Op_ADD_HL(Reg.HL);
        _mainOpcodes[0x2A] = Op_LD_HL_ptrNN;
        _mainOpcodes[0x2B] = Op_DEC_HL;
        _mainOpcodes[0x2C] = () => Op_INC_r(ref Reg.L);
        _mainOpcodes[0x2D] = () => Op_DEC_r(ref Reg.L);
        _mainOpcodes[0x2E] = () => Op_LD_r_n(ref Reg.L);
        _mainOpcodes[0x2F] = Op_CPL;

        _mainOpcodes[0x30] = Op_JR_NC_e;
        _mainOpcodes[0x31] = Op_LD_SP_nn;
        _mainOpcodes[0x32] = Op_LD_ptrNN_A;
        _mainOpcodes[0x33] = Op_INC_SP;
        _mainOpcodes[0x34] = Op_INC_ptrHL;
        _mainOpcodes[0x35] = Op_DEC_ptrHL;
        _mainOpcodes[0x36] = Op_LD_ptrHL_n;
        _mainOpcodes[0x37] = Op_SCF;
        _mainOpcodes[0x38] = Op_JR_C_e;
        _mainOpcodes[0x39] = () => Op_ADD_HL(Reg.SP);
        _mainOpcodes[0x3A] = Op_LD_A_ptrNN;
        _mainOpcodes[0x3B] = Op_DEC_SP;
        _mainOpcodes[0x3C] = () => Op_INC_r(ref Reg.A);
        _mainOpcodes[0x3D] = () => Op_DEC_r(ref Reg.A);
        _mainOpcodes[0x3E] = () => Op_LD_r_n(ref Reg.A);
        _mainOpcodes[0x3F] = Op_CCF;

        _mainOpcodes[0x40] = () => Op_LD_r_R(ref Reg.B, Reg.B);
        _mainOpcodes[0x41] = () => Op_LD_r_R(ref Reg.B, Reg.C);
        _mainOpcodes[0x42] = () => Op_LD_r_R(ref Reg.B, Reg.D);
        _mainOpcodes[0x43] = () => Op_LD_r_R(ref Reg.B, Reg.E);
        _mainOpcodes[0x44] = () => Op_LD_r_R(ref Reg.B, Reg.H);
        _mainOpcodes[0x45] = () => Op_LD_r_R(ref Reg.B, Reg.L);
        _mainOpcodes[0x46] = () => Op_LD_r_ptrHL(ref Reg.B);
        _mainOpcodes[0x47] = () => Op_LD_r_R(ref Reg.B, Reg.A);
        _mainOpcodes[0x48] = () => Op_LD_r_R(ref Reg.C, Reg.B);
        _mainOpcodes[0x49] = () => Op_LD_r_R(ref Reg.C, Reg.C);
        _mainOpcodes[0x4A] = () => Op_LD_r_R(ref Reg.C, Reg.D);
        _mainOpcodes[0x4B] = () => Op_LD_r_R(ref Reg.C, Reg.E);
        _mainOpcodes[0x4C] = () => Op_LD_r_R(ref Reg.C, Reg.H);
        _mainOpcodes[0x4D] = () => Op_LD_r_R(ref Reg.C, Reg.L);
        _mainOpcodes[0x4E] = () => Op_LD_r_ptrHL(ref Reg.C);
        _mainOpcodes[0x4F] = () => Op_LD_r_R(ref Reg.C, Reg.A);

        _mainOpcodes[0x50] = () => Op_LD_r_R(ref Reg.D, Reg.B);
        _mainOpcodes[0x51] = () => Op_LD_r_R(ref Reg.D, Reg.C);
        _mainOpcodes[0x52] = () => Op_LD_r_R(ref Reg.D, Reg.D);
        _mainOpcodes[0x53] = () => Op_LD_r_R(ref Reg.D, Reg.E);
        _mainOpcodes[0x54] = () => Op_LD_r_R(ref Reg.D, Reg.H);
        _mainOpcodes[0x55] = () => Op_LD_r_R(ref Reg.D, Reg.L);
        _mainOpcodes[0x56] = () => Op_LD_r_ptrHL(ref Reg.D);
        _mainOpcodes[0x57] = () => Op_LD_r_R(ref Reg.D, Reg.A);
        _mainOpcodes[0x58] = () => Op_LD_r_R(ref Reg.E, Reg.B);
        _mainOpcodes[0x59] = () => Op_LD_r_R(ref Reg.E, Reg.C);
        _mainOpcodes[0x5A] = () => Op_LD_r_R(ref Reg.E, Reg.D);
        _mainOpcodes[0x5B] = () => Op_LD_r_R(ref Reg.E, Reg.E);
        _mainOpcodes[0x5C] = () => Op_LD_r_R(ref Reg.E, Reg.H);
        _mainOpcodes[0x5D] = () => Op_LD_r_R(ref Reg.E, Reg.L);
        _mainOpcodes[0x5E] = () => Op_LD_r_ptrHL(ref Reg.E);
        _mainOpcodes[0x5F] = () => Op_LD_r_R(ref Reg.E, Reg.A);

        _mainOpcodes[0x60] = () => Op_LD_r_R(ref Reg.H, Reg.B);
        _mainOpcodes[0x61] = () => Op_LD_r_R(ref Reg.H, Reg.C);
        _mainOpcodes[0x62] = () => Op_LD_r_R(ref Reg.H, Reg.D);
        _mainOpcodes[0x63] = () => Op_LD_r_R(ref Reg.H, Reg.E);
        _mainOpcodes[0x64] = () => Op_LD_r_R(ref Reg.H, Reg.H);
        _mainOpcodes[0x65] = () => Op_LD_r_R(ref Reg.H, Reg.L);
        _mainOpcodes[0x66] = () => Op_LD_r_ptrHL(ref Reg.H);
        _mainOpcodes[0x67] = () => Op_LD_r_R(ref Reg.H, Reg.A);
        _mainOpcodes[0x68] = () => Op_LD_r_R(ref Reg.L, Reg.B);
        _mainOpcodes[0x69] = () => Op_LD_r_R(ref Reg.L, Reg.C);
        _mainOpcodes[0x6A] = () => Op_LD_r_R(ref Reg.L, Reg.D);
        _mainOpcodes[0x6B] = () => Op_LD_r_R(ref Reg.L, Reg.E);
        _mainOpcodes[0x6C] = () => Op_LD_r_R(ref Reg.L, Reg.H);
        _mainOpcodes[0x6D] = () => Op_LD_r_R(ref Reg.L, Reg.L);
        _mainOpcodes[0x6E] = () => Op_LD_r_ptrHL(ref Reg.L);
        _mainOpcodes[0x6F] = () => Op_LD_r_R(ref Reg.L, Reg.A);

        _mainOpcodes[0x70] = () => Op_LD_ptrHL(Reg.B);
        _mainOpcodes[0x71] = () => Op_LD_ptrHL(Reg.C);
        _mainOpcodes[0x72] = () => Op_LD_ptrHL(Reg.D);
        _mainOpcodes[0x73] = () => Op_LD_ptrHL(Reg.E);
        _mainOpcodes[0x74] = () => Op_LD_ptrHL(Reg.H);
        _mainOpcodes[0x75] = () => Op_LD_ptrHL(Reg.L);
        _mainOpcodes[0x76] = Op_HALT;
        _mainOpcodes[0x77] = () => Op_LD_ptrHL(Reg.A);
        _mainOpcodes[0x78] = () => Op_LD_r_R(ref Reg.A, Reg.B);
        _mainOpcodes[0x79] = () => Op_LD_r_R(ref Reg.A, Reg.C);
        _mainOpcodes[0x7A] = () => Op_LD_r_R(ref Reg.A, Reg.D);
        _mainOpcodes[0x7B] = () => Op_LD_r_R(ref Reg.A, Reg.E);
        _mainOpcodes[0x7C] = () => Op_LD_r_R(ref Reg.A, Reg.H);
        _mainOpcodes[0x7D] = () => Op_LD_r_R(ref Reg.A, Reg.L);
        _mainOpcodes[0x7E] = () => Op_LD_r_ptrHL(ref Reg.A);
        _mainOpcodes[0x7F] = () => Op_LD_r_R(ref Reg.A, Reg.A);

        _mainOpcodes[0x80] = () => Op_ADD_A(Reg.B);
        _mainOpcodes[0x81] = () => Op_ADD_A(Reg.C);
        _mainOpcodes[0x82] = () => Op_ADD_A(Reg.D);
        _mainOpcodes[0x83] = () => Op_ADD_A(Reg.E);
        _mainOpcodes[0x84] = () => Op_ADD_A(Reg.H);
        _mainOpcodes[0x85] = () => Op_ADD_A(Reg.L);
        _mainOpcodes[0x86] = Op_ADD_A_ptrHL;
        _mainOpcodes[0x87] = () => Op_ADD_A(Reg.A);
        _mainOpcodes[0x88] = () => Op_ADC_A(Reg.B);
        _mainOpcodes[0x89] = () => Op_ADC_A(Reg.C);
        _mainOpcodes[0x8A] = () => Op_ADC_A(Reg.D);
        _mainOpcodes[0x8B] = () => Op_ADC_A(Reg.E);
        _mainOpcodes[0x8C] = () => Op_ADC_A(Reg.H);
        _mainOpcodes[0x8D] = () => Op_ADC_A(Reg.L);
        _mainOpcodes[0x8E] = Op_ADC_A_ptrHL;
        _mainOpcodes[0x8F] = () => Op_ADC_A(Reg.A);

        _mainOpcodes[0x90] = () => Op_SUB(Reg.B);
        _mainOpcodes[0x91] = () => Op_SUB(Reg.C);
        _mainOpcodes[0x92] = () => Op_SUB(Reg.D);
        _mainOpcodes[0x93] = () => Op_SUB(Reg.E);
        _mainOpcodes[0x94] = () => Op_SUB(Reg.H);
        _mainOpcodes[0x95] = () => Op_SUB(Reg.L);
        _mainOpcodes[0x96] = Op_SUB_A_ptrHL;
        _mainOpcodes[0x97] = () => Op_SUB(Reg.A);
        _mainOpcodes[0x98] = () => Op_SBC(Reg.B);
        _mainOpcodes[0x99] = () => Op_SBC(Reg.C);
        _mainOpcodes[0x9A] = () => Op_SBC(Reg.D);
        _mainOpcodes[0x9B] = () => Op_SBC(Reg.E);
        _mainOpcodes[0x9C] = () => Op_SBC(Reg.H);
        _mainOpcodes[0x9D] = () => Op_SBC(Reg.L);
        _mainOpcodes[0x9E] = Op_SBC_A_ptrHL;
        _mainOpcodes[0x9F] = () => Op_SBC(Reg.A);

        _mainOpcodes[0xA0] = () => Op_AND(Reg.B);
        _mainOpcodes[0xA1] = () => Op_AND(Reg.C);
        _mainOpcodes[0xA2] = () => Op_AND(Reg.D);
        _mainOpcodes[0xA3] = () => Op_AND(Reg.E);
        _mainOpcodes[0xA4] = () => Op_AND(Reg.H);
        _mainOpcodes[0xA5] = () => Op_AND(Reg.L);
        _mainOpcodes[0xA6] = Op_AND_ptrHL;
        _mainOpcodes[0xA7] = () => Op_AND(Reg.A);
        _mainOpcodes[0xA8] = () => Op_XOR(Reg.B);
        _mainOpcodes[0xA9] = () => Op_XOR(Reg.C);
        _mainOpcodes[0xAA] = () => Op_XOR(Reg.D);
        _mainOpcodes[0xAB] = () => Op_XOR(Reg.E);
        _mainOpcodes[0xAC] = () => Op_XOR(Reg.H);
        _mainOpcodes[0xAD] = () => Op_XOR(Reg.L);
        _mainOpcodes[0xAE] = Op_XOR_A_ptrHL;
        _mainOpcodes[0xAF] = () => Op_XOR(Reg.A);

        _mainOpcodes[0xB0] = () => Op_OR(Reg.B);
        _mainOpcodes[0xB1] = () => Op_OR(Reg.C);
        _mainOpcodes[0xB2] = () => Op_OR(Reg.D);
        _mainOpcodes[0xB3] = () => Op_OR(Reg.E);
        _mainOpcodes[0xB4] = () => Op_OR(Reg.H);
        _mainOpcodes[0xB5] = () => Op_OR(Reg.L);
        _mainOpcodes[0xB6] = Op_OR_ptrHL;
        _mainOpcodes[0xB7] = () => Op_OR(Reg.A);
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
        _mainOpcodes[0xC4] = Op_CALL_NZ_nn;
        _mainOpcodes[0xC5] = Op_PUSH_BC;
        _mainOpcodes[0xC6] = Op_ADD_A_n;
        _mainOpcodes[0xC7] = () => Op_RST(0x00);
        _mainOpcodes[0xC8] = Op_RET_Z;
        _mainOpcodes[0xC9] = Op_RET;
        _mainOpcodes[0xCA] = Op_JP_Z_nn;
        _mainOpcodes[0xCB] = Op_CB;
        _mainOpcodes[0xCC] = Op_CALL_Z_nn;
        _mainOpcodes[0xCD] = Op_CALL_nn;
        _mainOpcodes[0xCE] = Op_ADC_A_n;
        _mainOpcodes[0xCF] = () => Op_RST(0x08);

        _mainOpcodes[0xD0] = Op_RET_NC;
        _mainOpcodes[0xD1] = Op_POP_DE;
        _mainOpcodes[0xD2] = Op_JP_NC_nn;
        _mainOpcodes[0xD3] = Op_OUT_ptrn_A;
        _mainOpcodes[0xD4] = Op_CALL_NC_nn;
        _mainOpcodes[0xD5] = Op_PUSH_DE;
        _mainOpcodes[0xD6] = Op_SUB_n;
        _mainOpcodes[0xD7] = () => Op_RST(0x10);
        _mainOpcodes[0xD8] = Op_RET_C;
        _mainOpcodes[0xD9] = Op_EXX;
        _mainOpcodes[0xDA] = Op_JP_C_nn;
        _mainOpcodes[0xDB] = Op_IN_A_n;
        _mainOpcodes[0xDC] = Op_CALL_C_nn;
        _mainOpcodes[0xDD] = Op_DD;
        _mainOpcodes[0xDE] = Op_SBC_n;
        _mainOpcodes[0xDF] = () => Op_RST(0x18);

        _mainOpcodes[0xE0] = Op_RET_PO;
        _mainOpcodes[0xE1] = Op_POP_HL;
        _mainOpcodes[0xE2] = Op_JP_PO_nn;
        _mainOpcodes[0xE3] = Op_EX_ptrSP_HL;
        _mainOpcodes[0xE4] = Op_CALL_PO_nn;
        _mainOpcodes[0xE5] = Op_PUSH_HL;
        _mainOpcodes[0xE6] = Op_AND_n;
        _mainOpcodes[0xE7] = () => Op_RST(0x20);
        _mainOpcodes[0xE8] = Op_RET_PE;
        _mainOpcodes[0xE9] = Op_JP_ptrHL;
        _mainOpcodes[0xEA] = Op_JP_PE_nn;
        _mainOpcodes[0xEB] = Op_EX_DE_HL;
        _mainOpcodes[0xEC] = Op_CALL_PE_nn;
        _mainOpcodes[0xED] = Op_ED;
        _mainOpcodes[0xEE] = Op_XOR_A_n;
        _mainOpcodes[0xEF] = () => Op_RST(0x28);

        _mainOpcodes[0xF0] = Op_RET_P;
        _mainOpcodes[0xF1] = Op_POP_AF;
        _mainOpcodes[0xF2] = Op_JP_P_nn;
        _mainOpcodes[0xF3] = Op_DI;
        _mainOpcodes[0xF4] = Op_CALL_P_nn;
        _mainOpcodes[0xF5] = Op_PUSH_AF;
        _mainOpcodes[0xF6] = Op_OR_n;
        _mainOpcodes[0xF7] = () => Op_RST(0x30);
        _mainOpcodes[0xF8] = Op_RET_M;
        _mainOpcodes[0xF9] = Op_LD_SP_HL;
        _mainOpcodes[0xFA] = Op_JP_M_nn;
        _mainOpcodes[0xFB] = Op_EI;
        _mainOpcodes[0xFC] = Op_CALL_M_nn;
        _mainOpcodes[0xFD] = Op_FD;
        _mainOpcodes[0xFE] = Op_CP_n;
        _mainOpcodes[0xFF] = () => Op_RST(0x38);

        _ddOpcodes[0x04] = () => Op_DD_INC_r(ref Reg.B);
        _ddOpcodes[0x05] = () => Op_DD_DEC_r(ref Reg.B);
        _ddOpcodes[0x06] = () => Op_DD_LD_r_n(ref Reg.B);
        _ddOpcodes[0x09] = Op_ADD_IX_BC;
        _ddOpcodes[0x0C] = () => Op_DD_INC_r(ref Reg.C);
        _ddOpcodes[0x0D] = () => Op_DD_DEC_r(ref Reg.C);
        _ddOpcodes[0x0E] = () => Op_DD_LD_r_n(ref Reg.C);
        _ddOpcodes[0x14] = () => Op_DD_INC_r(ref Reg.D);
        _ddOpcodes[0x15] = () => Op_DD_DEC_r(ref Reg.D);
        _ddOpcodes[0x16] = () => Op_DD_LD_r_n(ref Reg.D);
        _ddOpcodes[0x19] = Op_ADD_IX_DE;
        _ddOpcodes[0x1C] = () => Op_DD_INC_r(ref Reg.E);
        _ddOpcodes[0x1D] = () => Op_DD_DEC_r(ref Reg.E);
        _ddOpcodes[0x1E] = () => Op_DD_LD_r_n(ref Reg.E);
        _ddOpcodes[0x21] = Op_LD_IX_nn;
        _ddOpcodes[0x22] = Op_LD_ptrNN_IX;
        _ddOpcodes[0x23] = Op_INC_IX;
        _ddOpcodes[0x24] = () => Op_DD_INC_r(ref Reg.IXH);
        _ddOpcodes[0x25] = () => Op_DD_DEC_r(ref Reg.IXH);
        _ddOpcodes[0x26] = () => Op_DD_LD_r_n(ref Reg.IXH);
        _ddOpcodes[0x29] = Op_ADD_IX_IX;
        _ddOpcodes[0x2A] = Op_LD_IX_ptrnn;
        _ddOpcodes[0x2B] = Op_DEC_IX;
        _ddOpcodes[0x2C] = () => Op_DD_INC_r(ref Reg.IXL);
        _ddOpcodes[0x2D] = () => Op_DD_DEC_r(ref Reg.IXL);
        _ddOpcodes[0x2E] = () => Op_DD_LD_r_n(ref Reg.IXL);
        _ddOpcodes[0x34] = Op_INC_ptrIXd;
        _ddOpcodes[0x35] = Op_DEC_ptrIXd;
        _ddOpcodes[0x36] = Op_LD_ptrIXd_n;
        _ddOpcodes[0x39] = Op_ADD_IX_SP;
        _ddOpcodes[0x3C] = () => Op_DD_INC_r(ref Reg.A);
        _ddOpcodes[0x3D] = () => Op_DD_DEC_r(ref Reg.A);
        _ddOpcodes[0x3E] = () => Op_DD_LD_r_n(ref Reg.A);
        _ddOpcodes[0x40] = () => Op_DD_LD_r_R(ref Reg.B, Reg.B);
        _ddOpcodes[0x41] = () => Op_DD_LD_r_R(ref Reg.B, Reg.C);
        _ddOpcodes[0x42] = () => Op_DD_LD_r_R(ref Reg.B, Reg.D);
        _ddOpcodes[0x43] = () => Op_DD_LD_r_R(ref Reg.B, Reg.E);
        _ddOpcodes[0x44] = () => Op_LD_r_IXH(ref Reg.B);
        _ddOpcodes[0x45] = () => Op_LD_r_IXL(ref Reg.B);
        _ddOpcodes[0x46] = () => Op_LD_r_ptrIXd(ref Reg.B);
        _ddOpcodes[0x47] = () => Op_DD_LD_r_R(ref Reg.B, Reg.A);
        _ddOpcodes[0x48] = () => Op_DD_LD_r_R(ref Reg.C, Reg.B);
        _ddOpcodes[0x49] = () => Op_DD_LD_r_R(ref Reg.C, Reg.C);
        _ddOpcodes[0x4A] = () => Op_DD_LD_r_R(ref Reg.C, Reg.D);
        _ddOpcodes[0x4B] = () => Op_DD_LD_r_R(ref Reg.C, Reg.E);
        _ddOpcodes[0x4C] = () => Op_LD_r_IXH(ref Reg.C);
        _ddOpcodes[0x4D] = () => Op_LD_r_IXL(ref Reg.C);
        _ddOpcodes[0x4E] = () => Op_LD_r_ptrIXd(ref Reg.C);
        _ddOpcodes[0x4F] = () => Op_DD_LD_r_R(ref Reg.C, Reg.A);
        _ddOpcodes[0x50] = () => Op_DD_LD_r_R(ref Reg.D, Reg.B);
        _ddOpcodes[0x51] = () => Op_DD_LD_r_R(ref Reg.D, Reg.C);
        _ddOpcodes[0x52] = () => Op_DD_LD_r_R(ref Reg.D, Reg.D);
        _ddOpcodes[0x53] = () => Op_DD_LD_r_R(ref Reg.D, Reg.E);
        _ddOpcodes[0x54] = () => Op_LD_r_IXH(ref Reg.D);
        _ddOpcodes[0x55] = () => Op_LD_r_IXL(ref Reg.D);
        _ddOpcodes[0x56] = () => Op_LD_r_ptrIXd(ref Reg.D);
        _ddOpcodes[0x57] = () => Op_DD_LD_r_R(ref Reg.D, Reg.A);
        _ddOpcodes[0x58] = () => Op_DD_LD_r_R(ref Reg.E, Reg.B);
        _ddOpcodes[0x59] = () => Op_DD_LD_r_R(ref Reg.E, Reg.C);
        _ddOpcodes[0x5A] = () => Op_DD_LD_r_R(ref Reg.E, Reg.D);
        _ddOpcodes[0x5B] = () => Op_DD_LD_r_R(ref Reg.E, Reg.E);
        _ddOpcodes[0x5C] = () => Op_LD_r_IXH(ref Reg.E);
        _ddOpcodes[0x5D] = () => Op_LD_r_IXL(ref Reg.E);
        _ddOpcodes[0x5E] = () => Op_LD_r_ptrIXd(ref Reg.E);
        _ddOpcodes[0x5F] = () => Op_DD_LD_r_R(ref Reg.E, Reg.A);
        _ddOpcodes[0x60] = () => Op_LD_IXH_r(Reg.B);
        _ddOpcodes[0x61] = () => Op_LD_IXH_r(Reg.C);
        _ddOpcodes[0x62] = () => Op_LD_IXH_r(Reg.D);
        _ddOpcodes[0x63] = () => Op_LD_IXH_r(Reg.E);
        _ddOpcodes[0x64] = () => Op_LD_IXH_r(Reg.IXH);
        _ddOpcodes[0x65] = () => Op_LD_IXH_r(Reg.IXL);
        _ddOpcodes[0x66] = () => Op_LD_r_ptrIXd(ref Reg.H);
        _ddOpcodes[0x67] = () => Op_LD_IXH_r(Reg.A);
        _ddOpcodes[0x68] = () => Op_LD_IXL_r(Reg.B);
        _ddOpcodes[0x69] = () => Op_LD_IXL_r(Reg.C);
        _ddOpcodes[0x6A] = () => Op_LD_IXL_r(Reg.D);
        _ddOpcodes[0x6B] = () => Op_LD_IXL_r(Reg.E);
        _ddOpcodes[0x6C] = () => Op_LD_IXL_r(Reg.IXH);
        _ddOpcodes[0x6D] = () => Op_LD_IXL_r(Reg.IXL);
        _ddOpcodes[0x6E] = () => Op_LD_r_ptrIXd(ref Reg.L);
        _ddOpcodes[0x6F] = () => Op_LD_IXL_r(Reg.A);
        _ddOpcodes[0x70] = () => Op_LD_ptrIXd_r(Reg.B);
        _ddOpcodes[0x71] = () => Op_LD_ptrIXd_r(Reg.C);
        _ddOpcodes[0x72] = () => Op_LD_ptrIXd_r(Reg.D);
        _ddOpcodes[0x73] = () => Op_LD_ptrIXd_r(Reg.E);
        _ddOpcodes[0x74] = () => Op_LD_ptrIXd_r(Reg.H);
        _ddOpcodes[0x75] = () => Op_LD_ptrIXd_r(Reg.L);
        _ddOpcodes[0x77] = () => Op_LD_ptrIXd_r(Reg.A);
        _ddOpcodes[0x78] = () => Op_DD_LD_r_R(ref Reg.A, Reg.B);
        _ddOpcodes[0x79] = () => Op_DD_LD_r_R(ref Reg.A, Reg.C);
        _ddOpcodes[0x7A] = () => Op_DD_LD_r_R(ref Reg.A, Reg.D);
        _ddOpcodes[0x7B] = () => Op_DD_LD_r_R(ref Reg.A, Reg.E);
        _ddOpcodes[0x7C] = () => Op_LD_r_IXH(ref Reg.A);
        _ddOpcodes[0x7D] = () => Op_LD_r_IXL(ref Reg.A);
        _ddOpcodes[0x7E] = () => Op_LD_r_ptrIXd(ref Reg.A);
        _ddOpcodes[0x7F] = () => Op_DD_LD_r_R(ref Reg.A, Reg.A);
        _ddOpcodes[0x80] = () => Op_DD_ADD_A(Reg.B);
        _ddOpcodes[0x81] = () => Op_DD_ADD_A(Reg.C);
        _ddOpcodes[0x82] = () => Op_DD_ADD_A(Reg.D);
        _ddOpcodes[0x83] = () => Op_DD_ADD_A(Reg.E);
        _ddOpcodes[0x84] = () => Op_DD_ADD_A(Reg.IXH);
        _ddOpcodes[0x85] = () => Op_DD_ADD_A(Reg.IXL);
        _ddOpcodes[0x86] = Op_ADD_A_ptrIXd;
        _ddOpcodes[0x87] = () => Op_DD_ADD_A(Reg.A);
        _ddOpcodes[0x88] = () => Op_DD_ADC_A(Reg.B);
        _ddOpcodes[0x89] = () => Op_DD_ADC_A(Reg.C);
        _ddOpcodes[0x8A] = () => Op_DD_ADC_A(Reg.D);
        _ddOpcodes[0x8B] = () => Op_DD_ADC_A(Reg.E);
        _ddOpcodes[0x8C] = () => Op_DD_ADC_A(Reg.IXH);
        _ddOpcodes[0x8D] = () => Op_DD_ADC_A(Reg.IXL);
        _ddOpcodes[0x8E] = Op_ADC_A_ptrIXd;
        _ddOpcodes[0x8F] = () => Op_DD_ADC_A(Reg.A);
        _ddOpcodes[0x90] = () => Op_DD_SUB(Reg.B);
        _ddOpcodes[0x91] = () => Op_DD_SUB(Reg.C);
        _ddOpcodes[0x92] = () => Op_DD_SUB(Reg.D);
        _ddOpcodes[0x93] = () => Op_DD_SUB(Reg.E);
        _ddOpcodes[0x94] = () => Op_DD_SUB(Reg.IXH);
        _ddOpcodes[0x95] = () => Op_DD_SUB(Reg.IXL);
        _ddOpcodes[0x96] = Op_SUB_ptrIXd;
        _ddOpcodes[0x97] = () => Op_DD_SUB(Reg.A);
        _ddOpcodes[0x98] = () => Op_DD_SBC(Reg.B);
        _ddOpcodes[0x99] = () => Op_DD_SBC(Reg.C);
        _ddOpcodes[0x9A] = () => Op_DD_SBC(Reg.D);
        _ddOpcodes[0x9B] = () => Op_DD_SBC(Reg.E);
        _ddOpcodes[0x9C] = () => Op_DD_SBC(Reg.IXH);
        _ddOpcodes[0x9D] = () => Op_DD_SBC(Reg.IXL);
        _ddOpcodes[0x9E] = Op_SBC_A_ptrIXd;
        _ddOpcodes[0x9F] = () => Op_DD_SBC(Reg.A);
        _ddOpcodes[0xA0] = () => Op_DD_AND(Reg.B);
        _ddOpcodes[0xA1] = () => Op_DD_AND(Reg.C);
        _ddOpcodes[0xA2] = () => Op_DD_AND(Reg.D);
        _ddOpcodes[0xA3] = () => Op_DD_AND(Reg.E);
        _ddOpcodes[0xA4] = () => Op_DD_AND(Reg.IXH);
        _ddOpcodes[0xA5] = () => Op_DD_AND(Reg.IXL);
        _ddOpcodes[0xA6] = Op_AND_ptrIXd;
        _ddOpcodes[0xA7] = () => Op_DD_AND(Reg.A);
        _ddOpcodes[0xA8] = () => Op_DD_XOR(Reg.B);
        _ddOpcodes[0xA9] = () => Op_DD_XOR(Reg.C);
        _ddOpcodes[0xAA] = () => Op_DD_XOR(Reg.D);
        _ddOpcodes[0xAB] = () => Op_DD_XOR(Reg.E);
        _ddOpcodes[0xAC] = () => Op_DD_XOR(Reg.IXH);
        _ddOpcodes[0xAD] = () => Op_DD_XOR(Reg.IXL);
        _ddOpcodes[0xAE] = Op_XOR_A_ptrIXd;
        _ddOpcodes[0xAF] = () => Op_DD_XOR(Reg.A);
        _ddOpcodes[0xB0] = () => Op_DD_OR(Reg.B);
        _ddOpcodes[0xB1] = () => Op_DD_OR(Reg.C);
        _ddOpcodes[0xB2] = () => Op_DD_OR(Reg.D);
        _ddOpcodes[0xB3] = () => Op_DD_OR(Reg.E);
        _ddOpcodes[0xB4] = () => Op_DD_OR(Reg.IXH);
        _ddOpcodes[0xB5] = () => Op_DD_OR(Reg.IXL);
        _ddOpcodes[0xB6] = Op_OR_ptrIXd;
        _ddOpcodes[0xB7] = () => Op_DD_OR(Reg.A);
        _ddOpcodes[0xB8] = () => Op_DD_CP(Reg.B);
        _ddOpcodes[0xB9] = () => Op_DD_CP(Reg.C);
        _ddOpcodes[0xBA] = () => Op_DD_CP(Reg.D);
        _ddOpcodes[0xBB] = () => Op_DD_CP(Reg.E);
        _ddOpcodes[0xBC] = () => Op_DD_CP(Reg.IXH);
        _ddOpcodes[0xBD] = () => Op_DD_CP(Reg.IXL);
        _ddOpcodes[0xBE] = OP_CP_ptrIXd;
        _ddOpcodes[0xBF] = () => Op_DD_CP(Reg.A);
        _ddOpcodes[0xE1] = Op_POP_IX;
        _ddOpcodes[0xE3] = Op_EX_ptrSP_IX;
        _ddOpcodes[0xE5] = Op_PUSH_IX;
        _ddOpcodes[0xE9] = Op_JP_ptrIX;
        _ddOpcodes[0xF9] = Op_LD_SP_IX;

        _edOpcodes[0x42] = Op_SBC_HL_BC;
        _edOpcodes[0x43] = Op_LD_ptrNN_BC;
        _edOpcodes[0x44] = Op_NEG;
        _edOpcodes[0x45] = Op_RETN;
        _edOpcodes[0x46] = Op_IM_0;
        _edOpcodes[0x47] = Op_LD_I_A;
        _edOpcodes[0x4A] = Op_ADC_HL_BC;
        _edOpcodes[0x4B] = Op_LD_BC_ptrNN;
        _edOpcodes[0x4D] = Op_RETI;
        _edOpcodes[0x4F] = Op_LD_R_A;
        _edOpcodes[0x52] = Op_SBC_HL_DE;
        _edOpcodes[0x53] = Op_LD_ptrNN_DE;
        _edOpcodes[0x56] = Op_IM_1;
        _edOpcodes[0x57] = Op_LD_A_I;
        _edOpcodes[0x5A] = Op_ADC_HL_DE;
        _edOpcodes[0x5B] = Op_LD_DE_ptrNN;
        _edOpcodes[0x5E] = Op_IM_2;
        _edOpcodes[0x5F] = Op_LD_A_R;
        _edOpcodes[0x62] = Op_SBC_HL_HL;
        _edOpcodes[0x63] = Op_ED_LD_ptrNN_HL;
        _edOpcodes[0x67] = Op_RRD;
        _edOpcodes[0x6A] = Op_ADC_HL_HL;
        _edOpcodes[0x6B] = Op_ED_LD_HL_ptrNN;
        _edOpcodes[0x6F] = Op_RLD;
        _edOpcodes[0x72] = Op_SBC_HL_SP;
        _edOpcodes[0x73] = Op_LD_ptrNN_SP;
        _edOpcodes[0x7A] = Op_ADC_HL_SP;
        _edOpcodes[0x7B] = Op_LD_SP_ptrNN;
        _edOpcodes[0xA0] = Op_LDI;
        _edOpcodes[0xA1] = Op_CPI;
        _edOpcodes[0xB0] = Op_LDIR;
        _edOpcodes[0xB1] = Op_CPIR;
        _edOpcodes[0xA8] = Op_LDD;
        _edOpcodes[0xA9] = Op_CPD;
        _edOpcodes[0xB8] = Op_LDDR;
        _edOpcodes[0xB9] = Op_CPDR;

        _fdOpcodes[0x04] = () => Op_DD_INC_r(ref Reg.B);
        _fdOpcodes[0x05] = () => Op_DD_DEC_r(ref Reg.B);
        _fdOpcodes[0x06] = () => Op_DD_LD_r_n(ref Reg.B);
        _fdOpcodes[0x09] = Op_ADD_IY_BC;
        _fdOpcodes[0x0C] = () => Op_DD_INC_r(ref Reg.C);
        _fdOpcodes[0x0D] = () => Op_DD_DEC_r(ref Reg.C);
        _fdOpcodes[0x0E] = () => Op_DD_LD_r_n(ref Reg.C);
        _fdOpcodes[0x14] = () => Op_DD_INC_r(ref Reg.D);
        _fdOpcodes[0x15] = () => Op_DD_DEC_r(ref Reg.D);
        _fdOpcodes[0x16] = () => Op_DD_LD_r_n(ref Reg.D);
        _fdOpcodes[0x19] = Op_ADD_IY_DE;
        _fdOpcodes[0x1C] = () => Op_DD_INC_r(ref Reg.E);
        _fdOpcodes[0x1D] = () => Op_DD_DEC_r(ref Reg.E);
        _fdOpcodes[0x1E] = () => Op_DD_LD_r_n(ref Reg.E);
        _fdOpcodes[0x21] = Op_LD_IY_nn;
        _fdOpcodes[0x22] = Op_LD_ptrNN_IY;
        _fdOpcodes[0x23] = Op_INC_IY;
        _fdOpcodes[0x24] = () => Op_DD_INC_r(ref Reg.IYH);
        _fdOpcodes[0x25] = () => Op_DD_DEC_r(ref Reg.IYH);
        _fdOpcodes[0x26] = () => Op_DD_LD_r_n(ref Reg.IYH);
        _fdOpcodes[0x29] = Op_ADD_IY_IY;
        _fdOpcodes[0x2A] = Op_LD_IY_ptrnn;
        _fdOpcodes[0x2B] = Op_DEC_IY;
        _fdOpcodes[0x2C] = () => Op_DD_INC_r(ref Reg.IYL);
        _fdOpcodes[0x2D] = () => Op_DD_DEC_r(ref Reg.IYL);
        _fdOpcodes[0x2E] = () => Op_DD_LD_r_n(ref Reg.IYL);
        _fdOpcodes[0x34] = Op_INC_ptrIYd;
        _fdOpcodes[0x35] = Op_DEC_ptrIYd;
        _fdOpcodes[0x36] = Op_LD_ptrIYd_n;
        _fdOpcodes[0x39] = Op_ADD_IY_SP;
        _fdOpcodes[0x3C] = () => Op_DD_INC_r(ref Reg.A);
        _fdOpcodes[0x3D] = () => Op_DD_DEC_r(ref Reg.A);
        _fdOpcodes[0x3E] = () => Op_DD_LD_r_n(ref Reg.A);
        _fdOpcodes[0x40] = () => Op_DD_LD_r_R(ref Reg.B, Reg.B);
        _fdOpcodes[0x41] = () => Op_DD_LD_r_R(ref Reg.B, Reg.C);
        _fdOpcodes[0x42] = () => Op_DD_LD_r_R(ref Reg.B, Reg.D);
        _fdOpcodes[0x43] = () => Op_DD_LD_r_R(ref Reg.B, Reg.E);
        _fdOpcodes[0x44] = () => Op_LD_r_IYH(ref Reg.B);
        _fdOpcodes[0x45] = () => Op_LD_r_IYL(ref Reg.B);
        _fdOpcodes[0x46] = () => Op_LD_r_ptrIYd(ref Reg.B);
        _fdOpcodes[0x47] = () => Op_DD_LD_r_R(ref Reg.B, Reg.A);
        _fdOpcodes[0x48] = () => Op_DD_LD_r_R(ref Reg.C, Reg.B);
        _fdOpcodes[0x49] = () => Op_DD_LD_r_R(ref Reg.C, Reg.C);
        _fdOpcodes[0x4A] = () => Op_DD_LD_r_R(ref Reg.C, Reg.D);
        _fdOpcodes[0x4B] = () => Op_DD_LD_r_R(ref Reg.C, Reg.E);
        _fdOpcodes[0x4C] = () => Op_LD_r_IYH(ref Reg.C);
        _fdOpcodes[0x4D] = () => Op_LD_r_IYL(ref Reg.C);
        _fdOpcodes[0x4E] = () => Op_LD_r_ptrIYd(ref Reg.C);
        _fdOpcodes[0x4F] = () => Op_DD_LD_r_R(ref Reg.C, Reg.A);
        _fdOpcodes[0x50] = () => Op_DD_LD_r_R(ref Reg.D, Reg.B);
        _fdOpcodes[0x51] = () => Op_DD_LD_r_R(ref Reg.D, Reg.C);
        _fdOpcodes[0x52] = () => Op_DD_LD_r_R(ref Reg.D, Reg.D);
        _fdOpcodes[0x53] = () => Op_DD_LD_r_R(ref Reg.D, Reg.E);
        _fdOpcodes[0x54] = () => Op_LD_r_IYH(ref Reg.D);
        _fdOpcodes[0x55] = () => Op_LD_r_IYL(ref Reg.D);
        _fdOpcodes[0x56] = () => Op_LD_r_ptrIYd(ref Reg.D);
        _fdOpcodes[0x57] = () => Op_DD_LD_r_R(ref Reg.D, Reg.A);
        _fdOpcodes[0x58] = () => Op_DD_LD_r_R(ref Reg.E, Reg.B);
        _fdOpcodes[0x59] = () => Op_DD_LD_r_R(ref Reg.E, Reg.C);
        _fdOpcodes[0x5A] = () => Op_DD_LD_r_R(ref Reg.E, Reg.D);
        _fdOpcodes[0x5B] = () => Op_DD_LD_r_R(ref Reg.E, Reg.E);
        _fdOpcodes[0x5C] = () => Op_LD_r_IYH(ref Reg.E);
        _fdOpcodes[0x5D] = () => Op_LD_r_IYL(ref Reg.E);
        _fdOpcodes[0x5E] = () => Op_LD_r_ptrIYd(ref Reg.E);
        _fdOpcodes[0x5F] = () => Op_DD_LD_r_R(ref Reg.E, Reg.A);
        _fdOpcodes[0x60] = () => Op_LD_IYH_r(Reg.B);
        _fdOpcodes[0x61] = () => Op_LD_IYH_r(Reg.C);
        _fdOpcodes[0x62] = () => Op_LD_IYH_r(Reg.D);
        _fdOpcodes[0x63] = () => Op_LD_IYH_r(Reg.E);
        _fdOpcodes[0x64] = () => Op_LD_IYH_r(Reg.IYH);
        _fdOpcodes[0x65] = () => Op_LD_IYH_r(Reg.IYL);
        _fdOpcodes[0x66] = () => Op_LD_r_ptrIYd(ref Reg.H);
        _fdOpcodes[0x67] = () => Op_LD_IYH_r(Reg.A);
        _fdOpcodes[0x68] = () => Op_LD_IYL_r(Reg.B);
        _fdOpcodes[0x69] = () => Op_LD_IYL_r(Reg.C);
        _fdOpcodes[0x6A] = () => Op_LD_IYL_r(Reg.D);
        _fdOpcodes[0x6B] = () => Op_LD_IYL_r(Reg.E);
        _fdOpcodes[0x6C] = () => Op_LD_IYL_r(Reg.IYH);
        _fdOpcodes[0x6D] = () => Op_LD_IYL_r(Reg.IYL);
        _fdOpcodes[0x6E] = () => Op_LD_r_ptrIYd(ref Reg.L);
        _fdOpcodes[0x6F] = () => Op_LD_IYL_r(Reg.A);
        _fdOpcodes[0x70] = () => Op_LD_ptrIYd_r(Reg.B);
        _fdOpcodes[0x71] = () => Op_LD_ptrIYd_r(Reg.C);
        _fdOpcodes[0x72] = () => Op_LD_ptrIYd_r(Reg.D);
        _fdOpcodes[0x73] = () => Op_LD_ptrIYd_r(Reg.E);
        _fdOpcodes[0x74] = () => Op_LD_ptrIYd_r(Reg.H);
        _fdOpcodes[0x75] = () => Op_LD_ptrIYd_r(Reg.L);
        _fdOpcodes[0x77] = () => Op_LD_ptrIYd_r(Reg.A);
        _fdOpcodes[0x78] = () => Op_DD_LD_r_R(ref Reg.A, Reg.B);
        _fdOpcodes[0x79] = () => Op_DD_LD_r_R(ref Reg.A, Reg.C);
        _fdOpcodes[0x7A] = () => Op_DD_LD_r_R(ref Reg.A, Reg.D);
        _fdOpcodes[0x7B] = () => Op_DD_LD_r_R(ref Reg.A, Reg.E);
        _fdOpcodes[0x7C] = () => Op_LD_r_IYH(ref Reg.A);
        _fdOpcodes[0x7D] = () => Op_LD_r_IYL(ref Reg.A);
        _fdOpcodes[0x7E] = () => Op_LD_r_ptrIYd(ref Reg.A);
        _fdOpcodes[0x7F] = () => Op_DD_LD_r_R(ref Reg.A, Reg.A);
        _fdOpcodes[0x80] = () => Op_DD_ADD_A(Reg.B);
        _fdOpcodes[0x81] = () => Op_DD_ADD_A(Reg.C);
        _fdOpcodes[0x82] = () => Op_DD_ADD_A(Reg.D);
        _fdOpcodes[0x83] = () => Op_DD_ADD_A(Reg.E);
        _fdOpcodes[0x84] = () => Op_DD_ADD_A(Reg.IYH);
        _fdOpcodes[0x85] = () => Op_DD_ADD_A(Reg.IYL);
        _fdOpcodes[0x86] = Op_ADD_A_ptrIYd;
        _fdOpcodes[0x87] = () => Op_DD_ADD_A(Reg.A);
        _fdOpcodes[0x88] = () => Op_DD_ADC_A(Reg.B);
        _fdOpcodes[0x89] = () => Op_DD_ADC_A(Reg.C);
        _fdOpcodes[0x8A] = () => Op_DD_ADC_A(Reg.D);
        _fdOpcodes[0x8B] = () => Op_DD_ADC_A(Reg.E);
        _fdOpcodes[0x8C] = () => Op_DD_ADC_A(Reg.IYH);
        _fdOpcodes[0x8D] = () => Op_DD_ADC_A(Reg.IYL);
        _fdOpcodes[0x8E] = Op_ADC_A_ptrIYd;
        _fdOpcodes[0x8F] = () => Op_DD_ADC_A(Reg.A);
        _fdOpcodes[0x90] = () => Op_DD_SUB(Reg.B);
        _fdOpcodes[0x91] = () => Op_DD_SUB(Reg.C);
        _fdOpcodes[0x92] = () => Op_DD_SUB(Reg.D);
        _fdOpcodes[0x93] = () => Op_DD_SUB(Reg.E);
        _fdOpcodes[0x94] = () => Op_DD_SUB(Reg.IYH);
        _fdOpcodes[0x95] = () => Op_DD_SUB(Reg.IYL);
        _fdOpcodes[0x96] = Op_SUB_ptrIYd;
        _fdOpcodes[0x97] = () => Op_DD_SUB(Reg.A);
        _fdOpcodes[0x98] = () => Op_DD_SBC(Reg.B);
        _fdOpcodes[0x99] = () => Op_DD_SBC(Reg.C);
        _fdOpcodes[0x9A] = () => Op_DD_SBC(Reg.D);
        _fdOpcodes[0x9B] = () => Op_DD_SBC(Reg.E);
        _fdOpcodes[0x9C] = () => Op_DD_SBC(Reg.IYH);
        _fdOpcodes[0x9D] = () => Op_DD_SBC(Reg.IYL);
        _fdOpcodes[0x9E] = Op_SBC_A_ptrIYd;
        _fdOpcodes[0x9F] = () => Op_DD_SBC(Reg.A);
        _fdOpcodes[0xA0] = () => Op_DD_AND(Reg.B);
        _fdOpcodes[0xA1] = () => Op_DD_AND(Reg.C);
        _fdOpcodes[0xA2] = () => Op_DD_AND(Reg.D);
        _fdOpcodes[0xA3] = () => Op_DD_AND(Reg.E);
        _fdOpcodes[0xA4] = () => Op_DD_AND(Reg.IYH);
        _fdOpcodes[0xA5] = () => Op_DD_AND(Reg.IYL);
        _fdOpcodes[0xA6] = Op_AND_ptrIYd;
        _fdOpcodes[0xA7] = () => Op_DD_AND(Reg.A);
        _fdOpcodes[0xA8] = () => Op_DD_XOR(Reg.B);
        _fdOpcodes[0xA9] = () => Op_DD_XOR(Reg.C);
        _fdOpcodes[0xAA] = () => Op_DD_XOR(Reg.D);
        _fdOpcodes[0xAB] = () => Op_DD_XOR(Reg.E);
        _fdOpcodes[0xAC] = () => Op_DD_XOR(Reg.IYH);
        _fdOpcodes[0xAD] = () => Op_DD_XOR(Reg.IYL);
        _fdOpcodes[0xAE] = Op_XOR_A_ptrIYd;
        _fdOpcodes[0xAF] = () => Op_DD_XOR(Reg.A);
        _fdOpcodes[0xB0] = () => Op_DD_OR(Reg.B);
        _fdOpcodes[0xB1] = () => Op_DD_OR(Reg.C);
        _fdOpcodes[0xB2] = () => Op_DD_OR(Reg.D);
        _fdOpcodes[0xB3] = () => Op_DD_OR(Reg.E);
        _fdOpcodes[0xB4] = () => Op_DD_OR(Reg.IYH);
        _fdOpcodes[0xB5] = () => Op_DD_OR(Reg.IYL);
        _fdOpcodes[0xB6] = Op_OR_ptrIYd;
        _fdOpcodes[0xB7] = () => Op_DD_OR(Reg.A);
        _fdOpcodes[0xB8] = () => Op_DD_CP(Reg.B);
        _fdOpcodes[0xB9] = () => Op_DD_CP(Reg.C);
        _fdOpcodes[0xBA] = () => Op_DD_CP(Reg.D);
        _fdOpcodes[0xBB] = () => Op_DD_CP(Reg.E);
        _fdOpcodes[0xBC] = () => Op_DD_CP(Reg.IYH);
        _fdOpcodes[0xBD] = () => Op_DD_CP(Reg.IYL);
        _fdOpcodes[0xBE] = OP_CP_ptrIYd;
        _fdOpcodes[0xBF] = () => Op_DD_CP(Reg.A);
        _fdOpcodes[0xE1] = Op_POP_IY;
        _fdOpcodes[0xE3] = Op_EX_ptrSP_IY;
        _fdOpcodes[0xE5] = Op_PUSH_IY;
        _fdOpcodes[0xE9] = Op_JP_ptrIY;
        _fdOpcodes[0xF9] = Op_LD_SP_IY;

        _cbOpcodes[0x00] = () => Op_RLC(ref Reg.B);
        _cbOpcodes[0x01] = () => Op_RLC(ref Reg.C);
        _cbOpcodes[0x02] = () => Op_RLC(ref Reg.D);
        _cbOpcodes[0x03] = () => Op_RLC(ref Reg.E);
        _cbOpcodes[0x04] = () => Op_RLC(ref Reg.H);
        _cbOpcodes[0x05] = () => Op_RLC(ref Reg.L);
        _cbOpcodes[0x06] = Op_RLC_ptrHL;
        _cbOpcodes[0x07] = () => Op_RLC(ref Reg.A);
        _cbOpcodes[0x08] = () => Op_RRC(ref Reg.B);
        _cbOpcodes[0x09] = () => Op_RRC(ref Reg.C);
        _cbOpcodes[0x0A] = () => Op_RRC(ref Reg.D);
        _cbOpcodes[0x0B] = () => Op_RRC(ref Reg.E);
        _cbOpcodes[0x0C] = () => Op_RRC(ref Reg.H);
        _cbOpcodes[0x0D] = () => Op_RRC(ref Reg.L);
        _cbOpcodes[0x0E] = Op_RRC_ptrHL;
        _cbOpcodes[0x0F] = () => Op_RRC(ref Reg.A);
        _cbOpcodes[0x10] = () => Op_RL(ref Reg.B);
        _cbOpcodes[0x11] = () => Op_RL(ref Reg.C);
        _cbOpcodes[0x12] = () => Op_RL(ref Reg.D);
        _cbOpcodes[0x13] = () => Op_RL(ref Reg.E);
        _cbOpcodes[0x14] = () => Op_RL(ref Reg.H);
        _cbOpcodes[0x15] = () => Op_RL(ref Reg.L);
        _cbOpcodes[0x16] = Op_RL_ptrHL;
        _cbOpcodes[0x17] = () => Op_RL(ref Reg.A);
        _cbOpcodes[0x18] = () => Op_RR(ref Reg.B);
        _cbOpcodes[0x19] = () => Op_RR(ref Reg.C);
        _cbOpcodes[0x1A] = () => Op_RR(ref Reg.D);
        _cbOpcodes[0x1B] = () => Op_RR(ref Reg.E);
        _cbOpcodes[0x1C] = () => Op_RR(ref Reg.H);
        _cbOpcodes[0x1D] = () => Op_RR(ref Reg.L);
        _cbOpcodes[0x1E] = Op_RR_ptrHL;
        _cbOpcodes[0x1F] = () => Op_RR(ref Reg.A);
        _cbOpcodes[0x20] = () => Op_SLA(ref Reg.B);
        _cbOpcodes[0x21] = () => Op_SLA(ref Reg.C);
        _cbOpcodes[0x22] = () => Op_SLA(ref Reg.D);
        _cbOpcodes[0x23] = () => Op_SLA(ref Reg.E);
        _cbOpcodes[0x24] = () => Op_SLA(ref Reg.H);
        _cbOpcodes[0x25] = () => Op_SLA(ref Reg.L);
        _cbOpcodes[0x26] = Op_SLA_ptrHL;
        _cbOpcodes[0x27] = () => Op_SLA(ref Reg.A);
        _cbOpcodes[0x28] = () => Op_SRA(ref Reg.B);
        _cbOpcodes[0x29] = () => Op_SRA(ref Reg.C);
        _cbOpcodes[0x2A] = () => Op_SRA(ref Reg.D);
        _cbOpcodes[0x2B] = () => Op_SRA(ref Reg.E);
        _cbOpcodes[0x2C] = () => Op_SRA(ref Reg.H);
        _cbOpcodes[0x2D] = () => Op_SRA(ref Reg.L);
        _cbOpcodes[0x2E] = Op_SRA_ptrHL;
        _cbOpcodes[0x2F] = () => Op_SRA(ref Reg.A);
        _cbOpcodes[0x30] = () => Op_SLL(ref Reg.B);
        _cbOpcodes[0x31] = () => Op_SLL(ref Reg.C);
        _cbOpcodes[0x32] = () => Op_SLL(ref Reg.D);
        _cbOpcodes[0x33] = () => Op_SLL(ref Reg.E);
        _cbOpcodes[0x34] = () => Op_SLL(ref Reg.H);
        _cbOpcodes[0x35] = () => Op_SLL(ref Reg.L);
        _cbOpcodes[0x36] = Op_SLL_ptrHL;
        _cbOpcodes[0x37] = () => Op_SLL(ref Reg.A);
        _cbOpcodes[0x38] = () => Op_SRL(ref Reg.B);
        _cbOpcodes[0x39] = () => Op_SRL(ref Reg.C);
        _cbOpcodes[0x3A] = () => Op_SRL(ref Reg.D);
        _cbOpcodes[0x3B] = () => Op_SRL(ref Reg.E);
        _cbOpcodes[0x3C] = () => Op_SRL(ref Reg.H);
        _cbOpcodes[0x3D] = () => Op_SRL(ref Reg.L);
        _cbOpcodes[0x3E] = Op_SRL_ptrHL;
        _cbOpcodes[0x3F] = () => Op_SRL(ref Reg.A);
        _cbOpcodes[0x7E] = Op_BIT_7_ptrHL;
        _cbOpcodes[0xF8] = () => Op_SET_7(ref Reg.B);
        _cbOpcodes[0xF9] = () => Op_SET_7(ref Reg.C);
        _cbOpcodes[0xFA] = () => Op_SET_7(ref Reg.D);
        _cbOpcodes[0xFB] = () => Op_SET_7(ref Reg.E);
        _cbOpcodes[0xFC] = () => Op_SET_7(ref Reg.H);
        _cbOpcodes[0xFD] = () => Op_SET_7(ref Reg.L);
        _cbOpcodes[0xFF] = () => Op_SET_7(ref Reg.A);
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
        Reg.PC = 0x0000;
        Reg.AF = Reg.BC = Reg.DE = 0;
        Reg.HL = 0x0000;
        Reg.IX = Reg.IY = 0xFFFF;
        Reg.F = 0x00;
        Reg.I = Reg.R = 0;
        Reg.SP = 0x0000;
        _iff1 = false;
        _halted = false;
        EI_Pending = false;
        EI_EnableAfterInstruction = false;
        _interruptMode = 0;
        InterruptPending = false;
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
    private static void WriteFlag(Flags f, bool value)
    {
        if (value) Reg.F |= (byte)f;
        else Reg.F &= (byte)~f;
    }

    private static string GetFlagDebugValue(Flags f)
    {
        return (Reg.F & (byte)f) != 0 ? f.ToString() : ".";
    }

    private static void SetSZFlags(byte value)
    {
        WriteFlag(Flags.Z, value == 0);
        WriteFlag(Flags.S, (value & 0x80) != 0);    // if bit 7 is not 0, the number is negative
    }
    
    byte ImmediateByte()
    {
        return _machine.ReadByte((ushort)(Reg.PC + 1));
    }
    ushort ImmediateWord(bool prefixed = false)
    {
        if(!prefixed)
            return (ushort)(_machine.ReadByte((ushort)(Reg.PC + 1)) | 
                           (_machine.ReadByte((ushort)(Reg.PC + 2)) << 8));
        else
            return (ushort)(_machine.ReadByte((ushort)(Reg.PC + 2)) | 
                            (_machine.ReadByte((ushort)(Reg.PC + 3)) << 8));
    }
    private byte PeekNextByte() => ImmediateByte();
    
    private ushort IndexAddressingWithDisplacement(ushort indexPair)
    {
        sbyte d = (sbyte)_machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(indexPair + d);

        return addr;
    }
    
    private int Op_UNK()
    {
        throw new NotImplementedException(
            $"Unhandled opcode {_machine.ReadByte(Reg.PC):X2} at {Reg.PC:X4}. Next byte: {_machine.ReadByte((ushort)(Reg.PC+1)):X2}");
    }
    
    
    // Single step test methods
    public void SetInitialCPUState(Z80SingleStepTest test)
    {
        Reg.PC = test.Initial.PC;
        Reg.SP = test.Initial.SP;
        Reg.A = test.Initial.A;
        Reg.B = test.Initial.B;
        Reg.C = test.Initial.C;
        Reg.D = test.Initial.D;
        Reg.E = test.Initial.E;
        Reg.F = test.Initial.F;
        Reg.H = test.Initial.H;
        Reg.L = test.Initial.L;
        Reg.I =  test.Initial.I;
        Reg.R = test.Initial.R;
        //Reg.WZ = test.Initial.WZ;
        Reg.IX = test.Initial.IX;
        Reg.IY = test.Initial.IY;
        Reg.AF2 = test.Initial.AF_;
        Reg.BC2 = test.Initial.BC_;
        Reg.DE2 = test.Initial.DE_;
        Reg.HL2 = test.Initial.HL_;
        _interruptMode = test.Initial.IM;
        _iff1 = test.Initial.IFF1 != 0;
        _iff2 = test.Initial.IFF2 != 0;

        foreach (List<int> entry in test.Initial.RAM)
        {
            _machine.WriteByte((ushort)entry[0], (byte)entry[1]);
        }
    }
    
    public static List<string> CompareStates(CPUState expected, CPUState actual)
    {
        var errors = new List<string>();

        void Check(string name, int exp, int act)
        {
            if (exp != act)
                errors.Add($"{name}: expected {exp:X} got {act:X}");
        }
        
        //Check("F", expected.F, actual.F);
        Check("A", expected.A, actual.A);
        Check("B", expected.B, actual.B);
        Check("C", expected.C, actual.C);
        Check("D", expected.D, actual.D);
        Check("E", expected.E, actual.E);
        Check("H", expected.H, actual.H);
        Check("L", expected.L, actual.L);
        
        Check("I", expected.I, actual.I);
        Check("R", expected.R, actual.R);
        Check("EI", expected.EI, actual.EI);
        Check("Q", expected.Q, actual.Q);
        Check("P", expected.P, actual.P);
        
        Check("AF_", expected.AF_, actual.AF_);
        Check("BC_", expected.BC_, actual.BC_);
        Check("DE_", expected.DE_, actual.DE_);
        Check("HL_", expected.HL_, actual.HL_);
        
        Check("PC", expected.PC, actual.PC);
        Check("SP", expected.SP, actual.SP);
        Check("IX", expected.IX, actual.IX);
        Check("IY", expected.IY, actual.IY);
        //Check("WZ", expected.WZ, actual.WZ);
        
        //Check("IFF1", expected.IFF1, actual.IFF1);
        //Check("IFF2", expected.IFF2, actual.IFF2);
        Check("IM", expected.IM, actual.IM);

        for (int i = 0; i < expected.RAM.Count; i++)
        {
            ushort address = (ushort)expected.RAM[i][0];
            byte expectedValue = (byte)expected.RAM[i][1];
            byte actualValue = (byte)actual.RAM[i][1];

            if (expectedValue != actualValue)
            {
                errors.Add($"RAM mismatch at {address:X4}: expected {expectedValue:X2}, actual {actualValue:X2}");
            }
        }
        
        return errors;
    }

    public CPUState GetActualCPUState(Z80SingleStepTest test)
    {
        CPUState actualCPUState = new CPUState();
        
        actualCPUState.F = Reg.F;
        actualCPUState.A = Reg.A;
        actualCPUState.B = Reg.B;
        actualCPUState.C = Reg.C;
        actualCPUState.D = Reg.D;
        actualCPUState.E = Reg.E;
        actualCPUState.H = Reg.H;
        actualCPUState.L = Reg.L;
        
        actualCPUState.I = Reg.I;
        actualCPUState.R = Reg.R;

        actualCPUState.AF_ = Reg.AF2;
        actualCPUState.BC_ = Reg.BC2;
        actualCPUState.DE_ = Reg.DE2;
        actualCPUState.HL_ = Reg.HL2;
        
        actualCPUState.PC = Reg.PC;
        actualCPUState.SP = Reg.SP;
        actualCPUState.IX = Reg.IX;
        actualCPUState.IY = Reg.IY;
        actualCPUState.IFF1 = (byte)(_iff1 ? 1 : 0);
        actualCPUState.IFF2 = (byte)(_iff2 ? 1 : 0);
        actualCPUState.IM = (byte)_interruptMode;

        actualCPUState.RAM = new List<List<int>>();
        
        foreach (var entry in test.Initial.RAM)
        {
            ushort address = (ushort)entry[0];
            byte value = _machine.ReadByte(address);
            
            actualCPUState.RAM.Add(new List<int> {address, value});
        }
        
        return actualCPUState;
    }
    
    public CPUState GetExpectedCPUState(Z80SingleStepTest test)
    {
        CPUState expectedCPUState = new CPUState();
        
        expectedCPUState.F = test.Final.F;
        expectedCPUState.A = test.Final.A;
        expectedCPUState.B = test.Final.B;
        expectedCPUState.C = test.Final.C;
        expectedCPUState.D = test.Final.D;
        expectedCPUState.E = test.Final.E;
        expectedCPUState.H = test.Final.H;
        expectedCPUState.L = test.Final.L;
        
        expectedCPUState.I = test.Final.I;
        expectedCPUState.R = test.Final.R;

        expectedCPUState.AF_ = test.Final.AF_;
        expectedCPUState.BC_ = test.Final.BC_;
        expectedCPUState.DE_ = test.Final.DE_;
        expectedCPUState.HL_ = test.Final.HL_;
        
        expectedCPUState.PC = test.Final.PC;
        expectedCPUState.SP = test.Final.SP;
        expectedCPUState.IX = test.Final.IX;
        expectedCPUState.IY = test.Final.IY;
        expectedCPUState.IFF1 = test.Final.IFF1;
        expectedCPUState.IFF2 = test.Final.IFF2;
        expectedCPUState.IM = test.Final.IM;

        expectedCPUState.RAM = new List<List<int>>();
        
        foreach (var entry in test.Final.RAM)
        {
            expectedCPUState.RAM.Add(new List<int> {entry[0], entry[1]});
        }
        
        return expectedCPUState;
    }

    public string CheckFinalCPUState(Z80SingleStepTest test, StreamWriter sw)
    {
        CPUState actualCPUState = GetActualCPUState(test);
        CPUState expectedCPUState = GetExpectedCPUState(test);
        
        var errors = CompareStates(expectedCPUState, actualCPUState);
        string failed = "";
        
        if (errors.Count > 0 && !test.Name.StartsWith("DB"))
        {
            failed = test.Name;
            sw.WriteLine($"\nErrors in test {test.Name}:");
            foreach(var e in errors)
                sw.WriteLine($"  {e}");
        }

        return failed;
    }

    private void PrintStepThroughDebug(byte opcode)
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
        Console.WriteLine();
        Console.WriteLine($"Next Opcode: {opcode:X2}");
        switch (Console.ReadKey().Key)
        {
            case ConsoleKey.Escape:
                Environment.Exit(0);
                break;
        }
    }
}