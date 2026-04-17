// Pac-Man Z80 emulator.
// Started 1/29/26

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using MonoGame.ImGuiNet;

public class Game : Microsoft.Xna.Framework.Game
{
    int mode = 1;
    DynamicSoundEffectInstance _soundOut;
    RenderTarget2D _nativeRenderTarget;
    Texture2D _pixelTexture;
    SpriteFont _font;
    GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private double _cycleAccumulator = 0;
    KeyboardState _lastState;
    KeyboardState _currentState;
    GamePadState _currentGamePadState;
    GamePadState _lastGamePadState;
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

    IArcadeMachine _activeMachine;
    ImGuiRenderer _renderer;

    int interruptCycleCounter;
    const int CYCLES_PER_INTERRUPT = 51200;

    private const int _menuTitleOffset = 60;
    private const int _menuItemOffset = 40;
    
    private List<List<string>> _menuOptions = [];
    private List<List<string>> _mainMenu =
    [
        ["Pac-Man", "roms/pacman.zip", "pacman"],
        ["Ms. Pac-Man", "roms/mspacman.zip", "pacman"],
        ["Galaxian", "roms/galaxian.zip", "galaxian"],
        ["Matrix Demo", "roms/matrix.zip", "pacman"]
    ];
    
    private List<string> _menuPlay = [ "Play", "Play (Cocktail)" ];
    private List<List<string>> _pacManOptions =
    [
        ["Normal Color", "Neon Hack"],
        ["Normal Speed", "Fast Hack"],
        ["Free Play", "1 Coin Per Game", "1 Coin Per 2 Games", "2 Coins Per Game"],
        ["1 Life", "2 Lives", "3 Lives", "5 Lives"],
        ["10,000 Bonus", "15,000 Bonus", "20,000 Bonus", "No Bonus"],
        ["Hard", "Normal"],
        ["Alternate Names", "Normal Names"]
    ];
    
    private List<List<string>> _msPacManOptions =
    [
        ["Normal Color", "Neon Hack"],
        ["Normal Speed", "Fast Hack"],
        ["Free Play", "1 Coin Per Game", "1 Coin Per 2 Games", "2 Coins Per Game"],
        ["1 Life", "2 Lives", "3 Lives", "5 Lives"],
        ["10,000 Bonus", "15,000 Bonus", "20,000 Bonus", "No Bonus"],
        ["Hard", "Normal"]
    ];
    
    private int _selectedIndex = 0;
    private int _selectedSubIndex = 0;
    private List<int> _selectedSubOptionIndices = [0, 0, 1, 2, 0, 1, 1];
    private int _menuDepth = 0;
    private bool _isMenuOpen = true;
    private bool _isDebugOpen = false;
    
    string romFileName;

    public Game(string[] args)
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = (internalWidth * resScale) + (sidePadding * 2);
        _graphics.PreferredBackBufferHeight = (internalHeight * resScale) + (sidePadding * 2);
        if (args.Length > 0)
        {
            if(args[0] == "-f")
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
        _renderer = new ImGuiRenderer(this);
        _renderer.RebuildFontAtlas();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _nativeRenderTarget = new RenderTarget2D(GraphicsDevice, internalWidth, internalHeight);
        _font = Content.Load<SpriteFont>("ArcadeFont");
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
    }

    private void StartGame(string romFilePath, string windowTitle, string machineName)
    {
        Window.Title = windowTitle;
        romFileName = romFilePath;
        CalculateFloatScale(verticalScreenMode);
        
        _soundOut = new DynamicSoundEffectInstance(44100, AudioChannels.Mono);
        // Sound Buffer Handling
        _soundOut.BufferNeeded += (_, _) =>
        {
            short[] samples = _activeMachine.GetAudioSamples();
            
            // If the audio buffer is empty, submit silence to keep the thread alive
            if (samples == null || samples.Length == 0) {
                samples = new short[441];
            }
        
            byte[] byteArray = new byte[samples.Length * 2];
            Buffer.BlockCopy(samples, 0, byteArray, 0, byteArray.Length);
            _soundOut.SubmitBuffer(byteArray);
        };
        _soundOut.Play();
        bool[] hacks = new bool[2];
        switch (machineName)
        {
            case "pacman":
                 hacks[0] = _selectedSubOptionIndices[0] == 1;
                 hacks[1] = _selectedSubOptionIndices[1] == 1;
                _activeMachine = new PacManPCB(romFileName, verticalScreenMode, hacks);
                break;
            case "galaxian":
                _activeMachine = new GalaxianPCB(romFileName, verticalScreenMode);
                break;
        }
        
        _activeMachine.InitializeGraphics(GraphicsDevice);
        _activeMachine.subOptionIndices = _selectedSubOptionIndices;
        _activeMachine.mode = mode;
    }

