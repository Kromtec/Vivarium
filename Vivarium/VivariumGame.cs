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

    // Using .NET 10 specific array pooling optimizations if we wanted, 
    // but a simple array is fine for now.
    private Agent[] _agentPopulation;
    private Plant[] _plantPopulation;
    private Structure[] _structurePopulation;
    private Random _rng;

    // Stores the index of the agent currently at this coordinate.
    // -1 means the cell is empty.
    private GridCell[,] _gridMap;

    private double _fpsTimer;
    private int _framesCounter;
    private int _currentFps;

    // Store the keyboard state from the previous frame to detect key presses (edges)
    private KeyboardState _previousKeyboardState;

    private Camera2D _camera;
    private Inspector _inspector;
    private HUD _hud;
    private GenePoolWindow _genePoolWindow;
    private SpriteFont _sysFont;
    private SimulationGraph _simGraph;
    private WorldRenderer _worldRenderer;

    private bool _isPaused = false;
    private long _tickCount = 0; // Deterministic time source

    public VivariumGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        TargetElapsedTime = TimeSpan.FromSeconds(1d / FramesPerSecond);

        // VS 2026 / .NET 10 defaults to high-performance garbage collection settings
        // but we ensure we run at fixed time step for simulation stability.
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
        // Modern .NET 10 apps handle high-DPI scaling better
        _graphics.HardwareModeSwitch = false;
        _graphics.SynchronizeWithVerticalRetrace = true;
        _graphics.ApplyChanges();

        _rng = new Random(64);

        // .NET 10 JIT optimizes array allocation significantly
        _agentPopulation = new Agent[AgentCount];
        _plantPopulation = new Plant[PlantCount];
        _structurePopulation = new Structure[StructureCount];

        _gridMap = new GridCell[GridWidth, GridHeight];

        SpawnPopulation();

        _camera = new Camera2D(GraphicsDevice);

        // Calculate zoom to fit the grid on screen
        const float gridPixelWidth = GridWidth * CellSize;
        const float gridPixelHeight = GridHeight * CellSize;

        // Restrict zoom out so the height fits exactly the screen
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

            // 1. CLUSTER GROWTH
            // Try to attach to an existing neighbor
            if (i > 0 && _rng.NextDouble() > newClusterChance)
            {
                for (int attempt = 0; attempt < GrowthAttempts; attempt++)
                {
                    // Choose random "Parent" from those already placed
                    int parentIndex = _rng.Next(0, i);
                    T parent = populationSpan[parentIndex]; // The Interface/Generic helps here

                    // Random neighbor
                    int dx = _rng.Next(-1, 2);
                    int dy = _rng.Next(-1, 2);
                    if (dx == 0 && dy == 0) continue;

                    int tx = parent.X + dx;
                    int ty = parent.Y + dy;

                    if (tx >= 0 && tx < GridWidth && ty >= 0 && ty < GridHeight)
                    {
                        if (_gridMap[tx, ty] == GridCell.Empty)
                        {
                            // Call factory to build the concrete object
                            T newItem = createFactory(i, tx, ty);

                            populationSpan[i] = newItem;
                            _gridMap[tx, ty] = new GridCell(type, i);

                            placed = true;
                            break;
                        }
                    }
                }
            }

            // 2. FALLBACK / NEW SEED
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
                // --- DIAGNOSE START ---
                // We manually check the "raw" values of the cell, without using the == operator.
                GridCell occupiedCell = _gridMap[x, y];

                // If the Type is NOT 0 (Empty), TryGetRandomEmptySpot lied!
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
        // Removed texture loading as it is now in WorldRenderer

        _sysFont = Content.Load<SpriteFont>("SystemFont");

        _inspector = new Inspector(GraphicsDevice, _sysFont);
        _simGraph = new SimulationGraph(GraphicsDevice, _sysFont);
        _genePoolWindow = new GenePoolWindow(GraphicsDevice, _sysFont);
        _hud = new HUD(GraphicsDevice, _sysFont, _simGraph, _genePoolWindow);
        _worldRenderer = new WorldRenderer(GraphicsDevice);
        _worldRenderer.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        // Input Handling
        var keyboardState = Keyboard.GetState();
        if (keyboardState.IsKeyDown(Keys.Escape))
            Exit();

        // Toggle Pause
        if (keyboardState.IsKeyDown(Keys.Space) && !_previousKeyboardState.IsKeyDown(Keys.Space))
        {
            _isPaused = !_isPaused;
        }
        
        // Single Step (Right Arrow)
        bool singleStep = false;
        if (_isPaused && keyboardState.IsKeyDown(Keys.Right) && !_previousKeyboardState.IsKeyDown(Keys.Right))
        {
            singleStep = true;
        }

        _previousKeyboardState = keyboardState;

        // UI Updates
        _hud.UpdateInput();
        _genePoolWindow.UpdateInput();

        // If Gene Window is open, force pause (optional, but requested)
        bool effectivePause = _isPaused || _genePoolWindow.IsVisible;

        if (_genePoolWindow.IsVisible)
        {
            // Refresh data only once when opening? Or every frame?
            // For performance, let's refresh every frame for now, or we can optimize later.
            // Actually, refreshing every frame involves sorting 4000 agents. 
            // Let's do it only when it becomes visible or periodically?
            // For now, let's do it every frame to keep it simple and responsive.
            // Optimization: Only refresh if tickCount changed? But we are paused.
            // So we refresh once.
            _genePoolWindow.RefreshData(_agentPopulation);
        }

        // Input Blocking Logic
        var mouseState = Mouse.GetState();
        // If Gene Window is visible, it captures mouse globally (Modal)
        bool uiCapturesMouse = _hud.IsMouseOver(mouseState.Position) || _genePoolWindow.IsVisible;

        if (!effectivePause)
        {
            // Simulation Logic
            _tickCount++; // Increment deterministic clock

            Span<Agent> agentPopulationSpan = _agentPopulation.AsSpan();
            Span<Plant> plantPopulationSpan = _plantPopulation.AsSpan();

            // If we are single-stepping, clear previous visual feedback so we only see
            // what happens in this specific frame.
            if (singleStep)
            {
                for (int i = 0; i < agentPopulationSpan.Length; i++)
                {
                    agentPopulationSpan[i].AttackVisualTimer = 0;
                    agentPopulationSpan[i].FleeVisualTimer = 0;
                    // ReproductionVisualTimer is preserved to allow animation
                }
            }

            // --- BIOLOGICAL LOOP ---
            int aliveAgents = 0;
            for (int index = 0; index < agentPopulationSpan.Length; index++)
            {
                // Skip dead slots
                if (!agentPopulationSpan[index].IsAlive) continue;

                aliveAgents++;

                // Use ref to modify directly
                ref Agent currentAgent = ref agentPopulationSpan[index];

                // A. THINK & ACT
                // OPTIMIZATION: Time-Slicing
                // Update brains at 30Hz (every other frame) to reduce Debug CPU load by 50%.
                // (index + tick) % 2 == 0 ensures we update even/odd agents on alternating frames.
                if ((index + _tickCount) % 2 == 0)
                {
                    Brain.Think(ref currentAgent, _gridMap, _rng, _agentPopulation);
                }

                // Act every frame (physics/movement) using the last cached neuron activations
                Brain.Act(ref currentAgent, _gridMap, _rng, agentPopulationSpan, plantPopulationSpan);

                // B. AGING & METABOLISM
                currentAgent.Update(_gridMap);
            }

            int alivePlants = 0;
            for (int i = 0; i < plantPopulationSpan.Length; i++)
            {
                // Skip dead slots
                if (!plantPopulationSpan[i].IsAlive) continue;

                alivePlants++;

                // Use ref to modify directly
                ref Plant currentPlant = ref plantPopulationSpan[i];

                // A. AGING
                currentPlant.Update(_gridMap, _rng);

                // B. REPRODUCTION
                // If plant is mature, try to spawn a child
                if (currentPlant.CanReproduce())
                {
                    currentPlant.TryReproduce(plantPopulationSpan, _gridMap, _rng);
                }
            }

            _simGraph.Update(gameTime, aliveAgents, alivePlants);
        }
        else
        {
            // When paused, loop the animation for agents that are currently showing it
            Span<Agent> agentPopulationSpan = _agentPopulation.AsSpan();
            for (int i = 0; i < agentPopulationSpan.Length; i++)
            {
                ref Agent agent = ref agentPopulationSpan[i];
                if (agent.ReproductionVisualTimer > 0)
                {
                    agent.ReproductionVisualTimer--;

                    // Loop the animation while paused so the user can clearly see who reproduced
                    if (agent.ReproductionVisualTimer == 0)
                    {
                        agent.ReproductionVisualTimer = 30;
                    }
                }
            }
        }

        // Update Inspector Input (Selection) only if UI is not capturing mouse
        // Moved outside of (!effectivePause) so we can select while paused
        if (!uiCapturesMouse)
        {
            _inspector.UpdateInput(_camera, _gridMap, _agentPopulation, _plantPopulation, _structurePopulation, CellSize);
        }
        
        // Camera always updates so we can look around even when paused
        // We pass !uiCapturesMouse to allow the camera to sync its scroll state even if blocked
        _camera.HandleInput(Mouse.GetState(), Keyboard.GetState(), !uiCapturesMouse);

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

        // --- 2. SCREEN SPACE (Camera Off) ---
        // This draws the UI fixed on top of everything
        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend
        // No Matrix here! 
        );

        // Draw HUD (Graph, Stats, Timer)
        _hud.Draw(_spriteBatch, _tickCount, stats.LivingAgents, stats.LivingHerbivores, stats.LivingOmnivores, stats.LivingCarnivores, stats.LivingPlants, stats.LivingStructures);
        
        // Draw Inspector
        _inspector.DrawUI(_spriteBatch, _agentPopulation, _plantPopulation, _structurePopulation);

        // Draw Gene Pool Window (on top)
        _genePoolWindow.Draw(_spriteBatch);

        // Draw "PAUSED" text if paused (and not covered by Gene Window, or maybe on top of everything?
        // User requested PAUSED text to be visible even when Gene Window is open
        if (_isPaused || _genePoolWindow.IsVisible)
        {
            string pausedText = "PAUSED";
            Vector2 textSize = _sysFont.MeasureString(pausedText);
            Vector2 pos = new Vector2(
                (GraphicsDevice.Viewport.Width - textSize.X) / 2,
                30 // Top center, slightly down
            );
            
            // Draw text with shadow for visibility
            _spriteBatch.DrawString(_sysFont, pausedText, pos + new Vector2(2, 2), Color.Black);
            _spriteBatch.DrawString(_sysFont, pausedText, pos, Color.White);
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void UpdateFPSAndWindowTitle(GameTime gameTime, int livingAgents, int livingPlants, int livingStructures)
    {
        // --- FPS COUNTER ---
        // Increment frame counter
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

        // Add elapsed time
        _fpsTimer += gameTime.ElapsedGameTime.TotalSeconds;

        // Once per second, update the window title
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
        // Toggle the boolean flag
        _graphics.IsFullScreen = !_graphics.IsFullScreen;

        if (_graphics.IsFullScreen)
        {
            // Get the resolution of the user's monitor
            var screenWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            var screenHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

            // Update the backbuffer size to match the screen
            _graphics.PreferredBackBufferWidth = screenWidth;
            _graphics.PreferredBackBufferHeight = screenHeight;

            // "HardwareModeSwitch = false" means "Borderless Windowed Fullscreen".
            // This is usually preferred because Alt-Tab is faster and it handles multi-monitor better.
            // If set to true, it changes the actual signal to the monitor (Exclusive Mode).
            _graphics.HardwareModeSwitch = false;
        }
        else
        {
            // Revert to our simulation grid size
            _graphics.PreferredBackBufferWidth = GridWidth * CellSize;
            _graphics.PreferredBackBufferHeight = GridHeight * CellSize;
        }

        // Apply the changes to the GraphicsDevice
        _graphics.ApplyChanges();
    }
}