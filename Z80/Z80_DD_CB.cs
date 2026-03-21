using System;
using Reg = Registers;

public partial class Z80
{
    private int Op_DD_CB()
    {
        IncrementRegisterR();
        sbyte d = (sbyte)_machine.ReadByte((ushort)(Reg.PC + 2));
        byte opcode = _machine.ReadByte((ushort)(Reg.PC + 3));

        ushort addr = (ushort)(Reg.IX + d);
        byte value = _machine.ReadByte(addr);

        int group = opcode >> 6;
        int bit = (opcode >> 3) & 0x07;
        int reg = opcode & 0x07;

        byte result = value;

        switch (group)
        {
            case 1: // BIT
            {
                bool bitSet = (value & (1 << bit)) != 0;

                WriteFlag(Flags.Z, !bitSet);
                WriteFlag(Flags.H, true);
                WriteFlag(Flags.N, false);
                WriteFlag(Flags.P, !bitSet);

                if (bit == 7)
                    WriteFlag(Flags.S, bitSet);
                // else S unchanged

                break;
            }

            case 2: // RES
            {
                result = (byte)(value & ~(1 << bit));
                _machine.WriteByte(addr, result);
                int destinationReg = opcode & 0x07;
                if (destinationReg != 6) // 6 is (HL)/(IX+d), which is already handled
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
                if (destinationReg != 6) // 6 is (HL)/(IX+d), which is already handled
                {
                    SetRegisterByIndex(destinationReg, result);
                }
                break;
            }

            default:
                throw new NotImplementedException($"DD CB group {group}");
        }

        Reg.PC += 4;
        return 23;
    }

    private void SetRegisterByIndex(int index, byte value)
    {
        switch (index)
        {
            case 0: Reg.B = value; break;
            case 1: Reg.C = value; break;
            case 2: Reg.D = value; break;
            case 3: Reg.E = value; break;
            case 4: Reg.H = value; break;
            case 5: Reg.L = value; break;
            case 6: _machine.WriteByte(Reg.HL, value); break; // Memory access
            case 7: Reg.A = value; break;
        }
    }
}