    private bool KeyPressed(Keys key)
    {
        return _currentState.IsKeyDown(key) && _lastState.IsKeyUp(key);
    }

    private bool ButtonPressed(Buttons button)
    {
        return _currentGamePadState.IsButtonDown(button) && _lastGamePadState.IsButtonUp(button);
    }
    
    protected override void Update(GameTime gameTime)
    {
        _currentState = Keyboard.GetState();
        _currentGamePadState =  GamePad.GetState(PlayerIndex.One);

        if (_isMenuOpen)
        {
            switch (_menuDepth)
            {
                // up / down traverse current menu
                case 0:
                {
                    if (KeyPressed(Keys.Down) || ButtonPressed(Buttons.DPadDown))
                        _selectedIndex = (_selectedIndex + 1) % _mainMenu.Count;
                    if (KeyPressed(Keys.Up) || ButtonPressed(Buttons.DPadUp))
                        _selectedIndex = (_selectedIndex - 1 + _mainMenu.Count) % _mainMenu.Count;
                    break;
                }
                case 1:
                {
                    int _totalSubItems = _menuPlay.Count + _menuOptions.Count;
                
                    if (KeyPressed(Keys.Down) || ButtonPressed(Buttons.DPadDown))
                        _selectedSubIndex = (_selectedSubIndex + 1) % _totalSubItems;
                    if (KeyPressed(Keys.Up) || ButtonPressed(Buttons.DPadUp))
                        _selectedSubIndex = (_selectedSubIndex - 1 + _totalSubItems) % _totalSubItems;
                
                    // left/right to change options
                    if (_selectedSubIndex >= _menuPlay.Count)
                    {
                        int optionIndex = _selectedSubIndex - _menuPlay.Count;
                        int maxChoices = _menuOptions[optionIndex].Count;
                    
                        if (KeyPressed(Keys.Left) || ButtonPressed(Buttons.DPadLeft))
                            _selectedSubOptionIndices[optionIndex] = (_selectedSubOptionIndices[optionIndex] - 1 + maxChoices) % maxChoices;
                        if (KeyPressed(Keys.Right) || ButtonPressed(Buttons.DPadRight))
                            _selectedSubOptionIndices[optionIndex] = (_selectedSubOptionIndices[optionIndex] + 1) % maxChoices;
                    }

                    break;
                }
            }
            
            if (KeyPressed(Keys.Enter) || ButtonPressed(Buttons.A))
            {
                switch (_menuDepth)
                {
                    case 0:
                        _menuDepth = 1;
                        break;
                    case 1 when _selectedSubIndex is >= 0 and <= 1:
                        verticalScreenMode = _selectedSubIndex is 1;
                        StartGame(_mainMenu[_selectedIndex][1], _mainMenu[_selectedIndex][0], _mainMenu[_selectedIndex][2]);
                        _isMenuOpen = false;
                        paused = false;
                        break;
                }
            }
            
            if (KeyPressed(Keys.Back) || ButtonPressed(Buttons.B))
            {
                if (_menuDepth == 1)
                {
                    _selectedSubIndex = 0;
                    _menuDepth = 0;
                }
            }
            if (KeyPressed(Keys.Y) || ButtonPressed(Buttons.Y))
            {
                _isMenuOpen = false;
                paused = false;
            }
        }
        else
        {
            // Y to open menu
            if (KeyPressed(Keys.Y) || ButtonPressed(Buttons.Y))
            {
                _isMenuOpen = true;
                paused = true;
            }

            if (KeyPressed(Keys.Delete))
            {
                _isDebugOpen = !_isDebugOpen;
            }
        }
        if(mode == 1)
        {
            if (_currentState.IsKeyDown(Keys.Enter) && 
                (_currentState.IsKeyDown(Keys.LeftAlt) || _currentState.IsKeyDown(Keys.RightAlt)) &&
                _lastState.IsKeyUp(Keys.Enter))
            {
                ToggleFullscreen();
            }

            _lastState = _currentState;
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            if (_activeMachine != null && !paused)
            {
                _cycleAccumulator += gameTime.ElapsedGameTime.TotalSeconds * CPU_CLOCK_SPEED * _speedMultiplier;

                while (_cycleAccumulator > 0)
                {
                    int cycles = _activeMachine.Step(SteppingThrough);
                    _cycleAccumulator -= cycles;

                    interruptCycleCounter += cycles;
                    if (interruptCycleCounter >= CYCLES_PER_INTERRUPT)
                    {
                        _activeMachine.TriggerVBlankInterrupt();
                        interruptCycleCounter -= CYCLES_PER_INTERRUPT;
                    }
            
                    if (cycles <= 0) break; 
                }
            }
        }
        
        _lastGamePadState = _currentGamePadState;
        _lastState = _currentState;
        base.Update(gameTime);
    }

