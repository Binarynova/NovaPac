using System;
using Reg = Registers;

public partial class Z80
{
    private int Op_FD_CB()
    {
        IncrementRegisterR();
        sbyte d = (sbyte)_machine.ReadByte((ushort)(Reg.PC + 2));
        byte opcode = _machine.ReadByte((ushort)(Reg.PC + 3));

        ushort addr = (ushort)(Reg.IY + d);
        byte value = _machine.ReadByte(addr);

        int group = opcode >> 6;
        int bit = (opcode >> 3) & 0x07;
        int reg = opcode & 0x07;

        byte result = value;

        switch (group)
        {
            case 0: // Rotate/Shift
            {
                int op = (opcode >> 3) & 0x07;

                switch (op)
                {
                    case 2: // RL
                    {
                        byte oldCarry = (byte)(GetFlag(Flags.C) ? 1 : 0);
                        byte newCarry = (byte)((value & 0x80) >> 7);

                        result = (byte)((value << 1) | oldCarry);

                        WriteFlag(Flags.C, newCarry != 0);
                        ClearFlag(Flags.H | Flags.N);
                        SetSZFlags(result);
                        SetParity(result);

                        _machine.WriteByte(addr, result);

                        if (reg != 6)
                            SetRegisterByIndex(reg, result);

                        break;
                    }

                    // later: RLC, RRC, SLA, SRA, SRL, etc.
        
                    default:
                        throw new NotImplementedException($"FD CB rotate op {op}");
                }

                break;
            }
            
            case 1: // BIT
            {
                bool bitSet = (value & (1 << bit)) != 0;

                WriteFlag(Flags.Z, !bitSet);
                WriteFlag(Flags.H, true);
                WriteFlag(Flags.N, false);
                WriteFlag(Flags.P, !bitSet);

                WriteFlag(Flags.S, (bit == 7) && bitSet);
                
                WriteFlag(Flags.F5, (addr >> 13 & 1) != 0); 
                WriteFlag(Flags.F3, (addr >> 11 & 1) != 0);
                break;
            }

            case 2: // RES
            {
                result = (byte)(value & ~(1 << bit));
                _machine.WriteByte(addr, result);
                int destinationReg = opcode & 0x07;
                if (destinationReg != 6) // 6 is (HL)/(IY+d), which is already handled
                {
                    SetRegisterByIndex(destinationReg, result);
                }
                break;
            }

            case 3: // SET
            {
                result = (byte)(value | (1 << bit));
                _machine.WriteByte(addr, result);
                int destinationReg = opcode & 0x07;
                if (destinationReg != 6) // 6 is (HL)/(IY+d), which is already handled
                {
                    SetRegisterByIndex(destinationReg, result);
                }
                break;
            }

            default:
                throw new NotImplementedException($"FD CB group {group}");
        }

        Reg.PC += 4;
        return 23;
    }
}