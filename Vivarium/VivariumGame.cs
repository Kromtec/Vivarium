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

public class VivariumGame : Game
{
    public static long NextEntityId { get; set; } = 1;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    // Simulation Constants
    private const int GridHeight = 96;
    private const int GridWidth = (int)((GridHeight / 9) * 16);
    private const int CellSize = 1280 / GridHeight;
    private const int AgentCount = GridWidth * GridHeight / 8;
    private const int PlantCount = GridWidth * GridHeight / 8;
    private const int StructureCount = GridWidth * GridHeight / 64;
    public const double FramesPerSecond = 60d;

    private Agent[] _agentPopulation;
    private Plant[] _plantPopulation;
    private Structure[] _structurePopulation;
    private Random _rng;

    private GridCell[,] _gridMap;

    private double _fpsTimer;
    private int _framesCounter;
    private int _currentFps;

    private KeyboardState _previousKeyboardState;

    private Camera2D _camera;
    private Inspector _inspector;
    private HUD _hud;
    private GenePoolWindow _genePoolWindow;
    private GenomeCensus _genomeCensus; // New shared service
    private SpriteFont _sysFont;
    private SimulationGraph _simGraph;
    private WorldRenderer _worldRenderer;

    private bool _isPaused = false;
    private long _tickCount = 0; // Deterministic time source
    private bool _showExitConfirmation = false;

    public VivariumGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        TargetElapsedTime = TimeSpan.FromSeconds(1d / FramesPerSecond);
        IsFixedTimeStep = true;
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

        _rng = new Random(64);

        _agentPopulation = new Agent[AgentCount];
        _plantPopulation = new Plant[PlantCount];
        _structurePopulation = new Structure[StructureCount];

        _gridMap = new GridCell[GridWidth, GridHeight];

        SpawnPopulation();

        _camera = new Camera2D(GraphicsDevice);

        // Calculate zoom to fit the grid on screen
        const float gridPixelWidth = GridWidth * CellSize;
        const float gridPixelHeight = GridHeight * CellSize;

        _camera.MinZoom = screenHeight / gridPixelHeight;

        float zoomX = screenWidth / gridPixelWidth;
        float zoomY = screenHeight / gridPixelHeight;
        float initialZoom = Math.Min(zoomX, zoomY); // Fit entire grid

        _camera.Zoom = Math.Max(initialZoom, _camera.MinZoom);
        _camera.CenterOnGrid(GridWidth, GridHeight, CellSize);

        _simGraph = new SimulationGraph(GraphicsDevice, _sysFont);

