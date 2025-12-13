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
    private Texture2D _pixelTexture;
    private Texture2D _circleTexture;
    private Texture2D _starTexture;
    private Texture2D _roundedRectTexture;
    private Texture2D _arrowTexture;
    private Texture2D _dotTexture;
    private Texture2D _ringTexture;

    // Simulation Constants
    private const int GridHeight = 128;
    private const int GridWidth = (int)(GridHeight * 1.5);
    private const int CellSize = 1280 / GridHeight;
    private const float HalfCellSize = (CellSize * 0.5f);
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
            // Versuche an einen existierenden Nachbarn anzudocken
            if (i > 0 && _rng.NextDouble() > newClusterChance)
            {
                for (int attempt = 0; attempt < GrowthAttempts; attempt++)
                {
                    // Zufälligen "Parent" aus den bereits platzierten wählen
                    int parentIndex = _rng.Next(0, i);
                    T parent = populationSpan[parentIndex]; // Hier hilft das Interface/Generic

                    // Zufälliger Nachbar
                    int dx = _rng.Next(-1, 2);
                    int dy = _rng.Next(-1, 2);
                    if (dx == 0 && dy == 0) continue;

                    int tx = parent.X + dx;
                    int ty = parent.Y + dy;

                    if (tx >= 0 && tx < GridWidth && ty >= 0 && ty < GridHeight)
                    {
                        if (_gridMap[tx, ty] == GridCell.Empty)
                        {
                            // Factory aufrufen um das konkrete Objekt zu bauen
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
                // Wir prüfen manuell die "rohen" Werte der Zelle, ohne den == Operator zu nutzen.
                GridCell occupiedCell = _gridMap[x, y];

                // Wenn der Type NICHT 0 (Empty) ist, hat TryGetRandomEmptySpot gelogen!
                if (occupiedCell.Type != EntityType.Empty)
                {
                    throw new Exception($"FATALER LOGIK-FEHLER: TryGetRandomEmptySpot sagt {x},{y} ist leer, aber dort steht: {occupiedCell.Type} #{occupiedCell.Index}. \n" +
                                        $"Dies beweist, dass (Cell == GridCell.Empty) TRUE liefert, obwohl es FALSE sein sollte.");
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
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);

        // Generate a high-res circle (50px radius = 100px width)
        // We will scale this down significantly when drawing.
        _circleTexture = TextureGenerator.CreateCircle(GraphicsDevice, 50);

        _starTexture = TextureGenerator.CreateStar(GraphicsDevice, 50, 5);

        _roundedRectTexture = TextureGenerator.CreateRoundedRect(GraphicsDevice, 50, 20, 5);
        _arrowTexture = TextureGenerator.CreateTriangle(GraphicsDevice, 32);
        _dotTexture = TextureGenerator.CreateCircle(GraphicsDevice, 16); // Small dot
        _ringTexture = TextureGenerator.CreateRing(GraphicsDevice, 64, 8);

        _sysFont = Content.Load<SpriteFont>("SystemFont");

        _inspector = new Inspector(GraphicsDevice, _sysFont);
        _simGraph.LoadContent(GraphicsDevice);
        _hud = new HUD(GraphicsDevice, _sysFont, _simGraph);
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
                    Brain.Think(ref currentAgent, _gridMap, _rng);
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
        else if(!agent.IsAlive && cellAtPos != GridCell.Empty)
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

        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,
            SamplerState.LinearClamp,
            null,
            null,
            null,
            _camera.GetTransformation()
        );

        DrawStructures(out int livingStructures);
        DrawPlants(out int livingPlants);
        DrawKinshipLines();
        DrawAgents(out int livingAgents, out int livingHerbivore, out int livingOmnivore, out int livingCarnivore);

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

        // Draw HUD (Graph, Stats, Timer)
        _hud.Draw(_spriteBatch, _tickCount, livingAgents, livingHerbivore, livingOmnivore, livingCarnivore, livingPlants, livingStructures);

        _inspector.DrawUI(_spriteBatch, _agentPopulation, _plantPopulation, _structurePopulation);

        if (_isPaused)
        {
            string pauseText = "PAUSED";
            Vector2 textSize = _sysFont.MeasureString(pauseText);

            Vector2 textPos = new Vector2(
                GraphicsDevice.Viewport.Width / 2 - textSize.X / 2,
                20
            );

            _spriteBatch.DrawString(_sysFont, pauseText, textPos + new Vector2(2, 2), Color.Black);
            _spriteBatch.DrawString(_sysFont, pauseText, textPos, Color.Red);
        }

        UpdateFPSAndWindowTitle(gameTime, livingAgents, livingPlants, livingStructures);

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

    private void DrawAgents(out int livingAgents, out int livingHerbivore, out int livingOmnivore, out int livingCarnivore)
    {
        livingAgents = 0;
        livingHerbivore = 0;
        livingOmnivore = 0;
        livingCarnivore = 0;

        // Calculate the center of our source texture (needed for pivot point)
        var textureCenter = new Vector2(_circleTexture.Width / 2f, _circleTexture.Height / 2f);
        var arrowCenter = new Vector2(_arrowTexture.Width / 2f, _arrowTexture.Height / 2f);
        var dotCenter = new Vector2(_dotTexture.Width / 2f, _dotTexture.Height / 2f);
        var ringCenter = new Vector2(_ringTexture.Width / 2f, _ringTexture.Height / 2f);

        float baseScale = (float)CellSize / _circleTexture.Width;
        float arrowScale = ((float)CellSize / _arrowTexture.Width) * 0.6f; // Slightly smaller than cell
        float dotScale = ((float)CellSize / _dotTexture.Width) * 0.4f; // Small dot
        float ringBaseScale = (float)CellSize / _ringTexture.Width;

        const float agentAgeGrowthFactor = 1.0f / Agent.MaturityAge;

        Span<Agent> agentPopulationSpan = _agentPopulation.AsSpan();

        // PASS 1: Draw Agent Bodies (Bottom Layer)
        for (int i = 0; i < agentPopulationSpan.Length; i++)
        {
            ref Agent agent = ref agentPopulationSpan[i];
            if (!agent.IsAlive)
            {
                continue;
            }

            livingAgents++;
            switch(agent.Diet)
            {
                case DietType.Herbivore:
                    livingHerbivore++;
                    break;
                case DietType.Omnivore:
                    livingOmnivore++;
                    break;
                case DietType.Carnivore:
                    livingCarnivore++;
                    break;
            }

            // --- GROWTH LOGIC ---
            // Babies start small (0.3 scale) and grow to full size (1.0 scale) over 200 frames.
            // Math.Min ensures they stop growing at max size.
            float ageRatio = Math.Min(agent.Age * agentAgeGrowthFactor, 1.0f);
            float growthFactor = 0.3f + (0.7f * ageRatio);

            // Linear interpolation: Start at 30% size, end at 100% size
            float finalScale = baseScale * growthFactor;

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

        // PASS 2: Draw Visual Effects (Top Layer)
        // We iterate again to ensure effects are drawn ON TOP of all agent bodies (no occlusion by neighbors)
        for (int i = 0; i < agentPopulationSpan.Length; i++)
        {
            ref Agent agent = ref agentPopulationSpan[i];
            if (!agent.IsAlive) continue;

            // Re-calculate position/growth for effects
            float ageRatio = Math.Min(agent.Age * agentAgeGrowthFactor, 1.0f);
            float growthFactor = 0.3f + (0.7f * ageRatio);
            
            Vector2 position = new Vector2(
                agent.X * CellSize + HalfCellSize,
                agent.Y * CellSize + HalfCellSize
            );

            // Calculate offset based on current size so indicators stick to the edge
            // We normalize the direction vector to ensure consistent distance (circle vs square).
            // We use 0.85f to match the "diagonal" distance the user liked (1.414 * 0.6 ~= 0.85).
            float indicatorOffset = (CellSize * 0.85f) * growthFactor;

            // Scale indicators less aggressively than the agent body
            // Agent: 0.3 -> 1.0
            // Indicator: 0.65 -> 1.0
            float indicatorScaleFactor = 0.65f + (0.35f * ageRatio);

            // --- ATTACK VISUALIZATION ---
            if (agent.AttackVisualTimer > 0)
            {
                float alpha = agent.AttackVisualTimer / 15f; // Fade out
                float rotation = MathF.Atan2(agent.LastAttackDirY, agent.LastAttackDirX);
                
                Vector2 dir = new Vector2(agent.LastAttackDirX, agent.LastAttackDirY);
                if (dir != Vector2.Zero) dir.Normalize();

                // Offset the arrow so it appears on the edge of the agent
                Vector2 offset = dir * indicatorOffset;

                _spriteBatch.Draw(
                    _arrowTexture,
                    position + offset,
                    null,
                    agent.Color * alpha,
                    rotation,
                    arrowCenter,
                    arrowScale * indicatorScaleFactor,
                    SpriteEffects.None,
                    0f
                );
            }

            // --- FLEE VISUALIZATION ---
            if (agent.FleeVisualTimer > 0)
            {
                float alpha = agent.FleeVisualTimer / 15f; // Fade out
                
                Vector2 dir = new Vector2(agent.LastFleeDirX, agent.LastFleeDirY);
                if (dir != Vector2.Zero) dir.Normalize();

                // Offset the dot so it appears on the edge of the agent, in the direction of the threat
                Vector2 offset = dir * indicatorOffset;

                _spriteBatch.Draw(
                    _dotTexture,
                    position + offset,
                    null,
                    agent.Color * alpha, // Use agent color
                    0f,
                    dotCenter,
                    dotScale * indicatorScaleFactor,
                    SpriteEffects.None,
                    0f
                );
            }

            // --- REPRODUCTION VISUALIZATION ---
            if (agent.ReproductionVisualTimer > 0)
            {
                float t = 1.0f - (agent.ReproductionVisualTimer / 30f); // 0 to 1 over time
                float alpha = 1.0f - t; // Fade out
                
                // Start larger (0.8x) and expand further (3.0x) to be clearly visible
                float scale = ringBaseScale * (0.8f + (t * 2.2f)); 

                _spriteBatch.Draw(
                    _ringTexture,
                    position,
                    null,
                    Color.White * alpha,
                    0f,
                    ringCenter,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }

    private void DrawKinshipLines()
    {
        if (!_inspector.IsEntitySelected || _inspector.SelectedType != EntityType.Agent) return;

        // Get the selected agent
        // We need to find the agent with the selected ID.
        // Since we don't have a direct reference, we search.
        // Optimization: Inspector stores Index.
        // We can't access Inspector's private index, but we can use the public SelectedGridPos to find it in the map.
        
        var gridPos = _inspector.SelectedGridPos;
        var cell = _gridMap[gridPos.X, gridPos.Y];
        
        if (cell.Type != EntityType.Agent) return;

        ref Agent selectedAgent = ref _agentPopulation[cell.Index];
        if (!selectedAgent.IsAlive) return;

        const int BorderThickness = 2;
        const float agentAgeGrowthFactor = 1.0f / Agent.MaturityAge;

        // Calculate center and radius for selected agent
        Vector2 selectedCenter = new Vector2(
            (selectedAgent.X * CellSize) + HalfCellSize,
            (selectedAgent.Y * CellSize) + HalfCellSize
        );

        float selectedAgeRatio = Math.Min(selectedAgent.Age * agentAgeGrowthFactor, 1.0f);
        float selectedGrowth = 0.3f + (0.7f * selectedAgeRatio);
        float selectedRadius = (CellSize * selectedGrowth) * 0.5f;

        Span<Agent> agentPopulationSpan = _agentPopulation.AsSpan();
        for (int i = 0; i < agentPopulationSpan.Length; i++)
        {
            ref Agent other = ref agentPopulationSpan[i];
            if (!other.IsAlive || i == cell.Index) continue;

            // Check Kinship
            if (selectedAgent.IsDirectlyRelatedTo(ref other))
            {
                Vector2 otherCenter = new Vector2(
                    (other.X * CellSize) + HalfCellSize,
                    (other.Y * CellSize) + HalfCellSize
                );

                float otherAgeRatio = Math.Min(other.Age * agentAgeGrowthFactor, 1.0f);
                float otherGrowth = 0.3f + (0.7f * otherAgeRatio);
                float otherRadius = (CellSize * otherGrowth) * 0.5f;

                // Calculate direction and distance
                Vector2 direction = otherCenter - selectedCenter;
                float distance = direction.Length();

                if (distance > 0.001f)
                {
                    direction /= distance; // Normalize

                    // Calculate start and end points at the hull
                    // Clamp offsets to avoid crossing if agents overlap
                    float startOffset = Math.Min(selectedRadius, distance * 0.5f);
                    float endOffset = Math.Min(otherRadius, distance * 0.5f);

                    Vector2 lineStart = selectedCenter + (direction * startOffset);
                    Vector2 lineEnd = otherCenter - (direction * endOffset);

                    // Draw Line
                    DrawLine(lineStart, lineEnd, selectedAgent.OriginalColor * 0.5f, BorderThickness);
                }
            }
        }
    }

    private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness)
    {
        Vector2 edge = end - start;
        float angle = MathF.Atan2(edge.Y, edge.X);
        float length = edge.Length();

        _spriteBatch.Draw(
            _pixelTexture,
            start,
            null,
            color,
            angle,
            new Vector2(0, 0.5f), // Origin at middle-left of the 1x1 pixel for vertical centering
            new Vector2(length, thickness),
            SpriteEffects.None,
            0f
        );
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