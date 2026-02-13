using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using Microsoft.VisualBasic;
using Reg = Registers;

public class z80Cpu
{
    private Machine machine;
    bool[] parity = new bool[256];
    bool IFF1 = false;
    public bool halted = false;
    public z80Cpu(Machine machine)
    {
        this.machine = machine;
    }

    delegate int OpcodeHandler();
    OpcodeHandler[] mainOpcodes = new OpcodeHandler[256];
    OpcodeHandler[] ddOpcodes = new OpcodeHandler[256];
    OpcodeHandler[] edOpcodes = new OpcodeHandler[256];
    OpcodeHandler[] fdOpcodes = new OpcodeHandler[256];

    // Interrupts
    int InterruptMode;
    public bool InterruptPending;
    public bool EI_Pending = false;
    byte Port0;

    [Flags]
    enum Flags : byte
    {
        C = 1 << 0, N = 1 << 1, PV = 1 << 2, H = 1 << 4, Z = 1 << 6, S = 1 << 7
    }

    public int Step()
    {
        if(InterruptPending && IFF1) // InterruptPending turned on in Game1.cs at the end of a frame, mimicing VBLANK
        {
            // handle interrupt
            Console.WriteLine($"Interrupt clears HALT at PC={Reg.PC:X4}");
            PushWord(Reg.PC);
            InterruptPending = halted = false;
            IFF1 = false;

            switch(InterruptMode)
            {
                case 0:
                    break;
                case 1:
                    Reg.PC = 0x38;
                    break;
                case 2:
                    Reg.PC = machine.ReadWord((ushort)(Reg.I << 8 | Port0));
                    break;
            }
        }

        if (halted)
        {
            return 4; // basically perform NOP with no change to PC
        }

        byte opcode = machine.ReadByte(Reg.PC);
        Console.WriteLine($"0x{Reg.PC:X4} - {opcode:X2}");

        //if(opcode == 0x00)
        //    System.Diagnostics.Debugger.Break();

        int cycles = mainOpcodes[opcode]();

        if(EI_Pending == true)
        {
            EI_Pending = false;
            IFF1 = true;
        }

        return cycles;
    }

    void PushWord(ushort value)
    {
        Reg.SP--;                     // decrement stack pointer
        machine.WriteByte(Reg.SP, Reg.HighByte(value)); // write high byte first
        Reg.SP--;
        machine.WriteByte(Reg.SP, Reg.LowByte(value));  // then low byte
    }

    ushort PopWord()
    {
        byte low = machine.ReadByte(Reg.SP);
        Reg.SP++;
        byte high = machine.ReadByte(Reg.SP);
        Reg.SP++;
        
        ushort value = (ushort)((high << 8) | low);
        return value;
    }

