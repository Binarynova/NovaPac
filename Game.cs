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
    int mode = 1;
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
    bool SteppingThrough;
    const float _speedMultiplier = 1f;
    bool verticalScreenMode = false;
    
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
        if (Args[0] == "-f")
        {
            ToggleFullscreen();
        }
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromTicks(166667); // Exactly 1/60th of a second
        _graphics.SynchronizeWithVerticalRetrace = true; // VSync
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _nativeRenderTarget = new RenderTarget2D(GraphicsDevice, internalWidth, internalHeight);
        
        MyraEnvironment.Game = this;
        BuildMyraMenu();
    }

    private void BuildMyraMenu()
    {
        Grid grid = new()
        {
            ShowGridLines = false,
            RowSpacing = 8,
            ColumnSpacing = 8
        };

        grid.ColumnsProportions.Add(new Proportion(ProportionType.Part));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Part));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Part));
        grid.RowsProportions.Add(new Proportion(ProportionType.Part));
        grid.RowsProportions.Add(new Proportion(ProportionType.Part));
        grid.RowsProportions.Add(new Proportion(ProportionType.Part));
        
        Button button0 = new()
        {
            Width = 100,
            Height = 30,
            Content = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Text = "Pac-Man Rotated"
            }
        };
        Grid.SetColumn(button0, 0);
        Grid.SetRow(button0, 1);
        
        button0.Click += (_, _) =>
        {
            verticalScreenMode = true;
            StartGame("roms/pacman.zip", "Pac-Man");
            grid.Visible = false;
            Console.WriteLine("Button clicked.");
        };
        
        Button button = new()
        {
            Width = 100,
            Height = 30,
            Content = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Text = "Pac-Man"
            }
        };
        Grid.SetColumn(button, 1);
        Grid.SetRow(button, 1);
        
        button.Click += (_, _) =>
        {
            StartGame("roms/pacman.zip", "Pac-Man");
            grid.Visible = false;
            Console.WriteLine("Button clicked.");
        };
        
        
        Button button2 = new()
        {
            Width = 100,
            Height = 30,
            Content = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Text = "Matrix Demo"
            }
        };
        Grid.SetColumn(button2, 2);
        Grid.SetRow(button2, 1);
        
        button2.Click += (_, _) =>
        {
            StartGame("roms/matrix.zip", "Matrix Demo");
            grid.Visible = false;
            Console.WriteLine("Button clicked.");
        };
        
        grid.Widgets.Add(button0);
        grid.Widgets.Add(button);
        grid.Widgets.Add(button2);
        
        _desktop = new Desktop();
        _desktop.Root = grid;
        grid.Visible = true;
    }

    private void StartGame(string romFilePath, string windowTitle)
    {
        Window.Title = windowTitle;
        romFileName = romFilePath;
        UpdateRenderDestination(verticalScreenMode);
        
        _soundOut = new DynamicSoundEffectInstance(44100, AudioChannels.Mono);
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
        _soundOut.Play();
        
        _pacManPcb = new PacManPCB(romFileName, verticalScreenMode);
        _pacManPcb.InitializeGraphics(GraphicsDevice);
        _pacManPcb.mode = mode;
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

            if (_pacManPcb != null)
            {
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

        if (_pacManPcb != null)
        {
        
            var frame = _pacManPcb.GetDrawRequests(_pacManPcb.secondPlayerFlip);

            foreach (var drawRequest in frame)
            {
                _spriteBatch.Draw(drawRequest.Texture, drawRequest.Position, null,
                    Color.White, 0f, Vector2.Zero, 1f, drawRequest.Effects, 0f);
            }
            _spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);
            Vector2 screenCenter = new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f);
            Vector2 textureCenter = new Vector2(internalWidth / 2f, internalHeight / 2f);

            float rotation = 0f;
        
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            if (verticalScreenMode)
            {
                rotation = MathHelper.PiOver2;
                if (_pacManPcb.secondPlayerFlip)
                {
                    _spriteBatch.Draw(_nativeRenderTarget, screenCenter, null, Color.White, rotation, textureCenter, 3.0f, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f );
                }
                else
                    _spriteBatch.Draw(_nativeRenderTarget, screenCenter, null, Color.White, rotation, textureCenter, 3.0f, SpriteEffects.None, 0f);
            }
            else
            {
                _spriteBatch.Draw(_nativeRenderTarget, screenCenter, null, Color.White, 0f, textureCenter, 3.0f, SpriteEffects.None, 0f);
            }
        }
        GraphicsDevice.SetRenderTarget(null);
        _spriteBatch.End();
        
        // Render Myra UI
        _desktop.Render();
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
            _graphics.PreferredBackBufferWidth = (internalWidth * 3) + (sidePadding * 2);
            _graphics.PreferredBackBufferHeight = (internalHeight * 3) + (sidePadding * 2);
        }

        _graphics.ApplyChanges();
        UpdateRenderDestination(verticalScreenMode); 
    }

    private void UpdateRenderDestination(bool isRotated)
    {
        int screenWidth = GraphicsDevice.Viewport.Width;
        int screenHeight = GraphicsDevice.Viewport.Height;

        int effectiveWidth = screenWidth;
        int effectiveHeight = screenHeight;

        if (isRotated)
        {
            effectiveWidth = screenHeight;
            effectiveHeight = screenWidth;
        }
        
        float scaleX = screenWidth / (float)effectiveWidth;
        float scaleY = screenHeight / (float)effectiveHeight;

        int integerScale = (int)Math.Floor(Math.Min(scaleX, scaleY));

        if (integerScale < 1) integerScale = 1;

        int finalWidth = effectiveWidth * integerScale;
        int finalHeight = effectiveHeight * integerScale;

        int x = (screenWidth - finalWidth) / 2;
        int y = (screenHeight - finalHeight) / 2;

        _renderDestination = new Rectangle(x, y, finalWidth, finalHeight);
    }
}
