using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace pacman;

public class Game1 : Game
{
    private string[] Args;
    private SpriteBatch _spriteBatch;
    private KeyboardState _previousKeyboardState;
    Texture2D pixelTexture;
    const int resScale = 3;
    ConsoleKeyInfo menuChoice;
    int mode = 0;
    bool SteppingThrough;
    StreamWriter trace;
    
    Machine machine;
    Z80 cpu;

    long totalCyclesExecuted;
    int interruptCycleCounter;
    double emuTimer;
    double emulationSpeedPercent;
    const int pixelScale = 3;
    const int tileWidth = 8;
    const int spriteWidth = 16;
    const int CYCLES_PER_INTERRUPT = 25600;
    
    List<int[,]> tiles = [];
    List<int[,]> sprites = [];
    List<Color> colors = [];
    List<List<Color>> palettes = [];

    public Game1(string[] args)
    {
        Args = args;
        var graphics = new GraphicsDeviceManager(this);
        graphics.PreferredBackBufferWidth = 224 * resScale;
        graphics.PreferredBackBufferHeight = 288 * resScale;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        machine = new Machine();
        cpu = new Z80(machine);        

        base.Initialize();
        if (Args.Length != 0)
        {
            if (Args[0] == "-debug")
                SteppingThrough = true;
        }
        
        if(mode == 0)
        {
            Console.WriteLine("Pac-Man Menu");
            Console.WriteLine("------------");
            Console.WriteLine(" 1) Play ROM");
            Console.WriteLine(" 2) Display Char ROM");
            Console.WriteLine(" 3) Display Sprite ROM");
            Console.WriteLine(" 4) Single-Step Tests");
            Console.WriteLine(" Anything else) Quit");
            Console.WriteLine("------------");
            Console.Write(" > "); menuChoice = Console.ReadKey();
            Console.WriteLine("");
            switch (menuChoice.Key)
            {
                case ConsoleKey.D1:
                    mode = 1;
                    break;
                case ConsoleKey.D2:
                    mode = 2;
                    break;
                case ConsoleKey.D3:
                    mode = 3;
                    break;
                case ConsoleKey.D4:
                    mode = 4;
                    break;
                default:
                    Environment.Exit(0);
                    break;
            }
        }        
    }

