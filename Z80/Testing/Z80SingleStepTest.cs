using System.Collections.Generic;


public class Z80SingleStepTest
{
    public string Name { get; set; }

    public TestState Initial { get; set; }
    public TestState Final { get; set; }

    public class TestState
    {
        public ushort PC { get; set; }
        public ushort SP { get; set; }
        public byte A { get; set; }
        public byte B { get; set; }
        public byte C { get; set; }
        public byte D { get; set; }
        public byte E { get; set; }
        public byte F { get; set; }
        public byte H { get; set; }
        public byte L { get; set; }
        public ushort HL_ { get; set; }
        public ushort DE_ { get; set; }
        public ushort BC_ { get; set; }
        public ushort AF_ { get; set; }
        public ushort IX { get; set; }
        public ushort IY { get; set; }
        public ushort WZ { get; set; }
        public byte IM { get; set; }
        public byte EI { get; set; }
        public byte IFF1 { get; set; }
        public byte IFF2 { get; set; }
        public byte R { get; set; }
        public byte P { get; set; }
        public byte Q { get; set; }

        public List<List<int>> RAM { get; set; } = new();
    }
}