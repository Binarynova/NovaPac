using System;
using Reg = Registers;

public partial class Z80Cpu
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
                    case 0: // RLC
                    {
                        byte newCarry = (byte)((value & 0x80) >> 7);

                        result = (byte)((value << 1) | newCarry);

                        WriteFlag(Flags.C, newCarry != 0);
                        ClearFlag(Flags.H | Flags.N);
                        SetSZFlags(result);
                        SetParity(result);

                        _machine.WriteByte(addr, result);

                        if (reg != 6)
                            SetRegisterByIndex(reg, result);

                        break;
                    }
                    
                    case 1: // RRC
                    {
                        byte newCarry = (byte)(value & 0x01);

                        result = (byte)((value >> 1) | (newCarry << 7));

                        WriteFlag(Flags.C, newCarry != 0);
                        ClearFlag(Flags.H | Flags.N);
                        SetSZFlags(result);
                        SetParity(result);

                        _machine.WriteByte(addr, result);

                        if (reg != 6)
                            SetRegisterByIndex(reg, result);

                        break;
                    }
                    
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
                    
                    case 3: // RR
                    {
                        byte oldCarry = (byte)(GetFlag(Flags.C) ? 1 : 0);
                        byte newCarry = (byte)(value & 0x01);

                        result = (byte)((value >> 1) | (oldCarry << 7));

                        WriteFlag(Flags.C, newCarry != 0);
                        ClearFlag(Flags.H | Flags.N);
                        SetSZFlags(result);
                        SetParity(result);

                        _machine.WriteByte(addr, result);

                        if (reg != 6)
                            SetRegisterByIndex(reg, result);

                        break;
                    }
                    
                    case 4: // SLA
                    {
                        byte newCarry = (byte)((value & 0x80) >> 7);

                        result = (byte)(value << 1);

                        WriteFlag(Flags.C, newCarry != 0);
                        ClearFlag(Flags.H | Flags.N);
                        SetSZFlags(result);
                        SetParity(result);

                        _machine.WriteByte(addr, result);

                        if (reg != 6)
                            SetRegisterByIndex(reg, result);

                        break;
                    }
                    
                    case 5: // SRA
                    {
                        byte newCarry = (byte)(value & 0x01);
                        byte prevBit7 = (byte)((value & 0x80) >> 7);

                        result = (byte)((value >> 1) |  (prevBit7 << 7));

                        WriteFlag(Flags.C, newCarry != 0);
                        ClearFlag(Flags.H | Flags.N);
                        SetSZFlags(result);
                        SetParity(result);

                        _machine.WriteByte(addr, result);

                        if (reg != 6)
                            SetRegisterByIndex(reg, result);

                        break;
                    }
                    
                    case 6: // SLL
                    {
                        byte newCarry = (byte)((value & 0x80) >> 7);

                        result = (byte)((value << 1) | 0x1);

                        WriteFlag(Flags.C, newCarry != 0);
                        ClearFlag(Flags.H | Flags.N);
                        SetSZFlags(result);
                        SetParity(result);

                        _machine.WriteByte(addr, result);

                        if (reg != 6)
                            SetRegisterByIndex(reg, result);

                        break;
                    }
                    
                    case 7: // SRL
                    {
                        byte newCarry = (byte)(value & 0x01);

                        result = (byte)(value >> 1);

                        WriteFlag(Flags.C, newCarry != 0);
                        ClearFlag(Flags.H | Flags.N);
                        SetSZFlags(result);
                        SetParity(result);

                        _machine.WriteByte(addr, result);

                        if (reg != 6)
                            SetRegisterByIndex(reg, result);

                        break;
                    }
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
        }

        Reg.PC += 4;
        return 23;
    }
}