using System;
using Vivarium.Biology;
using Vivarium.Config;
using Vivarium.Entities;
using Vivarium.Engine;
using Vivarium.World;

namespace Vivarium.Tests;

/// <summary>
/// Factory for creating test data objects needed for integration testing.
/// Provides simple methods to create Agents, Plants, Genomes, Grids, etc.
/// </summary>
public static class TestDataFactory
{
	private static bool _configInitialized = false;

	/// <summary>
	/// Ensures ConfigProvider is initialized. Call this before creating any entities.
	/// Safe to call multiple times - will only initialize once.
	/// </summary>
	public static void EnsureConfigInitialized(int seed = 42)
	{
		if (_configInitialized) return;

		// Check if already initialized externally
		try
		{
			var _ = ConfigProvider.World; // Try to access - will throw if not initialized
			_configInitialized = true; // Already initialized
			return;
		}
		catch (InvalidOperationException)
		{
			// Not initialized, proceed
		}

		var config = SimulationConfig.CreateDefault(seed);
		ConfigProvider.Initialize(config);
		_configInitialized = true;
	}

	/// <summary>
	/// Creates a seeded Random for deterministic tests.
	/// </summary>
	public static Random CreateRandom(int seed = 42)
	{
		return new Random(seed);
	}

	/// <summary>
	/// Creates a minimal Agent with default genome and properties.
	/// Requires ConfigProvider to be initialized.
	/// </summary>
	public static Agent CreateAgent(int index = 0, int x = 0, int y = 0, int? seed = null)
	{
		EnsureConfigInitialized(seed ?? 42);
		var rng = CreateRandom(seed ?? 42);

		// Create agent using the factory method (creates genome internally)
		var agent = Agent.Create(index, x, y, rng);

		// Ensure decoded genome is populated
		agent.RefreshDecodedGenome();

		return agent;
	}

	/// <summary>
	/// Creates an Agent with specific diet type.
	/// </summary>
	public static Agent CreateAgentWithDiet(DietType diet, int index = 0, int x = 0, int y = 0, int? seed = null)
	{
		var agent = CreateAgent(index, x, y, seed);
		
		// Set diet directly (bypassing the config-based calculation for testing)
		// Note: Diet is set in Agent.Create based on initial traits
		// For testing, we can override the Diet property directly since it's private set
		var dietField = typeof(Agent).GetProperty("Diet");
		dietField?.SetValue(agent, diet);

		return agent;
	}

	/// <summary>
	/// Creates an Agent with specific energy level.
	/// </summary>
	public static Agent CreateAgentWithEnergy(float energy, int index = 0, int x = 0, int y = 0, int? seed = null)
	{
		var agent = CreateAgent(index, x, y, seed);
		
		// Set energy directly using reflection since Energy has private setter
		var energyField = typeof(Agent).GetProperty("Energy");
		
		// Use the ChangeEnergy method instead
		var gridMap = CreateEmptyGrid(10, 10);
		agent.ChangeEnergy(energy - agent.Energy, gridMap);

		return agent;
	}

	/// <summary>
	/// Creates a genome (Gene array) with random connections.
	/// </summary>
	public static Gene[] CreateGenome(int? seed = null)
	{
		EnsureConfigInitialized(seed ?? 42);
		var rng = CreateRandom(seed ?? 42);

		return Genetics.CreateGenome(rng);
	}

	/// <summary>
	/// Creates a genome with specific number of genes.
	/// </summary>
	public static Gene[] CreateGenomeWithLength(int length, int? seed = null)
	{
		EnsureConfigInitialized(seed ?? 42);
		var rng = CreateRandom(seed ?? 42);

		var genome = new Gene[length];
		float weightRange = ConfigProvider.Genetics.InitialWeightRange;

		for (int g = 0; g < length; g++)
		{
			int source = rng.Next(BrainConfig.NeuronCount);
			int sink = rng.Next(BrainConfig.NeuronCount);
			float weight = (float)((rng.NextDouble() * weightRange * 2) - weightRange);
			genome[g] = Gene.CreateConnection(source, sink, weight);
		}

		return genome;
	}

	/// <summary>
	/// Creates a simple genome with specific connections for testing.
	/// </summary>
	public static Gene[] CreateSimpleGenome(int connections = 10, int? seed = null)
	{
		return CreateGenomeWithLength(connections, seed);
	}

	/// <summary>
	/// Creates an empty grid (all cells are GridCell.Empty).
	/// </summary>
	public static GridCell[,] CreateEmptyGrid(int width = 10, int height = 10)
	{
		return new GridCell[width, height];
	}

