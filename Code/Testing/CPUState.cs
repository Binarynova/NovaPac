using System.Collections.Generic;

public class CPUState
{
    // 8-bit registers
    public byte A { get; set; }
    public byte B { get; set; }
    public byte C { get; set; }
    public byte D { get; set; }
    public byte E { get; set; }
    public byte F { get; set; }
    public byte H { get; set; }
    public byte L { get; set; }
    
    // special registers
    public byte I { get; set; }
    public byte R { get; set; }
    
    //16-bit registers
    public ushort PC { get; set; }
    public ushort SP { get; set; }
    public ushort IX { get; set; }
    public ushort IY { get; set; }
    public ushort WZ { get; set; }
    
    // alternate registers
    public ushort AF_ { get; set; }
    public ushort BC_ { get; set; }
    public ushort DE_ { get; set; }
    public ushort HL_ { get; set; }
    
    // interrupt state
    public byte IM { get; set; }
    public byte EI { get; set; }
    public byte IFF1 { get; set; }
    public byte IFF2 { get; set; }
    
    // undocumented flags
    public byte P { get; set; }
    public byte Q { get; set; }
    
    // RAM
    public List<List<int>> RAM { get; set; }
}