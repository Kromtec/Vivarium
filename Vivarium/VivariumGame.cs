using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Vivarium.Biology;
using Vivarium.Engine;
using Vivarium.Entities;
using Vivarium.Graphics;
using Vivarium.UI;
using Vivarium.World;

namespace Vivarium;

public class VivariumGame : Game
{
    public static long NextEntityId { get; set; } = 1;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _pixelTexture;
    private Texture2D _circleTexture;
    private Texture2D _starTexture;
    private Texture2D _roundedRectTexture;

    // Simulation Constants
    private const int GridHeight = 128;
    private const int GridWidth = (int)(GridHeight * 1.5);
    private const int CellSize = 1280 / GridHeight;
    private const float HalfCellSize = (CellSize * 0.5f);
    private const int AgentCount = GridWidth * GridHeight / 8;
    private const int PlantCount = GridWidth * GridHeight / 32;
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

    // Store the keyboard state from the previous frame to detect key presses (edges)
    private KeyboardState _previousKeyboardState;

    private Camera2D _camera;
    private Inspector _inspector;
    private SpriteFont _sysFont;

    private bool _isPaused = false;

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
        _graphics.PreferredBackBufferWidth = GridWidth * CellSize;
        _graphics.PreferredBackBufferHeight = GridHeight * CellSize;
        // Modern .NET 10 apps handle high-DPI scaling better
        _graphics.HardwareModeSwitch = false;
        _graphics.SynchronizeWithVerticalRetrace = true;
        _graphics.ApplyChanges();

        _rng = new Random(42);

        // .NET 10 JIT optimizes array allocation significantly
        _agentPopulation = new Agent[AgentCount];
        _plantPopulation = new Plant[PlantCount];
        _structurePopulation = new Structure[StructureCount];

        _gridMap = new GridCell[GridWidth, GridHeight];

        SpawnPopulation();

        _camera = new Camera2D(GraphicsDevice);

        _camera.CenterOnGrid(GridWidth, GridHeight, CellSize);

