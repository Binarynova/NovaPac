using System;
using Reg = Registers;

public partial class Z80
{
    private int Op_JR_e() // Opcode: 18
    {
        sbyte offset = (sbyte)ReadImmediateByte();
        ushort nextPC = (ushort)(Reg.PC + 2);

        Reg.PC = (ushort)(nextPC + offset);
        return 12;
    }

    private int Op_JR_Z_e() => JR_Cond(() => GetFlag(Flags.Z)); // Opcode: 28
    private int Op_JR_NZ_e() => JR_Cond(() => !GetFlag(Flags.Z)); // Opcode: 20
    private int Op_JR_C_e() => JR_Cond(() => GetFlag(Flags.C)); // Opcode: 38
    private int Op_JR_NC_e() => JR_Cond(() => !GetFlag(Flags.C)); // Opcode: 30

    private int Op_JP_nn() // Opcode: C3
    {
        // jump to address nn
        ushort address = ReadImmediateWord();
        Reg.PC = address;
        return 10;
    }

    private int Op_JP_NC_nn() // Opcode: D2
    {
        if (!GetFlag(Flags.C))
        {
            // jump to address nn
            ushort address = ReadImmediateWord();
            Reg.PC = address;
            return 10;
        }

        Reg.PC += 3;
        return 10;
    }

    private int Op_JP_NZ_nn() // Opcode: C2
    {
        if (!GetFlag(Flags.Z))
        {
            // jump to address nn
            ushort address = ReadImmediateWord();
            Reg.PC = address;
            return 10;
        }

        Reg.PC += 3;
        return 10;
    }

    private int Op_JP_M_nn() // Opcode: FA
    {
        if (GetFlag(Flags.S))
        {
            Reg.PC = ReadImmediateWord();
        }
        else
        {
            Reg.PC += 3;
        }

        return 10;
    }

    private int Op_JP_C_nn() // Opcode: D2
    {
        if (GetFlag(Flags.C))
        {
            Reg.PC = ReadImmediateWord();
        }
        else
        {
            Reg.PC += 3;
        }

        return 10;
    }

    private int Op_JP_Z_nn() // Opcode: CA
    {
        if (GetFlag(Flags.Z))
        {
            Reg.PC = ReadImmediateWord();
        }
        else
        {
            Reg.PC += 3;
        }

        return 10;
    }

    private static int Op_JP_ptrHL() // Opcode: E9
    {
        // jump to address at location stored HL
        Reg.PC = Reg.HL;
        return 4;
    }

    private int Op_RET() // Opcode: C9
    {
        Reg.PC = PopWord();
        return 10;
    }

    private int Op_RET_NZ() // Opcode: C0
    {
        if (!GetFlag(Flags.Z))
        {
            Reg.PC = PopWord();
            return 11;
        }

        Reg.PC += 1;
        return 5;
    }

    private int Op_RET_Z() // Opcode: C8
    {
        if (GetFlag(Flags.Z))
        {
            Reg.PC = PopWord();
            return 11;
        }

        Reg.PC += 1;
        return 5;
    }

    private int Op_RET_M() // Opcode: F8
    {
        if (GetFlag(Flags.S))
        {
            Reg.PC = PopWord();
            return 11;
        }

        Reg.PC += 1;
        return 5;
    }

    private int Op_RET_P() // Opcode: F0
    {
        if (!GetFlag(Flags.S))
        {
            Reg.PC = PopWord();
            return 11;
        }

        Reg.PC += 1;
        return 5;
    }

    private int Op_RET_NC() // Opcode: D0
    {
        if (!GetFlag(Flags.C))
        {
            Reg.PC = PopWord();
            return 11;
        }

        Reg.PC += 1;
        return 5;
    }

    private int Op_CALL_nn() // Opcode: CD
    {
        PushWord((ushort)(Reg.PC + 3));
        Reg.PC = ReadImmediateWord();
        return 17;
    }

    private int Op_CALL_M_nn() // Opcode: FB
    {
        if (GetFlag(Flags.S))
        {
            PushWord((ushort)(Reg.PC + 3));
            Reg.PC = ReadImmediateWord();
            return 17;
        }
        else
        {
            Reg.PC += 3;
            return 10;
        }
    }

    private int Op_RST(ushort vector) // Opcodes: C7 D7 E7 F7 CF DF EF FF
    {
        // vector can be 00, 08, 10, 18, 20, 28, 30, 38
        PushWord((ushort)(Reg.PC + 1));
        Reg.PC = vector;
        return 11;
    }

    private int Op_DJNZ_e() // Opcodes: 10
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

    #region Helper Methods

    private int JR_Cond(Func<bool> condition)
    {
        sbyte offset = (sbyte)ReadImmediateByte();
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

    #endregion
}