    private void DrawMenu()
    {
        _spriteBatch.Begin();
        _pixelTexture.SetData([Color.White]);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.Black * 0.8f);

        Vector2 pos = new (100, 100);

        switch (_menuDepth)
        {
            case 0:
            {
                _spriteBatch.DrawString(_font, "SELECT GAME", pos, Color.Yellow);
                pos.Y += _menuTitleOffset;

                for (int i = 0; i < _mainMenu.Count; i++)
                {
                    Color color = (i == _selectedIndex) ? Color.Cyan : Color.White;
                    string prefix = (i == _selectedIndex) ? "> " : "  ";
            
                    _spriteBatch.DrawString(_font, prefix + _mainMenu[i][0], pos, color);
                    pos.Y += _menuItemOffset;
                    if(i == 2) pos.Y += _menuItemOffset;
                }

                break;
            }
            case 1:
            {
                if (_mainMenu[_selectedIndex][0] == "Ms. Pac-Man")
                    _menuOptions = _msPacManOptions;
                else
                    _menuOptions = _pacManOptions;
                _spriteBatch.DrawString(_font, _mainMenu[_selectedIndex][0].ToUpper(), pos, Color.Yellow);
                pos.Y += _menuTitleOffset;

                for (int i = 0; i < _menuPlay.Count; i++)
                {
                    Color color = (i == _selectedSubIndex) ? Color.Cyan : Color.White;
                    string prefix = (i == _selectedSubIndex) ? "> " : "  ";
                
                    _spriteBatch.DrawString(_font, prefix + _menuPlay[i], pos, color);
                    pos.Y += _menuItemOffset;
                }

                if (_selectedIndex < 2)
                {
                    pos.Y += 20;
                    _spriteBatch.DrawString(_font, "OPTIONS", pos, Color.Yellow);
                    pos.Y += _menuItemOffset;
            
                    for (int i = 0; i < _menuOptions.Count; i++)
                    {
                        // The actual index in the vertical list is offset by the Play buttons
                        int listIndex = _menuPlay.Count + i;
                        Color color = (listIndex == _selectedSubIndex) ? Color.Cyan : Color.White;
                
                        string optionText = _menuOptions[i][_selectedSubOptionIndices[i]];
                
                        string displayText = (listIndex == _selectedSubIndex) ? $"< {optionText} >" : $"  {optionText}";
                
                        _spriteBatch.DrawString(_font, displayText, pos, color);
                        pos.Y += _menuItemOffset;
                        if(i == 1) pos.Y += _menuItemOffset;
                    }
                }

                break;
            }
        }
        
        _spriteBatch.End();
    }

    protected override void Draw(GameTime gameTime)
    {
        // Render game screen
        GraphicsDevice.SetRenderTarget(_nativeRenderTarget);
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(sortMode: SpriteSortMode.Deferred, samplerState: SamplerState.PointClamp);
        
        if (_activeMachine != null)
        {
            var frame = _activeMachine.GetDrawRequests(_activeMachine.secondPlayerFlip);

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
                const float rotation = MathHelper.PiOver2;
                if (_activeMachine.secondPlayerFlip)
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

        if (_isMenuOpen)
        {
            DrawMenu();
        }

        if (_isDebugOpen)
        {
            DrawGraphicsViewerWindow(gameTime);
        }
    }

    void DrawGraphicsViewerWindow(GameTime gameTime)
    {
        _renderer.BeginLayout(gameTime);
        GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
        _activeMachine?.DrawDebugUI(_renderer);
        _renderer.EndLayout();
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
        CalculateFloatScale(verticalScreenMode); 
    }

    private void CalculateFloatScale(bool isRotated)
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
    }
}
