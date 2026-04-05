using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

namespace pacman;

public class Game : Microsoft.Xna.Framework.Game
{
    int mode = 1;
    DynamicSoundEffectInstance _soundOut;
    RenderTarget2D _nativeRenderTarget;
    Texture2D _pixelTexture;
    SpriteFont _font;
    GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    Rectangle _renderDestination;
    private double _cycleAccumulator = 0;
    KeyboardState _lastState;
    private const double CPU_CLOCK_SPEED = 3072000; // 3.072 MHz
    const int resScale = 3;
    const int internalWidth = 224;
    const int internalHeight = 288;
    const int sidePadding = 20;
    bool SteppingThrough;
    const float _speedMultiplier = 1f;
    bool verticalScreenMode = false;
    float _floatScale = 1.0f;
    bool paused = false;
    
    PacManPCB _pacManPcb;
    ZEXDOC _zexdocTests;
    SSTests _singleStateTests;

    int interruptCycleCounter;
    const int CYCLES_PER_INTERRUPT = 51200;

    private List<List<string>> _games = new()
    {
        new List<string>{"roms/pacman.zip", "Pac-Man"},
        new List<string>{"roms/pacman.zip", "Pac-Man (Cocktail)"},
        new List<string>{"roms/mspacman.zip", "Ms. Pac-Man"},
        new List<string>{"roms/mspacman.zip", "Ms. Pac-Man (Cocktail)"},
        new List<string>{"roms/newpuckx.zip", "New Puck X"},
        new List<string>{"roms/matrix.zip", "Matrix Demo"}
    };
    private int _selectedIndex = 0;
    private bool _isMenuOpen = true;
    private GamePadState _oldState;
    
    string romFileName;

    public Game(string[] args)
    {
        string[] Args = args;
        _zexdocTests = new ZEXDOC();
        _singleStateTests = new SSTests();
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = (internalWidth * resScale) + (sidePadding * 2);
        _graphics.PreferredBackBufferHeight = (internalHeight * resScale) + (sidePadding * 2);
        if (Args.Length > 0)
        {
            if(Args[0] == "-f")
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
        _font = Content.Load<SpriteFont>("ArcadeFont");
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
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
        GamePadState newState =  GamePad.GetState(PlayerIndex.One);

        if (_isMenuOpen)
        {
            // up / down
            if (newState.DPad.Down == ButtonState.Pressed && _oldState.DPad.Down == ButtonState.Released)
                _selectedIndex = (_selectedIndex + 1) % _games.Count;
            if (newState.DPad.Up == ButtonState.Pressed && _oldState.DPad.Up == ButtonState.Released)
                _selectedIndex = (_selectedIndex - 1 + _games.Count) % _games.Count;
            if (newState.Buttons.A == ButtonState.Pressed && _oldState.Buttons.A == ButtonState.Released)
            {
                if (_selectedIndex == 1 || _selectedIndex == 3)
                    verticalScreenMode = true;
                else
                    verticalScreenMode = false;
                StartGame(_games[_selectedIndex][0], _games[_selectedIndex][1]);
                _isMenuOpen = false;
                paused = false;
            }
            if (newState.Buttons.Y == ButtonState.Pressed && _oldState.Buttons.Y == ButtonState.Released)
            {
                _isMenuOpen = false;
                paused = false;
            }
        }
        else
        {
            // start to open menu
            if (newState.Buttons.Y == ButtonState.Pressed && _oldState.Buttons.Y == ButtonState.Released)
            {
                _isMenuOpen = true;
                paused = true;
            }
        }
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

            if (_pacManPcb != null && !paused)
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
        
        _oldState = newState;
        _lastState = keyboardState;
        base.Update(gameTime);
    }

    private void DrawMenu()
    {
        _spriteBatch.Begin();
        _pixelTexture.SetData(new[] { Color.White });
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, 1280, 1280), Color.Black * 0.8f);

        Vector2 pos = new (100, 100);
        _spriteBatch.DrawString(_font, "SELECT GAME", pos, Color.Yellow);
        pos.Y += 60;

        for (int i = 0; i < _games.Count; i++)
        {
            Color color = (i == _selectedIndex) ? Color.Cyan : Color.White;
            string prefix = (i == _selectedIndex) ? "> " : "  ";
            
            _spriteBatch.DrawString(_font, prefix + _games[i][1], pos, color);
            pos.Y += 40;
        }
        
        _spriteBatch.End();
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
            Vector2 screenCenter = new (GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f);
            Vector2 textureCenter = new (internalWidth / 2f, internalHeight / 2f);

            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
            if (verticalScreenMode)
            {
                float rotation = MathHelper.PiOver2;
                if (_pacManPcb.secondPlayerFlip)
                {
                    _spriteBatch.Draw(_nativeRenderTarget, screenCenter, null, Color.White, rotation, textureCenter, _floatScale, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f );
                }
                else
                    _spriteBatch.Draw(_nativeRenderTarget, screenCenter, null, Color.White, rotation, textureCenter, _floatScale, SpriteEffects.None, 0f);
            }
            else
            {
                _spriteBatch.Draw(_nativeRenderTarget, screenCenter, null, Color.White, 0f, textureCenter, _floatScale, SpriteEffects.None, 0f);
            }
        }
        GraphicsDevice.SetRenderTarget(null);
        _spriteBatch.End();
        
        if(_isMenuOpen)
            DrawMenu();
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
        int screenWidth = GraphicsDevice.Viewport.Width - (sidePadding * 2);
        int screenHeight = GraphicsDevice.Viewport.Height - (sidePadding * 2);

        int contentWidth = internalWidth;
        int contentHeight = internalHeight;

        if (isRotated)
        {
            contentWidth = internalHeight;
            contentHeight = internalWidth;
        }
        
        float scaleX = screenWidth / (float)contentWidth;
        float scaleY = screenHeight / (float)contentHeight;

        _floatScale = Math.Min(scaleX, scaleY);
        
        int finalWidth = (int)(contentWidth * _floatScale);
        int finalHeight = (int)(contentHeight * _floatScale);

        int x = (GraphicsDevice.Viewport.Width - finalWidth) / 2;
        int y = (GraphicsDevice.Viewport.Height - finalHeight) / 2;

        _renderDestination = new Rectangle(x, y, finalWidth, finalHeight);
    }
}