        base.Initialize();
    }

    private void SpawnPopulation()
    {
        // Reset the map (-1 means empty)
        // Array.Fill is very fast in .NET 10
        // Since it's a 2D array, we treat it as a flat span or loop simply.
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                _gridMap[x, y] = GridCell.Empty;
            }
        }

        SpawnStructures();
        SpawnAgents();
        SpawnPlants();
    }

    private void SpawnStructures()
    {
        Span<Structure> structurePopulationSpan = _structurePopulation.AsSpan();

        // Configuration for clustering
        const double NewClusterChance = 0.05; // 5% chance to start a new cluster randomly
        const int GrowthAttempts = 10;        // How hard we try to attach to an existing cluster before giving up

        for (int i = 0; i < structurePopulationSpan.Length; i++)
        {
            bool placed = false;

            // 1. CLUSTER GROWTH LOGIC
            // Unless it's the very first item, we try to grow from an existing structure.
            // We also randomly force new clusters occasionally to spread them out.
            if (i > 0 && _rng.NextDouble() > NewClusterChance)
            {
                // Try multiple times to find a valid spot next to an existing structure
                for (int attempt = 0; attempt < GrowthAttempts; attempt++)
                {
                    // Pick a random "parent" from the structures we have already placed (indices 0 to i-1)
                    int parentIndex = _rng.Next(0, i);
                    var parent = structurePopulationSpan[parentIndex];

                    // Pick a random neighbor position (including diagonals)
                    int dx = _rng.Next(-1, 2);
                    int dy = _rng.Next(-1, 2);

                    // Skip if it's the same position (dx=0, dy=0)
                    if (dx == 0 && dy == 0) continue;

                    int tx = parent.X + dx;
                    int ty = parent.Y + dy;

                    // Check boundaries
                    if (tx >= 0 && tx < GridWidth && ty >= 0 && ty < GridHeight)
                    {
                        // Check if spot is empty (using your GridCell logic)
                        if (_gridMap[tx, ty] == GridCell.Empty)
                        {
                            // Found a valid spot next to a parent!
                            var structure = Structure.Create(i, tx, ty);
                            structurePopulationSpan[i] = structure;
                            _gridMap[structure.X, structure.Y] = new GridCell(EntityType.Structure, i);

                            placed = true;
                            break; // Stop trying, we are done with this structure
                        }
                    }
                }
            }

            // 2. FALLBACK / NEW CLUSTER SEED
            // If we decided to start a new cluster, OR if growth failed (e.g. parent was surrounded),
            // we pick a completely random spot.
            if (!placed)
            {
                if (TryGetRandomEmptySpot(_gridMap, out int x, out int y, _rng))
                {
                    var structure = Structure.Create(i, x, y);
                    structurePopulationSpan[i] = structure;
                    _gridMap[structure.X, structure.Y] = new GridCell(EntityType.Structure, i);
                }
            }
        }
    }

    private void SpawnPlants()
    {
        Span<Plant> plantPopulationSpan = _plantPopulation.AsSpan();

        for (int i = 0; i < plantPopulationSpan.Length; i++)
        {
            if (TryGetRandomEmptySpot(_gridMap, out int x, out int y, _rng))
            {
                var plant = Plant.Create(i, x, y);
                plantPopulationSpan[i] = plant;
                if (plant.IsAlive)
                {
                    _gridMap[plant.X, plant.Y] = new GridCell(EntityType.Plant, i);
                }
            }
        }
    }

    private void SpawnAgents()
    {
        Span<Agent> agentPopulationSpan = _agentPopulation.AsSpan();

        for (int i = 0; i < agentPopulationSpan.Length; i++)
        {
            if (TryGetRandomEmptySpot(_gridMap, out int x, out int y, _rng))
            {
                var agent = Agent.Create(i, x, y, _rng);
                agentPopulationSpan[i] = agent;
                _gridMap[agent.X, agent.Y] = new GridCell(EntityType.Agent, i);
            }
        }
    }

    public static bool TryGetRandomEmptySpot(GridCell[,] gridMap, out int x, out int y, Random rng)
    {
        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

        for (int i = 0; i < 5; i++)
        {
            int rx = rng.Next(0, gridWidth);
            int ry = rng.Next(0, gridHeight);

            if (gridMap[rx, ry] == GridCell.Empty)
            {
                x = rx;
                y = ry;
                return true;
            }
        }

        int totalCells = gridWidth * gridHeight;
        int startIndex = rng.Next(totalCells);

        for (int i = 0; i < totalCells; i++)
        {
            int currentIndex = (startIndex + i) % totalCells;

            int cx = currentIndex % gridWidth;
            int cy = currentIndex / gridWidth;

            if (gridMap[cx, cy] == GridCell.Empty)
            {
                x = cx;
                y = cy;
                return true;
            }
        }

        x = -1;
        y = -1;
        return false;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);

        // Generate a high-res circle (50px radius = 100px width)
        // We will scale this down significantly when drawing.
        _circleTexture = TextureGenerator.CreateCircle(GraphicsDevice, 50);

        _starTexture = TextureGenerator.CreateStar(GraphicsDevice, 50, 5);

        _roundedRectTexture = TextureGenerator.CreateRoundedRect(GraphicsDevice, 50, 20, 5);

        _sysFont = Content.Load<SpriteFont>("SystemFont");

        _inspector = new Inspector(GraphicsDevice, _sysFont);
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
        _camera.HandleInput(currentMouseState, currentKeyboardState);

        _inspector.UpdateInput(_camera, _gridMap, _agentPopulation, _plantPopulation, _structurePopulation);

        // --- 2. SIMULATION
        if (!_isPaused || singleStep)
        {
            Span<Agent> agentPopulationSpan = _agentPopulation.AsSpan();
            Span<Plant> plantPopulationSpan = _plantPopulation.AsSpan();

            // --- BIOLOGICAL LOOP ---

            for (int i = 0; i < agentPopulationSpan.Length; i++)
            {
                // Skip dead slots
                if (!agentPopulationSpan[i].IsAlive) continue;

                // Use ref to modify directly
                ref Agent currentAgent = ref agentPopulationSpan[i];

                // A. THINK & ACT
                Brain.Think(ref currentAgent, _gridMap, _rng);
                Brain.Act(ref currentAgent, _gridMap, agentPopulationSpan, plantPopulationSpan);

                // B. AGING & METABOLISM
                currentAgent.Update(_gridMap);

                // C. REPRODUCTION
                // If agent is mature, try to spawn a child
                if (currentAgent.CanReproduce())
                {
                    currentAgent.TryReproduce(agentPopulationSpan, _gridMap, _rng);
                }
            }

            for (int i = 0; i < plantPopulationSpan.Length; i++)
            {
                // Skip dead slots
                if (!plantPopulationSpan[i].IsAlive) continue;
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
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,
            SamplerState.LinearClamp,
            null,
            null,
            null,
            _camera.GetTransformation()
        );

        DrawAgents(out int livingAgents);
        DrawPlants(out int livingPlants);
        DrawStructures(out int livingStructures);

        // Draw the Selection Box inside the world
        float totalSeconds = (float)gameTime.TotalGameTime.TotalSeconds;
        _inspector.DrawSelectionMarker(_spriteBatch, CellSize, totalSeconds);

        _spriteBatch.End();

        // --- 2. SCREEN SPACE (Camera Off) ---
        // This draws the UI fixed on top of everything
        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend
            // No Matrix here! 
        );

        // We pass the raw arrays so the inspector can look up the data by index
        _inspector.DrawUI(_spriteBatch, _agentPopulation, _plantPopulation, _structurePopulation);

        if (_isPaused)
        {
            string pauseText = "PAUSED";
            Vector2 textSize = _sysFont.MeasureString(pauseText);

            Vector2 textPos = new Vector2(
                GraphicsDevice.Viewport.Width - textSize.X - 20,
                20
            );

            _spriteBatch.DrawString(_sysFont, pauseText, textPos + new Vector2(2, 2), Color.Black);
            _spriteBatch.DrawString(_sysFont, pauseText, textPos, Color.Red);
        }

        _spriteBatch.End();

        UpdateWindowTitle(gameTime, livingAgents, livingPlants, livingStructures);

        base.Draw(gameTime);
    }

    private void UpdateWindowTitle(GameTime gameTime, int livingAgents, int livingPlants, int livingStructures)
    {
        // --- FPS COUNTER ---
        // Increment frame counter
        _framesCounter++;

        // Add elapsed time
        _fpsTimer += gameTime.ElapsedGameTime.TotalSeconds;

        // Once per second, update the window title
        if (_fpsTimer >= 1.0d)
        {
            Window.Title = $"Vivarium - FPS: {_framesCounter} - Agents: {livingAgents} | Plants: {livingPlants} | Structures: {livingStructures}";
            _framesCounter = 0;
            _fpsTimer--;
        }
    }

    private void DrawStructures(out int livingStructures)
    {
        var textureCenter = new Vector2(_roundedRectTexture.Width / 2f, _roundedRectTexture.Height / 2f);
        Span<Structure> structurePopulationSpan = _structurePopulation.AsSpan();
        livingStructures = structurePopulationSpan.Length;
        for (int i = 0; i < structurePopulationSpan.Length; i++)
        {
            ref Structure structure = ref structurePopulationSpan[i];

            float structScale = ((float)CellSize / _roundedRectTexture.Width);

            // Calculate screen position
            Vector2 position = new Vector2(
                structure.X * CellSize + HalfCellSize,
                structure.Y * CellSize + HalfCellSize
            );
            _spriteBatch.Draw(
                _roundedRectTexture,
                position,
                null,
                structure.Color,
                0f,
                textureCenter,
                structScale,
                SpriteEffects.None,
                0f
            );
        }
    }

    private int DrawPlants(out int livingPlants)
    {
        livingPlants = 0;
        var textureCenter = new Vector2(_starTexture.Width / 2f, _starTexture.Height / 2f);
        float baseScale = (float)CellSize / _starTexture.Width;

        const float plantAgeGrowthFactor = 1.0f / Plant.MaturityAge;

        Span<Plant> plantPopulationSpan = _plantPopulation.AsSpan();
        for (int i = 0; i < plantPopulationSpan.Length; i++)
        {
            ref Plant plant = ref plantPopulationSpan[i];
            if (!plant.IsAlive)
            {
                _gridMap[plant.X, plant.Y] = GridCell.Empty;
                continue;
            }

            livingPlants++;

            // --- GROWTH LOGIC ---
            // Babies start small (0.3 scale) and grow to full size (1.0 scale) over 200 frames.
            // Math.Min ensures they stop growing at max size.
            float ageRatio = Math.Min(plant.Age * plantAgeGrowthFactor, 1.0f);

            // Linear interpolation: Start at 30% size, end at 100% size
            float finalScale = baseScale * (0.3f + (0.7f * ageRatio));

            // Calculate screen position
            Vector2 position = new Vector2(
                plant.X * CellSize + HalfCellSize,
                plant.Y * CellSize + HalfCellSize
            );
            _spriteBatch.Draw(
                _starTexture,
                position,
                null,
                plant.Color,
                0f,
                textureCenter, // Draw from the center of the texture!
                finalScale,
                SpriteEffects.None,
                0f
            );
        }

        return livingPlants;
    }

    private void DrawAgents(out int livingAgents)
    {
        livingAgents = 0;

        // Calculate the center of our source texture (needed for pivot point)
        var textureCenter = new Vector2(_circleTexture.Width / 2f, _circleTexture.Height / 2f);
        float baseScale = (float)CellSize / _circleTexture.Width;
        const float agentAgeGrowthFactor = 1.0f / Agent.MaturityAge;

        Span<Agent> agentPopulationSpan = _agentPopulation.AsSpan();
        for (int i = 0; i < agentPopulationSpan.Length; i++)
        {
            ref Agent agent = ref agentPopulationSpan[i];
            if (!agent.IsAlive)
            {
                _gridMap[agent.X, agent.Y] = GridCell.Empty;
                continue;
            }

            livingAgents++;

            // --- GROWTH LOGIC ---
            // Babies start small (0.3 scale) and grow to full size (1.0 scale) over 200 frames.
            // Math.Min ensures they stop growing at max size.
            float ageRatio = Math.Min(agent.Age * agentAgeGrowthFactor, 1.0f);

            // Linear interpolation: Start at 30% size, end at 100% size
            float finalScale = baseScale * (0.3f + (0.7f * ageRatio));

            // Calculate screen position
            // Important: Add half CellSize to X and Y so we draw at the CENTER of the grid cell
            Vector2 position = new Vector2(
                agent.X * CellSize + HalfCellSize,
                agent.Y * CellSize + HalfCellSize
            );

            _spriteBatch.Draw(
                _circleTexture,
                position,
                null,
                agent.Color,
                0f,
                textureCenter, // Draw from the center of the texture!
                finalScale,
                SpriteEffects.None,
                0f
            );
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