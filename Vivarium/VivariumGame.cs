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

        _simGraph = new SimulationGraph(new Rectangle(25, 25, 280, 100));

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
        _simGraph.LoadContent(GraphicsDevice);
        _hud = new HUD(GraphicsDevice, _sysFont, _simGraph);

        _worldRenderer = new WorldRenderer(GraphicsDevice);
        _worldRenderer.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState currentKeyboardState = Keyboard.GetState();
        var currentMouseState = Mouse.GetState();

        if (currentKeyboardState.IsKeyDown(Keys.Escape)) Exit();


        // --- 1. SYSTEM & INPUT

        // Toggle FULLSCREEN (F11)
        if (currentKeyboardState.IsKeyDown(Keys.F11) && !_previousKeyboardState.IsKeyDown(Keys.F11))
        {
            ToggleFullscreen();
        }
        // Toggle PAUSE (Space)
        if (currentKeyboardState.IsKeyDown(Keys.Space) && !_previousKeyboardState.IsKeyDown(Keys.Space))
        {
            _isPaused = !_isPaused;
        }
        // SINGLE STEP (.)
        bool singleStep = currentKeyboardState.IsKeyDown(Keys.OemPeriod) && !_previousKeyboardState.IsKeyDown(Keys.OemPeriod);

        // Save state for the next frame
        _previousKeyboardState = currentKeyboardState;

        // CAMERA UPDATE
        Rectangle worldBounds = new Rectangle(0, 0, GridWidth * CellSize, GridHeight * CellSize);
        _camera.HandleInput(currentMouseState, currentKeyboardState, worldBounds);

        // Only update inspector input if we are NOT clicking on the HUD
        if (!_hud.Bounds.Contains(currentMouseState.Position))
        {
            _inspector.UpdateInput(_camera, _gridMap, _agentPopulation, _plantPopulation, _structurePopulation, CellSize);
        }

        // --- 2. SIMULATION
        if (!_isPaused || singleStep)
        {
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

                int oldX = currentAgent.X;
                int oldY = currentAgent.Y;

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

                // Validation Overhead: Only check once per second (every 60 ticks)
                if (_tickCount % 60 == 0)
                {
                    ValidateAgentIntegrity(index, currentAgent, oldX, oldY);
                }

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

        // World Integrity Check: O(Width*Height). Only run periodically.
        if (_tickCount % 60 == 0)
        {
            ValidateWorldIntegrity();
        }

        base.Update(gameTime);
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void ValidateAgentIntegrity(int index, Agent agent, int oldX, int oldY)
    {
        var cellAtPos = _gridMap[agent.X, agent.Y];
        if (agent.IsAlive && (cellAtPos.Type != EntityType.Agent || cellAtPos.Index != index))
        {
            if (agent.X != oldX || agent.Y != oldY)
            {
                throw new Exception($"LOGIC ERROR FOR AGENT #{index}: It moved from {oldX},{oldY} to {agent.X},{agent.Y}, but the map shows: {cellAtPos.Type} #{cellAtPos.Index}");
            }
            else
            {
                throw new Exception($"LOGIC ERROR FOR AGENT #{index}: It is still at {agent.X},{agent.Y}, but the map shows: {cellAtPos.Type} #{cellAtPos.Index}");
            }
        }
        else if (!agent.IsAlive && cellAtPos != GridCell.Empty)
        {
            throw new Exception($"LOGIC ERROR FOR AGENT #{index}: It is dead at {agent.X},{agent.Y}, but the map shows: {cellAtPos.Type} #{cellAtPos.Index}");
        }
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void ValidateWorldIntegrity()
    {
        // 1. Check: Does the map point to the correct entity?
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                var cell = _gridMap[x, y];
                if (cell.Type == EntityType.Agent)
                {
                    ref var agent = ref _agentPopulation[cell.Index];
                    if (!agent.IsAlive) throw new Exception($"Map error at {x},{y}: References dead agent #{cell.Index}");
                    if (agent.X != x || agent.Y != y) throw new Exception($"Desync! Map says agent #{cell.Index} is at {x},{y}, but agent believes it's at {agent.X},{agent.Y}");
                }
                else if (cell.Type == EntityType.Plant)
                {
                    ref var plant = ref _plantPopulation[cell.Index];
                    if (!plant.IsAlive) throw new Exception($"Map error at {x},{y}: References dead plant #{cell.Index}");
                    if (plant.X != x || plant.Y != y) throw new Exception($"Desync! Map says plant #{cell.Index} is at {x},{y}, but plant believes it's at {plant.X},{plant.Y}");
                }
            }
        }

        // 2. Check: Is every living entity correctly placed in the map?
        for (int i = 0; i < _agentPopulation.Length; i++)
        {
            if (_agentPopulation[i].IsAlive)
            {
                var a = _agentPopulation[i];
                var cell = _gridMap[a.X, a.Y];
                if (cell.Type != EntityType.Agent || cell.Index != i)
                {
                    throw new Exception($"Zombie alert! Agent #{i} thinks it's at {a.X},{a.Y}, but the map shows: {cell.Type} #{cell.Index}");
                }
            }
        }
        // (Same for plants loop...)
        for (int i = 0; i < _plantPopulation.Length; i++)
        {
            if (_plantPopulation[i].IsAlive)
            {
                var p = _plantPopulation[i];
                var cell = _gridMap[p.X, p.Y];
                if (cell.Type != EntityType.Plant || cell.Index != i)
                {
                    throw new Exception($"Zombie alert! Plant #{i} thinks it's at {p.X},{p.Y}, but the map shows: {cell.Type} #{cell.Index}");
                }
            }
        }
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

        _inspector.DrawUI(_spriteBatch, _agentPopulation, _plantPopulation, _structurePopulation);

        if (_isPaused)
        {
            const string pauseText = "PAUSED";
            Vector2 textSize = _sysFont.MeasureString(pauseText);

            Vector2 textPos = new Vector2(
                GraphicsDevice.Viewport.Width / 2 - textSize.X / 2,
                20
            );

            _spriteBatch.DrawString(_sysFont, pauseText, textPos + new Vector2(2, 2), Color.Black);
            _spriteBatch.DrawString(_sysFont, pauseText, textPos, Color.Red);
        }

        UpdateFPSAndWindowTitle(gameTime, stats.LivingAgents, stats.LivingPlants, stats.LivingStructures);

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