public partial class Z80Cpu
{
    private int Op_LD_r_n(ref byte register)
    {
        register = ImmediateByte();

        Reg.P = Reg.Q = 0;
        
        Reg.PC += 2;
        return 7;
    }

    private int Op_LD_r_ptrHL(ref byte register)
    {
        register = _bus.ReadByte(Reg.HL);
        Reg.PC += 1;
        return 7;
    }

    private int Op_LD_r_R(ref byte destination, byte source)
    {
        destination = source;
        Reg.PC += 1;
        return 4;
    }
    
    private int Op_LD_ptrHL(byte register) // Opcode: 70 71 72 73 74 75 77
    {
        _bus.WriteByte(Reg.HL, register);
        Reg.PC += 1;
        return 7;
    }

    private int Op_LD_A_ptrBC() // Opcode: 0A
    {
        Reg.A = _bus.ReadByte(Reg.BC);
        
        Reg.P = Reg.Q = 0;
        wz_LD_A_ptrRR(Reg.BC);
        
        Reg.PC += 1;
        return 7;
    }

    private int Op_LD_A_ptrDE() // Opcode: 1A
    {
        Reg.A = _bus.ReadByte(Reg.DE);
        
        Reg.P = Reg.Q = 0;
        wz_LD_A_ptrRR(Reg.DE);
        
        Reg.PC += 1;
        return 7;
    }
    
    private int Op_LD_HL_ptrNN() // Opcode: 2A
    {
        ushort addr = ImmediateWord();
        Reg.L = _bus.ReadByte(addr);
        Reg.H = _bus.ReadByte((ushort)(addr + 1));
        Reg.PC += 3;
        return 16;
    }

    private int Op_LD_A_ptrNN() // Opcode: 3A
    {
        ushort addr = ImmediateWord();
        Reg.A = _bus.ReadByte(addr);
        Reg.PC += 3;
        return 13;
    }
    
    private int Op_LD_ptrBC_A() // Opcode: 02
    {
        _bus.WriteByte(Reg.BC, Reg.A);
        
        Reg.P = Reg.Q = 0;
        wz_LD_ptrRR_A(Reg.C);
        
        Reg.PC += 1;
        return 7;
    }

    private int Op_LD_ptrDE_A() // Opcode: 12
    {
        _bus.WriteByte(Reg.DE, Reg.A);
        
        Reg.P = Reg.Q = 0;
        wz_LD_ptrRR_A(Reg.E);
        
        Reg.PC += 1;
        return 7;
    }

    private int Op_LD_ptrNN_HL() // Opcode: 22
    {
        ushort address = ImmediateWord();
        _bus.WriteByte(address, Reg.L);
        _bus.WriteByte((ushort)(address + 1), Reg.H);
        
        Reg.WZ = (ushort)(address + 1);
        Reg.P = Reg.Q = 0;
        
        Reg.PC += 3;
        return 16;
    }

    private int Op_LD_ptrNN_A() // Opcode: 32
    {
        ushort address = ImmediateWord();
        _bus.WriteByte(address, Reg.A);
        Reg.PC += 3;
        return 13;
    }

    private int Op_LD_BC_nn() // Opcode: 01
    {
        Reg.P = Reg.Q = 0;
        
        Reg.BC = ImmediateWord();
        Reg.PC += 3;
        return 10;
    }

    private int Op_LD_DE_nn() // Opcode: 11
    {
        Reg.P = Reg.Q = 0;
        
        Reg.DE = ImmediateWord();
        Reg.PC += 3;
        return 10;
    }
    
    private int Op_LD_HL_nn() // Opcode: 21
    {
        Reg.P = Reg.Q = 0;
        
        Reg.HL = ImmediateWord();
        Reg.PC += 3;
        return 10;
    }

    private int Op_LD_SP_nn() // Opcode: 31
    {
        Reg.P = Reg.Q = 0;
        
        Reg.SP = ImmediateWord();
        Reg.PC += 3;
        return 10;
    }

    private int Op_LD_ptrHL_n() // Opcode: 36
    {
        _bus.WriteByte(Reg.HL, ImmediateByte());
        Reg.PC += 2;
        return 10;
    }
    
    private int Op_LD_SP_HL() // Opcode: F9
    {
        Reg.SP = Reg.HL;
        Reg.PC += 1;
        return 10;
    }
    
    #region HELPER_METHODS

    private void wz_LD_ptrRR_A(byte lowerReg)
    {
        byte wzLow = (byte)((lowerReg + 1) & 0xFF);
        byte wzHigh = Reg.A;
        Reg.WZ = (ushort)((wzHigh << 8) | wzLow);
    }

    private void wz_LD_A_ptrRR(ushort registerPair)
    {
        Reg.WZ = (ushort)(registerPair + 1);
    }
    
    #endregion
}