	/// <summary>
	/// Creates a grid with random entities scattered.
	/// </summary>
	public static GridCell[,] CreateGridWithRandomEntities(int width = 10, int height = 10, int agentCount = 5, int plantCount = 10, int? seed = null)
	{
		var grid = CreateEmptyGrid(width, height);
		var rng = CreateRandom(seed ?? 42);

		// Add agents
		for (int i = 0; i < agentCount; i++)
		{
			int x = rng.Next(width);
			int y = rng.Next(height);
			if (grid[x, y] == GridCell.Empty)
			{
				grid[x, y] = new GridCell(EntityType.Agent, i);
			}
		}

		// Add plants
		for (int i = 0; i < plantCount; i++)
		{
			int x = rng.Next(width);
			int y = rng.Next(height);
			if (grid[x, y] == GridCell.Empty)
			{
				grid[x, y] = new GridCell(EntityType.Plant, i + 100);
			}
		}

		return grid;
	}

	/// <summary>
	/// Creates a grid with specific entity placements.
	/// </summary>
	public static GridCell[,] CreateGrid((EntityType Type, int Index, int X, int Y)[] entities, int width = 10, int height = 10)
	{
		var grid = CreateEmptyGrid(width, height);

		foreach (var (type, index, x, y) in entities)
		{
			if (x >= 0 && x < width && y >= 0 && y < height)
			{
				grid[x, y] = new GridCell(type, index);
			}
		}

		return grid;
	}

	/// <summary>
	/// Creates a Plant.
	/// </summary>
	public static Plant CreatePlant(int index = 0, int x = 0, int y = 0, int? seed = null)
	{
		EnsureConfigInitialized(seed ?? 42);
		var rng = CreateRandom(seed ?? 42);

		return Plant.Create(index, x, y, rng);
	}

	/// <summary>
	/// Creates a Plant with specific energy level.
	/// </summary>
	public static Plant CreatePlantWithEnergy(float energy, int index = 0, int x = 0, int y = 0, int? seed = null)
	{
		EnsureConfigInitialized(seed ?? 42);
		var rng = CreateRandom(seed ?? 42);

		var plant = Plant.Create(index, x, y, rng);
		var grid = CreateEmptyGrid(10, 10);
		plant.ChangeEnergy(energy - plant.Energy, grid);

		return plant;
	}

	/// <summary>
	/// Creates a Structure.
	/// </summary>
	public static Structure CreateStructure(int index = 0, int x = 0, int y = 0)
	{
		return Structure.Create(index, x, y);
	}

	/// <summary>
	/// Creates an array of Agents (population).
	/// </summary>
	public static Agent[] CreateAgentPopulation(int count, int gridWidth = 10, int gridHeight = 10, int? seed = null)
	{
		var agents = new Agent[count];
		var rng = CreateRandom(seed ?? 42);

		for (int i = 0; i < count; i++)
		{
			int x = rng.Next(gridWidth);
			int y = rng.Next(gridHeight);
			agents[i] = CreateAgent(i, x, y, seed);
		}

		return agents;
	}

	/// <summary>
	/// Creates an array of Plants.
	/// </summary>
	public static Plant[] CreatePlantPopulation(int count, int gridWidth = 10, int gridHeight = 10, int? seed = null)
	{
		var plants = new Plant[count];
		var rng = CreateRandom(seed ?? 42);

		for (int i = 0; i < count; i++)
		{
			int x = rng.Next(gridWidth);
			int y = rng.Next(gridHeight);
			plants[i] = CreatePlant(i, x, y, seed);
		}

		return plants;
	}

	/// <summary>
	/// Creates a minimal Simulation for testing.
	/// Note: This creates the simulation but doesn't run it.
	/// </summary>
	public static Simulation CreateSimulation(int? seed = null)
	{
		EnsureConfigInitialized(seed ?? 42);

		var simulation = new Simulation();
		simulation.Initialize();

		return simulation;
	}

	/// <summary>
	/// Creates a Gene with specific values for testing.
	/// </summary>
	public static Gene CreateGene(int source, int sink, float weight)
	{
		return Gene.CreateConnection(source, sink, weight);
	}

	/// <summary>
	/// Creates multiple genes for testing.
	/// </summary>
	public static Gene[] CreateGenes(params (int Source, int Sink, float Weight)[] connections)
	{
		var genes = new Gene[connections.Length];

		for (int i = 0; i < connections.Length; i++)
		{
			genes[i] = Gene.CreateConnection(connections[i].Source, connections[i].Sink, connections[i].Weight);
		}

		return genes;
	}

	/// <summary>
	/// Resets the factory state (useful between tests).
	/// </summary>
	public static void Reset()
	{
		_configInitialized = false;
	}
}