    void BuildOpcodeTable()
    {
        for (int i = 0; i < 256; i++)
        {
            mainOpcodes[i] = Op_UNK;
            ddOpcodes[9] = Op_UNK;
            edOpcodes[i] = Op_UNK;
            fdOpcodes[9] = Op_UNK;
        }

        mainOpcodes[0x00] = Op_NOP;
        mainOpcodes[0x01] = Op_LD_BC_nn;
        mainOpcodes[0x03] = Op_INC_BC;
        mainOpcodes[0x04] = Op_INC_B;
        mainOpcodes[0x06] = Op_LD_B_n;
        mainOpcodes[0x07] = Op_RLCA;
        mainOpcodes[0x0B] = Op_DEC_BC;
        mainOpcodes[0x0C] = Op_INC_C;
        mainOpcodes[0x0D] = Op_DEC_C;
        mainOpcodes[0x0E] = Op_LD_C_n;
        mainOpcodes[0x0F] = Op_RRCA;

        mainOpcodes[0x10] = Op_DJNZ_e;
        mainOpcodes[0x11] = Op_LD_ptrDE_nn;
        mainOpcodes[0x12] = Op_LD_DE_A;
        mainOpcodes[0x13] = Op_INC_DE;
        mainOpcodes[0x14] = Op_INC_D;
        mainOpcodes[0x16] = Op_LD_D_n;
        mainOpcodes[0x18] = Op_JR_e;
        mainOpcodes[0x19] = Op_ADD_HL_DE;
        mainOpcodes[0x1A] = Op_LD_A_ptrDE;
        mainOpcodes[0x1C] = Op_INC_E;
        mainOpcodes[0x1E] = Op_LD_E_n;

        mainOpcodes[0x20] = Op_JR_NZ_e;
        mainOpcodes[0x21] = Op_LD_HL_nn;
        mainOpcodes[0x22] = Op_LD_nn_ptrHL;
        mainOpcodes[0x23] = Op_INC_HL;
        mainOpcodes[0x24] = Op_INC_H;
        mainOpcodes[0x26] = Op_LD_H_n;
        mainOpcodes[0x28] = Op_JR_Z_e;
        mainOpcodes[0x2A] = Op_LD_HL_ptrnn;
        mainOpcodes[0x2C] = Op_INC_L;
        mainOpcodes[0x2D] = Op_DEC_L;
        mainOpcodes[0x2E] = Op_LD_L_n;

        mainOpcodes[0x30] = Op_JR_NC_e;
        mainOpcodes[0x31] = Op_LD_SP_nn;
        mainOpcodes[0x32] = Op_LD_ptrnn_A;
        mainOpcodes[0x33] = Op_INC_SP;
        mainOpcodes[0x34] = Op_INC_ptrHL;
        mainOpcodes[0x35] = Op_DEC_ptrHL;
        mainOpcodes[0x36] = Op_LD_ptrHL_n;
        mainOpcodes[0x38] = Op_JR_C_e;
        mainOpcodes[0x3A] = Op_LD_A_ptrNN;
        mainOpcodes[0x3C] = Op_INC_A;
        mainOpcodes[0x3D] = Op_DEC_A;
        mainOpcodes[0x3E] = Op_LD_A_n;
        mainOpcodes[0x3F] = Op_CCF;

        mainOpcodes[0x40] = Op_LD_B_B;
        mainOpcodes[0x41] = Op_LD_B_C;
        mainOpcodes[0x42] = Op_LD_B_D;
        mainOpcodes[0x43] = Op_LD_B_E;
        mainOpcodes[0x44] = Op_LD_B_H;
        mainOpcodes[0x45] = Op_LD_B_L;
        mainOpcodes[0x46] = Op_LD_B_ptrHL;
        mainOpcodes[0x47] = Op_LD_B_A;
        mainOpcodes[0x48] = Op_LD_C_B;
        mainOpcodes[0x49] = Op_LD_C_C;
        mainOpcodes[0x4A] = Op_LD_C_D;
        mainOpcodes[0x4B] = Op_LD_C_E;
        mainOpcodes[0x4C] = Op_LD_C_H;
        mainOpcodes[0x4D] = Op_LD_C_L;
        mainOpcodes[0x4E] = Op_LD_C_ptrHL;
        mainOpcodes[0x4F] = Op_LD_C_A;

        mainOpcodes[0x50] = Op_LD_D_B;
        mainOpcodes[0x51] = Op_LD_D_C;
        mainOpcodes[0x52] = Op_LD_D_D;
        mainOpcodes[0x53] = Op_LD_D_E;
        mainOpcodes[0x54] = Op_LD_D_H;
        mainOpcodes[0x55] = Op_LD_D_L;
        mainOpcodes[0x56] = Op_LD_D_ptrHL;
        mainOpcodes[0x57] = Op_LD_D_A;
        mainOpcodes[0x58] = Op_LD_E_B;
        mainOpcodes[0x59] = Op_LD_E_C;
        mainOpcodes[0x5A] = Op_LD_E_D;
        mainOpcodes[0x5B] = Op_LD_E_E;
        mainOpcodes[0x5C] = Op_LD_E_H;
        mainOpcodes[0x5D] = Op_LD_E_L;
        mainOpcodes[0x5E] = Op_LD_E_ptrHL;
        mainOpcodes[0x5F] = Op_LD_E_A;

        mainOpcodes[0x60] = Op_LD_H_B;
        mainOpcodes[0x61] = Op_LD_H_C;
        mainOpcodes[0x62] = Op_LD_H_D;
        mainOpcodes[0x63] = Op_LD_H_E;
        mainOpcodes[0x64] = Op_LD_H_H;
        mainOpcodes[0x65] = Op_LD_H_L;
        mainOpcodes[0x66] = Op_LD_H_ptrHL;
        mainOpcodes[0x67] = Op_LD_H_A;
        mainOpcodes[0x68] = Op_LD_L_B;
        mainOpcodes[0x69] = Op_LD_L_C;
        mainOpcodes[0x6A] = Op_LD_L_D;
        mainOpcodes[0x6B] = Op_LD_L_E;
        mainOpcodes[0x6C] = Op_LD_L_H;
        mainOpcodes[0x6D] = Op_LD_L_L;
        mainOpcodes[0x6E] = Op_LD_L_ptrHL;
        mainOpcodes[0x6F] = Op_LD_L_A;

        mainOpcodes[0x70] = Op_LD_ptrHL_B;
        mainOpcodes[0x71] = Op_LD_ptrHL_C;
        mainOpcodes[0x72] = Op_LD_ptrHL_D;
        mainOpcodes[0x73] = Op_LD_ptrHL_E;
        mainOpcodes[0x74] = Op_LD_ptrHL_H;
        mainOpcodes[0x75] = Op_LD_ptrHL_L;
        mainOpcodes[0x76] = Op_HALT;
        mainOpcodes[0x77] = Op_LD_ptrHL_A;
        mainOpcodes[0x78] = Op_LD_A_B;
        mainOpcodes[0x79] = Op_LD_A_C;
        mainOpcodes[0x7A] = Op_LD_A_D;
        mainOpcodes[0x7B] = Op_LD_A_E;
        mainOpcodes[0x7C] = Op_LD_A_H;
        mainOpcodes[0x7D] = Op_LD_A_L;
        mainOpcodes[0x7E] = Op_LD_A_ptrHL;
        mainOpcodes[0x7F] = Op_LD_A_A;

        mainOpcodes[0x80] = Op_ADD_A_B;
        mainOpcodes[0x81] = Op_ADD_A_C;
        mainOpcodes[0x82] = Op_ADD_A_D;
        mainOpcodes[0x83] = Op_ADD_A_E;
        mainOpcodes[0x84] = Op_ADD_A_H;
        mainOpcodes[0x85] = Op_ADD_A_L;
        mainOpcodes[0x86] = Op_ADD_A_ptrHL;
        mainOpcodes[0x87] = Op_ADD_A_A;

        mainOpcodes[0x88] = Op_ADC_A_B;
        mainOpcodes[0x89] = Op_ADC_A_C;
        mainOpcodes[0x8A] = Op_ADC_A_D;
        mainOpcodes[0x8B] = Op_ADC_A_E;
        mainOpcodes[0x8C] = Op_ADC_A_H;
        mainOpcodes[0x8D] = Op_ADC_A_L;
        mainOpcodes[0x8E] = Op_ADC_A_ptrHL;
        mainOpcodes[0x8F] = Op_ADC_A_A;

        mainOpcodes[0x90] = Op_SUB_A_B;
        mainOpcodes[0x91] = Op_SUB_A_C;
        mainOpcodes[0x92] = Op_SUB_A_D;
        mainOpcodes[0x93] = Op_SUB_A_E;
        mainOpcodes[0x94] = Op_SUB_A_H;
        mainOpcodes[0x95] = Op_SUB_A_L;
        mainOpcodes[0x96] = Op_SUB_A_ptrHL;
        mainOpcodes[0x97] = Op_SUB_A_A;

        mainOpcodes[0xA0] = Op_AND_B;
        mainOpcodes[0xA1] = Op_AND_C;
        mainOpcodes[0xA2] = Op_AND_D;
        mainOpcodes[0xA3] = Op_AND_E;
        mainOpcodes[0xA4] = Op_AND_H;
        mainOpcodes[0xA5] = Op_AND_L;
        mainOpcodes[0xA6] = Op_AND_ptrHL;
        mainOpcodes[0xA7] = Op_AND_A;
        mainOpcodes[0xAF] = Op_XOR_A_A;

        mainOpcodes[0xB0] = Op_OR_B;
        mainOpcodes[0xB1] = Op_OR_C;
        mainOpcodes[0xB2] = Op_OR_D;
        mainOpcodes[0xB3] = Op_OR_E;
        mainOpcodes[0xB4] = Op_OR_H;
        mainOpcodes[0xB5] = Op_OR_L;
        mainOpcodes[0xB6] = Op_OR_ptrHL;
        mainOpcodes[0xB7] = Op_OR_A;
        mainOpcodes[0xB9] = Op_CP_C;
        mainOpcodes[0xBE] = Op_CP_ptrHL;

        mainOpcodes[0xC0] = Op_RET_NZ;
        mainOpcodes[0xC1] = Op_POP_BC;
        mainOpcodes[0xC3] = Op_JP_nn;
        mainOpcodes[0xC5] = Op_PUSH_BC;
        mainOpcodes[0xC7] = () => Op_RST(0x00);
        mainOpcodes[0xC8] = Op_RET_Z;
        mainOpcodes[0xC9] = Op_RET;
        mainOpcodes[0xCA] = Op_JP_Z_nn;
        mainOpcodes[0xCD] = Op_CALL_nn;
        mainOpcodes[0xCF] = () => Op_RST(0x08);

        mainOpcodes[0xD0] = Op_RET_NC;
        mainOpcodes[0xD1] = Op_POP_DE;
        mainOpcodes[0xD2] = Op_JP_NC_nn;
        mainOpcodes[0xD3] = Op_OUT_ptrn_A;
        mainOpcodes[0xD5] = Op_PUSH_DE;
        mainOpcodes[0xD7] = () => Op_RST(0x10);
        mainOpcodes[0xDD] = Op_DD;
        mainOpcodes[0xDF] = () => Op_RST(0x18);

        mainOpcodes[0xE1] = Op_POP_HL;
        mainOpcodes[0xE5] = Op_PUSH_HL;
        mainOpcodes[0xE6] = Op_AND_n;
        mainOpcodes[0xE7] = () => Op_RST(0x20);
        mainOpcodes[0xE9] = Op_JP_ptrHL;
        mainOpcodes[0xEB] = Op_EX_DE_HL;
        mainOpcodes[0xED] = Op_ED;
        mainOpcodes[0xEE] = Op_XOR_A_n;
        mainOpcodes[0xEF] = () => Op_RST(0x28);

        mainOpcodes[0xF1] = Op_POP_AF;
        mainOpcodes[0xF3] = Op_DI;
        mainOpcodes[0xF5] = Op_PUSH_AF;
        mainOpcodes[0xF7] = () => Op_RST(0x30);
        mainOpcodes[0xF8] = Op_RET_M;
        mainOpcodes[0xFA] = Op_JP_M_nn;
        mainOpcodes[0xFB] = Op_EI;
        mainOpcodes[0xFC] = Op_CALL_M_nn;
        mainOpcodes[0xFD] = Op_FD;
        mainOpcodes[0xFE] = Op_CP_n;
        mainOpcodes[0xFF] = () => Op_RST(0x38);

        ddOpcodes[0x19] = Op_ADD_IX_DE;
        ddOpcodes[0x21] = Op_LD_IX_nn;
        ddOpcodes[0x70] = Op_LD_ptrIXd_B;
        ddOpcodes[0x71] = Op_LD_ptrIXd_C;
        ddOpcodes[0x72] = Op_LD_ptrIXd_D;
        ddOpcodes[0x73] = Op_LD_ptrIXd_E;
        ddOpcodes[0x74] = Op_LD_ptrIXd_H;
        ddOpcodes[0x75] = Op_LD_ptrIXd_L;
        ddOpcodes[0x77] = Op_LD_ptrIXd_A;
        ddOpcodes[0x7E] = Op_LD_A_ptrIXd;
        ddOpcodes[0xE1] = Op_POP_IX;

        edOpcodes[0x42] = Op_SBC_HL_BC;
        edOpcodes[0x46] = Op_IM_0;
        edOpcodes[0x47] = Op_LD_I_A;
        edOpcodes[0x52] = Op_SBC_HL_DE;
        edOpcodes[0x56] = Op_IM_1;
        edOpcodes[0x5E] = Op_IM_2;
        edOpcodes[0x62] = Op_SBC_HL_HL;
        edOpcodes[0x72] = Op_SBC_HL_SP;
        edOpcodes[0xB0] = Op_LDIR;

        fdOpcodes[0x21] = Op_LD_IY_nn;
        fdOpcodes[0x6E] = Op_LD_L_ptrIYd;
        fdOpcodes[0xE1] = Op_POP_IY;
    }

