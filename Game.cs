// Pac-Man Z80 emulator.
// Started 1/29/26

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using ImGuiNET;
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
    const float _speedMultiplier = 1f;
    bool verticalScreenMode = false;
    float _floatScale = 1.0f;
    bool paused = false;
    string romPath;

    private enum MenuState
    {
        SelectingGame,
        ConfiguringDips
    }

    private MenuState _currentMenuState = MenuState.SelectingGame;
    private int _dipVerticalIndex = 0; // Tracks which dipswitch you are tweaking
    
    IArcadeMachine _activeMachine;
    ImGuiRenderer _renderer;
    
    private int _loadState = 0;
    // 0 = Not Loading
    // 1 = Load Triggered (Waiting to Draw)
    // 2 = Loading Screen Drawn (Ready to Load ROMs)
    
    private string _pendingRomName;
    private string _pendingWindowTitle;

    int interruptCycleCounter;
    const int CYCLES_PER_INTERRUPT = 51200;

    private List<GameMenuItem> _gameMenu;
    private int _menuVerticalIndex = 0;

    private void InitializeMenu()
    {
        _gameMenu =
        [
            new GameMenuItem
            {
                GameName = "Pac-Man",
                Variants =
                [
                    new RomVariant
                    {
                        DisplayName = "Midway",
                        RomId = "pacman",
                        DipSwitches =
                        [
                            new DipSwitch { Name = "Rotation", Options = ["Standard", "Rotated"], SelectedIndex = 0},
                            new DipSwitch { Name = "Coinage", Options = ["Free Play", "1 Coin/1 Credit", "1 Coin/2 Credits", "2 Coins/1 Credit"], SelectedIndex = 1 },
                            new DipSwitch { Name = "Lives", Options = ["1", "2", "3", "5"], SelectedIndex = 2 },
                            new DipSwitch { Name = "Bonus", Options = ["10,000", "15,000", "20,000", "None"], SelectedIndex = 0 },
                            new DipSwitch { Name = "Difficulty", Options = ["Hard", "Normal"], SelectedIndex = 1 },
                            new DipSwitch { Name = "Ghost Names", Options = ["Alternate", "Normal"], SelectedIndex = 1 }
                        ]
                    },
                    new RomVariant
                    {
                        DisplayName = "Speedup Hack",
                        RomId = "pacmanf",
                        DipSwitches =
                        [
                            new DipSwitch { Name = "Rotation", Options = ["Standard", "Rotated"], SelectedIndex = 0},
                            new DipSwitch { Name = "Coinage", Options = ["Free Play", "1 Coin/1 Credit", "1 Coin/2 Credits", "2 Coins/1 Credit"], SelectedIndex = 1 },
                            new DipSwitch { Name = "Lives", Options = ["1", "2", "3", "5"], SelectedIndex = 2 },
                            new DipSwitch { Name = "Bonus", Options = ["10,000", "15,000", "20,000", "None"], SelectedIndex = 0 },
                            new DipSwitch { Name = "Difficulty", Options = ["Hard", "Normal"], SelectedIndex = 1 },
                            new DipSwitch { Name = "Ghost Names", Options = ["Alternate", "Normal"], SelectedIndex = 1 }
                        ]
                    }
                ]
            },
            
            new GameMenuItem
            {
                GameName = "Ms. Pac-Man",
                Variants =
                [
                    new RomVariant
                    {
                        DisplayName = "Midway/GCC",
                        RomId = "mspacman",
                        DipSwitches =
                        [
                            new DipSwitch { Name = "Rotation", Options = ["Standard", "Rotated"], SelectedIndex = 0},
                            new DipSwitch { Name = "Coinage", Options = ["Free Play", "1 Coin/1 Credit", "1 Coin/2 Credits", "2 Coins/1 Credit"], SelectedIndex = 1 },
                            new DipSwitch { Name = "Lives", Options = ["1", "2", "3", "5"], SelectedIndex = 2 },
                            new DipSwitch { Name = "Bonus", Options = ["10,000", "15,000", "20,000", "None"], SelectedIndex = 0 },
                            new DipSwitch { Name = "Difficulty", Options = ["Hard", "Normal"], SelectedIndex = 1 }
                        ]
                    },
                    new RomVariant
                    {
                        DisplayName = "Speedup Hack",
                        RomId = "mspacmnf",
                        DipSwitches =
                        [
                            new DipSwitch { Name = "Rotation", Options = ["Standard", "Rotated"], SelectedIndex = 0},
                            new DipSwitch { Name = "Coinage", Options = ["Free Play", "1 Coin/1 Credit", "1 Coin/2 Credits", "2 Coins/1 Credit"], SelectedIndex = 1 },
                            new DipSwitch { Name = "Lives", Options = ["1", "2", "3", "5"], SelectedIndex = 2 },
                            new DipSwitch { Name = "Bonus", Options = ["10,000", "15,000", "20,000", "None"], SelectedIndex = 0 },
                            new DipSwitch { Name = "Difficulty", Options = ["Hard", "Normal"], SelectedIndex = 1 }
                        ]
                    }
                ]
            },
            
            new GameMenuItem
            {
                GameName = "Pac-Man Plus",
                Variants =
                [
                    new RomVariant
                    {
                        DisplayName = "Midway",
                        RomId = "pacplus",
                        DipSwitches =
                        [
                            new DipSwitch { Name = "Rotation", Options = ["Standard", "Rotated"], SelectedIndex = 0},
                            new DipSwitch { Name = "Coinage", Options = ["Free Play", "1 Coin/1 Credit", "1 Coin/2 Credits", "2 Coins/1 Credit"], SelectedIndex = 1 },
                            new DipSwitch { Name = "Lives", Options = ["1", "2", "3", "5"], SelectedIndex = 2 },
                            new DipSwitch { Name = "Bonus", Options = ["10,000", "15,000", "20,000", "None"], SelectedIndex = 0 },
                            new DipSwitch { Name = "Difficulty", Options = ["Hard", "Normal"], SelectedIndex = 1 },
                            new DipSwitch { Name = "Ghost Names", Options = ["Alternate", "Normal"], SelectedIndex = 1 }
                        ]
                    }
                ]
            },
            
            new GameMenuItem
            {
                GameName = "Jr. Pac-Man",
                Variants =
                [
                    new RomVariant
                    {
                        DisplayName = "Jr. Pac-Man",
                        RomId = "jrpacman",
                        DipSwitches = 
                        [
                            new DipSwitch { Name = "Rotation", Options = ["Standard", "Rotated"], SelectedIndex = 0 }
                        ]
                    }
                ]
            },
            
            new GameMenuItem
            {
                GameName = "Matrix Effect",
                Variants =
                [
                    new RomVariant
                    {
                        DisplayName = "Demo",
                        RomId = "matrix",
                        DipSwitches = 
                        [
                            new DipSwitch { Name = "Rotation", Options = ["Standard", "Rotated"], SelectedIndex = 0 }
                        ]
                    }
                ]
            }
        ];
    }
    
    private bool _isMenuOpen = true;
    private bool _isDebugOpen = false;
    
    string romFileNameandPath;

    private List<int> ConvertDipSwitchesToIndices(RomVariant romVariant)
    {
        List<int> indices = new();

        for (int i = 0; i < romVariant.DipSwitches.Count; i++)
            indices.Add(romVariant.DipSwitches[i].SelectedIndex);

        return indices;
    }

    public Game(string[] args)
    {
        InitializeMenu();
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

        LoadConfigFile();
    }

    void LoadConfigFile()
    {
        string configFile = "config.txt";

        if (!File.Exists(configFile))
        {
            Console.WriteLine("Config file not found. Please enter the path to your ROM directory:");
            string userEnteredRomPath = Console.ReadLine();
            
            File.WriteAllText("config.txt", userEnteredRomPath);
        }
        
        List<string> lines = File.ReadAllLines(configFile).ToList();
        romPath = lines[0];
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _nativeRenderTarget = new RenderTarget2D(GraphicsDevice, internalWidth, internalHeight);
        _font = Content.Load<SpriteFont>("ArcadeFont");
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
    }

    private void StartGame(string romFileName, string windowTitle, List<int> indices)
    {
        if (indices[0] == 1)
            verticalScreenMode = true;
        DrawLoadingMessage();
        Window.Title = windowTitle;
        romFileNameandPath = romPath + romFileName;
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
        _activeMachine = new PacManPCB(romFileNameandPath, indices);
        
        _activeMachine.InitializeGraphics(GraphicsDevice);
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
        GameMenuItem currentGame = _gameMenu[_menuVerticalIndex];
        RomVariant currentVariant = currentGame.Variants[currentGame.SelectedVariantIndex];
        
        // If the loading screen was drawn last frame, do the heavy work now
        if (_loadState == 2)
        {
            List<int> indices = ConvertDipSwitchesToIndices(currentVariant);
            StartGame(_pendingRomName, _pendingWindowTitle, indices);
            _loadState = 0; // Reset state
            paused = false;
        }
        
        _currentState = Keyboard.GetState();
        _currentGamePadState =  GamePad.GetState(PlayerIndex.One);

        if (_isMenuOpen)
        {
            switch (_currentMenuState)
            {
                case MenuState.SelectingGame:
                    // Vertical Navigation (Switching Games)
                    if (KeyPressed(Keys.Up) || ButtonPressed(Buttons.DPadUp) || ButtonPressed(Buttons.LeftThumbstickUp))
                    {
                        _menuVerticalIndex--;
                        if (_menuVerticalIndex < 0) _menuVerticalIndex = _gameMenu.Count - 1; // Wrap to bottom
                    }

                    if (KeyPressed(Keys.Down) || ButtonPressed(Buttons.DPadDown) || ButtonPressed(Buttons.LeftThumbstickDown))
                    {
                        _menuVerticalIndex++;
                        if (_menuVerticalIndex >= _gameMenu.Count) _menuVerticalIndex = 0; // Wrap to top
                    }

                    // Horizontal Navigation (Switching ROM variants)
                    if ((KeyPressed(Keys.Left) || ButtonPressed(Buttons.DPadLeft) || ButtonPressed(Buttons.LeftThumbstickLeft)) && currentGame.Variants.Count > 1)
                    {
                        currentGame.SelectedVariantIndex--;
                        if (currentGame.SelectedVariantIndex < 0)
                            currentGame.SelectedVariantIndex = currentGame.Variants.Count - 1;
                    }

                    if ((KeyPressed(Keys.Right) || ButtonPressed(Buttons.DPadRight) || ButtonPressed(Buttons.LeftThumbstickRight)) && currentGame.Variants.Count > 1)
                    {
                        currentGame.SelectedVariantIndex++;
                        if (currentGame.SelectedVariantIndex >= currentGame.Variants.Count)
                            currentGame.SelectedVariantIndex = 0;
                    }

                    if ((KeyPressed(Keys.Tab) || ButtonPressed(Buttons.Back)) && currentVariant.DipSwitches.Count > 0)
                    {
                        _currentMenuState = MenuState.ConfiguringDips;
                        _dipVerticalIndex = 0;
                    }
                    
                    // Starting the Game
                    if (KeyPressed(Keys.Enter) || ButtonPressed(Buttons.A))
                    {
                        string selectedRomId = currentGame.Variants[currentGame.SelectedVariantIndex].RomId;
                        string gameName = currentGame.GameName;
                        string gameVariant = currentGame.Variants[currentGame.SelectedVariantIndex].DisplayName;

                        _pendingWindowTitle = gameName + " (" + gameVariant + ") - " + selectedRomId;
                        _pendingRomName = selectedRomId;
                        _loadState = 1; // Trigger the loading screen
                        _isMenuOpen = false; // Close the menu
                        paused = false;
                    }

                    if (KeyPressed(Keys.Y) || ButtonPressed(Buttons.Y))
                    {
                        _isMenuOpen = false;
                        paused = false;
                    }
                    break;
                
                case MenuState.ConfiguringDips:
                    // Up/Down changes WHICH dipswitch we are tweaking
                    if (KeyPressed(Keys.Up) || ButtonPressed(Buttons.DPadUp) || ButtonPressed(Buttons.LeftThumbstickUp))
                    {
                        _dipVerticalIndex--;
                        if (_dipVerticalIndex < 0)
                            _dipVerticalIndex = currentVariant.DipSwitches.Count - 1;
                    }

                    if (KeyPressed(Keys.Down) || ButtonPressed(Buttons.DPadDown) || ButtonPressed(Buttons.LeftThumbstickDown))
                    {
                        _dipVerticalIndex++;
                        if (_dipVerticalIndex >= currentVariant.DipSwitches.Count)
                            _dipVerticalIndex = 0;
                    }

                    DipSwitch activeDip = currentVariant.DipSwitches[_dipVerticalIndex];

                    // Left/Right changes the selected OPTION for that switch
                    if (KeyPressed(Keys.Left) || ButtonPressed(Buttons.DPadLeft) || ButtonPressed(Buttons.LeftThumbstickLeft))
                    {
                        activeDip.SelectedIndex--;
                        if (activeDip.SelectedIndex < 0)
                            activeDip.SelectedIndex = currentVariant.DipSwitches[_dipVerticalIndex].Options.Count - 1;
                    }

                    if (KeyPressed(Keys.Right) || ButtonPressed(Buttons.DPadRight) || ButtonPressed(Buttons.LeftThumbstickRight))
                    {
                        activeDip.SelectedIndex++;
                        if (activeDip.SelectedIndex >= currentVariant.DipSwitches[_dipVerticalIndex].Options.Count)
                            activeDip.SelectedIndex = 0;
                    }

                    // Press TAB or ESC to go back to game selection
                    if (KeyPressed(Keys.Tab) || ButtonPressed(Buttons.Back))
                    {
                        _currentMenuState = MenuState.SelectingGame;
                    }
                    break;
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
                    int cycles = _activeMachine.Step();
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
        GameMenuItem currentGame = _gameMenu[_menuVerticalIndex];
        RomVariant currentVariant = currentGame.Variants[currentGame.SelectedVariantIndex];
        
        _spriteBatch.Begin();
        _pixelTexture.SetData([Color.White]);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.Black * 0.8f);

        Vector2 pos = new (100, 100);

        _spriteBatch.DrawString(_font, "SELECT GAME", pos, Color.Yellow);
        pos.Y += 80;

        switch (_currentMenuState)
        {
            case MenuState.SelectingGame:
            {
                for (int i = 0; i < _gameMenu.Count; i++)
                {
                    GameMenuItem game = _gameMenu[i];
                    bool isSelected = (i == _menuVerticalIndex);

                    // Highlight the selected game in Yellow, others in White
                    Color gameColor = isSelected && game.Variants.Count == 1 ? Color.Cyan : Color.White;
                    _spriteBatch.DrawString(_font, game.GameName, pos, gameColor);
                    pos.Y += 30; // Move down for the next line
            
                    if (game.Variants.Count > 1)
                    {
                        // Draw with arrows to indicate horizontal scrolling
                        string variantText = $"< {game.Variants[game.SelectedVariantIndex].DisplayName} >";
                        _spriteBatch.DrawString(_font, variantText, pos + new Vector2(20, 0), isSelected ? Color.Cyan : Color.White);
                        pos.Y += 30; // Extra space for the sub-menu
                    }

                    pos.Y += 20; // Extra spacing between game blocks
                }

                break;
            }
            case MenuState.ConfiguringDips:
            {
                _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.Black * 0.8f);
            
                Vector2 dipPos = new Vector2(120, 150);
    
                // Header showing WHICH variant we are editing
                string header = $"SETTINGS: {currentGame.GameName} ({currentVariant.DisplayName})";
                _spriteBatch.DrawString(_font, header, dipPos, Color.Cyan);
                dipPos.Y += 40;

                for (int i = 0; i < currentVariant.DipSwitches.Count; i++)
                {
                    DipSwitch dip = currentVariant.DipSwitches[i];
                    bool isSelected = (i == _dipVerticalIndex);
                    Color textColor = isSelected ? Color.Yellow : Color.White;

                    // Draw the name of the setting (e.g., "Lives")
                    _spriteBatch.DrawString(_font, dip.Name, dipPos, textColor);

                    // Draw the current setting value (e.g., "< 3 >")
                    string optionText = $"< {dip.Options[dip.SelectedIndex]} >";
                    _spriteBatch.DrawString(_font, optionText, dipPos + new Vector2(200, 0), textColor);

                    dipPos.Y += 30;
                    if (i == 0) // first "dipswitch" is always rotation, separate it from the real dipswitches
                        dipPos.Y += 30;
                }

                break;
            }
        }

        _spriteBatch.End();
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_loadState == 1)
        {
            GraphicsDevice.Clear(Color.Black);
            DrawLoadingMessage();
            
            _loadState = 2;
            return;
        }
        // Render game screen
        GraphicsDevice.SetRenderTarget(_nativeRenderTarget);
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(sortMode: SpriteSortMode.Deferred, samplerState: SamplerState.PointClamp);
        
        if (_activeMachine != null)
        {
            var frame = _activeMachine.GetDrawRequests(_activeMachine.secondPlayerFlip);

            foreach (DrawRequest drawRequest in frame)
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

    void DrawLoadingMessage()
    {
        _spriteBatch.Begin();
        _pixelTexture.SetData([Color.White]);
        _spriteBatch.Draw(_pixelTexture,
            new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.Black * 0.8f);

        Vector2 pos = new(100, 100);

        _spriteBatch.DrawString(_font, "LOADING...", pos, Color.Yellow);
        _spriteBatch.End();
    }
}

public class RomVariant
{
    public string DisplayName { get; set; }
    public string RomId { get; set; } // The actual string passed to LoadRom (e.g., "pacfast")
    public List<DipSwitch> DipSwitches { get; set; } = new();
}

public class GameMenuItem
{
    public string GameName { get; set; }
    public List<RomVariant> Variants { get; set; } = new();
    
    // This remembers which variant is currently selected for this specific game
    public int SelectedVariantIndex { get; set; } = 0; 
}

public class DipSwitch
{
    public string Name { get; set; } // e.g., "Lives", "Bonus", "Coinage"
    public List<string> Options { get; set; } // e.g., ["3", "4", "5"]
    public int SelectedIndex { get; set; } = 0; // The current toggle state
    
    // Optional: You could add a byte mask here later to easily compile 
    // these settings into the raw byte the Z80 reads!
}