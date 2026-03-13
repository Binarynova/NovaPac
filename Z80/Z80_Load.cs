using Reg = Registers;

public partial class z80Cpu
{
    
    
    private static int Op_LD_A_A() { Reg.PC += 1; return 4; }
    private static int Op_LD_A_B() { Reg.A = Reg.B; Reg.PC += 1; return 4; }
    private static int Op_LD_A_C() { Reg.A = Reg.C; Reg.PC += 1; return 4; }
    private static int Op_LD_A_D() { Reg.A = Reg.D; Reg.PC += 1; return 4; }
    private static int Op_LD_A_E() { Reg.A = Reg.E; Reg.PC += 1; return 4; }
    private static int Op_LD_A_H() { Reg.A = Reg.H; Reg.PC += 1; return 4; }
    private static int Op_LD_A_L() { Reg.A = Reg.L; Reg.PC += 1; return 4; }
    private int Op_LD_A_n() { Reg.A = ReadImmediateByte(); Reg.PC += 2; return 7; }

    private static int Op_LD_B_A() { Reg.B = Reg.A; Reg.PC += 1; return 4; }
    private static int Op_LD_B_B() { Reg.PC += 1; return 4; }
    private static int Op_LD_B_C() { Reg.B = Reg.C; Reg.PC += 1; return 4; }
    private static int Op_LD_B_D() { Reg.B = Reg.D; Reg.PC += 1; return 4; }
    private static int Op_LD_B_E() { Reg.B = Reg.E; Reg.PC += 1; return 4; }
    private static int Op_LD_B_H() { Reg.B = Reg.H; Reg.PC += 1; return 4; }
    private static int Op_LD_B_L() { Reg.B = Reg.L; Reg.PC += 1; return 4; }
    private int Op_LD_B_n() { Reg.B = ReadImmediateByte(); Reg.PC += 2; return 7; }

    private static int Op_LD_C_A() { Reg.C = Reg.A; Reg.PC += 1; return 4; }
    private static int Op_LD_C_B() { Reg.C = Reg.B; Reg.PC += 1; return 4; }
    private static int Op_LD_C_C() { Reg.PC += 1; return 4; }
    private static int Op_LD_C_D() { Reg.C = Reg.D; Reg.PC += 1; return 4; }
    private static int Op_LD_C_E() { Reg.C = Reg.E; Reg.PC += 1; return 4; }
    private static int Op_LD_C_H() { Reg.C = Reg.H; Reg.PC += 1; return 4; }
    private static int Op_LD_C_L() { Reg.C = Reg.L; Reg.PC += 1; return 4; }
    private int Op_LD_C_n() { Reg.C = ReadImmediateByte(); Reg.PC += 2; return 7; }

    private static int Op_LD_D_A() { Reg.D = Reg.A; Reg.PC += 1; return 4; }
    private static int Op_LD_D_B() { Reg.D = Reg.B; Reg.PC += 1; return 4; }
    private static int Op_LD_D_C() { Reg.D = Reg.C; Reg.PC += 1; return 4; }
    private static int Op_LD_D_D() { Reg.PC += 1; return 4; }
    private static int Op_LD_D_E() { Reg.D = Reg.E; Reg.PC += 1; return 4; }
    private static int Op_LD_D_H() { Reg.D = Reg.H; Reg.PC += 1; return 4; }
    private static int Op_LD_D_L() { Reg.D = Reg.L; Reg.PC += 1; return 4; }
    private int Op_LD_D_n() { Reg.D = ReadImmediateByte(); Reg.PC += 2; return 7; }

    private static int Op_LD_E_A() { Reg.E = Reg.A; Reg.PC += 1; return 4; }
    private static int Op_LD_E_B() { Reg.E = Reg.B; Reg.PC += 1; return 4; }
    private static int Op_LD_E_C() { Reg.E = Reg.C; Reg.PC += 1; return 4; }
    private static int Op_LD_E_D() { Reg.E = Reg.D; Reg.PC += 1; return 4; }
    private static int Op_LD_E_E() { Reg.PC += 1; return 4; }
    private static int Op_LD_E_H() { Reg.E = Reg.H; Reg.PC += 1; return 4; }
    private static int Op_LD_E_L() { Reg.E = Reg.L; Reg.PC += 1; return 4; }
    private int Op_LD_E_n() { Reg.E = ReadImmediateByte(); Reg.PC += 2; return 7; }

    private static int Op_LD_H_A() { Reg.H = Reg.A; Reg.PC += 1; return 4; }
    private static int Op_LD_H_B() { Reg.H = Reg.B; Reg.PC += 1; return 4; }
    private static int Op_LD_H_C() { Reg.H = Reg.C; Reg.PC += 1; return 4; }
    private static int Op_LD_H_D() { Reg.H = Reg.D; Reg.PC += 1; return 4; }
    private static int Op_LD_H_E() { Reg.H = Reg.E; Reg.PC += 1; return 4; }
    private static int Op_LD_H_H() { Reg.PC += 1; return 4; }
    private static int Op_LD_H_L() { Reg.H = Reg.L; Reg.PC += 1; return 4; }
    private int Op_LD_H_n() { Reg.H = ReadImmediateByte(); Reg.PC += 2; return 7; }