    void InitParity()
    {
        for (int i = 0; i < 256; i++)
        {
            int count = 0;
            for (int b = 0; b < 8; b++)
                if ((i & (1 << b)) != 0) count++;

            parity[i] = (count % 2) == 0;
        }
    }

    void SetParity(byte value)
    {
        WriteFlag(Flags.PV, parity[value]);
    }

    void CheckINCOverflow(byte value)
    {
        if (value == 0x80)
            SetFlag(Flags.PV);
        else
            ClearFlag(Flags.PV);
    }

    void CheckDECOverflow(byte value)
    {
        if (value == 0x7F)
            SetFlag(Flags.PV);
        else
            ClearFlag(Flags.PV);
    }

    public void Reset()
    {
        // reset CPU values
        machine.ClearRAM();
        Reg.PC = 0x0000;
        Reg.A = Reg.B = Reg.C = Reg.D = Reg.E = 0;
        Reg.HL = 0x4000;
        Reg.F = 0;
        Reg.SP = 0xFFFF;
        IFF1 = false;
        halted = false;
        machine.ReadROMsIntoMemory();
        EI_Pending = false;
        InterruptPending = false;
        BuildOpcodeTable();
        InitParity();
    }
    

    void SetFlag(Flags f)
    {
        Reg.F |= (byte)f;
    }
    void ClearFlag(Flags f)
    {
        Reg.F &= (byte)~f;
    }
    bool GetFlag(Flags f)
    {
        return (Reg.F & (byte)f) != 0;
    }
    void ToggleFlag(Flags f)
    {
        Reg.F = (byte)(Reg.F ^ (byte)f);
    }
    void WriteFlag(Flags f, bool value)
    {
        if (value) SetFlag(f);
        else ClearFlag(f);
    }

    void SetSZFlags(byte value)
    {
        WriteFlag(Flags.Z, value == 0);
        WriteFlag(Flags.S, (value & 0x80) != 0);
    }
    
    byte ReadImmediate() => machine.ReadByte((ushort)(Reg.PC + 1));
    sbyte ReadSignedOffset() => (sbyte)ReadImmediate(); // needs to be sbyte to properly handled sign, allow for backward jumps
    
    static ushort ReadWordLE(Machine machine, ushort addr)
    {
        // read word (little-endian direction)
        byte low = machine.ReadByte(addr);
        byte high = machine.ReadByte((ushort)(addr + 1));
        return (ushort)((high << 8) | low);
    }

    static void WriteWordLE(Machine machine, ushort addr, ushort value)
    {
        machine.WriteByte(addr, (byte)(value & 0xFF));
        machine.WriteByte((ushort)(addr + 1), (byte)(value >> 8));
    }

    int AND(byte value, int cycles)
    {
        Reg.A &= value;

        ClearFlag(Flags.C | Flags.N);
        SetFlag(Flags.H);       
        SetSZFlags(Reg.A);
        SetParity(Reg.A);
        Reg.PC += 1;        
        return cycles;
    }

    int OR(byte value, int cycles)
    {
        Reg.A |= value;

        ClearFlag(Flags.C | Flags.N | Flags.H);
        SetSZFlags(Reg.A);
        SetParity(Reg.A);
        Reg.PC += 1;
        return cycles;
    }


    // OPCODES
    int Op_UNK()
    {
        throw new NotImplementedException(
            $"Unhandled opcode {machine.ReadByte(Reg.PC):X2} at {Reg.PC:X4}"
        );
    }

    int Op_ED()
    {
        byte opcode = ReadImmediate();
        // prefix opcodes increment Reg.PC by 1, remaining bytes should be incremented in suffix opcode
        Reg.PC++;
        return edOpcodes[opcode]();
    }

    int Op_DD()
    {
        byte opcode = ReadImmediate();
        // prefix opcodes increment Reg.PC by 1, remaining bytes should be incremented in suffix opcode
        Reg.PC++;
        return ddOpcodes[opcode]();
    }

    int Op_FD()
    {
        byte opcode = ReadImmediate();

        Reg.PC++;
        return fdOpcodes[opcode]();
    }

