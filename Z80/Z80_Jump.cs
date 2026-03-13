using System;
using Reg = Registers;

public partial class Z80
{
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
    private int Op_JR_e()
    {
        sbyte offset = (sbyte)ReadImmediateByte();
        ushort nextPC = (ushort)(Reg.PC + 2);

        Reg.PC = (ushort)(nextPC + offset);
        return 12;
    }
    private int Op_JR_Z_e() => JR_Cond(() => GetFlag(Flags.Z));
    private int Op_JR_NZ_e() => JR_Cond(() => !GetFlag(Flags.Z));
    private int Op_JR_C_e() => JR_Cond(() => GetFlag(Flags.C));
    private int Op_JR_NC_e() => JR_Cond(() => !GetFlag(Flags.C));
    
    
    
    private int Op_JP_nn()
    {
        // jump to address nn
        ushort address = ReadImmediateWord();
        Reg.PC = address;
        return 10;
    }
    private int Op_JP_NC_nn()
    {
        if(!GetFlag(Flags.C))
        {
            // jump to address nn
            ushort address = ReadImmediateWord();
            Reg.PC = address;
            return 10;            
        }

        Reg.PC += 3;
        return 10;
    }
    private int Op_JP_NZ_nn()
    {
        if(!GetFlag(Flags.Z))
        {
            // jump to address nn
            ushort address = ReadImmediateWord();
            Reg.PC = address;
            return 10;            
        }

        Reg.PC += 3;
        return 10;
    }
    private int Op_JP_M_nn()
    {
        if(GetFlag(Flags.S))
        {
            Reg.PC = ReadImmediateWord();
        }
        else
        {
            Reg.PC += 3;
        }
        return 10;
    }
    private int Op_JP_C_nn()
    {
        if(GetFlag(Flags.C))
        {
            Reg.PC = ReadImmediateWord();
        }
        else
        {
            Reg.PC += 3;            
        }
        return 10;
    }
    private int Op_JP_Z_nn()
    {
        if(GetFlag(Flags.Z))
        {
            Reg.PC = ReadImmediateWord();
        }
        else
        {
            Reg.PC += 3;            
        }
        return 10;
    }
    private static int Op_JP_ptrHL()
    {
        // jump to address at location stored HL
        Reg.PC = Reg.HL;
        return 4;
    }
    
    private int Op_RET()
    {
        Reg.PC = PopWord();
        return 10;
    }
    private int Op_RET_NZ()
    {
        if(!GetFlag(Flags.Z))
        {            
            Reg.PC = PopWord();
            return 11;
        }       
        Reg.PC += 1; 
        return 5;
    }
    private int Op_RET_Z()
    {
        if(GetFlag(Flags.Z))
        {            
            Reg.PC = PopWord();
            return 11;
        }       
        Reg.PC += 1; 
        return 5;
    }
    private int Op_RET_M()
    {
        if(GetFlag(Flags.S))
        {            
            Reg.PC = PopWord();
            return 11;
        }       
        Reg.PC += 1; 
        return 5;
    }
    private int Op_RET_P()
    {
        if(!GetFlag(Flags.S))
        {            
            Reg.PC = PopWord();
            return 11;
        }       
        Reg.PC += 1; 
        return 5;
    }
    private int Op_RET_NC()
    {
        if(!GetFlag(Flags.C))
        {            
            Reg.PC = PopWord();
            return 11;
        }       
        Reg.PC += 1; 
        return 5;
    }
    
    private int Op_CALL_nn()
    {
        PushWord((ushort)(Reg.PC + 3));
        Reg.PC = ReadImmediateWord();
        return 17;
    }
    private int Op_CALL_M_nn()
    {
        if(GetFlag(Flags.S))
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
    
    private int Op_RST(ushort vector)
    {
        // vector can be 00, 08, 10, 18, 20, 28, 30, 38
        PushWord((ushort)(Reg.PC + 1));
        Reg.PC = vector;
        return 11;
    }
    private int Op_DJNZ_e()
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
}