using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Vivarium.Biology;
using Vivarium.Config;
using Vivarium.Engine;
using Vivarium.Entities;
using Vivarium.UI;
using Vivarium.Visuals;
using Vivarium.World;

namespace Vivarium;

public enum GameState
{
    TitleScreen,
    Simulation
}

public class VivariumGame : Game
{
    public static long NextEntityId { get; set; } = 1;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _pixel;

    // Simulation Constants (read from config)
    public static double FramesPerSecond => ConfigProvider.FramesPerSecond;

    private Simulation _simulation;
    private GameState _gameState = GameState.TitleScreen;

    private double _fpsTimer;
    private int _framesCounter;
    private int _currentFps;

    private KeyboardState _previousKeyboardState;
    private MouseState _previousMouseState;

    private Camera2D _camera;
    private Inspector _inspector;
    private HUD _hud;
    private TitleScreen _titleScreen;
    private GenePoolWindow _genePoolWindow;
    private SettingsWindow _settingsWindow;
    private BrainInspectorWindow _brainInspectorWindow;
    private GenomeCensus _genomeCensus;
    private SpriteFont _sysFont;
    private SimulationGraph _simGraph;
    private WorldRenderer _worldRenderer;

    private bool _isPaused = false;
    private bool _showExitConfirmation = false;

    public VivariumGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        TargetElapsedTime = TimeSpan.FromSeconds(1d / FramesPerSecond);
        IsFixedTimeStep = true;