    int Op_LDIR()
    {        
        ClearFlag(Flags.N | Flags.H);
        if(Reg.BC == 0)
        {
            Reg.PC += 2;
            return 16;
        }

        machine.WriteByte(Reg.DE, machine.ReadByte(Reg.HL));
        Reg.HL += 1;
        Reg.DE += 1;
        Reg.BC -= 1;
        if(Reg.BC != 0)
        {
            SetFlag(Flags.PV);
        }
        else
        {
            ClearFlag(Flags.PV);
            Reg.PC += 2;
        }

        return (Reg.BC != 0) ? 21 : 16;
    }

    int Op_LD_IX_nn()
    {
        Reg.IX = ReadWordLE(machine, (ushort)(Reg.PC + 1));
        Reg.PC += 3;
        return 14;
    }

    int Op_LD_ptrIXd_B()
    {
        sbyte d = (sbyte)machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        machine.WriteByte(addr, Reg.B);

        Reg.PC += 2;
        return 19;
    }

    int Op_LD_ptrIXd_C()
    {
        sbyte d = (sbyte)machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        machine.WriteByte(addr, Reg.C);

        Reg.PC += 2;
        return 19;
    }

    int Op_LD_ptrIXd_D()
    {
        sbyte d = (sbyte)machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        machine.WriteByte(addr, Reg.D);

        Reg.PC += 2;
        return 19;
    }

    int Op_LD_ptrIXd_E()
    {
        sbyte d = (sbyte)machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        machine.WriteByte(addr, Reg.E);

        Reg.PC += 2;
        return 19;
    }

    int Op_LD_ptrIXd_H()
    {
        sbyte d = (sbyte)machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        machine.WriteByte(addr, Reg.H);

        Reg.PC += 2;
        return 19;
    }

    int Op_LD_ptrIXd_L()
    {
        sbyte d = (sbyte)machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        machine.WriteByte(addr, Reg.L);

        Reg.PC += 2;
        return 19;
    }

    int Op_LD_ptrIXd_A()
    {
        sbyte d = (sbyte)machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        machine.WriteByte(addr, Reg.A);

        Reg.PC += 2;
        return 19;
    }

    int Op_LD_A_ptrIXd()
    {
        sbyte d = (sbyte)machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        Reg.A = machine.ReadByte(addr);
        
        Reg.PC += 2;
        return 19;
    }

    byte ADD(byte acc, byte value)
    {
        ushort sum = (ushort)(acc + value);
        byte result = (byte)sum;

        WriteFlag(Flags.C, sum > 0xFF);
        WriteFlag(Flags.H, ((acc & 0x0F) + (value & 0x0F)) > 0x0F);                
        ClearFlag(Flags.N);        
        SetSZFlags(result);
        WriteFlag(Flags.PV, ((acc ^ result) & (value ^ result) & 0x80) != 0);

        return (byte)sum;
    }

    ushort ADDWord(ushort acc, ushort value)
    {
        uint sum = (uint)(acc + value);

        WriteFlag(Flags.C, sum > 0xFFFF);
        WriteFlag(Flags.H, ((acc & 0x0FFF) + (value & 0x0FFF)) > 0x0FFF);                
        ClearFlag(Flags.N);

        return (ushort)sum;
    }

    byte SUB(byte acc, byte value)
    {
        byte result = (byte)(acc - value);

        WriteFlag(Flags.C, acc < value);
        WriteFlag(Flags.H, (acc & 0x0F) < (value & 0x0F));
        SetFlag(Flags.N); 
        SetSZFlags(result);
        WriteFlag(Flags.PV, ((acc ^ value) & (acc ^ result) & 0x80) != 0);

        return (byte)result;
    }

    byte ADC(byte acc, byte value)
    {
        int carry = GetFlag(Flags.C) ? 1 : 0;
        ushort sum = (ushort)(acc + value + carry);
        byte result = (byte)sum;

        WriteFlag(Flags.C, sum > 0xFF);
        WriteFlag(Flags.H, ((acc & 0x0F) + (value & 0x0F) + carry) > 0x0F);                
        ClearFlag(Flags.N);        
        SetSZFlags(result);
        WriteFlag(Flags.PV, ((acc ^ result) & (value ^ result) & 0x80) != 0);

        return (byte)sum;        
    }

    byte SBC(byte acc, byte value)
    {
        int carry = GetFlag(Flags.C) ? 1 : 0;
        byte result = (byte)(acc - value - carry);

        WriteFlag(Flags.C, acc < (value + carry));
        WriteFlag(Flags.H, (acc & 0x0F) < ((value & 0x0F) + carry));
        SetFlag(Flags.N); 
        SetSZFlags(result);
        WriteFlag(Flags.PV, ((acc ^ value) & (acc ^ result) & 0x80) != 0);

        return (byte)result;
    }

    ushort SBCWord(ushort acc, ushort value)
    {
        int carry = GetFlag(Flags.C) ? 1 : 0;
        ushort result = (ushort)(acc - value - carry);

        WriteFlag(Flags.C, acc < (value + carry));
        WriteFlag(Flags.H, (acc & 0x0FFF) < ((value & 0x0FFF) + carry));
        SetFlag(Flags.N);

        // 16-bit signed overflow detection
        bool overflow = ((acc ^ value) & (acc ^ result) & 0x8000) != 0;
        WriteFlag(Flags.PV, overflow);

        // S/Z for 16-bit
        WriteFlag(Flags.S, (result & 0x8000) != 0);
        WriteFlag(Flags.Z, result == 0);

        return (ushort)result;
    }

    int Op_CP_n()
    {
        byte n = ReadImmediate();
        byte result = (byte)(Reg.A - n);        

        WriteFlag(Flags.C, Reg.A < n);
        WriteFlag(Flags.H, (Reg.A & 0x0F) < (n & 0x0F));
        SetFlag(Flags.N);
        SetSZFlags(result);
        WriteFlag(Flags.PV, ((Reg.A ^ n) & (Reg.A ^ result) & 0x80) != 0);

        Reg.PC += 2;
        return 7;
    }

    int Op_CP_C()
    {        
        byte result = (byte)(Reg.A - Reg.C);        

        WriteFlag(Flags.C, Reg.A < Reg.C);
        WriteFlag(Flags.H, (Reg.A & 0x0F) < (Reg.C & 0x0F));
        SetFlag(Flags.N);
        SetSZFlags(result);
        WriteFlag(Flags.PV, ((Reg.A ^ Reg.C) & (Reg.A ^ result) & 0x80) != 0);

        Reg.PC += 1;
        return 4;
    }

    int Op_CP_ptrHL()
    {
        byte value = machine.ReadByte(Reg.HL);
        byte result = (byte)(Reg.A - value);        

        WriteFlag(Flags.C, Reg.A < value);
        WriteFlag(Flags.H, (Reg.A & 0x0F) < (value & 0x0F));
        SetFlag(Flags.N);
        SetSZFlags(result);
        WriteFlag(Flags.PV, ((Reg.A ^ value) & (Reg.A ^ result) & 0x80) != 0);

        Reg.PC += 1;
        return 7;
    }