    private static int Op_LD_L_A() { Reg.L = Reg.A; Reg.PC += 1; return 4; }
    private static int Op_LD_L_B() { Reg.L = Reg.B; Reg.PC += 1; return 4; }
    private static int Op_LD_L_C() { Reg.L = Reg.C; Reg.PC += 1; return 4; }
    private static int Op_LD_L_D() { Reg.L = Reg.D; Reg.PC += 1; return 4; }
    private static int Op_LD_L_E() { Reg.L = Reg.E; Reg.PC += 1; return 4; }
    private static int Op_LD_L_H() { Reg.L = Reg.H; Reg.PC += 1; return 4; }
    private static int Op_LD_L_L() { Reg.PC += 1; return 4; }
    private int Op_LD_L_n() { Reg.L = ReadImmediateByte(); Reg.PC += 2; return 7; }
  
    
    private int Op_LD_ptrHL_A() { machine.WriteByte(Reg.HL, Reg.A, Reg.PC); Reg.PC += 1; return 7; }
    private int Op_LD_ptrHL_B() { machine.WriteByte(Reg.HL, Reg.B, Reg.PC); Reg.PC += 1; return 7; }
    private int Op_LD_ptrHL_C() { machine.WriteByte(Reg.HL, Reg.C, Reg.PC); Reg.PC += 1; return 7; }
    private int Op_LD_ptrHL_D() { machine.WriteByte(Reg.HL, Reg.D, Reg.PC); Reg.PC += 1; return 7; }
    private int Op_LD_ptrHL_E() { machine.WriteByte(Reg.HL, Reg.E, Reg.PC); Reg.PC += 1; return 7; }    
    private int Op_LD_ptrHL_H() { machine.WriteByte(Reg.HL, Reg.H, Reg.PC); Reg.PC += 1; return 7; }
    private int Op_LD_ptrHL_L() { machine.WriteByte(Reg.HL, Reg.L, Reg.PC); Reg.PC += 1; return 7; }

    private int Op_LD_A_ptrHL() { Reg.A = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }
    private int Op_LD_B_ptrHL() { Reg.B = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }
    private int Op_LD_C_ptrHL() { Reg.C = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }
    private int Op_LD_D_ptrHL() { Reg.D = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }
    private int Op_LD_E_ptrHL() { Reg.E = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }
    private int Op_LD_H_ptrHL() { Reg.H = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }
    private int Op_LD_L_ptrHL() { Reg.L = machine.ReadByte(Reg.HL); Reg.PC += 1; return 7; }
    
    private int Op_LD_A_ptrDE()
    {
        Reg.A = machine.ReadByte(Reg.DE);
        Reg.PC += 1;
        return 7;
    }
    private int Op_LD_A_ptrNN()
    {
        ushort addr = ReadImmediateWord();
        Reg.A = machine.ReadByte(addr);
        Reg.PC += 3;
        return 13;
    }
    private int Op_LD_HL_nn()
    {
        // load nn into HL
        Reg.HL = ReadImmediateWord();
        Reg.PC += 3;
        return 10;
    }    
    private int Op_LD_SP_nn()
    {
        // load nn into SP
        Reg.SP = ReadImmediateWord();
        Reg.PC += 3;
        return 10;
    }
    private int Op_LD_BC_nn()
    {
        // load nn into BC
        Reg.BC = ReadImmediateWord();
        Reg.PC += 3;
        return 10;
    }
    private int Op_LD_ptrHL_n()
    {
        machine.WriteByte(Reg.HL, ReadImmediateByte(), Reg.PC);
        Reg.PC += 2;
        return 10;
    }
    private int Op_LD_DE_nn()
    {
        Reg.DE = ReadImmediateWord();
        Reg.PC += 3;
        return 10;
    }
    private int Op_LD_ptrnn_A()
    {
        // store value of Accumulator in memory at the location nn
        ushort address = ReadImmediateWord();
        machine.WriteByte(address, Reg.A, Reg.PC);
        Reg.PC += 3;
        return 13;
    }
    private int Op_LD_ptrnn_HL()
    {
        // store value of HL in memory at the location nn
        ushort address = ReadImmediateWord();
        machine.WriteByte(address, Reg.L, Reg.PC);
        machine.WriteByte((ushort)(address + 1), Reg.H, Reg.PC);
        Reg.PC += 3;
        return 16;
    }
    
    private int Op_LD_HL_ptrnn()
    {
        Reg.HL = machine.ReadByte(ReadImmediateWord());
        Reg.PC += 3;
        return 16;
    }
    
    private int Op_LD_DE_A()
    {
        // stores the value of A into RAM at address DE
        machine.WriteByte(Reg.DE, Reg.A, Reg.PC);
        Reg.PC += 1;
        return 7;
    }
}