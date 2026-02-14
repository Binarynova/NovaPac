using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace pacman;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    Texture2D pixelTexture;
    private int resScale = 3;
    ConsoleKeyInfo menuChoice;
    int mode = 0;

    Machine machine;
    z80Cpu cpu;

    long totalCyclesExecuted = 0;
    double emuTimer = 0;
    double emulationSpeedPercent = 0;
    
    int[,] tile = new int[8,8];

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 224 * resScale;
        _graphics.PreferredBackBufferHeight = 288 * resScale;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        machine = new Machine();
        cpu = new z80Cpu(machine);

        base.Initialize();

        Console.WriteLine("Pac-Man Menu");
        Console.WriteLine("------------");
        Console.WriteLine(" 1) Play ROM");
        Console.WriteLine(" 2) Display Char ROM");
        Console.WriteLine(" Anything else) Quit");
        Console.WriteLine("------------");
        Console.Write(" > "); menuChoice = Console.ReadKey();
        Console.WriteLine("");
        if(menuChoice.Key == ConsoleKey.D1)
        {
            mode = 1;
        }
        else if(menuChoice.Key == ConsoleKey.D2)
        {
            mode = 2;
        }
        else
        {
            Environment.Exit(0);
        }
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Create a 1x1 white texture
        pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        pixelTexture.SetData([Color.White]);

        cpu.Reset();

        for(int i = 0; i < 8; i++)
        {
            byte pixelQuad = machine.charROM[i];
            for(int r = 0; r < 4; r++)
            {
                tile[r,7-i] = 0;
                byte lowBitMask = (byte)Math.Pow(2, r);
                byte highBitMask = (byte)Math.Pow(2,r+4);
                if((pixelQuad & highBitMask) != 0)
                {
                    tile[r,7-i] += 2;
                }
                if((pixelQuad & lowBitMask) != 0)
                {
                    tile[r,7-i] += 1;
                }                
            }
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if(mode == 1)
        {            
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            int cyclesThisFrame = 0;
            const int CYCLES_PER_FRAME = 51200; // 3_072_000 cycles per second / 60 frames per second

            while (cyclesThisFrame < CYCLES_PER_FRAME)
            {
                cyclesThisFrame += cpu.Step();
            }

            totalCyclesExecuted += cyclesThisFrame;
            cpu.InterruptPending = true;

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
        else if(mode == 2)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
        }
        
        base.Update(gameTime);
    }


    protected override void Draw(GameTime gameTime)
    {
        if(mode == 1)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            const ushort VRAM_START = 0x4C00; // or the actual base address
            const ushort VRAM_END   = 0x4FFF; // tile numbers only for now

            int tilesPerRow = 32; // how many tiles per row in this tiny debug view
            int totalTiles = VRAM_END - VRAM_START + 1;

            // The "screen rectangle" size in pixels
            int pixelSize = 4; // one pixel per tile, scaled up for visibility

            for (int i = 0; i < totalTiles; i++)
            {
                int row = i / tilesPerRow;
                int col = i % tilesPerRow;

                ushort addr = (ushort)(VRAM_START + i);
                byte tileNumber = machine.ReadByte(addr);

                // Map tile number to a simple color for debug
                Color pixelColor = new Color(
                    r: (tileNumber * 7) % 256,
                    g: (tileNumber * 13) % 256,
                    b: (tileNumber * 3) % 256
                );

                int x = col * pixelSize;
                int y = row * pixelSize;

                _spriteBatch.Draw(pixelTexture,
                    new Rectangle(x, y, pixelSize, pixelSize),
                    pixelColor);
            }

            _spriteBatch.End();
        }
        else if(mode == 2)
        {
            GraphicsDevice.Clear(Color.Black);
        }
    }        
}
