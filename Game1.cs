using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using System.Text.Json;
using Reg = Registers;
using Myra;
using Myra.Graphics2D.UI;

namespace pacman;

public class Game1 : Game
{
    private Desktop desktop;
    private double _cycleAccumulator = 0;
    KeyboardState _lastState;
    private const double CPU_CLOCK_SPEED = 3072000; // 3.072 MHz
    private string[] Args;
    const int resScale = 3;
    int internalWidth = 224;
    int internalHeight = 288;
    int sidePadding = 20;
    private SpriteBatch _spriteBatch;
    private KeyboardState _previousKeyboardState;
    Texture2D pixelTexture;
    ConsoleKeyInfo menuChoice;
    int mode = 0;
    bool SteppingThrough;
    StreamWriter trace;
    const float _speedMultiplier = 1f;
    int tileViewerPaletteIndex = 0;
    GraphicsDeviceManager graphics;
    Rectangle _renderDestination;

    List<int> tileViewerPalettes = [1, 3, 5, 7, 9, 14, 15, 16, 17, 18, 20, 21, 22, 23, 24, 25, 26, 27, 29, 30, 31];
    
    Pacman pacmanMachine;
    Zexdoc zexdocMachine;
    Z80 cpu;

    int interruptCycleCounter;
    const int tileWidth = 8;
    const int spriteWidth = 16;
    const int CYCLES_PER_INTERRUPT = 51200;
    
    List<int[,]> tiles = [];
    List<int[,]> sprites = [];
    List<Color> colors = [];
    List<List<Color>> palettes = [];
    string romFileName;
    RenderTarget2D _nativeRenderTarget;

    public Game1(string[] args)
    {
        Args = args;
        graphics = new GraphicsDeviceManager(this);
        graphics.PreferredBackBufferWidth = (internalWidth * resScale) + (sidePadding * 2);
        graphics.PreferredBackBufferHeight = (internalHeight * resScale) + (sidePadding * 2);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromTicks(166667); // Exactly 1/60th of a second
        graphics.SynchronizeWithVerticalRetrace = true; // VSync
    }

    protected override void Initialize()
    {
        UpdateRenderDestination();
        if (Args.Length != 0)
        {
            if (Args[0] == "-debug")
                SteppingThrough = true;
        }
        
        if(mode == 0)
        {
            Console.WriteLine("Official ROMs:");
            Console.WriteLine(" 1) Pac-Man");
            
            Console.WriteLine("\nUnofficial or Homebrew ROMs");
            Console.WriteLine(" 2) Matrix Homebrew");
            Console.WriteLine(" 3) New Puck-X");
            
            Console.WriteLine("\nDebug:");
            Console.WriteLine(" 4) View Tile/Sprite ROM");
            
            Console.WriteLine("\nTests:");
            Console.WriteLine(" 5) ZEXDOC");
            Console.WriteLine(" 6) SSTs");
            
            Console.WriteLine("\nAnything else) Quit");
            Console.Write(" > "); menuChoice = Console.ReadKey();
            Console.WriteLine("");
            switch (menuChoice.Key)
            {
                case ConsoleKey.D1:
                    mode = 1;
					Window.Title = $"Pac-Man";
                    romFileName = "roms/pacman.zip";
                    break;
                case ConsoleKey.D2:
                    mode = 1;
                    Window.Title = "Matrix Homebrew by Scott Lawrence";
                    romFileName = "roms/matrix.zip";
                    break;
                case ConsoleKey.D3:
                    mode = 1;
                    Window.Title = $"New Puck-X (Unofficial)";
                    romFileName = "roms/newpuckx.zip";
                    break;
                case ConsoleKey.D4:
                    mode = 2;
                    romFileName = "roms/pacman.zip";
                    break;
                case ConsoleKey.D5:
                    mode = 4;
                    break;
                case ConsoleKey.D6:
                    Console.WriteLine("Running single-step tests...");
                    RunSingleStepTests();
                    break;
                case ConsoleKey.D7:
                    mode = 1;
                    Window.Title = $"Ms. Pac-Man";
                    romFileName = "roms/mspacman.zip";
                    break;
                default:
                    Environment.Exit(0);
                    break;
            }
        }

        if (mode == 4)
        {
            RunZexdocTests();
            Environment.Exit(0);
        }
        else
        {
            pacmanMachine = new Pacman(romFileName);
            cpu = new Z80(pacmanMachine);
            base.Initialize();
        }
    }

