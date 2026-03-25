using System.Xml.Schema;
using Reg = Registers;

public partial class Z80
{
    private int Op_ED() // Opcode: ED
    {
        byte opcode = PeekNextByte();
        IncrementRegisterR();
        return _edOpcodes[opcode]();
    }

    private int Op_NEG() // Opcode: ED 44
    {
        Reg.A = (byte)(0 - Reg.A);
        Reg.PC += 2;
        return 8;
    }

    private int Op_LD_BC_ptrNN() // Opcode: ED 4B
    {
        ushort nn = ImmediateWord(true);

        Reg.C = _machine.ReadByte(nn);
        Reg.B = _machine.ReadByte((ushort)(nn + 1));
        
        Reg.PC += 4;
        return 20;
    }
    
    private int Op_LD_ptrNN_BC() // Opcode: ED 43
    {
        ushort addr = ImmediateWord(true);
        _machine.WriteByte(addr, Reg.C);
        _machine.WriteByte((ushort)(addr + 1), Reg.B);
        Reg.PC += 4;
        return 20;
    }

    private int Op_RETN() // Opcode: ED 45
    {
        _iff1 = _iff2;

        Reg.PC = PopWord();
        return 14;
    }
    
    private int Op_RETI() // Opcode: ED 4D
    {
        Reg.PC = PopWord();

        return 14;
    }
    
    private int Op_LD_ptrNN_DE() // Opcode: ED 53
    {
        ushort addr = ImmediateWord(true);
        _machine.WriteByte(addr, Reg.E);
        _machine.WriteByte((ushort)(addr + 1), Reg.D);
        Reg.PC += 4;
        return 20;
    }
    
    private int Op_LD_ptrNN_SP() // Opcode: ED 73
    {
        ushort addr = ImmediateWord(true);
        
        _machine.WriteByte(addr, (byte)(Reg.SP & 0x00FF));
        _machine.WriteByte((ushort)(addr + 1), (byte)((Reg.SP & 0xFF00) >> 8));
        Reg.PC += 4;
        return 20;
    }
    
    private int Op_LD_DE_ptrNN() // Opcode: ED 5B
    {
        ushort nn = ImmediateWord(true);

        Reg.E = _machine.ReadByte(nn);
        Reg.D = _machine.ReadByte((ushort)(nn + 1));
        
        Reg.PC += 4;
        return 20;
    }
    
    private int Op_LD_SP_ptrNN() // Opcode: ED 7B
    {
        ushort nn = ImmediateWord(true);

        byte low = _machine.ReadByte(nn);
        byte high = _machine.ReadByte((ushort)(nn + 1));
        Reg.SP = (ushort)((high << 8) | low);
        
        Reg.PC += 4;
        return 20;
    }

    private static int Op_SBC_HL_BC() // Opcode: ED 42
    {
        Reg.HL = SBCWord(Reg.HL, Reg.BC);
        Reg.PC += 2;
        return 15;
    }

    private static int Op_SBC_HL_DE() // Opcode: ED 52
    {
        Reg.HL = SBCWord(Reg.HL, Reg.DE);
        Reg.PC += 2;
        return 15;
    }

    private static int Op_SBC_HL_HL() // Opcode: ED 62
    {
        Reg.HL = SBCWord(Reg.HL, Reg.HL);
        Reg.PC += 2;
        return 15;
    }

    private static int Op_SBC_HL_SP() // Opcode: ED 72
    {
        Reg.HL = SBCWord(Reg.HL, Reg.SP);
        Reg.PC += 2;
        return 15;
    }
    
    private int Op_ADC_HL_BC() // Opcode: ED 4A
    {
        ushort val1 = Reg.HL;
        ushort val2 = Reg.BC;
        int carry = GetFlag(Flags.C) ? 1 : 0;
        int result = val1 + val2 + carry;

        // Set Flags
        WriteFlag(Flags.N, false);
        WriteFlag(Flags.C, result > 0xFFFF);
        WriteFlag(Flags.Z, (ushort)result == 0);
        WriteFlag(Flags.S, (result & 0x8000) != 0);
        WriteFlag(Flags.H, ((val1 & 0x0FFF) + (val2 & 0x0FFF) + carry) > 0x0FFF);
        WriteFlag(Flags.P, ((val1 ^ result) & (val2 ^ result) & 0x8000) != 0);
    
        // Undocumented F3/F5 usually mirror bits 13 and 11 of the result
        WriteFlag(Flags.F5, (result & 0x2000) != 0);
        WriteFlag(Flags.F3, (result & 0x0800) != 0);

        Reg.HL = (ushort)result;
        Reg.PC += 2; // ED 4A
        return 15;
    }
    
