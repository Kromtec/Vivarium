using System;
using Vivarium.Biology;
using Vivarium.Config;
using Vivarium.Entities;
using Vivarium.World;

namespace Vivarium.Engine;

public class Simulation
{
    // Read from config
    public int GridHeight => ConfigProvider.World.GridHeight;
    public int GridWidth => ConfigProvider.World.GridWidth;
    public int CellSize => ConfigProvider.World.CellSize;
    public int AgentCount => ConfigProvider.World.AgentPoolSize;
    public int PlantCount => ConfigProvider.World.PlantPoolSize;
    public int StructureCount => ConfigProvider.World.StructurePoolSize;

    public Agent[] AgentPopulation { get; private set; }
    public Plant[] PlantPopulation { get; private set; }
    public Structure[] StructurePopulation { get; private set; }
    public GridCell[,] GridMap { get; private set; }
    public Random Rng { get; private set; }
    public long TickCount { get; private set; }

    public int AliveAgents { get; private set; }
    public int AlivePlants { get; private set; }
    public int AliveStructures { get; private set; }

    public Simulation()
    {
        var world = ConfigProvider.World;
        AgentPopulation = new Agent[world.AgentPoolSize];
        PlantPopulation = new Plant[world.PlantPoolSize];
        StructurePopulation = new Structure[world.StructurePoolSize];
        GridMap = new GridCell[world.GridWidth, world.GridHeight];
        Rng = new Random(world.Seed);
    }

    public void Initialize()
    {
        SpawnPopulation();
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
            if (i > 0 && Rng.NextDouble() > newClusterChance)
            {
                for (int attempt = 0; attempt < GrowthAttempts; attempt++)
                {
                    int parentIndex = Rng.Next(0, i);
                    T parent = populationSpan[parentIndex];

                    int dx = Rng.Next(-1, 2);
                    int dy = Rng.Next(-1, 2);
                    if (dx == 0 && dy == 0) continue;

                    int tx = (parent.X + dx + GridWidth) % GridWidth;
                    int ty = (parent.Y + dy + GridHeight) % GridHeight;

                    if (GridMap[tx, ty] == GridCell.Empty)
                    {
                        T newItem = createFactory(i, tx, ty);

                        populationSpan[i] = newItem;
                        GridMap[tx, ty] = new GridCell(type, i);

                        placed = true;
                        break;
                    }
                }
            }

            // Fallback
            if (!placed)
            {
                if (WorldSensor.TryGetRandomEmptySpot(GridMap, out int x, out int y, Rng))
                {
                    T newItem = createFactory(i, x, y);

                    populationSpan[i] = newItem;
                    GridMap[x, y] = new GridCell(type, i);
                }
            }
        }
    }

    private void SpawnStructures()
    {
        SpawnClustered(
            StructurePopulation.AsSpan(),
            EntityType.Structure,
            newClusterChance: 0.2,
            createFactory: (index, x, y) => Structure.Create(index, x, y)
        );
        AliveStructures = StructurePopulation.Length;
    }

    private void SpawnPlants()
    {
        SpawnClustered(
            PlantPopulation.AsSpan(),
            EntityType.Plant,
            newClusterChance: 0.1,
            createFactory: (index, x, y) => Plant.Create(index, x, y, Rng)
        );
    }

    private void SpawnAgents()
    {
        Span<Agent> agentPopulationSpan = AgentPopulation.AsSpan();

        for (int index = 0; index < agentPopulationSpan.Length; index++)
        {
            if (WorldSensor.TryGetRandomEmptySpot(GridMap, out int x, out int y, Rng))
            {
                GridCell occupiedCell = GridMap[x, y];

                if (occupiedCell.Type != EntityType.Empty)
                {
                    throw new Exception($"FATAL LOGIC ERROR: TryGetRandomEmptySpot says {x},{y} is empty, but found: {occupiedCell.Type} #{occupiedCell.Index}. \n" +
                                         "This proves that (Cell == GridCell.Empty) returns TRUE, although it should be FALSE.");
                }
                var agent = Agent.Create(index, x, y, Rng);
                agentPopulationSpan[index] = agent;
                GridMap[agent.X, agent.Y] = new GridCell(EntityType.Agent, index);
            }
        }
    }

    public void Update()
    {
        TickCount++;

        Span<Agent> agentPopulationSpan = AgentPopulation.AsSpan();
        Span<Plant> plantPopulationSpan = PlantPopulation.AsSpan();

        // Biological Loop
        int aliveAgents = 0;
        for (int index = 0; index < agentPopulationSpan.Length; index++)
        {
            if (!agentPopulationSpan[index].IsAlive) continue;

            aliveAgents++;

            ref Agent currentAgent = ref agentPopulationSpan[index];

            // Think & Act
            // Time-Slicing
            if ((index + TickCount) % 2 == 0)
            {
                Brain.Think(ref currentAgent, GridMap, Rng, AgentPopulation);
            }

            Brain.Act(ref currentAgent, GridMap, Rng, agentPopulationSpan, plantPopulationSpan);

            // Aging & Metabolism
            currentAgent.Update();
        }
        AliveAgents = aliveAgents;

        int alivePlants = 0;
        for (int i = 0; i < plantPopulationSpan.Length; i++)
        {
            if (!plantPopulationSpan[i].IsAlive) continue;

            alivePlants++;

            ref Plant currentPlant = ref plantPopulationSpan[i];

            // Aging
            currentPlant.Update(GridMap, Rng);

            // Reproduction
            if (currentPlant.CanReproduce())
            {
                currentPlant.TryReproduce(plantPopulationSpan, GridMap, Rng);
            }
        }
        AlivePlants = alivePlants;
    }
}