    protected override void LoadContent()
    {
        _nativeRenderTarget = new RenderTarget2D(GraphicsDevice, 224, 288);
        PlayTestBeep();
        MyraEnvironment.Game = this;

        var grid = new Grid
        {
            RowSpacing = 8,
            ColumnSpacing = 8
        };

        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        desktop = new Desktop();
        desktop.Root = grid;
        
        
        trace = new StreamWriter("trace.txt");
        trace.AutoFlush = false;
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        // Create a 1x1 white texture
        pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        pixelTexture.SetData([Color.White]);

        cpu.Reset();
        if (mode != 4)
        {
            ReadTiles();
            ReadSprites();
            ReadColors();
            ReadPalettes();
        }
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();
        if(mode == 1)
        {
            if (keyboardState.IsKeyDown(Keys.Enter) && 
                (keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt)) &&
                _lastState.IsKeyUp(Keys.Enter))
            {
                ToggleFullscreen();
            }

            _lastState = keyboardState;
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Space))
                SteppingThrough = true;
            
            _cycleAccumulator += gameTime.ElapsedGameTime.TotalSeconds * CPU_CLOCK_SPEED * _speedMultiplier;

            while (_cycleAccumulator > 0)
            {
                int cycles = cpu.Step(SteppingThrough);
                _cycleAccumulator -= cycles;

                interruptCycleCounter += cycles;
                if (interruptCycleCounter >= CYCLES_PER_INTERRUPT)
                {
                    cpu.RequestInterrupt();
                    interruptCycleCounter -= CYCLES_PER_INTERRUPT;
                }
            
                if (cycles <= 0) break; 
            }
        }
        else if(mode is 2 or 3)
        {            
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            else if (keyboardState.IsKeyDown(Keys.Left) && _previousKeyboardState.IsKeyUp(Keys.Left) ||
                     keyboardState.IsKeyDown(Keys.Right) && _previousKeyboardState.IsKeyUp(Keys.Right))
            {
                // switch ROMs to view
                switch(mode)
                {
                    case 2:
                        mode = 3;
                        break;
                    case 3:
                        mode = 2;
                        break;
                }
            }
            
            else if (keyboardState.IsKeyDown(Keys.Up) && _previousKeyboardState.IsKeyUp(Keys.Up))
            {
                if (tileViewerPaletteIndex == 20)
                    tileViewerPaletteIndex = 0;
                else
                    tileViewerPaletteIndex++;
            }
            
            else if (keyboardState.IsKeyDown(Keys.Down) && _previousKeyboardState.IsKeyUp(Keys.Down))
            {
                if (tileViewerPaletteIndex == 0)
                    tileViewerPaletteIndex = 20;
                else
                    tileViewerPaletteIndex--;
            }
        }
        
        _previousKeyboardState = keyboardState;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_nativeRenderTarget);
        if(mode == 1)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(sortMode: SpriteSortMode.Deferred, samplerState: SamplerState.PointClamp);

            // draw in three sections
            // 1: vram 4000 to 403F is the bottom two rows of tiles, right-to-left, top-to-bottom, starting off-screen two tiles to the right.
            // 2: vram 43c0 to 43ff is the top two rows of tiles, right-to-left, top-to-bottom, starting off-screen two tiles to the right.
            // 3: vram 4040 to 43bf is the main grid of the game, top-to-bottom, right-to-left, starting on screen at the top-right below section 2

            //for(int tileRow = 0; tileRow < 2; tileRow++) // top rows
            //{
            //    for(int tileCol = 0; tileCol < 32; tileCol++)
            //    {
            //        ushort vram = (ushort)(0x43C0 + tileCol + (0x20 * tileRow));
            //        ushort pram = (ushort)(vram + 0x400);
            //        int xPos = 232 - (tileCol * 8);
            //        int yPos = tileRow * 8;
            //        byte tileNumber = pacmanMachine.ReadByte(vram);
            //        if(tileNumber is >= 0x47 and <= 0x49 || tileNumber == 0x55)
            //            Console.WriteLine($"Tile {tileNumber:X2}");
            //        byte paletteNumber = pacmanMachine.ReadByte(pram);
            //        DrawTile(tileNumber, paletteNumber & 0x3F, xPos, yPos);
            //    }
            //}

            for (int i = 0x3DF; i >= 0x3C0; i--)
            {
                int tileAddress = 0x4000 + i;
                int paletteAddress = 0x4400 + i;

                byte tileIndex = pacmanMachine.ReadByte((ushort)tileAddress);
                byte paletteIndex = pacmanMachine.ReadByte((ushort)paletteAddress);

                const int yPos = 0;
                int xPos = 232 - (i - 0x3C0) * 8;
                
                DrawTile(tileIndex, paletteIndex & 0x3F, xPos, yPos);
            }
            for (int i = 0x3FF; i >= 0x3E0; i--)
            {
                int tileAddress = 0x4000 + i;
                int paletteAddress = 0x4400 + i;

                byte tileIndex = pacmanMachine.ReadByte((ushort)tileAddress);
                byte paletteIndex = pacmanMachine.ReadByte((ushort)paletteAddress);

                const int yPos = 8;
                int xPos = 232 - (i - 0x3E0) * 8;
                
                DrawTile(tileIndex, paletteIndex & 0x3F, xPos, yPos);
            }
            
            for(int tileRow = 0; tileRow < 32; tileRow++) // main grid
            {
                for(int tileCol = 0; tileCol < 28; tileCol++)
                {
                    ushort vram = (ushort)(0x4040 + (0x20 * tileCol) + tileRow);
                    ushort pram = (ushort)(vram + 0x400);
                    int xPos = 216 - (tileCol * 8);
                    int yPos = 16 + (tileRow * 8);
                    byte tileNumber = pacmanMachine.ReadByte(vram);
                    byte paletteNumber = pacmanMachine.ReadByte(pram);
                    DrawTile(tileNumber, paletteNumber & 0x3F, xPos, yPos);
                }
            }

            for(int tileRow = 0; tileRow < 2; tileRow++) // bottom rows
            {
                for(int tileCol = 0; tileCol < 32; tileCol++)
                {
                    ushort vram = (ushort)(0x4000 + tileCol + (0x20 * tileRow));
                    ushort pram = (ushort)(vram + 0x400);
                    int xPos = 232 - (tileCol * 8);
                    int yPos = 272 + (tileRow * 8);
                    byte tileNumber = pacmanMachine.ReadByte(vram);
                    byte paletteNumber = pacmanMachine.ReadByte(pram);
                    DrawTile(tileNumber, paletteNumber & 0x3F, xPos, yPos);
                }
            }
            
            /////// Draw Sprites
            for (int sprite = 7; sprite >= 0; sprite--)
            {
                int offset = sprite * 2;
                int attr = pacmanMachine.GetSpriteRam(offset);
                
                int rawX = pacmanMachine.GetSpriteRam2(offset);
                int rawY = pacmanMachine.GetSpriteRam2(offset + 1);
                int paletteIndex = pacmanMachine.GetSpriteRam(offset + 1);
                
                int spriteIndex = (attr & 0xFC) >> 2;
                bool xFlip = (attr & 0x02) != 0;
                bool yFlip = (attr & 0x01) != 0;

                int screenX = 224 - rawX + 15;
                int screenY = 288 - 16 - rawY;
                DrawSprite(spriteIndex, paletteIndex & 0x3F, screenX, screenY, xFlip, yFlip);
            }

            _spriteBatch.End();
        }
        else if(mode == 2)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(pixelTexture,new Rectangle(0, 0, 435, 435), Color.Black); // tile grid
            int offset = 1;
            for(int i = 0; i < 16; i++)
            {
                for(int j = 0; j < 16; j++)
                {
                    int tileIndex = j * 16 + i;
                    int tileXPos = i * (tileWidth + offset) + offset;
                    int tileYPos = j * (tileWidth + offset) + offset;
                    DrawTile(tileIndex, tileViewerPalettes[tileViewerPaletteIndex], tileXPos, tileYPos);
                }
            }

            _spriteBatch.End();
        }
        else if(mode == 3)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(pixelTexture,new Rectangle(0, 0, 411, 411), Color.Black); // tile grid
            int offset = 1;
            for(int i = 0; i < 8; i++)
            {
                for(int j = 0; j < 8; j++)
                {
                    int spriteIndex = j * 8 + i;
                    int spriteXPos = i * (spriteWidth + offset) + offset;
                    int spriteYPos = j * (spriteWidth + offset) + offset;
                    DrawSprite(spriteIndex, tileViewerPalettes[tileViewerPaletteIndex], spriteXPos, spriteYPos, false, false);
                }
            }

            _spriteBatch.End();
        }

        desktop.Render();
        GraphicsDevice.SetRenderTarget(null);
        
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(_nativeRenderTarget, _renderDestination, Color.White);
        _spriteBatch.End();
    }

    int GetPixelValue(byte pixelData, int pixelIndex)
    {
        int paletteValue = 0;
        // returns the palette value of a pixel
        switch(pixelIndex)
        {
            case 0 or 4:
                if((pixelData & 0x80) != 0)
                    paletteValue += 2;
                if((pixelData & 0x08) != 0)
                    paletteValue += 1;
                break;
            case 1 or 5:
                if((pixelData & 0x40) != 0)
                    paletteValue += 2;
                if((pixelData & 0x04) != 0)
                    paletteValue += 1;
                break;
            case 2 or 6:
                if((pixelData & 0x20) != 0)
                    paletteValue += 2;
                if((pixelData & 0x02) != 0)
                    paletteValue += 1;
                break;
            case 3 or 7:
                if((pixelData & 0x10) != 0)
                    paletteValue += 2;
                if((pixelData & 0x01) != 0)
                    paletteValue += 1;
                break;
        }

        return paletteValue;
    }

    void DrawTile(int tileIndex, int paletteIndex, int xPos, int yPos)
    {
        for(int i = 0; i < 8; i++)
        {
            int x = i;
            for(int j = 0; j < 8; j++)
            {
                int y = j;
                int colorIndex = tiles[tileIndex][i, j];
                _spriteBatch.Draw(pixelTexture,
                    new Rectangle(x + xPos, y + yPos, 1, 1),
                    palettes[paletteIndex][colorIndex]);
            }
        }
    }

    void DrawSprite(int spriteIndex, int paletteIndex, int xPos, int yPos, bool flipX, bool flipY)
    {
        int x, y;
        for(int i = 0; i < 16; i++)
        {
            if (flipX)
                x = 15-i;
            else
                x = i;
            for(int j = 0; j < 16; j++)
            {
                if (flipY)
                    y = 15 - j;
                else
                    y = j;
                
                int colorIndex = sprites[spriteIndex][i, j];
                if (colorIndex == 0) continue; // transparency
                
                _spriteBatch.Draw(pixelTexture,
                    new Rectangle(x+xPos, y+yPos, 1, 1),
                    palettes[paletteIndex][colorIndex]);
            }
        }
    }

    void ReadSprites()
    {
        for(int spIndex = 0; spIndex < 64; spIndex++)
        {
            int[,] sprite = new int[16,16];
            for(int i = 0; i < 8; i++) // bottom right
            {
                byte spriteQuad = pacmanMachine.spriteMemory[(0x00 + i) + (0x40 * spIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[15-i,12+r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // top right
            {
                byte spriteQuad = pacmanMachine.spriteMemory[(0x08 + i) + (0x40 * spIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[15-i,r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // top right 2
            {
                byte spriteQuad = pacmanMachine.spriteMemory[(0x10 + i) + (0x40 * spIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[15-i,4+r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // top right 3
            {
                byte spriteQuad = pacmanMachine.spriteMemory[(0x18 + i) + (0x40 * spIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[15-i,8+r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // bottom right
            {
                byte spriteQuad = pacmanMachine.spriteMemory[(0x20 + i) + (0x40 * spIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[7-i,12+r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // top right
            {
                byte spriteQuad = pacmanMachine.spriteMemory[(0x28 + i) + (0x40 * spIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[7-i,r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // top right 2
            {
                byte spriteQuad = pacmanMachine.spriteMemory[(0x30 + i) + (0x40 * spIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[7-i,4+r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // top right 3
            {
                byte spriteQuad = pacmanMachine.spriteMemory[(0x38 + i) + (0x40 * spIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[7-i,8+r] = GetPixelValue(spriteQuad, r);
                }
            }
            sprites.Add(sprite);
        }
    }

    void ReadTiles()
    {
        for(int tileIndex = 0; tileIndex < 256; tileIndex++)
        {
            int[,] tile = new int[8,8];
            for(int i = 0; i < 8; i++) // first 8 bytes of tile
            {
                byte pixelQuad = pacmanMachine.charMemory[i + (tileIndex * 16)];
                for(int r = 4; r < 8; r++)
                {
                    tile[7-i,r] = GetPixelValue(pixelQuad,r);
                }
            }
            for(int i = 8; i < 16; i++) // second 8 bytes of tile
            {
                byte pixelQuad = pacmanMachine.charMemory[i + (tileIndex * 16)];
                for(int r = 0; r < 4; r++)
                {
                    tile[15-i,r] = GetPixelValue(pixelQuad, r);
                }
            }
            
            tiles.Add(tile);
        }
    }

    void ReadColors()
    {
        // hard-coded because the ROM stores them as intensities of output on hardware, not as color
        colors =
        [
            new Color(0, 0, 0, 0), // 0 alpha to produce transparency
            new Color(255, 0, 0, 255),
            new Color(222, 151, 81, 255),
            new Color(255, 184, 255, 255),
            new Color(0, 0, 0, 255),
            new Color(0, 255, 255, 255),
            new Color(71, 184, 255, 255),
            new Color(255, 184, 81, 255),
            new Color(0, 0, 0, 255),
            new Color(255, 255, 0, 255),
            new Color(0, 0, 0, 255),
            new Color(33, 33, 255, 255),
            new Color(0, 255, 0, 255),
            new Color(71, 184, 174, 255),
            new Color(255, 184, 174, 255),
            new Color(222, 222, 255, 255)
        ];
    }

    void ReadPalettes()
    {
        for (int i = 0; i < 32; i++)
        {
            palettes.Add([
                colors[pacmanMachine.paletteMemory[4*i + 0]],
                colors[pacmanMachine.paletteMemory[4*i + 1]],
                colors[pacmanMachine.paletteMemory[4*i + 2]],
                colors[pacmanMachine.paletteMemory[4*i + 3]]
            ]);
        }
        // second 32 palettes are just black
        for (int i = 0; i < 32; i++)
        {
            palettes.Add([
            new Color(0,0,0,255),
            new Color(0,0,0,255),
            new Color(0,0,0,255),
            new Color(0,0,0,255)]);
        }
    }

    private void RunSingleStepTests()
    {
        // Load
        IMemoryProvider sstMachine = new SSTMachine();
        cpu = new Z80(sstMachine);
        int hexCode = 0;
        int passedCount;
        
        StreamWriter sw = new("testlog.txt");
        
        Console.WriteLine($"\nMain Instructions:");
        while (hexCode <= 0xFF)
        {
            if (hexCode is 0xCB or 0xDD or 0xED or 0xFD)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"  {hexCode:X2}");
                hexCode++;
                continue;
            }
            
            string testFile = $"tests/{hexCode:x2}.json";
            
        
            string json = File.ReadAllText(testFile);
            var tests = JsonSerializer.Deserialize<List<Z80SingleStepTest>>(json);
        
            passedCount = 0;
        
            foreach (Z80SingleStepTest test in tests)
            {
                cpu.Reset();
                cpu.SetInitialCPUState(test);
                cpu.Step();
                string error = cpu.CheckFinalCPUState(test, sw);
                if (error == "")
                    passedCount++;
            }

            Console.ForegroundColor = passedCount == 1000 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write($"  {hexCode:X2}");
            if ((hexCode & 0x0F) == 0x0F)
                Console.WriteLine();
            hexCode++;
        }

        hexCode = 0x00;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"\nED Instructions:");
        while (hexCode <= 0xBF)
        {
            if (cpu._edOpcodes[hexCode].Method.Name != "Op_UNK")
            {
                string testFile = $"tests/ed {hexCode:x2}.json";
                string json = File.ReadAllText(testFile);
                var tests = JsonSerializer.Deserialize<List<Z80SingleStepTest>>(json);
            
                passedCount = 0;
            
                foreach (Z80SingleStepTest test in tests)
                {
                    cpu.Reset();
                    cpu.SetInitialCPUState(test);
                    cpu.Step();
                    string error = cpu.CheckFinalCPUState(test, sw);
                    if (error == "")
                        passedCount++;
                }

                Console.ForegroundColor = passedCount == 1000 ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write($"  {hexCode:X2}");
                if ((hexCode & 0x0F) == 0x0F)
                    Console.WriteLine();
                hexCode++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"  {hexCode:X2}");
                if ((hexCode & 0x0F) == 0x0F)
                    Console.WriteLine();
                hexCode++;
            }
        }
        
        hexCode = 0x00;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"\nDD Instructions:");
        while (hexCode <= 0xFF)
        {
            if (cpu._ddOpcodes[hexCode].Method.Name != "Op_UNK")
            {
                string testFile = $"tests/dd {hexCode:x2}.json";
                string json = File.ReadAllText(testFile);
                var tests = JsonSerializer.Deserialize<List<Z80SingleStepTest>>(json);
            
                passedCount = 0;
            
                foreach (Z80SingleStepTest test in tests)
                {
                    cpu.Reset();
                    cpu.SetInitialCPUState(test);
                    cpu.Step();
                    string error = cpu.CheckFinalCPUState(test, sw);
                    if (error == "")
                        passedCount++;
                }

                Console.ForegroundColor = passedCount == 1000 ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write($"  {hexCode:X2}");
                if ((hexCode & 0x0F) == 0x0F)
                    Console.WriteLine();
                hexCode++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"  {hexCode:X2}");
                if ((hexCode & 0x0F) == 0x0F)
                    Console.WriteLine();
                hexCode++;
            }
        }
        
        hexCode = 0x00;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"\nFD Instructions:");
        while (hexCode <= 0xFF)
        {
            if (cpu._fdOpcodes[hexCode].Method.Name != "Op_UNK")
            {
                string testFile = $"tests/fd {hexCode:x2}.json";
                string json = File.ReadAllText(testFile);
                var tests = JsonSerializer.Deserialize<List<Z80SingleStepTest>>(json);
            
                passedCount = 0;
            
                foreach (Z80SingleStepTest test in tests)
                {
                    cpu.Reset();
                    cpu.SetInitialCPUState(test);
                    cpu.Step();
                    string error = cpu.CheckFinalCPUState(test, sw);
                    if (error == "")
                        passedCount++;
                }

                Console.ForegroundColor = passedCount == 1000 ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write($"  {hexCode:X2}");
                if ((hexCode & 0x0F) == 0x0F)
                    Console.WriteLine();
                hexCode++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"  {hexCode:X2}");
                if ((hexCode & 0x0F) == 0x0F)
                    Console.WriteLine();
                hexCode++;
            }
        }
        
        hexCode = 0x00;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"\nCB Instructions:");
        while (hexCode <= 0xFF)
        {
            if (cpu._cbOpcodes[hexCode].Method.Name != "Op_UNK" || hexCode >= 0x40)
            {
                string testFile = $"tests/cb {hexCode:x2}.json";
                string json = File.ReadAllText(testFile);
                var tests = JsonSerializer.Deserialize<List<Z80SingleStepTest>>(json);
            
                passedCount = 0;
            
                foreach (Z80SingleStepTest test in tests)
                {
                    cpu.Reset();
                    cpu.SetInitialCPUState(test);
                    cpu.Step();
                    string error = cpu.CheckFinalCPUState(test, sw);
                    if (error == "")
                        passedCount++;
                }

                Console.ForegroundColor = passedCount == 1000 ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write($"  {hexCode:X2}");
                if ((hexCode & 0x0F) == 0x0F)
                    Console.WriteLine();
                hexCode++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"  {hexCode:X2}");
                if ((hexCode & 0x0F) == 0x0F)
                    Console.WriteLine();
                hexCode++;
            }
        }
        
        hexCode = 0x00;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"\nDD CB Instructions:");
        while (hexCode <= 0xFF)
        {
            string testFile = $"tests/dd cb __ {hexCode:x2}.json";
            string json = File.ReadAllText(testFile);
            var tests = JsonSerializer.Deserialize<List<Z80SingleStepTest>>(json);
        
            passedCount = 0;
        
            foreach (Z80SingleStepTest test in tests)
            {
                cpu.Reset();
                cpu.SetInitialCPUState(test);
                cpu.Step();
                string error = cpu.CheckFinalCPUState(test, sw);
                if (error == "")
                    passedCount++;
            }

            Console.ForegroundColor = passedCount == 1000 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write($"  {hexCode:X2}");
            if ((hexCode & 0x0F) == 0x0F)
                Console.WriteLine();
            hexCode++;
        }
        
        hexCode = 0x00;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"\nFD CB Instructions:");
        while (hexCode <= 0xFF)
        {
            string testFile = $"tests/fd cb __ {hexCode:x2}.json";
            string json = File.ReadAllText(testFile);
            var tests = JsonSerializer.Deserialize<List<Z80SingleStepTest>>(json);
        
            passedCount = 0;
        
            foreach (Z80SingleStepTest test in tests)
            {
                cpu.Reset();
                cpu.SetInitialCPUState(test);
                cpu.Step();
                string error = cpu.CheckFinalCPUState(test, sw);
                if (error == "")
                    passedCount++;
            }

            Console.ForegroundColor = passedCount == 1000 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write($"  {hexCode:X2}");
            if ((hexCode & 0x0F) == 0x0F)
                Console.WriteLine();
            hexCode++;
        }

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("\nTests complete. Errors written to: testlog.txt");
        sw.Close();
        Environment.Exit(0);
    }
    
    private void RunZexdocTests()
    {
        zexdocMachine = new Zexdoc();
        cpu = new Z80(zexdocMachine);
        // ... Initialization code ...
        Reg.SP = 0xF000;
        Reg.PC = 0x0100;

        while (true)
        {
            // 1. Intercept the CP/M Call 5 BEFORE executing the opcode
            if (Reg.PC == 0x0005)
            {
                HandleCpmCall();
                // This manually performs the RET and continues the loop
                continue; 
            }

            // 2. Safety check: Did we hit the exit?
            if (Reg.PC == 0x0000) break;

            // 3. Execute the instruction
            byte opcode = zexdocMachine.ReadByte(Reg.PC);
            cpu._mainOpcodes[opcode]();
        
            // Note: Your opcodes handle the PC incrementing, so we don't do it here.
        }
    
        Console.WriteLine("\nTests Finished.");
    }

    private void HandleCpmCall()
    {
        if (Reg.C == 2) // Output character
        {
            Console.Write((char)Reg.E);
        }
        else if (Reg.C == 9) // Output string
        {
            ushort addr = Reg.DE;
            byte current;
            while ((current = zexdocMachine.ReadByte(addr++)) != (byte)'$')
            {
                Console.Write((char)current);
            }
        }

        // IMPORTANT: After handling the print, we must return to where 
        // the Zexdoc code called us from.
        cpu.Op_RET(); 
    }

    private void PlayTestBeep()
    {
        int sampleRate = 44_000;
        var dynamicSound = new DynamicSoundEffectInstance(sampleRate, AudioChannels.Mono);
        short volume = short.MaxValue / 2;
        
        float frequency = 440f; // A4 note
        TimeSpan duration = TimeSpan.FromMilliseconds(100);
        int sampleCount = dynamicSound.GetSampleSizeInBytes(duration) / 2; // 2 bytes per 16-bit sample
        byte[] buffer = new byte[sampleCount * 2];
        
        for (int i = 0; i < sampleCount; i++)
        {
            // Calculate the sine value (-1.0 to 1.0)
            float time = (float)i / sampleRate;
            short sample = (short)(Math.Sin(2 * Math.PI * frequency * time) * volume);
    
            // Convert short to 2 bytes (Little Endian)
            buffer[i * 2] = (byte)(sample & 0xFF);
            buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }

        dynamicSound.SubmitBuffer(buffer);
        dynamicSound.Play();
    }
    
    public void ToggleFullscreen()
    {
        graphics.IsFullScreen = !graphics.IsFullScreen;

        // Use "Borderless" mode for a smoother experience
        graphics.HardwareModeSwitch = false; 

        if (graphics.IsFullScreen)
        {
            // Set to monitor's native resolution
            graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        }
        else
        {
            // Back to your 3x windowed mode with padding
            graphics.PreferredBackBufferWidth = (224 * 3) + (sidePadding * 2);
            graphics.PreferredBackBufferHeight = (288 * 3) + (sidePadding * 2);
        }

        graphics.ApplyChanges();
    
        // After applying changes, recalculate where the game renders
        UpdateRenderDestination(); 
    }

    private void UpdateRenderDestination()
    {
        int screenWidth = GraphicsDevice.Viewport.Width;
        int screenHeight = GraphicsDevice.Viewport.Height;

        // 1. Calculate the raw float scales
        float scaleX = (float)screenWidth / 224f;
        float scaleY = (float)screenHeight / 288f;

        // 2. Find the smallest one and "Floor" it to the nearest whole number
        // This is the "Integer Scale"
        int integerScale = (int)Math.Floor(Math.Min(scaleX, scaleY));

        // 3. Safety check: Ensure scale is at least 1
        if (integerScale < 1) integerScale = 1;

        // 4. Calculate the resulting dimensions
        int finalWidth = 224 * integerScale;
        int finalHeight = 288 * integerScale;

        // 5. Center it
        int x = (screenWidth - finalWidth) / 2;
        int y = (screenHeight - finalHeight) / 2;

        _renderDestination = new Rectangle(x, y, finalWidth, finalHeight);
    }
}