    int JR_Cond(Func<bool> condition)
    {
        sbyte offset = (sbyte)ReadImmediate();
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

    int Op_ADD_A_A() { Reg.A = ADD(Reg.A, Reg.A); Reg.PC += 1; return 4; }
    int Op_ADD_A_B() { Reg.A = ADD(Reg.A, Reg.B); Reg.PC += 1; return 4; }
    int Op_ADD_A_C() { Reg.A = ADD(Reg.A, Reg.C); Reg.PC += 1; return 4; }
    int Op_ADD_A_D() { Reg.A = ADD(Reg.A, Reg.D); Reg.PC += 1; return 4; }
    int Op_ADD_A_E() { Reg.A = ADD(Reg.A, Reg.E); Reg.PC += 1; return 4; }
    int Op_ADD_A_H() { Reg.A = ADD(Reg.A, Reg.H); Reg.PC += 1; return 4; }
    int Op_ADD_A_L() { Reg.A = ADD(Reg.A, Reg.L); Reg.PC += 1; return 4; }
    int Op_ADD_A_ptrHL() { Reg.A = ADD(Reg.A, machine.ReadByte(Reg.HL)); Reg.PC += 1; return 7; }
    int Op_ADD_IX_DE() { Reg.IX = ADDWord(Reg.IX, Reg.DE); Reg.PC += 2; return 15; }
    int Op_ADD_HL_DE() { Reg.HL = ADDWord(Reg.HL, Reg.DE); Reg.PC += 1; return 11; }

    int Op_ADC_A_A() { Reg.A = ADC(Reg.A, Reg.A); Reg.PC += 1; return 4; }
    int Op_ADC_A_B() { Reg.A = ADC(Reg.A, Reg.B); Reg.PC += 1; return 4; }
    int Op_ADC_A_C() { Reg.A = ADC(Reg.A, Reg.C); Reg.PC += 1; return 4; }
    int Op_ADC_A_D() { Reg.A = ADC(Reg.A, Reg.D); Reg.PC += 1; return 4; }
    int Op_ADC_A_E() { Reg.A = ADC(Reg.A, Reg.E); Reg.PC += 1; return 4; }
    int Op_ADC_A_H() { Reg.A = ADC(Reg.A, Reg.H); Reg.PC += 1; return 4; }
    int Op_ADC_A_L() { Reg.A = ADC(Reg.A, Reg.L); Reg.PC += 1; return 4; }
    int Op_ADC_A_ptrHL() { Reg.A = ADC(Reg.A, machine.ReadByte(Reg.HL)); Reg.PC += 1; return 7; }

    int Op_SUB_A_A() { Reg.A = SUB(Reg.A, Reg.A); Reg.PC += 1; return 4; }
    int Op_SUB_A_B() { Reg.A = SUB(Reg.A, Reg.B); Reg.PC += 1; return 4; }
    int Op_SUB_A_C() { Reg.A = SUB(Reg.A, Reg.C); Reg.PC += 1; return 4; }
    int Op_SUB_A_D() { Reg.A = SUB(Reg.A, Reg.D); Reg.PC += 1; return 4; }
    int Op_SUB_A_E() { Reg.A = SUB(Reg.A, Reg.E); Reg.PC += 1; return 4; }
    int Op_SUB_A_H() { Reg.A = SUB(Reg.A, Reg.H); Reg.PC += 1; return 4; }
    int Op_SUB_A_L() { Reg.A = SUB(Reg.A, Reg.L); Reg.PC += 1; return 4; }
    int Op_SUB_A_ptrHL() { Reg.A = SUB(Reg.A, machine.ReadByte(Reg.HL)); Reg.PC += 1; return 7; }

    int Op_SBC_A_A() { Reg.A = SBC(Reg.A, Reg.A); Reg.PC += 1; return 4; }
    int Op_SBC_A_B() { Reg.A = SBC(Reg.A, Reg.B); Reg.PC += 1; return 4; }
    int Op_SBC_A_C() { Reg.A = SBC(Reg.A, Reg.C); Reg.PC += 1; return 4; }
    int Op_SBC_A_D() { Reg.A = SBC(Reg.A, Reg.D); Reg.PC += 1; return 4; }
    int Op_SBC_A_E() { Reg.A = SBC(Reg.A, Reg.E); Reg.PC += 1; return 4; }
    int Op_SBC_A_H() { Reg.A = SBC(Reg.A, Reg.H); Reg.PC += 1; return 4; }
    int Op_SBC_A_L() { Reg.A = SBC(Reg.A, Reg.L); Reg.PC += 1; return 4; }
    int Op_SBC_A_ptrHL() { Reg.A = SBC(Reg.A, machine.ReadByte(Reg.HL)); Reg.PC += 1; return 7; }

    int Op_SBC_HL_BC() { Reg.HL = SBCWord(Reg.HL, Reg.BC); Reg.PC += 2; return 15; }
    int Op_SBC_HL_DE() { Reg.HL = SBCWord(Reg.HL, Reg.DE); Reg.PC += 2; return 15; }
    int Op_SBC_HL_HL() { Reg.HL = SBCWord(Reg.HL, Reg.HL); Reg.PC += 2; return 15; }
    int Op_SBC_HL_SP() { Reg.HL = SBCWord(Reg.HL, Reg.SP); Reg.PC += 2; return 15; }

    int Op_NOP() { Reg.PC += 1; return 4; }

    int Op_LD_A_A() { Reg.PC += 2; return 4; }
    int Op_LD_A_B() { Reg.A = Reg.B; Reg.PC += 1; return 4; }
    int Op_LD_A_C() { Reg.A = Reg.C; Reg.PC += 1; return 4; }
    int Op_LD_A_D() { Reg.A = Reg.D; Reg.PC += 1; return 4; }
    int Op_LD_A_E() { Reg.A = Reg.E; Reg.PC += 1; return 4; }
    int Op_LD_A_H() { Reg.A = Reg.H; Reg.PC += 1; return 4; }
    int Op_LD_A_L() { Reg.A = Reg.L; Reg.PC += 1; return 4; }
    int Op_LD_A_n() { Reg.A = ReadImmediate(); Reg.PC += 2; return 7; }

    int Op_LD_B_A() { Reg.B = Reg.A; Reg.PC += 1; return 4; }
    int Op_LD_B_B() { Reg.PC += 1; return 4; }
    int Op_LD_B_C() { Reg.B = Reg.C; Reg.PC += 1; return 4; }
    int Op_LD_B_D() { Reg.B = Reg.D; Reg.PC += 1; return 4; }
    int Op_LD_B_E() { Reg.B = Reg.E; Reg.PC += 1; return 4; }
    int Op_LD_B_H() { Reg.B = Reg.H; Reg.PC += 1; return 4; }
    int Op_LD_B_L() { Reg.B = Reg.L; Reg.PC += 1; return 4; }
    int Op_LD_B_n() { Reg.B = ReadImmediate(); Reg.PC += 2; return 7; }

    int Op_LD_C_A() { Reg.C = Reg.A; Reg.PC += 1; return 4; }
    int Op_LD_C_B() { Reg.C = Reg.B; Reg.PC += 1; return 4; }
    int Op_LD_C_C() { Reg.PC += 1; return 4; }
    int Op_LD_C_D() { Reg.C = Reg.D; Reg.PC += 1; return 4; }
    int Op_LD_C_E() { Reg.C = Reg.E; Reg.PC += 1; return 4; }
    int Op_LD_C_H() { Reg.C = Reg.H; Reg.PC += 1; return 4; }
    int Op_LD_C_L() { Reg.C = Reg.L; Reg.PC += 1; return 4; }
    int Op_LD_C_n() { Reg.C = ReadImmediate(); Reg.PC += 2; return 7; }

    int Op_LD_D_A() { Reg.D = Reg.A; Reg.PC += 1; return 4; }
    int Op_LD_D_B() { Reg.D = Reg.B; Reg.PC += 1; return 4; }
    int Op_LD_D_C() { Reg.D = Reg.C; Reg.PC += 1; return 4; }
    int Op_LD_D_D() { Reg.PC += 1; return 4; }
    int Op_LD_D_E() { Reg.D = Reg.E; Reg.PC += 1; return 4; }
    int Op_LD_D_H() { Reg.D = Reg.H; Reg.PC += 1; return 4; }
    int Op_LD_D_L() { Reg.D = Reg.L; Reg.PC += 1; return 4; }
    int Op_LD_D_n() { Reg.D = ReadImmediate(); Reg.PC += 2; return 7; }

    int Op_LD_E_A() { Reg.E = Reg.A; Reg.PC += 1; return 4; }
    int Op_LD_E_B() { Reg.E = Reg.B; Reg.PC += 1; return 4; }
    int Op_LD_E_C() { Reg.E = Reg.C; Reg.PC += 1; return 4; }
    int Op_LD_E_D() { Reg.E = Reg.D; Reg.PC += 1; return 4; }
    int Op_LD_E_E() { Reg.PC += 1; return 4; }
    int Op_LD_E_H() { Reg.E = Reg.H; Reg.PC += 1; return 4; }
    int Op_LD_E_L() { Reg.E = Reg.L; Reg.PC += 1; return 4; }
    int Op_LD_E_n() { Reg.E = ReadImmediate(); Reg.PC += 2; return 7; }

    int Op_LD_H_A() { Reg.H = Reg.A; Reg.PC += 1; return 4; }
    int Op_LD_H_B() { Reg.H = Reg.B; Reg.PC += 1; return 4; }
    int Op_LD_H_C() { Reg.H = Reg.C; Reg.PC += 1; return 4; }
    int Op_LD_H_D() { Reg.H = Reg.D; Reg.PC += 1; return 4; }
    int Op_LD_H_E() { Reg.H = Reg.E; Reg.PC += 1; return 4; }
    int Op_LD_H_H() { Reg.PC += 1; return 4; }
    int Op_LD_H_L() { Reg.H = Reg.L; Reg.PC += 1; return 4; }
    int Op_LD_H_n() { Reg.H = ReadImmediate(); Reg.PC += 2; return 7; }

    int Op_LD_L_A() { Reg.L = Reg.A; Reg.PC += 1; return 4; }
    int Op_LD_L_B() { Reg.L = Reg.B; Reg.PC += 1; return 4; }
    int Op_LD_L_C() { Reg.L = Reg.C; Reg.PC += 1; return 4; }
    int Op_LD_L_D() { Reg.L = Reg.D; Reg.PC += 1; return 4; }
    int Op_LD_L_E() { Reg.L = Reg.E; Reg.PC += 1; return 4; }
    int Op_LD_L_H() { Reg.L = Reg.H; Reg.PC += 1; return 4; }
    int Op_LD_L_L() { Reg.PC += 1; return 4; }
    int Op_LD_L_n() { Reg.L = ReadImmediate(); Reg.PC += 2; return 7; }

    int Op_LD_I_A() { Reg.I = Reg.A; Reg.PC++; return 9; }    

    int Op_LD_ptrHL_A() { machine.WriteByte(Reg.HL, Reg.A); Reg.PC += 1; return 7; }
    int Op_LD_ptrHL_B() { machine.WriteByte(Reg.HL, Reg.B); Reg.PC += 1; return 7; }
    int Op_LD_ptrHL_C() { machine.WriteByte(Reg.HL, Reg.C); Reg.PC += 1; return 7; }
    int Op_LD_ptrHL_D() { machine.WriteByte(Reg.HL, Reg.D); Reg.PC += 1; return 7; }
    int Op_LD_ptrHL_E() { machine.WriteByte(Reg.HL, Reg.E); Reg.PC += 1; return 7; }    
    int Op_LD_ptrHL_H() { machine.WriteByte(Reg.HL, Reg.H); Reg.PC += 1; return 7; }
    int Op_LD_ptrHL_L() { machine.WriteByte(Reg.HL, Reg.L); Reg.PC += 1; return 7; }

    int Op_LD_A_ptrHL() { Reg.A = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }
    int Op_LD_B_ptrHL() { Reg.B = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }
    int Op_LD_C_ptrHL() { Reg.C = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }
    int Op_LD_D_ptrHL() { Reg.D = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }
    int Op_LD_E_ptrHL() { Reg.E = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }
    int Op_LD_H_ptrHL() { Reg.H = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }
    int Op_LD_L_ptrHL() { Reg.L = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }

    int Op_AND_A() => AND(Reg.A, 4);
    int Op_AND_B() => AND(Reg.B, 4);
    int Op_AND_C() => AND(Reg.C, 4);
    int Op_AND_D() => AND(Reg.D, 4);
    int Op_AND_E() => AND(Reg.E, 4);
    int Op_AND_H() => AND(Reg.H, 4);
    int Op_AND_L() => AND(Reg.L, 4);
    int Op_AND_ptrHL() => AND(machine.ReadByte(Reg.HL), 7);
    int Op_AND_n() => AND(ReadImmediate(), 7);

    int Op_OR_A() => OR(Reg.A, 4);
    int Op_OR_B() => OR(Reg.B, 4);
    int Op_OR_C() => OR(Reg.C, 4);
    int Op_OR_D() => OR(Reg.D, 4);
    int Op_OR_E() => OR(Reg.E, 4);
    int Op_OR_H() => OR(Reg.H, 4);
    int Op_OR_L() => OR(Reg.L, 4);
    int Op_OR_ptrHL() => OR(machine.ReadByte(Reg.HL), 7);

    int Op_INC_BC() { Reg.BC++; Reg.PC += 1; return 6; }
    int Op_INC_DE() { Reg.DE++; Reg.PC += 1; return 6; }
    int Op_INC_HL() { Reg.HL++; Reg.PC += 1; return 6; }
    int Op_INC_SP() { Reg.SP++; Reg.PC += 1; return 6; }

    int Op_POP_BC() { Reg.BC = PopWord(); Reg.PC += 1; return 10; }
    int Op_POP_DE() { Reg.DE = PopWord(); Reg.PC += 1; return 10; }
    int Op_POP_HL() { Reg.HL = PopWord(); Reg.PC += 1; return 10; }
    int Op_POP_AF() { Reg.AF = PopWord(); Reg.F &= 0xD7; Reg.PC += 1; return 10; }
    int Op_POP_IX() { Reg.IX = PopWord(); Reg.PC += 1; return 14; }
    int Op_POP_IY() { Reg.IY = PopWord(); Reg.PC += 1; return 14; }

    int Op_PUSH_BC() { PushWord(Reg.BC); Reg.PC += 1; return 11; }
    int Op_PUSH_DE() { PushWord(Reg.DE); Reg.PC += 1; return 11; }
    int Op_PUSH_HL() { PushWord(Reg.HL); Reg.PC += 1; return 11; }
    int Op_PUSH_AF() { PushWord(Reg.AF); Reg.PC += 1; return 11; }

    int Op_DI() { IFF1 = false; Reg.PC += 1; return 4; }
    int Op_EI() { EI_Pending = true; Reg.PC += 1; return 4; }

    int Op_IM_0() { InterruptMode = 0; Reg.PC++; return 8; }
    int Op_IM_1() { InterruptMode = 1; Reg.PC++; return 8; }
    int Op_IM_2() { InterruptMode = 2; Reg.PC++; return 8; }
    
    int Op_JR_Z_e() => JR_Cond(() => GetFlag(Flags.Z));
    int Op_JR_NZ_e() => JR_Cond(() => !GetFlag(Flags.Z));
    int Op_JR_C_e() => JR_Cond(() => GetFlag(Flags.C));
    int Op_JR_NC_e() => JR_Cond(() => !GetFlag(Flags.C));

    int Op_JR_e()
    {
        sbyte offset = (sbyte)ReadImmediate();
        ushort nextPC = (ushort)(Reg.PC + 2);

        Reg.PC = (ushort)(nextPC + offset);
        return 12;
    }

    int Op_JP_nn()
    {
        // jump to address nn
        ushort address = ReadWordLE(machine, (ushort)(Reg.PC + 1));
        Reg.PC = address;
        return 10;
    }

    int Op_JP_NC_nn()
    {
        if(!GetFlag(Flags.C))
        {
            // jump to address nn
            ushort address = ReadWordLE(machine, (ushort)(Reg.PC + 1));
            Reg.PC = address;
            return 10;            
        }

        Reg.PC += 3;
        return 10;
    }

    int Op_JP_ptrHL()
    {
        // jump to address at location stored HL
        Reg.PC = Reg.HL;
        return 4;
    }

    int Op_LD_A_ptrDE()
    {
        Reg.A = machine.ReadByte((ushort)(Reg.DE));
        Reg.PC += 1;
        return 7;
    }

    int Op_LD_A_ptrNN()
    {
        ushort addr = ReadWordLE(machine, (ushort)(Reg.PC + 1));
        Reg.A = machine.ReadByte(addr);
        Reg.PC += 3;
        return 13;
    }
    
    int Op_LD_HL_nn()
    {
        // load nn into HL
        Reg.HL = ReadWordLE(machine, (ushort)(Reg.PC + 1));
        Reg.PC += 3;
        return 10;
    }    

    int Op_LD_SP_nn()
    {
        // load nn into SP
        Reg.SP = ReadWordLE(machine, (ushort)(Reg.PC + 1));
        Reg.PC += 3;
        return 10;
    }

    int Op_LD_BC_nn()
    {
        // load nn into BC
        Reg.BC = ReadWordLE(machine, (ushort)(Reg.PC + 1));
        Reg.PC += 3;
        return 10;
    }

    int Op_LD_ptrHL_n()
    {
        machine.WriteByte(Reg.HL, ReadImmediate());
        Reg.PC += 2;
        return 10;
    }

    int Op_LD_DE_A()
    {
        // stores the value of A into RAM at address DE
        machine.WriteByte(Reg.DE, Reg.A);
        Reg.PC += 1;
        return 7;
    }

    int Op_XOR_A_A()
    {
        // xor the accumulator with itself
        Reg.A = (byte)(Reg.A ^ Reg.A);
        Reg.PC += 1;

        SetSZFlags(Reg.A);
        ClearFlag(Flags.C | Flags.H | Flags.N);
        SetParity(Reg.A);

        return 4;
    }

    int Op_XOR_A_n()
    {
        // xor the accumulator with itself
        Reg.A = (byte)(Reg.A ^ ReadImmediate());
        Reg.PC += 2;

        SetSZFlags(Reg.A);
        ClearFlag(Flags.C | Flags.H | Flags.N);
        SetParity(Reg.A);

        return 7;
    }

    int Op_INC_A()
    {
        if ((Reg.A & 0x0F) == 0x0F)  // if lower nibble is F, half-carry will occur when adding 1
            SetFlag(Flags.H);
        else
            ClearFlag(Flags.H);

        Reg.A += 1;

        SetSZFlags(Reg.A);
        ClearFlag(Flags.N);
        CheckINCOverflow(Reg.A);
        Reg.PC += 1;

        return 4;
    }

    int Op_INC_B()
    {
        if ((Reg.B & 0x0F) == 0x0F)  // if lower nibble is F, half-carry will occur when adding 1
            SetFlag(Flags.H);
        else
            ClearFlag(Flags.H);

        Reg.B += 1;

        SetSZFlags(Reg.B);
        ClearFlag(Flags.N);
        CheckINCOverflow(Reg.B);
        Reg.PC += 1;

        return 4;
    }

    int Op_INC_C()
    {
        if ((Reg.C & 0x0F) == 0x0F)  // if lower nibble is F, half-carry will occur when adding 1
            SetFlag(Flags.H);
        else
            ClearFlag(Flags.H);

        Reg.C += 1;

        SetSZFlags(Reg.C);
        ClearFlag(Flags.N);
        CheckINCOverflow(Reg.C);
        Reg.PC += 1;

        return 4;
    }

    int Op_INC_D()
    {
        if ((Reg.D & 0x0F) == 0x0F)  // if lower nibble is F, half-carry will occur when adding 1
            SetFlag(Flags.H);
        else
            ClearFlag(Flags.H);

        Reg.D += 1;

        SetSZFlags(Reg.D);
        ClearFlag(Flags.N);
        CheckINCOverflow(Reg.D);
        Reg.PC += 1;

        return 4;
    }

    int Op_INC_E()
    {
        if ((Reg.E & 0x0F) == 0x0F)  // if lower nibble is F, half-carry will occur when adding 1
            SetFlag(Flags.H);
        else
            ClearFlag(Flags.H);

        Reg.E += 1;

        SetSZFlags(Reg.E);
        ClearFlag(Flags.N);
        CheckINCOverflow(Reg.E);
        Reg.PC += 1;

        return 4;
    }

    int Op_INC_H()
    {
        if ((Reg.H & 0x0F) == 0x0F)  // if lower nibble is F, half-carry will occur when adding 1
            SetFlag(Flags.H);
        else
            ClearFlag(Flags.H);

        Reg.H += 1;

        SetSZFlags(Reg.H);
        ClearFlag(Flags.N);
        CheckINCOverflow(Reg.H);
        Reg.PC += 1;

        return 4;
    }

    int Op_INC_L()
    {
        if ((Reg.L & 0x0F) == 0x0F)  // if lower nibble is F, half-carry will occur when adding 1
            SetFlag(Flags.H);
        else
            ClearFlag(Flags.H);

        Reg.L += 1;

        SetSZFlags(Reg.L);
        ClearFlag(Flags.N);
        CheckINCOverflow(Reg.L);
        Reg.PC += 1;

        return 4;
    }

    int Op_INC_ptrHL()
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

    int Op_DEC_L()
    {
        if ((Reg.L & 0x0F) == 0)  // if minuend's lower nibble is 0, borrow will occur when subtracting 1
            SetFlag(Flags.H);
        else
            ClearFlag(Flags.H);

        Reg.L -= 1;

        CheckDECOverflow(Reg.L);
        SetSZFlags(Reg.L);
        SetFlag(Flags.N);
        Reg.PC += 1;

        return 4;
    }

    int Op_DEC_A()
    {
        if ((Reg.A & 0x0F) == 0)  // if minuend's lower nibble is 0, borrow will occur when subtracting 1
            SetFlag(Flags.H);
        else
            ClearFlag(Flags.H);

        Reg.A -= 1;

        CheckDECOverflow(Reg.A);
        SetSZFlags(Reg.A);
        SetFlag(Flags.N);
        Reg.PC += 1;

        return 4;
    }

    int Op_DEC_C()
    {
        if ((Reg.C & 0x0F) == 0)  // if minuend's lower nibble is 0, borrow will occur when subtracting 1
            SetFlag(Flags.H);
        else
            ClearFlag(Flags.H);

        Reg.C -= 1;

        CheckDECOverflow(Reg.C);
        SetSZFlags(Reg.C);
        SetFlag(Flags.N);
        Reg.PC += 1;

        return 4;
    }

    int Op_DEC_ptrHL()
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

    int Op_DJNZ_e()
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

    int Op_LD_ptrnn_A()
    {
        // store value of Accumulator in memory at the location nn
        ushort address = ReadWordLE(machine, (ushort)(Reg.PC + 1));
        machine.WriteByte(address, Reg.A);
        Reg.PC += 3;
        return 13;
    }

    int Op_LD_nn_ptrHL()
    {
        // store value of HL in memory at the location nn
        ushort address = ReadWordLE(machine, (ushort)(Reg.PC + 1));
        machine.WriteByte(address, Reg.L);
        machine.WriteByte((ushort)(address + 1), Reg.H);
        Reg.PC += 3;
        return 16;
    }


    int Op_OUT_ptrn_A()
    {
        byte n = ReadImmediate();
        ushort address = (ushort)((Reg.A << 8) | n);
        Port0 = Reg.A;
        Reg.PC += 2;
        return 11;
    }

    int Op_HALT()
    {
        //Console.WriteLine($"HALT executed at PC={Reg.PC:X4}");
        halted = true;
        //Reg.PC += 1;
        return 4;
    }

    int Op_DEC_BC()
    {
        Reg.BC--;
        Reg.PC += 1;
        return 6;
    }

    int Op_CCF()
    {
        WriteFlag(Flags.H, GetFlag(Flags.C));
        ClearFlag(Flags.N);
        ToggleFlag(Flags.C);

        Reg.PC += 1;
        return 4;
    }

    int Op_RET()
    {
        Reg.PC = PopWord();
        return 10;
    }

    int Op_RET_NZ()
    {
        if(!GetFlag(Flags.Z))
        {            
            Reg.PC = PopWord();
            return 11;
        }       
        Reg.PC += 1; 
        return 5;
    }

    int Op_RET_Z()
    {
        if(GetFlag(Flags.Z))
        {            
            Reg.PC = PopWord();
            return 11;
        }       
        Reg.PC += 1; 
        return 5;
    }

    int Op_RET_M()
    {
        if(GetFlag(Flags.S))
        {            
            Reg.PC = PopWord();
            return 11;
        }       
        Reg.PC += 1; 
        return 5;
    }

    int Op_RET_NC()
    {
        if(!GetFlag(Flags.C))
        {            
            Reg.PC = PopWord();
            return 11;
        }       
        Reg.PC += 1; 
        return 5;
    }

    int Op_LD_ptrDE_nn()
    {
        Reg.DE = ReadWordLE(machine, (ushort)(Reg.PC + 1));
        Reg.PC += 3;
        return 10;
    }

    int Op_RST(ushort vector)
    {
        // vector can be 00, 08, 10, 18, 20, 28, 30, 38
        PushWord((ushort)(Reg.PC + 1));
        Reg.PC = vector;
        return 11;
    }

    int Op_CALL_nn()
    {
        PushWord((ushort)(Reg.PC + 3));
        Reg.PC = ReadWordLE(machine, (ushort)(Reg.PC + 1));
        return 17;
    }

    int Op_CALL_M_nn()
    {
        if(GetFlag(Flags.S))
        {
            PushWord((ushort)(Reg.PC + 3));
            Reg.PC = ReadWordLE(machine, (ushort)(Reg.PC + 1));
            return 17;
        }
        else
        {
            Reg.PC += 3;
            return 10;
        }
    }

    int Op_JP_M_nn()
    {
        if(GetFlag(Flags.S))
        {
            Reg.PC = ReadWordLE(machine, (ushort)(Reg.PC + 1));
        }
        else
        {
            Reg.PC += 3;
        }
        return 10;
    }

    int Op_EX_DE_HL()
    {
        ushort temp = Reg.DE;
        Reg.DE = Reg.HL;
        Reg.HL = temp;

        Reg.PC += 1;
        return 4;
    }

    int Op_LD_HL_ptrnn()
    {
        Reg.HL = machine.ReadByte(ReadWordLE(machine, (ushort)(Reg.PC + 1)));
        Reg.PC += 3;
        return 16;
    }

    int Op_RLCA()
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

    int Op_JP_Z_nn()
    {
        if(GetFlag(Flags.Z))
        {
            Reg.PC = ReadWordLE(machine, (ushort)(Reg.PC + 1));
        }
        Reg.PC += 3;
        return 10;
    }

    int Op_RRCA()
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

    int Op_LD_IY_nn()
    {
        Reg.IY = ReadWordLE(machine, (ushort)(Reg.PC + 1));
        Reg.PC += 4;
        return 14;
    }

    int Op_LD_L_ptrIYd()
    {
        sbyte d = (sbyte)machine.ReadByte((ushort)(Reg.PC + 2));
        ushort addr = (ushort)(Reg.IX + d);
        Reg.L = machine.ReadByte(addr);
        
        Reg.PC += 3;
        return 19;
    }
}