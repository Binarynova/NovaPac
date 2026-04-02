using System;
using System.Net.Http;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using Myra;
using Myra.Graphics2D.UI;

namespace pacman;

public class Game : Microsoft.Xna.Framework.Game
{
    int mode = 0;
    private Desktop _desktop;
    DynamicSoundEffectInstance _soundOut;
    RenderTarget2D _nativeRenderTarget;
    GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    Rectangle _renderDestination;
    private double _cycleAccumulator = 0;
    KeyboardState _lastState;
    private const double CPU_CLOCK_SPEED = 3072000; // 3.072 MHz
    private string[] Args;
    const int resScale = 3;
    const int internalWidth = 224;
    const int internalHeight = 288;
    const int sidePadding = 20;
    ConsoleKeyInfo menuChoice;
    bool SteppingThrough;
    const float _speedMultiplier = 1f;
    
    PacManPCB _pacManPcb;
    ZEXDOC _zexdocTests;
    SSTests _singleStateTests;

    int interruptCycleCounter;
    const int CYCLES_PER_INTERRUPT = 51200;
    
    string romFileName;

    public Game(string[] args)
    {
        Args = args;
        _zexdocTests = new ZEXDOC();
        _singleStateTests = new SSTests();
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = (internalWidth * resScale) + (sidePadding * 2);
        _graphics.PreferredBackBufferHeight = (internalHeight * resScale) + (sidePadding * 2);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromTicks(166667); // Exactly 1/60th of a second
        _graphics.SynchronizeWithVerticalRetrace = true; // VSync
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
            mode = 1;
            Window.Title = $"Pac-Man";
            romFileName = "roms/pacman.zip";
            //Console.WriteLine("Official ROMs:");
            //Console.WriteLine(" 1) Pac-Man");
            //
            //Console.WriteLine("\nUnofficial or Homebrew ROMs");
            //Console.WriteLine(" 2) Matrix Homebrew");
            //Console.WriteLine(" 3) New Puck-X");
            //
            //Console.WriteLine("\nDebug:");
            //Console.WriteLine(" 4) View Tile/Sprite ROM");
            //
            //Console.WriteLine("\nTests:");
            //Console.WriteLine(" 5) ZEXDOC");
            //Console.WriteLine(" 6) SSTs");
            //
            //Console.WriteLine("\nAnything else) Quit");
            //Console.Write(" > "); menuChoice = Console.ReadKey();
            //Console.WriteLine("");
            //switch (menuChoice.Key)
            //{
            //    case ConsoleKey.D1:
            //        mode = 1;
			//		Window.Title = $"Pac-Man";
            //        romFileName = "roms/pacman.zip";
            //        break;
            //    case ConsoleKey.D2:
            //        mode = 1;
            //        Window.Title = "Matrix Homebrew by Scott Lawrence";
            //        romFileName = "roms/matrix.zip";
            //        break;
            //    case ConsoleKey.D3:
            //        mode = 1;
            //        Window.Title = $"New Puck-X (Unofficial)";
            //        romFileName = "roms/newpuckx.zip";
            //        break;
            //    case ConsoleKey.D4:
            //        mode = 2;
            //        romFileName = "roms/pacman.zip";
            //        break;
            //    case ConsoleKey.D5:
            //        mode = 4;
            //        break;
            //    case ConsoleKey.D6:
            //        Console.WriteLine("Running single-step tests...");
            //        _singleStateTests.Run();
            //        break;
            //    case ConsoleKey.D7:
            //        mode = 1;
            //        Window.Title = $"Ms. Pac-Man";
            //        romFileName = "roms/mspacman.zip";
            //        break;
            //    default:
            //        Environment.Exit(0);
            //        break;
            //}
        }

