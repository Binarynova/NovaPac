public static class RomSets
{
    public record RomFile(string Filename, uint Offset, uint Length);

    public record RomRegion(uint Size, string Type, uint Flags, List<RomFile> Files);

    public record MachineRomSet(string Name, string Parent, List<RomRegion> Regions)
    {
        public RomRegion this[string name] =>
            Regions.FirstOrDefault(r => r.Type.Equals(name, StringComparison.OrdinalIgnoreCase));
    };

    static MachineRomSet Pacman = new ("pacman", null, [
        new RomRegion(0x10000, "maincpu", 0, [
            new RomFile("pacman.6e", 0x0000, 0x1000),
            new RomFile("pacman.6f", 0x1000, 0x1000),
            new RomFile("pacman.6h", 0x2000, 0x1000),
            new RomFile("pacman.6j", 0x3000, 0x1000)
        ]),

        new RomRegion(0x2000, "gfx1", 0, [
            new RomFile("pacman.5e", 0x0000, 0x1000),
            new RomFile("pacman.5f", 0x1000, 0x1000)
        ]),
        
        new RomRegion(0x0120, "proms", 0, [
            new RomFile("82s123.7f", 0x0000, 0x0020),
            new RomFile("82s126.4a", 0x0020, 0x0100)
        ]),
        
        new RomRegion(0x0200, "namco", 0, [
            new RomFile("82s126.1m", 0x0000, 0x0100),
            new RomFile("82s126.3m", 0x0100, 0x0100),
        ])
    ]);
    
    static MachineRomSet Pacmanf = new ("pacmanf", null, [
        new RomRegion(0x10000, "maincpu", 0, [
            new RomFile("pacman.6e", 0x0000, 0x1000),
            new RomFile("pacfast.6f", 0x1000, 0x1000),
            new RomFile("pacman.6h", 0x2000, 0x1000),
            new RomFile("pacman.6j", 0x3000, 0x1000)
        ]),

        new RomRegion(0x2000, "gfx1", 0, [
            new RomFile("pacman.5e", 0x0000, 0x1000),
            new RomFile("pacman.5f", 0x1000, 0x1000)
        ]),
        
        new RomRegion(0x0120, "proms", 0, [
            new RomFile("82s123.7f", 0x0000, 0x0020),
            new RomFile("82s126.4a", 0x0020, 0x0100)
        ]),
        
        new RomRegion(0x0200, "namco", 0, [
            new RomFile("82s126.1m", 0x0000, 0x0100),
            new RomFile("82s126.3m", 0x0100, 0x0100),
        ])
    ]);
    
    static MachineRomSet Matrix = new ("matrix", null, [
        new RomRegion(0x10000, "maincpu", 0, [
            new RomFile("pacman.6e", 0x0000, 0x1000),
            new RomFile("pacman.6f", 0x1000, 0x1000),
            new RomFile("pacman.6h", 0x2000, 0x1000),
            new RomFile("pacman.6j", 0x3000, 0x1000)
        ]),

        new RomRegion(0x2000, "gfx1", 0, [
            new RomFile("pacman.5e", 0x0000, 0x1000),
            new RomFile("pacman.5f", 0x1000, 0x1000)
        ]),
        
        new RomRegion(0x0120, "proms", 0, [
            new RomFile("82s123.7f", 0x0000, 0x0020),
            new RomFile("82s126.4a", 0x0020, 0x0100)
        ]),
        
        new RomRegion(0x0200, "namco", 0, [
            new RomFile("82s126.1m", 0x0000, 0x0100),
            new RomFile("82s126.3m", 0x0100, 0x0100),
        ])
    ]);
    
    static MachineRomSet MsPacman = new ("mspacman", "pacman", [
        new RomRegion(0x20000, "maincpu", 0, [
            new RomFile("pacman.6e", 0x0000, 0x1000),
            new RomFile("pacman.6f", 0x1000, 0x1000),
            new RomFile("pacman.6h", 0x2000, 0x1000),
            new RomFile("pacman.6j", 0x3000, 0x1000),
            new RomFile("u5", 0x8000, 0x0800),
            new RomFile("u6", 0x9000, 0x1000),
            new RomFile("u7", 0xB000, 0x1000)
        ]),

        new RomRegion(0x2000, "gfx1", 0, [
            new RomFile("5e", 0x0000, 0x1000),
            new RomFile("5f", 0x1000, 0x1000)
        ]),
        
        new RomRegion(0x0120, "proms", 0, [
            new RomFile("82s123.7f", 0x0000, 0x0020),
            new RomFile("82s126.4a", 0x0020, 0x0100)
        ]),
        
        new RomRegion(0x0200, "namco", 0, [
            new RomFile("82s126.1m", 0x0000, 0x0100),
            new RomFile("82s126.3m", 0x0100, 0x0100),
        ])
    ]);
    
    static MachineRomSet MsPacmanf = new ("mspacmnf", "pacman", [
        new RomRegion(0x20000, "maincpu", 0, [
            new RomFile("pacman.6e", 0x0000, 0x1000),
            new RomFile("pacfast.6f", 0x1000, 0x1000),
            new RomFile("pacman.6h", 0x2000, 0x1000),
            new RomFile("pacman.6j", 0x3000, 0x1000),
            new RomFile("u5", 0x8000, 0x0800),
            new RomFile("u6", 0x9000, 0x1000),
            new RomFile("u7", 0xB000, 0x1000)
        ]),

        new RomRegion(0x2000, "gfx1", 0, [
            new RomFile("5e", 0x0000, 0x1000),
            new RomFile("5f", 0x1000, 0x1000)
        ]),
        
        new RomRegion(0x0120, "proms", 0, [
            new RomFile("82s123.7f", 0x0000, 0x0020),
            new RomFile("82s126.4a", 0x0020, 0x0100)
        ]),
        
        new RomRegion(0x0200, "namco", 0, [
            new RomFile("82s126.1m", 0x0000, 0x0100),
            new RomFile("82s126.3m", 0x0100, 0x0100),
        ])
    ]);
    
    // A helper to find a ROM set by name
    public static MachineRomSet Get(string name) => name.ToLower() switch
    {
        "pacman" => Pacman,
        "pacmanf" => Pacmanf,
        "matrix" => Matrix,
        "mspacman" => MsPacman,
        "mspacmnf" => MsPacmanf,
        _ => throw new Exception("Game not found")
    };
}