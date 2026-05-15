// Pac-Man Z80 emulator.
// Started 1/29/26

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
#if DEBUG
using ImGuiNET;
using MonoGame.ImGuiNet;
#endif
using System.Reflection;
using Microsoft.Xna.Framework.Content;

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
    KeyboardState _currentKeyboardState;
    KeyboardState _lastKeyboardState;
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
    List<SoundEffect> menuSounds = [];

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
#if DEBUG
    ImGuiRenderer _renderer;
#endif
    
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
        
        string prefix = "pacman.Content."; 
        // Swap out the default file-based manager for our embedded memory loader
        Content = new EmbeddedContentManager(Services, prefix);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromTicks(166667); // Exactly 1/60th of a second
        _graphics.SynchronizeWithVerticalRetrace = true; // VSync
    }

    protected override void Initialize()
    {
#if DEBUG
        _renderer = new ImGuiRenderer(this);
        _renderer.RebuildFontAtlas();
#endif
        base.Initialize();
        romPath = "roms/";

        ScanRoms();
    }

    protected override void LoadContent()
{
    _spriteBatch = new SpriteBatch(GraphicsDevice);
    _nativeRenderTarget = new RenderTarget2D(GraphicsDevice, internalWidth, internalHeight);
    _font = Content.Load<SpriteFont>("ArcadeFont"); // Assuming the font is still a standard .xnb
    _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);

    // 1. Get the current assembly payload
    var assembly = Assembly.GetExecutingAssembly();
    
    // 2. Embedded resources use dot-notation: "RootNamespace.FolderName.Filename.Extension"
    // Change "MyPacManProject" to your actual project namespace!
    string resourcePrefix = "pacman.Content."; 

    foreach (string name in assembly.GetManifestResourceNames())
    {
        Console.WriteLine("EMBEDDED: " + name);
    }
    
    Texture2D LoadTextureRaw(string filename)
    {
        string resourceName = resourcePrefix + filename;
        
        // Pull the raw image binary directly from the embedded assembly memory
        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream != null)
            {
                return Texture2D.FromStream(GraphicsDevice, stream);
            }
        }
        
        Console.WriteLine($"Warning: Embedded resource not found for game art at {resourceName}");
        return null;
    }

    // Your existing load assignments stay exactly the same!
    _gameMenu[0].BoxArt = LoadTextureRaw("pacman_px.png");
    _gameMenu[1].BoxArt = LoadTextureRaw("mspacman_px.png");
    _gameMenu[2].BoxArt = LoadTextureRaw("pacplus_px.png");
    _gameMenu[3].BoxArt = LoadTextureRaw("matrix_px.png");
    
    controllerButtons.Add(LoadTextureRaw("xbox_a.png"));
    controllerButtons.Add(LoadTextureRaw("xbox_b.png"));
    controllerButtons.Add(LoadTextureRaw("xbox_x.png"));
    controllerButtons.Add(LoadTextureRaw("xbox_y.png"));
    controllerButtons.Add(LoadTextureRaw("xbox_menu.png"));
    controllerButtons.Add(LoadTextureRaw("xbox_view.png"));
    
    // Assuming sounds are still standard Content pipeline .xnb files
    menuSounds.Add(Content.Load<SoundEffect>("eat_dot_0"));
    menuSounds.Add(Content.Load<SoundEffect>("eat_dot_1"));
}

    private void StartGame(string romFileName, string windowTitle, List<int> indices)
    {
        verticalScreenMode = indices[0] == 1;
        
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
        return _currentKeyboardState.IsKeyDown(key) && _lastKeyboardState.IsKeyUp(key);
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
        
        _currentKeyboardState = Keyboard.GetState();
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
                                menuSounds[0].Play();
                                currentMenuMode = MenuMode.BoxNavigation;
                            }
                            if (KeyPressed(Keys.Back) || ButtonPressed(Buttons.B))
                            {
                                menuSounds[1].Play();
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
                                menuSounds[0].Play();
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
                                menuSounds[0].Play();
                                if (configIndex == 0)
                                {
                                    currentGame.SelectedVariantIndex++;
                                    if (currentGame.SelectedVariantIndex >= currentGame.Variants.Count)
                                        currentGame.SelectedVariantIndex = 0;
                                }
                            }
                            if (KeyPressed(Keys.Back) || ButtonPressed(Buttons.B))
                            {
                                menuSounds[1].Play();
                                currentMenuMode = MenuMode.GridNavigation;
                            }
                            if (KeyPressed(Keys.Enter) || ButtonPressed(Buttons.A))
                            {
                                switch (configIndex)
                                {
                                    case 1:
                                        // show dipswitch menu
                                        menuSounds[0].Play();
                                        _currentMenuState = MenuState.ConfiguringDips;
                                        _dipVerticalIndex = 0;
                                        break;
                                    case 2:
                                        // launch game
                                        RomVariant selectedVariantToLaunch = currentGame.Variants[currentGame.SelectedVariantIndex];
                                        if (selectedVariantToLaunch.IsAvailable)
                                        {
                                            menuSounds[0].Play();
                                            string selectedRomId =
                                                currentGame.Variants[currentGame.SelectedVariantIndex].RomId;
                                            string gameName = currentGame.GameName;
                                            string gameVariant = currentGame.Variants[currentGame.SelectedVariantIndex]
                                                .DisplayName;

                                            _pendingWindowTitle =
                                                gameName + " (" + gameVariant + ") - " + selectedRomId;
                                            _pendingRomName = selectedRomId;
                                            _loadState = 1; // Trigger the loading screen
                                            _isMenuOpen = false; // Close the menu
                                            paused = false;
                                        }

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
                        menuSounds[0].Play();
                        activeDip.SelectedIndex--;
                        if (activeDip.SelectedIndex < 0)
                            activeDip.SelectedIndex = currentVariant.DipSwitches[_dipVerticalIndex].Options.Count - 1;
                    }

                    if (KeyPressed(Keys.Right) || ButtonPressed(Buttons.DPadRight) || ButtonPressed(Buttons.LeftThumbstickRight))
                    {
                        menuSounds[0].Play();
                        activeDip.SelectedIndex++;
                        if (activeDip.SelectedIndex >= currentVariant.DipSwitches[_dipVerticalIndex].Options.Count)
                            activeDip.SelectedIndex = 0;
                    }

                    // Press TAB or the B button to go back to game selection
                    if (KeyPressed(Keys.Back) || ButtonPressed(Buttons.B))
                    {
                        menuSounds[1].Play();
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
                                menuSounds[0].Play();
                                if (!paused)
                                    _currentMenuState = MenuState.SelectingGame;
                                else
                                {
                                    _isMenuOpen = !_isMenuOpen;
                                    paused = !paused;
                                }
                                break;
                            case 1:
                                menuSounds[0].Play();
                                base.Initialize();
                                _currentMenuState = MenuState.SelectingGame;
                                currentMenuMode = MenuMode.GridNavigation;
                                break;
                            case 2:
                                Exit();
                                break;
                        }
                    }
                    if (KeyPressed(Keys.Back) || ButtonPressed(Buttons.B))
                    {
                        menuSounds[1].Play();
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
                menuSounds[0].Play();
                _isMenuOpen = !_isMenuOpen;
                _currentMenuState = MenuState.ConfirmingQuit;
                paused = !paused;
            }
            if (KeyPressed(Keys.Delete))
            {
                #if DEBUG
                _isDebugOpen = !_isDebugOpen;
#endif
            }
        }
        if(mode == 1)
        {
            if (KeyPressed(Keys.F11))
            {
                ToggleFullscreen();
            }

            _lastKeyboardState = _currentKeyboardState;

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
        _lastKeyboardState = _currentKeyboardState;
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
            int gridStartX = GraphicsDevice.Viewport.Width / 2 - boxWidth - spacingX / 2;
            int gridStartY = GraphicsDevice.Viewport.Height / 2 - boxHeight - spacingY / 2 + 20;

            Vector2 titleSize = _font.MeasureString("SELECT GAME");
            _spriteBatch.DrawString(_font, "SELECT GAME", new Vector2(GraphicsDevice.Viewport.Width / 2 - titleSize.X / 2, 75), Color.Yellow);

            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 2; col++)
                {
                    int index = col + row * 2;
                    GameMenuItem game = _gameMenu[index];
                    bool isSelectedBox = selectedCol == col && selectedRow == row;

                    int xPos = gridStartX + col * (boxWidth + spacingX);
                    int yPos = gridStartY + row * (boxHeight + spacingY);

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
                        RomVariant currentVar = game.Variants[game.SelectedVariantIndex];
                        
                        // Variant Selector
                        string varText = configIndex == 0 ? $"< {currentVar.DisplayName} >" : currentVar.DisplayName;
                        Vector2 varSize = _font.MeasureString(varText);
                        _spriteBatch.DrawString(_font, varText, new Vector2(xPos + boxWidth / 2 - varSize.X / 2, yPos + boxHeight + 15), configIndex == 0 ? Color.Yellow : Color.White);

                        // Settings Button
                        string dipText = configIndex == 1 ? "> SETTINGS <" : "SETTINGS";
                        Vector2 dipSize = _font.MeasureString(dipText);
                        _spriteBatch.DrawString(_font, dipText, new Vector2(xPos + boxWidth / 2 - dipSize.X / 2, yPos + boxHeight + 45), configIndex == 1 ? Color.Yellow : Color.White);

                        // Start Button
                        string baseStartText = currentVar.IsAvailable ? "START GAME" : "ROM MISSING";
                        string startText = configIndex == 2 ? $"> {baseStartText} <" : baseStartText;
                        Vector2 startSize = _font.MeasureString(startText);
                        Color startColor;
                        if (configIndex == 2)
                        {
                            startColor = currentVar.IsAvailable ? Color.Yellow : Color.Red;
                        }
                        else
                        {
                            startColor = currentVar.IsAvailable ? Color.White : Color.DimGray;
                        }
                        _spriteBatch.DrawString(_font, startText, new Vector2(xPos + boxWidth / 2 - startSize.X / 2, yPos + boxHeight + 75), startColor);
                    }
                    else
                    {
                        // If not interacting with the box, just show the main game name underneath
                        Vector2 nameSize = _font.MeasureString(game.GameName);
                        _spriteBatch.DrawString(_font, game.GameName, new Vector2(xPos + boxWidth / 2 - nameSize.X / 2, yPos + boxHeight + 20), isSelectedBox ? Color.Cyan : Color.Gray);
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
                bool isSelected = i == QuitOptionsIndex;
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
            
            int screenWidth = _graphics.GraphicsDevice.Viewport.Width;
            int screenHeight = _graphics.GraphicsDevice.Viewport.Height;

            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
            if (verticalScreenMode)
            {
                const float rotation = MathHelper.PiOver2;
                if (_activeMachine.secondPlayerFlip)
                {
                    _spriteBatch.Draw(_nativeRenderTarget, screenCenter, null, Color.White, rotation, textureCenter, _floatScale, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f );
                    
                    _spriteBatch.DrawString(_font, "  Coin", new Vector2(screenWidth-255, 199), Color.White, rotation, textureCenter, 1, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f );
                    _spriteBatch.Draw(controllerButtons[3], new Vector2(screenWidth-252, 272), null, Color.White, rotation, textureCenter, 1, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f );
                    _spriteBatch.DrawString(_font, "  P1 Start", new Vector2(screenWidth-230, 135), Color.White, rotation, textureCenter, 1, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f );
                    _spriteBatch.Draw(controllerButtons[4], new Vector2(screenWidth-227, 275), null, Color.White, rotation, textureCenter, 1, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f );
                    _spriteBatch.DrawString(_font, "  P2 Start", new Vector2(screenWidth-205, 135), Color.White, rotation, textureCenter, 1, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f );
                    _spriteBatch.Draw(controllerButtons[5], new Vector2(screenWidth-202, 275), null, Color.White, rotation, textureCenter, 1, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f );
                    _spriteBatch.DrawString(_font, "  Menu", new Vector2(screenWidth-180, 199), Color.White, rotation, textureCenter, 1, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f );
                    _spriteBatch.Draw(controllerButtons[1], new Vector2(screenWidth-177, 272), null, Color.White, rotation, textureCenter, 1, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f );
                }
                else
                {
                    _spriteBatch.Draw(_nativeRenderTarget, screenCenter, null, Color.White, rotation, textureCenter, _floatScale, SpriteEffects.None, 0f);
                    
                    _spriteBatch.DrawString(_font, "  Coin", new Vector2(-25, screenHeight-75), Color.White, rotation, textureCenter, 1, SpriteEffects.None, 0f );
                    _spriteBatch.Draw(controllerButtons[3], new Vector2(-17, screenHeight-80), null, Color.White, rotation, textureCenter, 1, SpriteEffects.None, 0f );
                    _spriteBatch.DrawString(_font, "  P1 Start", new Vector2(-50, screenHeight-75), Color.White, rotation, textureCenter, 1, SpriteEffects.None, 0f );
                    _spriteBatch.Draw(controllerButtons[4], new Vector2(-47, screenHeight-79), null, Color.White, rotation, textureCenter, 1, SpriteEffects.None, 0f );
                    _spriteBatch.DrawString(_font, "  P2 Start", new Vector2(-77, screenHeight-75), Color.White, rotation, textureCenter, 1, SpriteEffects.None, 0f );
                    _spriteBatch.Draw(controllerButtons[5], new Vector2(-72, screenHeight-79), null, Color.White, rotation, textureCenter, 1, SpriteEffects.None, 0f );
                    _spriteBatch.DrawString(_font, "B Menu", new Vector2(-104, screenHeight-75), Color.White, rotation, textureCenter, 1, SpriteEffects.None, 0f );
                    _spriteBatch.Draw(controllerButtons[1], new Vector2(-97, screenHeight-80), null, Color.White, rotation, textureCenter, 1, SpriteEffects.None, 0f );
                }
            }
            else
            {
                _spriteBatch.Draw(_nativeRenderTarget, screenCenter, null, Color.White, 0f, textureCenter, _floatScale, SpriteEffects.None, 0f);
                
                _spriteBatch.DrawString(_font, "  Coin", new Vector2(screenWidth-180, screenHeight-125), Color.White);
                _spriteBatch.Draw(controllerButtons[3], new Rectangle(screenWidth-190, screenHeight-130, 28, 28), Color.White);
                _spriteBatch.DrawString(_font, "  P1 Start", new Vector2(screenWidth-180, screenHeight-97), Color.White);
                _spriteBatch.Draw(controllerButtons[4], new Rectangle(screenWidth-190, screenHeight-102, 24, 24), Color.White);
                _spriteBatch.DrawString(_font, "  P2 Start", new Vector2(screenWidth-180, screenHeight-72), Color.White);
                _spriteBatch.Draw(controllerButtons[5], new Rectangle(screenWidth-190, screenHeight-78, 24, 24), Color.White);
                _spriteBatch.DrawString(_font, "  Menu", new Vector2(screenWidth-180, screenHeight-47), Color.White);
                _spriteBatch.Draw(controllerButtons[1], new Rectangle(screenWidth-190, screenHeight-54, 28, 28), Color.White);
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
        #if DEBUG
        _renderer.BeginLayout(gameTime);
        GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
        _activeMachine?.DrawDebugUI(_renderer);
        _renderer.EndLayout();
#endif
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
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 800;
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

    private void ScanRoms()
    {
        foreach (var game in _gameMenu)
        {
            foreach (var variant in game.Variants)
            {
                // expected path
                string expectedPath = Path.Combine(romPath, variant.RomId + ".zip");
                // set availability flag
                variant.IsAvailable = File.Exists(expectedPath);
            }
        }
    }
}

public class RomVariant
{
    public string DisplayName { get; init; }
    public string RomId { get; init; }
    public List<DipSwitch> DipSwitches { get; init; } = [];
    public bool IsAvailable { get; set; } = false;
}

public class GameMenuItem
{
    public string GameName { get; init; }
    public List<RomVariant> Variants { get; init; } = [];
    public Texture2D BoxArt { get; set; }
    
    public int SelectedVariantIndex { get; set; } = 0; 
}

public class DipSwitch
{
    public string Name { get; init; }
    public List<string> Options { get; init; }
    public int SelectedIndex { get; set; } = 0;
}

public class EmbeddedContentManager : ContentManager
{
    private readonly Assembly _assembly;
    private readonly string _resourcePrefix;

    public EmbeddedContentManager(IServiceProvider serviceProvider, string resourcePrefix) 
        : base(serviceProvider)
    {
        _assembly = Assembly.GetExecutingAssembly();
        _resourcePrefix = resourcePrefix;
    }

    protected override Stream OpenStream(string assetName)
    {
        // Standard asset names don't include the extension, so we append .xnb
        // Replace backslashes with forward slashes just in case, then map to dot-notation
        string cleanAssetName = assetName.Replace('\\', '/').Replace('/', '.');
        string resourceName = _resourcePrefix + cleanAssetName + ".xnb";

        Stream stream = _assembly.GetManifestResourceStream(resourceName);
        
        if (stream == null)
        {
            throw new ContentLoadException($"Embedded asset '{resourceName}' not found.");
        }

        return stream;
    }
}