        if (mode == 4)
        {
            _zexdocTests.Run();
            Environment.Exit(0);
        }
        else
        {
            _soundOut = new DynamicSoundEffectInstance(44100, AudioChannels.Mono);
            _soundOut.Play();
            _pacManPcb = new PacManPCB(romFileName);
            _pacManPcb.InitializeGraphics(GraphicsDevice);
            _pacManPcb.mode = mode;
            base.Initialize();
        }
    }

    protected override void LoadContent()
    {
        // Myra UI Begin
        _nativeRenderTarget = new RenderTarget2D(GraphicsDevice, 224, 288);
        MyraEnvironment.Game = this;

        Grid grid = new Grid
        {
            RowSpacing = 8,
            ColumnSpacing = 8
        };

        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        _desktop = new Desktop();
        _desktop.Root = grid;
        // Myra UI End
        
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Sound Buffer Handling
        _soundOut.BufferNeeded += (_, _) =>
        {
            short[] samples = _pacManPcb.GetAudioSamples();
            
            // If the audio buffer is empty, submit silence to keep the thread alive
            if (samples == null || samples.Length == 0) {
                samples = new short[441];
            }

            byte[] byteArray = new byte[samples.Length * 2];
            Buffer.BlockCopy(samples, 0, byteArray, 0, byteArray.Length);
            _soundOut.SubmitBuffer(byteArray);
        };
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
                int cycles = _pacManPcb.Step(SteppingThrough);
                _cycleAccumulator -= cycles;

                interruptCycleCounter += cycles;
                if (interruptCycleCounter >= CYCLES_PER_INTERRUPT)
                {
                    _pacManPcb.TriggerVBlankInterrupt();
                    interruptCycleCounter -= CYCLES_PER_INTERRUPT;
                }
            
                if (cycles <= 0) break; 
            }
        }
        else if(mode == 2)
        {            
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            else if (keyboardState.IsKeyDown(Keys.Left) && _lastState.IsKeyUp(Keys.Left) ||
                     keyboardState.IsKeyDown(Keys.Right) && _lastState.IsKeyUp(Keys.Right))
            {
                // switch ROMs to view
                switch(_pacManPcb.graphicsViewerMode)
                {
                    case 0:
                        _pacManPcb.graphicsViewerMode = 1;
                        break;
                    case 1:
                        _pacManPcb.graphicsViewerMode = 0;
                        break;
                }
            }
            
            else if (keyboardState.IsKeyDown(Keys.Up) && _lastState.IsKeyUp(Keys.Up))
            {
                if (_pacManPcb.tileViewerPaletteIndex == 20)
                    _pacManPcb.tileViewerPaletteIndex = 0;
                else
                    _pacManPcb.tileViewerPaletteIndex++;
            }
            
            else if (keyboardState.IsKeyDown(Keys.Down) && _lastState.IsKeyUp(Keys.Down))
            {
                if (_pacManPcb.tileViewerPaletteIndex == 0)
                    _pacManPcb.tileViewerPaletteIndex = 20;
                else
                    _pacManPcb.tileViewerPaletteIndex--;
            }
        }
        
        _lastState = keyboardState;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Render game screen
        GraphicsDevice.SetRenderTarget(_nativeRenderTarget);
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(sortMode: SpriteSortMode.Deferred, samplerState: SamplerState.PointClamp);
        
        var frame = _pacManPcb.GetDrawRequests();

        foreach (var drawRequest in frame)
        {
            _spriteBatch.Draw(drawRequest.Texture, drawRequest.Position, null,
                              Color.White, 0f, Vector2.Zero, 1f, drawRequest.Effects, 0f);
        }
        _spriteBatch.End();

        // Render Myra UI
        _desktop.Render();
        GraphicsDevice.SetRenderTarget(null);
        
        // Center game in screen
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(_nativeRenderTarget, _renderDestination, Color.White);
        _spriteBatch.End();
    }
    
    private void ToggleFullscreen()
    {
        _graphics.IsFullScreen = !_graphics.IsFullScreen;
        _graphics.HardwareModeSwitch = false; 

        if (_graphics.IsFullScreen)
        {
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        }
        else
        {
            _graphics.PreferredBackBufferWidth = (224 * 3) + (sidePadding * 2);
            _graphics.PreferredBackBufferHeight = (288 * 3) + (sidePadding * 2);
        }

        _graphics.ApplyChanges();
        UpdateRenderDestination(); 
    }

    private void UpdateRenderDestination()
    {
        int screenWidth = GraphicsDevice.Viewport.Width;
        int screenHeight = GraphicsDevice.Viewport.Height;

        float scaleX = screenWidth / 224f;
        float scaleY = screenHeight / 288f;

        int integerScale = (int)Math.Floor(Math.Min(scaleX, scaleY));

        if (integerScale < 1) integerScale = 1;

        int finalWidth = 224 * integerScale;
        int finalHeight = 288 * integerScale;

        int x = (screenWidth - finalWidth) / 2;
        int y = (screenHeight - finalHeight) / 2;

        _renderDestination = new Rectangle(x, y, finalWidth, finalHeight);
    }
}