    private int Op_ADC_HL_DE() // Opcode: ED 5A
    {
        ushort val1 = Reg.HL;
        ushort val2 = Reg.DE;
        int carry = GetFlag(Flags.C) ? 1 : 0;
        int result = val1 + val2 + carry;

        // Set Flags
        WriteFlag(Flags.N, false);
        WriteFlag(Flags.C, result > 0xFFFF);
        WriteFlag(Flags.Z, (ushort)result == 0);
        WriteFlag(Flags.S, (result & 0x8000) != 0);
        WriteFlag(Flags.H, ((val1 & 0x0FFF) + (val2 & 0x0FFF) + carry) > 0x0FFF);
        WriteFlag(Flags.P, ((val1 ^ result) & (val2 ^ result) & 0x8000) != 0);
    
        // Undocumented F3/F5 usually mirror bits 13 and 11 of the result
        WriteFlag(Flags.F5, (result & 0x2000) != 0);
        WriteFlag(Flags.F3, (result & 0x0800) != 0);

        Reg.HL = (ushort)result;
        Reg.PC += 2; // ED 4A
        return 15;
    }
    
    private int Op_ADC_HL_HL() // Opcode: ED 6A
    {
        ushort val1 = Reg.HL;
        ushort val2 = Reg.HL;
        int carry = GetFlag(Flags.C) ? 1 : 0;
        int result = val1 + val2 + carry;

        // Set Flags
        WriteFlag(Flags.N, false);
        WriteFlag(Flags.C, result > 0xFFFF);
        WriteFlag(Flags.Z, (ushort)result == 0);
        WriteFlag(Flags.S, (result & 0x8000) != 0);
        WriteFlag(Flags.H, ((val1 & 0x0FFF) + (val2 & 0x0FFF) + carry) > 0x0FFF);
        WriteFlag(Flags.P, ((val1 ^ result) & (val2 ^ result) & 0x8000) != 0);
    
        // Undocumented F3/F5 usually mirror bits 13 and 11 of the result
        WriteFlag(Flags.F5, (result & 0x2000) != 0);
        WriteFlag(Flags.F3, (result & 0x0800) != 0);

        Reg.HL = (ushort)result;
        Reg.PC += 2; // ED 4A
        return 15;
    }
    
    private int Op_ADC_HL_SP() // Opcode: ED 7A
    {
        ushort val1 = Reg.HL;
        ushort val2 = Reg.SP;
        int carry = GetFlag(Flags.C) ? 1 : 0;
        int result = val1 + val2 + carry;

        // Set Flags
        WriteFlag(Flags.N, false);
        WriteFlag(Flags.C, result > 0xFFFF);
        WriteFlag(Flags.Z, (ushort)result == 0);
        WriteFlag(Flags.S, (result & 0x8000) != 0);
        WriteFlag(Flags.H, ((val1 & 0x0FFF) + (val2 & 0x0FFF) + carry) > 0x0FFF);
        WriteFlag(Flags.P, ((val1 ^ result) & (val2 ^ result) & 0x8000) != 0);
    
        // Undocumented F3/F5 usually mirror bits 13 and 11 of the result
        WriteFlag(Flags.F5, (result & 0x2000) != 0);
        WriteFlag(Flags.F3, (result & 0x0800) != 0);

        Reg.HL = (ushort)result;
        Reg.PC += 2; // ED 4A
        return 15;
    }

    private static int Op_LD_I_A() // Opcode: ED 47
    {
        Reg.I = Reg.A;
        Reg.PC += 2;
        return 9;
    }

    private int Op_IM_0() // Opcode: ED 46
    {
        _interruptMode = 0;
        Reg.PC += 2;
        return 8;
    }

    private int Op_IM_1() // Opcode: ED 56
    {
        _interruptMode = 1;
        Reg.PC += 2;
        return 8;
    }

    private int Op_IM_2() // Opcode: ED 5E
    {
        _interruptMode = 2;
        Reg.PC += 2;
        return 8;
    }
    
    private int Op_LDI() // Opcode: ED A0
    {
        byte value = _machine.ReadByte(Reg.HL);
        _machine.WriteByte(Reg.DE, value);

        Reg.HL++;
        Reg.DE++;
        Reg.BC--;
        
        ClearFlag(Flags.N | Flags.H);
        WriteFlag(Flags.P, (byte)(Reg.BC - 1) != 0);
        
        Reg.PC += 2;
        return 16;
    }

    private int Op_LDIR() // Opcode: ED B0
    {
        // Transfer one byte
        byte value = _machine.ReadByte(Reg.HL);
        _machine.WriteByte(Reg.DE, value);

        Reg.HL++;
        Reg.DE++;
        Reg.BC--;

        // Flags
        ClearFlag(Flags.N | Flags.H);
        WriteFlag(Flags.P, Reg.BC != 0); // repeat flag

        // PC handling
        if (Reg.BC == 0)
        {
            Reg.PC += 2; // move past ED B0
            return 16;
        }
        else
        {
            // stay on ED B0 until BC == 0
            return 21;
        }
    }

    private int Op_CPIR() // Opcode: ED B1
    {
        // Compare HL with Accumulator
        InternalCP(_machine.ReadByte(Reg.HL));

        Reg.HL++;
        Reg.BC--;

        // Flags
        ClearFlag(Flags.N | Flags.H);
        WriteFlag(Flags.P, Reg.BC != 0); // repeat flag

        // PC handling
        if (Reg.BC == 0 || GetFlag(Flags.Z))
        {
            // terminate
            Reg.PC += 2; // move past ED B1
            return 16;
        }
        else
        {
            // stay on ED B0 until BC == 0
            return 21;
        }
    }
}