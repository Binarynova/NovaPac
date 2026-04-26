using System;
using System.Collections.Generic;
using System.Linq;

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
    
    static MachineRomSet Pacmanplus = new ("pacplus", null, [
        new RomRegion(0x10000, "maincpu", 0, [
            new RomFile("pacplus.6e", 0x0000, 0x1000),
            new RomFile("pacplus.6f", 0x1000, 0x1000),
            new RomFile("pacplus.6h", 0x2000, 0x1000),
            new RomFile("pacplus.6j", 0x3000, 0x1000)
        ]),

        new RomRegion(0x2000, "gfx1", 0, [
            new RomFile("pacplus.5e", 0x0000, 0x1000),
            new RomFile("pacplus.5f", 0x1000, 0x1000)
        ]),
        
        new RomRegion(0x0120, "proms", 0, [
            new RomFile("pacplus.7f", 0x0000, 0x0020),
            new RomFile("pacplus.4a", 0x0020, 0x0100)
        ]),
        
        new RomRegion(0x0200, "namco", 0, [
            new RomFile("82s126.1m", 0x0000, 0x0100),
            new RomFile("82s126.3m", 0x0100, 0x0100),
        ])
    ]);
    
    static MachineRomSet JrPacman = new ("jrpacman", null, [
        new RomRegion(0x10000, "maincpu", 0, [
            new RomFile("jr.pac-man_8d_11-9-83.8d", 0x0000, 0x2000),
            new RomFile("jr.pac-man_8e_11-9-83.8e", 0x2000, 0x2000),
            new RomFile("jr.pac-man_8h_11-9-83.8h", 0x8000, 0x2000),
            new RomFile("jr.pac-man_8j_11-9-83.8j", 0xA000, 0x2000),
            new RomFile("jr.pac-man_8k_11-9-83.8k", 0xC000, 0x2000),
        ]),

        new RomRegion(0x4000, "gfx1", 0, [
            new RomFile("jr.pac-man_2c_11-9-83.2c", 0x0000, 0x2000),
            new RomFile("jr.pac-man_2e_11-9-83.2e", 0x2000, 0x2000)
        ]),
        
        new RomRegion(0x0120, "proms", 0, [
            new RomFile("a290-27axv-bxhd.9e", 0x0000, 0x0100),
            new RomFile("a290-27axv-cxhd.9f", 0x0000, 0x0100),
            new RomFile("a290-27axv-axhd.9p", 0x0020, 0x0100)
        ]),
        
        new RomRegion(0x0200, "namco", 0, [
            new RomFile("a290-27axv-dxhd.7p", 0x0000, 0x0100),
            new RomFile("a290-27axv-exhd.5s", 0x0100, 0x0100),
        ])
    ]);
    
    static MachineRomSet JrPacmanf = new ("jrpacmanf", null, [
        new RomRegion(0x10000, "maincpu", 0, [
            new RomFile("fast_jr.8d", 0x0000, 0x2000),
            new RomFile("jr.pac-man_8e_11-9-83.8e", 0x2000, 0x2000),
            new RomFile("jr.pac-man_8h_11-9-83.8h", 0x8000, 0x2000),
            new RomFile("jr.pac-man_8j_11-9-83.8j", 0xA000, 0x2000),
            new RomFile("jr.pac-man_8k_11-9-83.8k", 0xC000, 0x2000),
        ]),

        new RomRegion(0x4000, "gfx1", 0, [
            new RomFile("jr.pac-man_2c_11-9-83.2c", 0x0000, 0x2000),
            new RomFile("jr.pac-man_2e_11-9-83.2e", 0x2000, 0x2000)
        ]),
        
        new RomRegion(0x0120, "proms", 0, [
            new RomFile("a290-27axv-bxhd.9e", 0x0000, 0x0100),
            new RomFile("a290-27axv-cxhd.9f", 0x0000, 0x0100),
            new RomFile("a290-27axv-axhd.9p", 0x0020, 0x0100)
        ]),
        
        new RomRegion(0x0200, "namco", 0, [
            new RomFile("a290-27axv-dxhd.7p", 0x0000, 0x0100),
            new RomFile("a290-27axv-exhd.5s", 0x0100, 0x0100),
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
        "pacplus" => Pacmanplus,
        "jrpacman" => JrPacman,
        "jrpacmanf" => JrPacmanf,
        _ => throw new Exception("Game not found")
    };
}