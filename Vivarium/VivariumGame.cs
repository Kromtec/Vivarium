using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Vivarium.Biology;
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

    // Simulation Constants
    public const double FramesPerSecond = 60d;

    private Simulation _simulation;
    private GameState _gameState = GameState.TitleScreen;

    private double _fpsTimer;
    private int _framesCounter;
    private int _currentFps;

    private KeyboardState _previousKeyboardState;

    private Camera2D _camera;
    private Inspector _inspector;
    private HUD _hud;
    private TitleScreen _titleScreen;
    private GenePoolWindow _genePoolWindow;
    private GenomeCensus _genomeCensus; // New shared service
    private SpriteFont _sysFont;
    private SimulationGraph _simGraph;
    private WorldRenderer _worldRenderer;

    private bool _isPaused = false;
    private bool _showExitConfirmation = false;
    private int _seed;

    public VivariumGame(int seed = 64)
    {
        _seed = seed;
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

        _simulation = new Simulation(_seed);
        _simulation.Initialize();

        _camera = new Camera2D(GraphicsDevice);

        // Calculate zoom to fit the grid on screen
        const float gridPixelWidth = Simulation.GridWidth * Simulation.CellSize;
        const float gridPixelHeight = Simulation.GridHeight * Simulation.CellSize;

        _camera.MinZoom = screenHeight / gridPixelHeight;

        float zoomX = screenWidth / gridPixelWidth;
        float zoomY = screenHeight / gridPixelHeight;
        float initialZoom = Math.Min(zoomX, zoomY); // Fit entire grid

        _camera.Zoom = Math.Max(initialZoom, _camera.MinZoom);
        _camera.CenterOnGrid(Simulation.GridWidth, Simulation.GridHeight, Simulation.CellSize);

        _simGraph = new SimulationGraph(GraphicsDevice, _sysFont);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _sysFont = Content.Load<SpriteFont>("SystemFont");
        
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _titleScreen = new TitleScreen(GraphicsDevice, _sysFont);

        _genomeCensus = new GenomeCensus(); // Initialize Census

        _inspector = new Inspector(GraphicsDevice, _sysFont, _genomeCensus);
        _simGraph = new SimulationGraph(GraphicsDevice, _sysFont);
        _genePoolWindow = new GenePoolWindow(GraphicsDevice, _sysFont, _genomeCensus);
        _hud = new HUD(GraphicsDevice, _sysFont, _simGraph, _genePoolWindow);
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
        }

        bool effectivePause = _isPaused || _genePoolWindow.IsVisible || _showExitConfirmation;

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
        bool uiCapturesMouse = _hud.IsMouseOver(mouseState.Position) || _genePoolWindow.IsVisible;

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
            _inspector.UpdateInput(_camera, _simulation.GridMap, _simulation.AgentPopulation, _simulation.PlantPopulation, _simulation.StructurePopulation, Simulation.CellSize);
        }

        // Camera
        Rectangle worldBounds = new Rectangle(0, 0, Simulation.GridWidth * Simulation.CellSize, Simulation.GridHeight * Simulation.CellSize);
        _camera.HandleInput(Mouse.GetState(), Keyboard.GetState(), !uiCapturesMouse, worldBounds);

        base.Update(gameTime);
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
            Simulation.CellSize
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

        // Activity Log
        ActivityLog.Draw(_spriteBatch, _sysFont, GraphicsDevice);

        // Paused Text
        if (_isPaused || _genePoolWindow.IsVisible)
        {
            string pausedText = "PAUSED";
            Vector2 textSize = _sysFont.MeasureString(pausedText);
            Vector2 pos = new Vector2(
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
        int width = 440;
        int height = 150;
        Rectangle rect = new Rectangle(
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

        string title = "EXIT APPLICATION?";
        string subtitle = "Press ENTER to Confirm or ESC to Cancel";

        Vector2 titleSize = _sysFont.MeasureString(title);
        Vector2 subSize = _sysFont.MeasureString(subtitle);

        _spriteBatch.DrawString(_sysFont, title, new Vector2(rect.X + (width - titleSize.X) / 2, rect.Y + 40), UITheme.HeaderColor);
        _spriteBatch.DrawString(_sysFont, subtitle, new Vector2(rect.X + (width - subSize.X) / 2, rect.Y + 80), UITheme.TextColorPrimary);
    }

    private void UpdateFPSAndWindowTitle(GameTime gameTime, int livingAgents, int livingPlants, int livingStructures)
    {
        // FPS Counter
        _framesCounter++;

        if (_currentFps > 0)
        {
            string fpsText = $"{_currentFps} FPS";
            Vector2 textSize = _sysFont.MeasureString(fpsText);

            Vector2 textPos = new Vector2(
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
            _graphics.PreferredBackBufferWidth = Simulation.GridWidth * Simulation.CellSize;
            _graphics.PreferredBackBufferHeight = Simulation.GridHeight * Simulation.CellSize;
        }

        _graphics.ApplyChanges();
    }
}