        base.Initialize();
    }

    private void SpawnPopulation()
    {
        SpawnStructures();
        SpawnPlants();
        SpawnAgents();
    }

    private void SpawnClustered<T>(
        Span<T> populationSpan,
        EntityType type,
        double newClusterChance,
        Func<int, int, int, T> createFactory) where T : struct, IGridEntity
    {
        const int GrowthAttempts = 10;

        for (int i = 0; i < populationSpan.Length; i++)
        {
            bool placed = false;

            // Cluster Growth
            if (i > 0 && _rng.NextDouble() > newClusterChance)
            {
                for (int attempt = 0; attempt < GrowthAttempts; attempt++)
                {
                    int parentIndex = _rng.Next(0, i);
                    T parent = populationSpan[parentIndex];

                    int dx = _rng.Next(-1, 2);
                    int dy = _rng.Next(-1, 2);
                    if (dx == 0 && dy == 0) continue;

                    int tx = (parent.X + dx + GridWidth) % GridWidth;
                    int ty = (parent.Y + dy + GridHeight) % GridHeight;

                    if (_gridMap[tx, ty] == GridCell.Empty)
                    {
                        T newItem = createFactory(i, tx, ty);

                        populationSpan[i] = newItem;
                        _gridMap[tx, ty] = new GridCell(type, i);

                        placed = true;
                        break;
                    }
                }
            }

            // Fallback
            if (!placed)
            {
                if (WorldSensor.TryGetRandomEmptySpot(_gridMap, out int x, out int y, _rng))
                {
                    T newItem = createFactory(i, x, y);

                    populationSpan[i] = newItem;
                    _gridMap[x, y] = new GridCell(type, i);
                }
            }
        }
    }

    private void SpawnStructures()
    {
        SpawnClustered(
        _structurePopulation.AsSpan(),
        EntityType.Structure,
        newClusterChance: 0.2,
        createFactory: (index, x, y) => Structure.Create(index, x, y)
    );
    }

    private void SpawnPlants()
    {
        SpawnClustered(
            _plantPopulation.AsSpan(),
            EntityType.Plant,
            newClusterChance: 0.1,
            createFactory: (index, x, y) => Plant.Create(index, x, y, _rng)
        );
    }

    private void SpawnAgents()
    {
        Span<Agent> agentPopulationSpan = _agentPopulation.AsSpan();

        for (int index = 0; index < agentPopulationSpan.Length; index++)
        {
            if (WorldSensor.TryGetRandomEmptySpot(_gridMap, out int x, out int y, _rng))
            {
                GridCell occupiedCell = _gridMap[x, y];

                if (occupiedCell.Type != EntityType.Empty)
                {
                    throw new Exception($"FATAL LOGIC ERROR: TryGetRandomEmptySpot says {x},{y} is empty, but found: {occupiedCell.Type} #{occupiedCell.Index}. \n" +
                                        $"This proves that (Cell == GridCell.Empty) returns TRUE, although it should be FALSE.");
                }
                var agent = Agent.Create(index, x, y, _rng);
                agentPopulationSpan[index] = agent;
                _gridMap[agent.X, agent.Y] = new GridCell(EntityType.Agent, index);
            }
        }
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _sysFont = Content.Load<SpriteFont>("SystemFont");

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
            if (_genePoolWindow.RequiresRefresh || (!_isPaused && _tickCount % 60 == 0))
            {
                _genePoolWindow.RefreshData(_agentPopulation);
                _genePoolWindow.RequiresRefresh = false;
            }
        }

        // Input Blocking
        var mouseState = Mouse.GetState();
        bool uiCapturesMouse = _hud.IsMouseOver(mouseState.Position) || _genePoolWindow.IsVisible;

        if (!effectivePause || singleStep)
        {
            // Simulation
            _tickCount++;

            Span<Agent> agentPopulationSpan = _agentPopulation.AsSpan();
            Span<Plant> plantPopulationSpan = _plantPopulation.AsSpan();

            if (singleStep)
            {
                // Enable Logging for the selected agent
                if (_inspector.IsEntitySelected && _inspector.SelectedType == EntityType.Agent)
                {
                    ActivityLog.SetTarget(_inspector.SelectedEntityId);
                    ActivityLog.Enable(_tickCount);
                }

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

            // Biological Loop
            int aliveAgents = 0;
            for (int index = 0; index < agentPopulationSpan.Length; index++)
            {
                if (!agentPopulationSpan[index].IsAlive) continue;

                aliveAgents++;

                ref Agent currentAgent = ref agentPopulationSpan[index];

                // Think & Act
                // Time-Slicing
                if ((index + _tickCount) % 2 == 0)
                {
                    Brain.Think(ref currentAgent, _gridMap, _rng, _agentPopulation);
                }

                Brain.Act(ref currentAgent, _gridMap, _rng, agentPopulationSpan, plantPopulationSpan);

                // Aging & Metabolism
                currentAgent.Update(_gridMap);
            }

            int alivePlants = 0;
            for (int i = 0; i < plantPopulationSpan.Length; i++)
            {
                if (!plantPopulationSpan[i].IsAlive) continue;

                alivePlants++;

                ref Plant currentPlant = ref plantPopulationSpan[i];

                // Aging
                currentPlant.Update(_gridMap, _rng);

                // Reproduction
                if (currentPlant.CanReproduce())
                {
                    currentPlant.TryReproduce(plantPopulationSpan, _gridMap, _rng);
                }
            }

            _simGraph.Update(gameTime, aliveAgents, alivePlants);
        }
        else
        {
            Span<Agent> agentPopulationSpan = _agentPopulation.AsSpan();
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
            _inspector.UpdateInput(_camera, _gridMap, _agentPopulation, _plantPopulation, _structurePopulation, CellSize);
        }

        // Camera
        Rectangle worldBounds = new Rectangle(0, 0, GridWidth * CellSize, GridHeight * CellSize);
        _camera.HandleInput(Mouse.GetState(), Keyboard.GetState(), !uiCapturesMouse, worldBounds);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // Draw World
        RenderStats stats = _worldRenderer.Draw(
            gameTime,
            _camera,
            _gridMap,
            _agentPopulation,
            _plantPopulation,
            _structurePopulation,
            _inspector,
            CellSize
        );

        // Screen Space
        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend
        );

        // HUD
        _hud.Draw(_spriteBatch, _tickCount, stats.LivingAgents, stats.LivingHerbivores, stats.LivingOmnivores, stats.LivingCarnivores, stats.LivingPlants, stats.LivingStructures);

        // Inspector
        _inspector.DrawUI(_spriteBatch, _agentPopulation, _plantPopulation, _structurePopulation);

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

        Texture2D pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });

        _spriteBatch.Draw(pixel, new Rectangle(rect.X + 4, rect.Y + 4, width, height), Color.Black * 0.5f);
        _spriteBatch.Draw(pixel, rect, UITheme.PanelBgColor);
        _spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, width, 2), UITheme.BorderColor);
        _spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y + height - 2, width, 2), UITheme.BorderColor);
        _spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 2, height), UITheme.BorderColor);
        _spriteBatch.Draw(pixel, new Rectangle(rect.X + width - 2, rect.Y, 2, height), UITheme.BorderColor);

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
            _graphics.PreferredBackBufferWidth = GridWidth * CellSize;
            _graphics.PreferredBackBufferHeight = GridHeight * CellSize;
        }

        _graphics.ApplyChanges();
    }
}