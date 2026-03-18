using Reg = Registers;

public partial class Z80
{
    private static int Op_LD_A(byte register) // Opcode: 78 79 7A 7B 7C 7D 7F
    {
        Reg.A = register;
        Reg.PC += 1;
        return 4;
    }

    private int Op_LD_A_n() // Opcode: 3E
    {
        Reg.A = ReadImmediateByte();
        Reg.PC += 2;
        return 7;
    }

    private int Op_LD_A_ptrHL() // Opcode: 7E
    {
        Reg.A = machine.ReadByte(Reg.HL);
        Reg.PC += 1;
        return 7;
    }
    
    private static int Op_LD_B(byte register) // Opcode: 40 41 42 43 44 45 47
    {
        Reg.B = register;
        Reg.PC += 1;
        return 4;
    }

    private int Op_LD_B_n() // Opcode: 06
    {
        Reg.B = ReadImmediateByte();
        Reg.PC += 2;
        return 7;
    }
    
    private int Op_LD_B_ptrHL() // Opcode: 46
    {
        Reg.B = machine.ReadByte(Reg.HL);
        Reg.PC += 1;
        return 7;
    }
    
    private static int Op_LD_C(byte register) // Opcode: 48 49 4A 4B 4C 4D 4F
    {
        Reg.C = register;
        Reg.PC += 1;
        return 4;
    }

    private int Op_LD_C_n() // Opcode: 0E
    {
        Reg.C = ReadImmediateByte();
        Reg.PC += 2;
        return 7;
    }
    
    private int Op_LD_C_ptrHL() // Opcode: 4E
    {
        Reg.C = machine.ReadByte(Reg.HL);
        Reg.PC += 1;
        return 7;
    }

    private static int Op_LD_D(byte register) // Opcode: 50 51 52 53 54 55 57
    {
        Reg.D = register;
        Reg.PC += 1;
        return 4;
    }

    private int Op_LD_D_n() // Opcode: 16
    {
        Reg.D = ReadImmediateByte();
        Reg.PC += 2;
        return 7;
    }
    
    private int Op_LD_D_ptrHL() // Opcode: 56
    {
        Reg.D = machine.ReadByte(Reg.HL);
        Reg.PC += 1;
        return 7;
    }
    
    private static int Op_LD_E(byte register) // Opcode: 58 59 5A 5B 5C 5D 5F
    {
        Reg.E = register;
        Reg.PC += 1;
        return 4;
    }

    private int Op_LD_E_n() // Opcode: 1E
    {
        Reg.E = ReadImmediateByte();
        Reg.PC += 2;
        return 7;
    }
    
    private int Op_LD_E_ptrHL() // Opcode: 5E
    {
        Reg.E = machine.ReadByte(Reg.HL);
        Reg.PC += 1;
        return 7;
    }
    
    private static int Op_LD_H(byte register) // Opcode: 60 61 62 63 64 65 67
    {
        Reg.H = register;
        Reg.PC += 1;
        return 4;
    }

    private int Op_LD_H_n() // Opcode: 26
    {
        Reg.H = ReadImmediateByte();
        Reg.PC += 2;
        return 7;
    }
    
    private int Op_LD_H_ptrHL() // Opcode: 66
    {
        Reg.H = machine.ReadByte(Reg.HL);
        Reg.PC += 1;
        return 7;
    }

    private static int Op_LD_L(byte register) // Opcode: 68 69 6A 6B 6C 6D 6F
    {
        Reg.L = register;
        Reg.PC += 1;
        return 4;
    }

    private int Op_LD_L_n() // Opcode: 2E
    {
        Reg.L = ReadImmediateByte();
        Reg.PC += 2;
        return 7;
    }
    
    private int Op_LD_L_ptrHL() // Opcode: 6E
    {
        Reg.L = machine.ReadByte(Reg.HL);
        Reg.PC += 1;
        return 7;
    }
    
    private int Op_LD_ptrHL(byte register) // Opcode: 70 71 72 73 74 75 77
    {
        machine.WriteByte(Reg.HL, register);
        Reg.PC += 1;
        return 7;
    }

    private int Op_LD_A_ptrBC() // Opcode: 0A
    {
        Reg.A = machine.ReadByte(Reg.BC);
        Reg.WZ = (ushort)(Reg.BC + 1);
        Reg.PC += 1;
        return 7;
    }

    private int Op_LD_A_ptrDE() // Opcode: 1A
    {
        Reg.A = machine.ReadByte(Reg.DE);
        Reg.PC += 1;
        return 7;
    }
    
    private int Op_LD_HL_ptrNN() // Opcode: 2A
    {
        ushort addr = ReadImmediateWord();
        Reg.L = machine.ReadByte(addr);
        Reg.H = machine.ReadByte((ushort)(addr + 1));
        Reg.PC += 3;
        return 16;
    }

    private int Op_LD_A_ptrNN() // Opcode: 3A
    {
        ushort addr = ReadImmediateWord();
        Reg.A = machine.ReadByte(addr);
        Reg.PC += 3;
        return 13;
    }
    
    private int Op_LD_ptrBC_A() // Opcode: 02
    {
        machine.WriteByte(Reg.BC, Reg.A);
        Reg.WZ = (ushort)((Reg.A << 8) | ((Reg.C + 1) & 0xFF));
        Reg.PC += 1;
        return 7;
    }

    private int Op_LD_ptrDE_A() // Opcode: 12
    {
        machine.WriteByte(Reg.DE, Reg.A);
        Reg.PC += 1;
        return 7;
    }

    private int Op_LD_ptrNN_HL() // Opcode: 22
    {
        ushort address = ReadImmediateWord();
        machine.WriteByte(address, Reg.L);
        machine.WriteByte((ushort)(address + 1), Reg.H);
        Reg.PC += 3;
        return 16;
    }

    private int Op_LD_ptrNN_A() // Opcode: 32
    {
        ushort address = ReadImmediateWord();
        machine.WriteByte(address, Reg.A);
        Reg.PC += 3;
        return 13;
    }

    private int Op_LD_BC_nn() // Opcode: 01
    {
        Reg.BC = ReadImmediateWord();
        Reg.PC += 3;
        return 10;
    }

    private int Op_LD_DE_nn() // Opcode: 11
    {
        Reg.DE = ReadImmediateWord();
        Reg.PC += 3;
        return 10;
    }
    
    private int Op_LD_HL_nn() // Opcode: 21
    {
        Reg.HL = ReadImmediateWord();
        Reg.PC += 3;
        return 10;
    }

    private int Op_LD_SP_nn() // Opcode: 31
    {
        Reg.SP = ReadImmediateWord();
        Reg.PC += 3;
        return 10;
    }

    private int Op_LD_ptrHL_n() // Opcode: 36
    {
        machine.WriteByte(Reg.HL, ReadImmediateByte());
        Reg.PC += 2;
        return 10;
    }
    
    private int Op_LD_SP_HL() // Opcode: F9
    {
        Reg.SP = Reg.HL;
        Reg.PC += 3;
        return 10;
    }
}