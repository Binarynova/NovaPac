using System;
using Reg = Registers;

public partial class Z80
{
    private int Op_DD_CB()
    {
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

                break;
            }

            case 3: // SET
            {
                result = (byte)(value | (1 << bit));
                _machine.WriteByte(addr, result);

                break;
            }

            default:
                throw new NotImplementedException($"DD CB group {group}");
        }

        Reg.PC += 4;
        return 23;
    }
}