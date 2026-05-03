// Pac-Man Z80 emulator.
// Started 1/29/26

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using MonoGame.ImGuiNet;

public enum MenuMode
{
    GridNavigation,
    BoxNavigation
}

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
    const int internalWidth = 224;
    const int internalHeight = 288;
    const int sidePadding = 20;
    const float _speedMultiplier = 1f;
    bool verticalScreenMode = false;
    float _floatScale = 1.0f;
    bool paused = false;
    string romPath;

    List<Texture2D> controllerButtons = [];

    private MenuMode currentMenuMode = MenuMode.GridNavigation;
    private int selectedCol = 0; // 2x2 boxes column
    private int selectedRow = 0; // 2x2 boxes row
    private int configIndex = 0; // 0 for variant selector, 1 for showing config menu, 2 for Start Game

    private enum MenuState
    {
        SelectingGame,
        ConfiguringDips,
        ConfirmingQuit
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

    string[] QuitOptions = ["Back to Game", "Exit to Menu", "Exit to Desktop"];
    int QuitOptionsIndex = 0;

    private void InitializeMenu()
    {
        _gameMenu =
        [
            new GameMenuItem
            {
                GameName = "Pac-Man",
                BoxArt = null,
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
                BoxArt = null,
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
                BoxArt = null,
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
                GameName = "Matrix Effect",
                BoxArt = null,
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

    private static List<int> ConvertDipSwitchesToIndices(RomVariant romVariant)
    {
        List<int> indices = [];

        foreach (DipSwitch t in romVariant.DipSwitches)
            indices.Add(t.SelectedIndex);

        return indices;
    }

    public Game(string[] args)
    {
        InitializeMenu();
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1280; //(internalWidth * resScale) + (sidePadding * 2);
        _graphics.PreferredBackBufferHeight = 800; //(internalHeight * resScale) + (sidePadding * 2);
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
        const string configFile = "config.txt";

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

        _gameMenu[0].BoxArt = Content.Load<Texture2D>("pacman_px");
        _gameMenu[1].BoxArt = Content.Load<Texture2D>("mspacman_px");
        _gameMenu[2].BoxArt = Content.Load<Texture2D>("pacplus_px");
        _gameMenu[3].BoxArt = Content.Load<Texture2D>("matrix_px");
        
        controllerButtons.Add(Content.Load<Texture2D>("xbox_a"));
        controllerButtons.Add(Content.Load<Texture2D>("xbox_b"));
        controllerButtons.Add(Content.Load<Texture2D>("xbox_x"));
        controllerButtons.Add(Content.Load<Texture2D>("xbox_y"));
        controllerButtons.Add(Content.Load<Texture2D>("xbox_menu"));
        controllerButtons.Add(Content.Load<Texture2D>("xbox_view"));
    }

    private void StartGame(string romFileName, string windowTitle, List<int> indices)
    {
        if (indices[0] == 1)
            verticalScreenMode = true;
        else
        {
            verticalScreenMode = false;
        }
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
                    switch (currentMenuMode)
                    {
                        case MenuMode.GridNavigation:
                            if (KeyPressed(Keys.Up) || ButtonPressed(Buttons.DPadUp) ||
                                ButtonPressed(Buttons.LeftThumbstickUp))
                            {
                                selectedRow--;
                                if (selectedRow < 0) selectedRow = 1; // wrap
                            }
                            if (KeyPressed(Keys.Down) || ButtonPressed(Buttons.DPadDown) ||
                                ButtonPressed(Buttons.LeftThumbstickDown))
                            {
                                selectedRow++;
                                if (selectedRow > 1) selectedRow = 0; // wrap
                            }
                            if (KeyPressed(Keys.Left) || ButtonPressed(Buttons.DPadLeft) ||
                                ButtonPressed(Buttons.LeftThumbstickLeft))
                            {
                                selectedCol--;
                                if (selectedCol < 0) selectedCol = 1; // wrap
                            }
                            if (KeyPressed(Keys.Right) || ButtonPressed(Buttons.DPadRight) ||
                                ButtonPressed(Buttons.LeftThumbstickRight))
                            {
                                selectedCol++;
                                if (selectedCol > 1) selectedCol = 0; // wrap
                            }
                            
                            _menuVerticalIndex = selectedCol + (selectedRow * 2);

                            if (KeyPressed(Keys.Enter) || ButtonPressed(Buttons.A))
                            {
                                currentMenuMode = MenuMode.BoxNavigation;
                            }
                            if (KeyPressed(Keys.Back) || ButtonPressed(Buttons.B))
                            {
                                _currentMenuState = MenuState.ConfirmingQuit;
                            }
                            break;
                        case MenuMode.BoxNavigation:
                            if (KeyPressed(Keys.Up) || ButtonPressed(Buttons.DPadUp) ||
                                ButtonPressed(Buttons.LeftThumbstickUp))
                            {
                                configIndex--;
                                if (configIndex < 0) configIndex = 2; // wrap
                            }
                            if (KeyPressed(Keys.Down) || ButtonPressed(Buttons.DPadDown) ||
                                ButtonPressed(Buttons.LeftThumbstickDown))
                            {
                                configIndex++;
                                if (configIndex > 2) configIndex = 0; // wrap
                            }
                            if (KeyPressed(Keys.Left) || ButtonPressed(Buttons.DPadLeft) ||
                                ButtonPressed(Buttons.LeftThumbstickLeft))
                            {
                                if (configIndex == 0)
                                {
                                    currentGame.SelectedVariantIndex--;
                                    if (currentGame.SelectedVariantIndex < 0)
                                        currentGame.SelectedVariantIndex = currentGame.Variants.Count - 1;
                                }
                            }
                            if (KeyPressed(Keys.Right) || ButtonPressed(Buttons.DPadRight) ||
                                ButtonPressed(Buttons.LeftThumbstickRight))
                            {
                                if (configIndex == 0)
                                {
                                    currentGame.SelectedVariantIndex++;
                                    if (currentGame.SelectedVariantIndex >= currentGame.Variants.Count)
                                        currentGame.SelectedVariantIndex = 0;
                                }
                            }
                            if (KeyPressed(Keys.Back) || ButtonPressed(Buttons.B))
                            {
                                currentMenuMode = MenuMode.GridNavigation;
                            }
                            if (KeyPressed(Keys.Enter) || ButtonPressed(Buttons.A))
                            {
                                switch (configIndex)
                                {
                                    case 1:
                                        // show dipswitch menu
                                        _currentMenuState = MenuState.ConfiguringDips;
                                        _dipVerticalIndex = 0;
                                        break;
                                    case 2:
                                        // launch game
                                        string selectedRomId = currentGame.Variants[currentGame.SelectedVariantIndex].RomId;
                                        string gameName = currentGame.GameName;
                                        string gameVariant = currentGame.Variants[currentGame.SelectedVariantIndex].DisplayName;

                                        _pendingWindowTitle = gameName + " (" + gameVariant + ") - " + selectedRomId;
                                        _pendingRomName = selectedRomId;
                                        _loadState = 1; // Trigger the loading screen
                                        _isMenuOpen = false; // Close the menu
                                        paused = false;
                                        break;
                                }
                            }
                            break;
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

                    // Press TAB or the B button to go back to game selection
                    if (KeyPressed(Keys.Back) || ButtonPressed(Buttons.B))
                    {
                        _currentMenuState = MenuState.SelectingGame;
                    }
                    break;
                
                case MenuState.ConfirmingQuit:
                    if (KeyPressed(Keys.Up) || ButtonPressed(Buttons.DPadUp) ||
                        ButtonPressed(Buttons.LeftThumbstickUp))
                    {
                        QuitOptionsIndex--;
                        if (QuitOptionsIndex < 0) QuitOptionsIndex = 2; // wrap
                    }
                    if (KeyPressed(Keys.Down) || ButtonPressed(Buttons.DPadDown) ||
                        ButtonPressed(Buttons.LeftThumbstickDown))
                    {
                        QuitOptionsIndex++;
                        if (QuitOptionsIndex > 2) QuitOptionsIndex = 0; // wrap
                    }
                    if (KeyPressed(Keys.Enter) || ButtonPressed(Buttons.A) )
                    {
                        switch (QuitOptionsIndex)
                        {
                            case 0:
                                if (!paused)
                                    _currentMenuState = MenuState.SelectingGame;
                                else
                                {
                                    _isMenuOpen = !_isMenuOpen;
                                    paused = !paused;
                                }
                                break;
                            case 1:
                                base.Initialize();
                                _currentMenuState = MenuState.SelectingGame;
                                break;
                            case 2:
                                Exit();
                                break;
                        }
                    }
                    if (KeyPressed(Keys.Back) || ButtonPressed(Buttons.B))
                    {
                        if (!paused)
                            _currentMenuState = MenuState.SelectingGame;
                        else
                        {
                            _isMenuOpen = !_isMenuOpen;
                            paused = !paused;
                        }
                    }
                    break;
            }
        }
        else
        {
            if (KeyPressed(Keys.Back) || ButtonPressed(Buttons.B))
            {
                _isMenuOpen = !_isMenuOpen;
                _currentMenuState = MenuState.ConfirmingQuit;
                paused = !paused;
            }
            if (KeyPressed(Keys.Delete))
            {
                _isDebugOpen = !_isDebugOpen;
            }
        }
        if(mode == 1)
        {
            if (KeyPressed(Keys.F11))
            {
                ToggleFullscreen();
            }

            _lastState = _currentState;

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
    _spriteBatch.Begin();
    _pixelTexture.SetData([Color.White]);
    _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.Black * 0.8f);
    
    switch (_currentMenuState)
    {
        case MenuState.SelectingGame:
        {
            // Grid Dimensions and Math
            const int boxWidth = 521;
            const int boxHeight = 200;
            const int spacingX = 60;
            const int spacingY = 100;
            
            // Center the entire 2x2 block on the screen
            int gridStartX = (GraphicsDevice.Viewport.Width / 2) - boxWidth - (spacingX / 2);
            int gridStartY = (GraphicsDevice.Viewport.Height / 2) - boxHeight - (spacingY / 2) + 20;

            Vector2 titleSize = _font.MeasureString("SELECT GAME");
            _spriteBatch.DrawString(_font, "SELECT GAME", new Vector2((GraphicsDevice.Viewport.Width / 2) - (titleSize.X / 2), 75), Color.Yellow);

            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 2; col++)
                {
                    int index = col + (row * 2);
                    GameMenuItem game = _gameMenu[index];
                    bool isSelectedBox = (selectedCol == col && selectedRow == row);

                    int xPos = gridStartX + (col * (boxWidth + spacingX));
                    int yPos = gridStartY + (row * (boxHeight + spacingY));

                    if (game.BoxArt != null)
                    {
                        _spriteBatch.Draw(game.BoxArt, new Rectangle(xPos, yPos, boxWidth, boxHeight), Color.White);
                    }
                    else
                    {
                        // Fallback gray box if art is missing
                        _spriteBatch.Draw(_pixelTexture, new Rectangle(xPos, yPos, boxWidth, boxHeight), Color.DarkSlateGray);
                    }
                    
                    // highlight border if this box is selected
                    Color borderColor = isSelectedBox ? Color.Cyan : Color.DimGray;
                    const int borderThickness = 4;
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(xPos, yPos, boxWidth, borderThickness), borderColor);
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(xPos, yPos + boxHeight, boxWidth, borderThickness), borderColor);
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(xPos, yPos, borderThickness, boxHeight), borderColor);
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(xPos + boxWidth, yPos, borderThickness, boxHeight + borderThickness), borderColor);

                    // Draw the Box Navigation / Info
                    if (isSelectedBox && currentMenuMode == MenuMode.BoxNavigation)
                    {
                        // Variant Selector
                        string varText = $"< {game.Variants[game.SelectedVariantIndex].DisplayName} >";
                        Vector2 varSize = _font.MeasureString(varText);
                        _spriteBatch.DrawString(_font, varText, new Vector2(xPos + (boxWidth / 2) - (varSize.X / 2), yPos + boxHeight + 15), configIndex == 0 ? Color.Yellow : Color.White);

                        // Settings Button
                        const string dipText = "SETTINGS";
                        Vector2 dipSize = _font.MeasureString(dipText);
                        _spriteBatch.DrawString(_font, dipText, new Vector2(xPos + (boxWidth / 2) - (dipSize.X / 2), yPos + boxHeight + 45), configIndex == 1 ? Color.Yellow : Color.White);

                        // Start Button
                        const string startText = "START GAME";
                        Vector2 startSize = _font.MeasureString(startText);
                        _spriteBatch.DrawString(_font, startText, new Vector2(xPos + (boxWidth / 2) - (startSize.X / 2), yPos + boxHeight + 75), configIndex == 2 ? Color.Yellow : Color.White);
                    }
                    else
                    {
                        // If not interacting with the box, just show the main game name underneath
                        Vector2 nameSize = _font.MeasureString(game.GameName);
                        _spriteBatch.DrawString(_font, game.GameName, new Vector2(xPos + (boxWidth / 2) - (nameSize.X / 2), yPos + boxHeight + 20), isSelectedBox ? Color.Cyan : Color.Gray);
                    }
                }
            }
            break;
        }
        case MenuState.ConfiguringDips:
        {
            GameMenuItem currentGame = _gameMenu[_menuVerticalIndex];
            RomVariant currentVariant = currentGame.Variants[currentGame.SelectedVariantIndex];
            
            _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.Black * 0.8f);
            
            Vector2 dipPos = new(120, 150);
    
            // Header showing variant we are editing
            string header = $"SETTINGS: {currentGame.GameName} ({currentVariant.DisplayName})";
            _spriteBatch.DrawString(_font, header, dipPos, Color.Cyan);
            dipPos.Y += 40;

            for (int i = 0; i < currentVariant.DipSwitches.Count; i++)
            {
                DipSwitch dip = currentVariant.DipSwitches[i];
                bool isSelected = i == _dipVerticalIndex;
                Color textColor = isSelected ? Color.Yellow : Color.White;

                // Draw the name of the setting (e.g., "Lives")
                _spriteBatch.DrawString(_font, dip.Name, dipPos, textColor);

                // Draw the current setting value (e.g., "< 3 >")
                string optionText = $"< {dip.Options[dip.SelectedIndex]} >";
                _spriteBatch.DrawString(_font, optionText, dipPos + new Vector2(200, 0), textColor);

                dipPos.Y += 30;
                if (i == 0) // first setting is always rotation, separate it from the real dipswitches
                    dipPos.Y += 30;
            }

            break;
        }
        case MenuState.ConfirmingQuit:
        {
            _spriteBatch.Draw(_pixelTexture,
                new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.Black * 0.8f);
            
            Vector2 dipPos = new(120, 150);

            // Header showing variant we are editing
            const string header = "PAUSED";
            _spriteBatch.DrawString(_font, header, dipPos, Color.Yellow);
            for (int i = 0; i < QuitOptions.Length; i++)
            {
                dipPos.Y += 40;
                bool isSelected = (i == QuitOptionsIndex);
                _spriteBatch.DrawString(_font, QuitOptions[i], dipPos, isSelected ? Color.Cyan : Color.White);
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
                    
                    _spriteBatch.DrawString(_font, "  Coin", new Vector2(1025, 199), Color.White, rotation, textureCenter, 1, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f );
                    _spriteBatch.Draw(controllerButtons[3], new Vector2(1028, 272), null, Color.White, rotation, textureCenter, 1, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f );
                    _spriteBatch.DrawString(_font, "  P1 Start", new Vector2(1050, 135), Color.White, rotation, textureCenter, 1, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f );
                    _spriteBatch.Draw(controllerButtons[4], new Vector2(1053, 275), null, Color.White, rotation, textureCenter, 1, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f );
                    _spriteBatch.DrawString(_font, "  P2 Start", new Vector2(1075, 135), Color.White, rotation, textureCenter, 1, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f );
                    _spriteBatch.Draw(controllerButtons[5], new Vector2(1078, 275), null, Color.White, rotation, textureCenter, 1, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f );
                    _spriteBatch.DrawString(_font, "  Menu", new Vector2(1100, 199), Color.White, rotation, textureCenter, 1, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f );
                    _spriteBatch.Draw(controllerButtons[1], new Vector2(1103, 272), null, Color.White, rotation, textureCenter, 1, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f );
                }
                else
                {
                    _spriteBatch.Draw(_nativeRenderTarget, screenCenter, null, Color.White, rotation, textureCenter, _floatScale, SpriteEffects.None, 0f);
                    
                    _spriteBatch.DrawString(_font, "  Coin", new Vector2(-25, 725), Color.White, rotation, textureCenter, 1, SpriteEffects.None, 0f );
                    _spriteBatch.Draw(controllerButtons[3], new Vector2(-17, 720), null, Color.White, rotation, textureCenter, 1, SpriteEffects.None, 0f );
                    _spriteBatch.DrawString(_font, "  P1 Start", new Vector2(-50, 725), Color.White, rotation, textureCenter, 1, SpriteEffects.None, 0f );
                    _spriteBatch.Draw(controllerButtons[4], new Vector2(-47, 721), null, Color.White, rotation, textureCenter, 1, SpriteEffects.None, 0f );
                    _spriteBatch.DrawString(_font, "  P2 Start", new Vector2(-77, 725), Color.White, rotation, textureCenter, 1, SpriteEffects.None, 0f );
                    _spriteBatch.Draw(controllerButtons[5], new Vector2(-72, 721), null, Color.White, rotation, textureCenter, 1, SpriteEffects.None, 0f );
                    _spriteBatch.DrawString(_font, "B Menu", new Vector2(-104, 725), Color.White, rotation, textureCenter, 1, SpriteEffects.None, 0f );
                    _spriteBatch.Draw(controllerButtons[1], new Vector2(-97, 720), null, Color.White, rotation, textureCenter, 1, SpriteEffects.None, 0f );
                }
            }
            else
            {
                _spriteBatch.Draw(_nativeRenderTarget, screenCenter, null, Color.White, 0f, textureCenter, _floatScale, SpriteEffects.None, 0f);
                
                _spriteBatch.DrawString(_font, "  Coin", new Vector2(1100, 675), Color.White);
                _spriteBatch.Draw(controllerButtons[3], new Rectangle(1090, 670, 28, 28), Color.White);
                _spriteBatch.DrawString(_font, "  P1 Start", new Vector2(1100,703), Color.White);
                _spriteBatch.Draw(controllerButtons[4], new Rectangle(1090, 698, 24, 24), Color.White);
                _spriteBatch.DrawString(_font, "  P2 Start", new Vector2(1100, 728), Color.White);
                _spriteBatch.Draw(controllerButtons[5], new Rectangle(1090, 722, 24, 24), Color.White);
                _spriteBatch.DrawString(_font, "  Menu", new Vector2(1100, 753), Color.White);
                _spriteBatch.Draw(controllerButtons[1], new Rectangle(1090, 746, 28, 28), Color.White);
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
            _graphics.PreferredBackBufferWidth = 1280; //(internalWidth * 3) + (sidePadding * 2);
            _graphics.PreferredBackBufferHeight = 800; //(internalHeight * 3) + (sidePadding * 2);
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
    public List<DipSwitch> DipSwitches { get; set; } = [];
}

public class GameMenuItem
{
    public string GameName { get; set; }
    public List<RomVariant> Variants { get; set; } = [];
    public Texture2D BoxArt { get; set; }
    
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