        // Ensure the game runs at full speed even when not in focus
        InactiveSleepTime = TimeSpan.Zero;
    }

    protected override void Initialize()
    {
        // Start in Fullscreen
        _graphics.IsFullScreen = true;
        var screenWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        var screenHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

        _graphics.PreferredBackBufferWidth = screenWidth;
        _graphics.PreferredBackBufferHeight = screenHeight;
        _graphics.HardwareModeSwitch = false;
        _graphics.SynchronizeWithVerticalRetrace = true;
        _graphics.ApplyChanges();

        _simulation = new Simulation();
        _simulation.Initialize();

        _camera = new Camera2D(GraphicsDevice);

        // Calculate zoom to fit the grid on screen
        float gridPixelWidth = _simulation.GridWidth * _simulation.CellSize;
        float gridPixelHeight = _simulation.GridHeight * _simulation.CellSize;

        _camera.MinZoom = screenHeight / gridPixelHeight;

        float zoomX = screenWidth / gridPixelWidth;
        float zoomY = screenHeight / gridPixelHeight;
        float initialZoom = Math.Min(zoomX, zoomY); // Fit entire grid

        _camera.Zoom = Math.Max(initialZoom, _camera.MinZoom);
        _camera.CenterOnGrid(_simulation.GridWidth, _simulation.GridHeight, _simulation.CellSize);

        _simGraph = new SimulationGraph(GraphicsDevice, _sysFont);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _sysFont = Content.Load<SpriteFont>("SystemFont");

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);

        _titleScreen = new TitleScreen(GraphicsDevice, _sysFont);

        _genomeCensus = new GenomeCensus(); // Initialize Census

        _inspector = new Inspector(GraphicsDevice, _sysFont, _genomeCensus);
        _genePoolWindow = new GenePoolWindow(GraphicsDevice, _sysFont, _genomeCensus);
        _settingsWindow = new SettingsWindow(GraphicsDevice, _sysFont, ConfigProvider.Current);
        _hud = new HUD(GraphicsDevice, _sysFont, _simGraph, _genePoolWindow, _settingsWindow);
        _brainInspectorWindow = new BrainInspectorWindow(GraphicsDevice, _sysFont);

        _worldRenderer = new WorldRenderer(GraphicsDevice);
        _worldRenderer.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        // Input
        var keyboardState = Keyboard.GetState();
        var mouseState = Mouse.GetState();

        if (_gameState == GameState.TitleScreen)
        {
            _titleScreen.Update(mouseState, keyboardState, _previousKeyboardState, ref _gameState, Exit);
            _previousKeyboardState = keyboardState;
            _previousMouseState = mouseState;
            base.Update(gameTime);
            return;
        }

        // ESC
        if (keyboardState.IsKeyDown(Keys.Escape) && !_previousKeyboardState.IsKeyDown(Keys.Escape))
        {
            if (_showExitConfirmation)
            {
                _showExitConfirmation = false;
            }
            else if (_genePoolWindow.IsVisible)
            {
                _genePoolWindow.IsVisible = false;
            }
            else if (_inspector.IsEntitySelected)
            {
                _inspector.Deselect();
            }
            else
            {
                _showExitConfirmation = true;
            }
        }

        // Exit Confirmation
        bool singleStep = false;

        if (_showExitConfirmation)
        {
            if (keyboardState.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter))
            {
                Exit();
            }
        }
        else
        {
            // Pause
            if (keyboardState.IsKeyDown(Keys.Space) && !_previousKeyboardState.IsKeyDown(Keys.Space))
            {
                _isPaused = !_isPaused;
            }

            // Single Step
            if (_isPaused && !_genePoolWindow.IsVisible && keyboardState.IsKeyDown(Keys.OemPeriod) && !_previousKeyboardState.IsKeyDown(Keys.OemPeriod))
            {
                singleStep = true;
            }

            // Fullscreen
            if (keyboardState.IsKeyDown(Keys.F11) && !_previousKeyboardState.IsKeyDown(Keys.F11))
            {
                ToggleFullscreen();
            }
        }

        _previousKeyboardState = keyboardState;

        // UI
        if (!_showExitConfirmation)
        {
            _hud.UpdateInput();
            _genePoolWindow.UpdateInput();

            int scrollDelta = (mouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue);
            _settingsWindow.HandleInput(mouseState, scrollDelta);
        }

        bool effectivePause = _isPaused || _genePoolWindow.IsVisible || _showExitConfirmation || _settingsWindow.IsVisible;

        if (_genePoolWindow.IsVisible)
        {
            // Only refresh if requested or periodically (every 60 ticks ~ 1 second)
            if (_genePoolWindow.RequiresRefresh || (!_isPaused && _simulation.TickCount % 60 == 0))
            {
                _genePoolWindow.RefreshData(_simulation.AgentPopulation);
                _genePoolWindow.RequiresRefresh = false;
            }
        }

        // Input Blocking
        bool uiCapturesMouse = _hud.IsMouseOver(mouseState.Position) || _genePoolWindow.IsVisible || _brainInspectorWindow.IsVisible || _settingsWindow.IsVisible;
        bool inspectorCapturesMouse = _inspector.IsMouseOver(mouseState.Position);

        // Handle Brain Inspector Request from Inspector
        if (_inspector.WantsToOpenBrainInspector)
        {
            _brainInspectorWindow.SetTarget(_inspector.BrainInspectorTarget);
            _inspector.ClearBrainInspectorRequest();
            //_inspector.Deselect(); // Close inspector to focus on brain
        }

        // Brain Inspector Logic (Pause/Step)
        if (_brainInspectorWindow.IsVisible)
        {
            _brainInspectorWindow.UpdateInput(mouseState, _previousMouseState, ref _isPaused);
        }

        if (!effectivePause || singleStep)
        {
            // Simulation
            if (singleStep)
            {
                // Enable Logging for the selected agent
                if (_inspector.IsEntitySelected && _inspector.SelectedType == EntityType.Agent)
                {
                    ActivityLog.SetTarget(_inspector.SelectedEntityId);
                    ActivityLog.Enable(_simulation.TickCount);
                }

                Span<Agent> agentPopulationSpan = _simulation.AgentPopulation.AsSpan();
                for (int i = 0; i < agentPopulationSpan.Length; i++)
                {
                    agentPopulationSpan[i].AttackVisualTimer = 0;
                    agentPopulationSpan[i].FleeVisualTimer = 0;
                }
            }
            else
            {
                // If not single stepping (running normally), disable log
                ActivityLog.Disable();
            }

            _simulation.Update();

            _simGraph.Update(gameTime, _simulation.AliveAgents, _simulation.AlivePlants);
        }
        else
        {
            Span<Agent> agentPopulationSpan = _simulation.AgentPopulation.AsSpan();
            for (int i = 0; i < agentPopulationSpan.Length; i++)
            {
                ref Agent agent = ref agentPopulationSpan[i];
                if (agent.ReproductionVisualTimer > 0)
                {
                    agent.ReproductionVisualTimer--;

                    if (agent.ReproductionVisualTimer == 0)
                    {
                        agent.ReproductionVisualTimer = 30;
                    }
                }
            }
        }

        // Inspector Input
        if (!uiCapturesMouse)
        {
            _inspector.UpdateInput(mouseState, _previousMouseState, _camera, _simulation.GridMap, _simulation.AgentPopulation, _simulation.PlantPopulation, _simulation.StructurePopulation, _simulation.CellSize);
        }

        // Camera
        Rectangle worldBounds = new(0, 0, _simulation.GridWidth * _simulation.CellSize, _simulation.GridHeight * _simulation.CellSize);
        _camera.HandleInput(Mouse.GetState(), Keyboard.GetState(), !(uiCapturesMouse || inspectorCapturesMouse), worldBounds);

        base.Update(gameTime);
        _previousMouseState = mouseState;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        if (_gameState == GameState.TitleScreen)
        {
            _titleScreen.Draw(_spriteBatch);
            base.Draw(gameTime);
            return;
        }

        // Draw World
        RenderStats stats = _worldRenderer.Draw(
            gameTime,
            _camera,
            _simulation.GridMap,
            _simulation.AgentPopulation,
            _simulation.PlantPopulation,
            _simulation.StructurePopulation,
            _inspector,
            _simulation.CellSize
        );

        // Screen Space
        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend
        );

        // HUD
        _hud.Draw(_spriteBatch, _simulation.TickCount, stats.LivingAgents, stats.LivingHerbivores, stats.LivingOmnivores, stats.LivingCarnivores, stats.LivingPlants, stats.LivingStructures);

        // Inspector
        _inspector.DrawUI(_spriteBatch, _simulation.AgentPopulation, _simulation.PlantPopulation, _simulation.StructurePopulation);

        // Gene Pool Window
        _genePoolWindow.Draw(_spriteBatch);
        _brainInspectorWindow.Draw(_spriteBatch);
        _settingsWindow.Draw(_spriteBatch);

        // Activity Log
        ActivityLog.Draw(_spriteBatch, _sysFont, GraphicsDevice);

        // Paused Text
        if (_isPaused || _genePoolWindow.IsVisible || _settingsWindow.IsVisible)
        {
            const string pausedText = "PAUSED";
            Vector2 textSize = _sysFont.MeasureString(pausedText);
            Vector2 pos = new(
                (GraphicsDevice.Viewport.Width - textSize.X) / 2,
                30
            );

            _spriteBatch.DrawString(_sysFont, pausedText, pos + new Vector2(2, 2), Color.Black);
            _spriteBatch.DrawString(_sysFont, pausedText, pos, Color.White);
        }

        // Exit Confirmation

        if (_showExitConfirmation)
        {
            DrawExitConfirmation();
        }

        UpdateFPSAndWindowTitle(gameTime, stats.LivingAgents, stats.LivingPlants, stats.LivingStructures);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawExitConfirmation()
    {
        const int width = 440;
        const int height = 150;
        Rectangle rect = new(
            (GraphicsDevice.Viewport.Width - width) / 2,
            (GraphicsDevice.Viewport.Height - height) / 2,
            width,
            height
        );

        // Use shared pixel texture
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X + 4, rect.Y + 4, width, height), Color.Black * 0.5f);
        _spriteBatch.Draw(_pixel, rect, UITheme.PanelBgColor);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, width, 2), UITheme.BorderColor);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y + height - 2, width, 2), UITheme.BorderColor);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, 2, height), UITheme.BorderColor);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X + width - 2, rect.Y, 2, height), UITheme.BorderColor);

        const string title = "EXIT APPLICATION?";
        const string subtitle = "Press ENTER to Confirm or ESC to Cancel";

        Vector2 titleSize = _sysFont.MeasureString(title);
        Vector2 subSize = _sysFont.MeasureString(subtitle);

        _spriteBatch.DrawString(_sysFont, title, new Vector2(rect.X + ((width - titleSize.X) / 2), rect.Y + 40), UITheme.HeaderColor);
        _spriteBatch.DrawString(_sysFont, subtitle, new Vector2(rect.X + ((width - subSize.X) / 2), rect.Y + 80), UITheme.TextColorPrimary);
    }

    private void UpdateFPSAndWindowTitle(GameTime gameTime, int livingAgents, int livingPlants, int livingStructures)
    {
        // FPS Counter
        _framesCounter++;

        if (_currentFps > 0)
        {
            string fpsText = $"{_currentFps} FPS";
            Vector2 textSize = _sysFont.MeasureString(fpsText);

            Vector2 textPos = new(
                GraphicsDevice.Viewport.Width - textSize.X - 20,
                GraphicsDevice.Viewport.Height - 20
            );

            _spriteBatch.DrawString(_sysFont, fpsText, textPos + new Vector2(2, 2), Color.Black);
            _spriteBatch.DrawString(_sysFont, fpsText, textPos, Color.Turquoise);
        }

        _fpsTimer += gameTime.ElapsedGameTime.TotalSeconds;

        if (_fpsTimer >= 1.0d)
        {
            Window.Title = $"Vivarium - FPS: {_framesCounter} - Agents: {livingAgents} | Plants: {livingPlants} | Structures: {livingStructures}";
            _currentFps = _framesCounter;
            _framesCounter = 0;
            _fpsTimer--;
        }
    }

    private void ToggleFullscreen()
    {
        _graphics.IsFullScreen = !_graphics.IsFullScreen;

        if (_graphics.IsFullScreen)
        {
            var screenWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            var screenHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

            _graphics.PreferredBackBufferWidth = screenWidth;
            _graphics.PreferredBackBufferHeight = screenHeight;

            _graphics.HardwareModeSwitch = false;
        }
        else
        {
            _graphics.PreferredBackBufferWidth = _simulation.GridWidth * _simulation.CellSize;
            _graphics.PreferredBackBufferHeight = _simulation.GridHeight * _simulation.CellSize;
        }

        _graphics.ApplyChanges();
    }
}