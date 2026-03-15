using System.Collections.Generic;
using System.Text.Json.Serialization;


public class Z80SingleStepTest
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("initial")]
    public TestState Initial { get; set; }
    
    [JsonPropertyName("final")]
    public TestState Final { get; set; }
    
    public class TestState
    {
        [JsonPropertyName("pc")]
        public ushort PC { get; set; }
        [JsonPropertyName("sp")]
        public ushort SP { get; set; }
        [JsonPropertyName("a")]
        public byte A { get; set; }
        [JsonPropertyName("b")]
        public byte B { get; set; }
        [JsonPropertyName("c")]
        public byte C { get; set; }
        [JsonPropertyName("d")]
        public byte D { get; set; }
        [JsonPropertyName("e")]
        public byte E { get; set; }
        [JsonPropertyName("f")]
        public byte F { get; set; }
        [JsonPropertyName("h")]
        public byte H { get; set; }
        [JsonPropertyName("l")]
        public byte L { get; set; }
        [JsonPropertyName("i")]
        public byte I { get; set; }
        [JsonPropertyName("r")]
        public byte R { get; set; }
        [JsonPropertyName("ei")]
        public byte EI { get; set; }
        [JsonPropertyName("wz")]
        public ushort WZ { get; set; }
        [JsonPropertyName("ix")]
        public ushort IX { get; set; }
        [JsonPropertyName("iy")]
        public ushort IY { get; set; }
        [JsonPropertyName("af_")]
        public ushort AF_ { get; set; }
        [JsonPropertyName("bc_")]
        public ushort BC_ { get; set; }
        [JsonPropertyName("de_")]
        public ushort DE_ { get; set; }
        [JsonPropertyName("hl_")]
        public ushort HL_ { get; set; }
        [JsonPropertyName("im")]
        public byte IM { get; set; }
        [JsonPropertyName("p")]
        public byte P { get; set; }
        [JsonPropertyName("q")]
        public byte Q { get; set; }
        [JsonPropertyName("iff1")]
        public byte IFF1 { get; set; }
        [JsonPropertyName("iff2")]
        public byte IFF2 { get; set; }

        [JsonPropertyName("ram")]
        public List<List<int>> RAM { get; set; } = new();
    }
}