    protected override void LoadContent()
    {
        trace = new StreamWriter("trace.txt");
        trace.AutoFlush = false;
        cpu.SetTraceWriter(trace);
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Create a 1x1 white texture
        pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        pixelTexture.SetData([Color.White]);

        cpu.Reset();
        ReadTiles();
        ReadSprites();
        ReadColors();
        ReadPalettes();
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();
        if(mode == 1)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                trace?.Close();
                Exit();
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Space))
                SteppingThrough = true;

            int cyclesThisFrame = 0;
            const int CYCLES_PER_FRAME = 51200; // 3_072_000 cycles per second / 60 frames per second

            while (cyclesThisFrame < CYCLES_PER_FRAME)
            {
                int cycles = cpu.Step(SteppingThrough);
                cyclesThisFrame += cycles;
                interruptCycleCounter += cycles;

                if(interruptCycleCounter >= CYCLES_PER_INTERRUPT)
                {
                    cpu.RequestInterrupt();
                    interruptCycleCounter -= CYCLES_PER_INTERRUPT;
                }
            }

            totalCyclesExecuted += cyclesThisFrame;

            emuTimer += gameTime.ElapsedGameTime.TotalSeconds;

            if(emuTimer >= 1.0)
            {
                double expectedCycles = 3072000 * emuTimer;
                emulationSpeedPercent = (totalCyclesExecuted / expectedCycles) * 100.0;
                Window.Title = $"Pac-Man | Speed: {emulationSpeedPercent:F1}%";
                
                totalCyclesExecuted = 0;
                emuTimer = 0;            
            }
        }
        else if(mode is 2 or 3)
        {            
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            else if (keyboardState.IsKeyDown(Keys.Tab) && _previousKeyboardState.IsKeyUp(Keys.Tab))
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
        }
        else if (mode is 4)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
        }
            
        
        _previousKeyboardState = keyboardState;
        base.Update(gameTime);
    }


    protected override void Draw(GameTime gameTime)
    {
        if(mode == 1)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // draw in three sections
            // 1: vram 4000 to 403F is the bottom two rows of tiles, right-to-left, top-to-bottom, starting off-screen two tiles to the right.
            // 2: vram 43c0 to 43ff is the top two rows of tiles, right-to-left, top-to-bottom, starting off-screen two tiles to the right.
            // 3: vram 4040 to 43bf is the main grid of the game, top-to-bottom, right-to-left, starting on screen at the top-right below section 2

            // 224x288 game pixels -> 896x1152 screen pixels
            // 912 is two tiles off-screen right
            // -16 is two tiles off-screen left

            for(int tileRow = 0; tileRow < 2; tileRow++)
            {
                for(int tileCol = 0; tileCol < 32; tileCol++)
                {
                    ushort vram = (ushort)(0x43C0 + tileCol + (0x20 * tileRow));
                    ushort pram = (ushort)(vram + 0x400);
                    int xPos = 696 - (tileCol * 8 * pixelScale);
                    int yPos = tileRow * 8 * pixelScale;
                    byte tileNumber = machine.ReadByte(vram);
                    byte paletteNumber = machine.ReadByte(pram);
                    DrawTile(tileNumber, paletteNumber, xPos, yPos);
                }
            }

            for(int tileRow = 0; tileRow < 32; tileRow++) // main grid
            {
                for(int tileCol = 0; tileCol < 28; tileCol++)
                {
                    ushort vram = (ushort)(0x4040 + (0x20 * tileCol) + tileRow);
                    ushort pram = (ushort)(vram + 0x400);
                    int xPos = 648 - (tileCol * 8 * pixelScale);
                    int yPos = 48 + (tileRow * 8 * pixelScale);
                    byte tileNumber = machine.ReadByte(vram);
                    byte paletteNumber = machine.ReadByte(pram);
                    DrawTile(tileNumber, paletteNumber, xPos, yPos);
                }
            }

            for(int tileRow = 0; tileRow < 2; tileRow++)
            {
                for(int tileCol = 0; tileCol < 32; tileCol++)
                {
                    ushort vram = (ushort)(0x4000 + tileCol + (0x20 * tileRow));
                    ushort pram = (ushort)(vram + 0x400);
                    int xPos = 696 - (tileCol * 8 * pixelScale);
                    int yPos = 816 + (tileRow * 8 * pixelScale);
                    byte tileNumber = machine.ReadByte(vram);
                    byte paletteNumber = machine.ReadByte(pram);
                    DrawTile(tileNumber, paletteNumber, xPos, yPos);
                }
            }

            _spriteBatch.End();
        }
        else if(mode == 2)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(pixelTexture,new Rectangle(0, 0, 435, 435), Color.Gray); // tile grid
            int offset = 3;
            for(int i = 0; i < 16; i++)
            {
                for(int j = 0; j < 16; j++)
                {
                    int tileIndex = j * 16 + i;
                    int tileXPos = i * (tileWidth * pixelScale + offset) + offset;
                    int tileYPos = j * (tileWidth * pixelScale + offset) + offset;
                    DrawTile(tileIndex, 1, tileXPos, tileYPos);
                }
            }

            _spriteBatch.End();
        }
        else if(mode == 3)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(pixelTexture,new Rectangle(0, 0, 411, 411), Color.Gray); // tile grid
            int offset = 3;
            for(int i = 0; i < 8; i++)
            {
                for(int j = 0; j < 8; j++)
                {
                    int spriteIndex = j * 8 + i;
                    int spriteXPos = i * (spriteWidth * pixelScale + offset) + offset;
                    int spriteYPos = j * (spriteWidth * pixelScale + offset) + offset;
                    DrawSprite(spriteIndex, spriteXPos, spriteYPos);
                }
            }

            _spriteBatch.End();
        }
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
            int x = i * pixelScale;
            for(int j = 0; j < 8; j++)
            {
                int y = j * pixelScale;
                int colorIndex = tiles[tileIndex][i, j];
                _spriteBatch.Draw(pixelTexture,
                    new Rectangle(x + xPos, y + yPos, pixelScale, pixelScale),
                    palettes[paletteIndex][colorIndex]);
            }
        }
    }

    void DrawSprite(int spriteIndex, int xPos, int yPos)
    {
        for(int i = 0; i < 16; i++)
        {
            int x = (i * pixelScale);
            for(int j = 0; j < 16; j++)
            {
                int y = (j * pixelScale);
                if(sprites[spriteIndex][i,j] == 0)
                {
                    _spriteBatch.Draw(pixelTexture,
                    new Rectangle(x+xPos, y+yPos, pixelScale, pixelScale),
                    new Color(0,0,0));
                }
                else if(sprites[spriteIndex][i,j] == 1)
                {
                    _spriteBatch.Draw(pixelTexture,
                    new Rectangle(x+xPos, y+yPos, pixelScale, pixelScale),
                    new Color(222,222,225));
                }
                else if(sprites[spriteIndex][i,j] == 2)
                {
                    _spriteBatch.Draw(pixelTexture,
                    new Rectangle(x+xPos, y+yPos, pixelScale, pixelScale),
                    new Color(33,33,255));
                }
                else if(sprites[spriteIndex][i,j] == 3)
                {
                    _spriteBatch.Draw(pixelTexture,
                    new Rectangle(x+xPos, y+yPos, pixelScale, pixelScale),
                    new Color(255,0,0));
                }
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
                byte spriteQuad = machine.spriteROM[(0x00 + i) + (0x40 * spIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[15-i,12+r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // top right
            {
                byte spriteQuad = machine.spriteROM[(0x08 + i) + (0x40 * spIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[15-i,r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // top right 2
            {
                byte spriteQuad = machine.spriteROM[(0x10 + i) + (0x40 * spIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[15-i,4+r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // top right 3
            {
                byte spriteQuad = machine.spriteROM[(0x18 + i) + (0x40 * spIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[15-i,8+r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // bottom right
            {
                byte spriteQuad = machine.spriteROM[(0x20 + i) + (0x40 * spIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[7-i,12+r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // top right
            {
                byte spriteQuad = machine.spriteROM[(0x28 + i) + (0x40 * spIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[7-i,r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // top right 2
            {
                byte spriteQuad = machine.spriteROM[(0x30 + i) + (0x40 * spIndex)];
                for (int r = 0; r < 4; r++)
                {
                    sprite[7-i,4+r] = GetPixelValue(spriteQuad, r);
                }
            }

            for(int i = 0; i < 8; i++) // top right 3
            {
                byte spriteQuad = machine.spriteROM[(0x38 + i) + (0x40 * spIndex)];
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
                byte pixelQuad = machine.charROM[i + (tileIndex * 16)];
                for(int r = 4; r < 8; r++)
                {
                    tile[7-i,r] = GetPixelValue(pixelQuad,r);
                }
            }
            for(int i = 8; i < 16; i++) // second 8 bytes of tile
            {
                byte pixelQuad = machine.charROM[i + (tileIndex * 16)];
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
            new Color(0, 0, 0),
            new Color(255, 0, 0),
            new Color(222, 151, 81),
            new Color(255, 184, 255),
            new Color(0, 0, 0),
            new Color(0, 255, 255),
            new Color(71, 184, 255),
            new Color(255, 184, 81),
            new Color(0, 0, 0),
            new Color(255, 255, 0),
            new Color(0, 0, 0),
            new Color(33, 33, 255),
            new Color(0, 255, 0),
            new Color(71, 184, 174),
            new Color(255, 184, 174),
            new Color(222, 222, 255)
        ];
        for (int i = 16; i < 32; i++)
        {
            colors.Add(new Color(0, 0, 0));
        }
    }

    void ReadPalettes()
    {
        for (int i = 0; i < 32; i++)
        {
            palettes.Add([
                colors[machine.paletteROM[4*i + 0]],
                colors[machine.paletteROM[4*i + 1]],
                colors[machine.paletteROM[4*i + 2]],
                colors[machine.paletteROM[4*i + 3]]
            ]);
        }
    }
}
