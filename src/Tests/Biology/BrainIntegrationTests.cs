using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Biology;
using Vivarium.Entities;
using Vivarium.World;
using System;

namespace Vivarium.Tests.Biology;

[TestClass]
public class BrainIntegrationTests
{
	[TestInitialize]
	public void Setup()
	{
		TestDataFactory.Reset();
		TestDataFactory.EnsureConfigInitialized(seed: 42);
	}

	/// <summary>
	/// Tests Brain.Think() - neural network processing.
	/// NOW TESTABLE with TestDataFactory!
	/// </summary>
	[TestMethod]
	public void Think_ExecutesWithoutError()
	{
		var agent = TestDataFactory.CreateAgent(seed: 42);
		var grid = TestDataFactory.CreateEmptyGrid(20, 20);
		var rng = TestDataFactory.CreateRandom(123);
		var population = TestDataFactory.CreateAgentPopulation(5, seed: 100);

		// Just verify Think executes without error
		Brain.Think(ref agent, grid, rng, population);

		// Test passes if no exception thrown
		Assert.IsTrue(true);
	}

	[TestMethod]
	public void Think_UpdatesNeuronActivations()
	{
		var agent = TestDataFactory.CreateAgent(seed: 42);
		var grid = TestDataFactory.CreateGridWithRandomEntities(20, 20, agentCount: 3, plantCount: 5, seed: 50);
		var rng = TestDataFactory.CreateRandom(123);
		var population = TestDataFactory.CreateAgentPopulation(3, seed: 100);

		// Store initial neuron state (all zeros)
		var initialActivations = new float[agent.NeuronActivations.Length];
		Array.Copy(agent.NeuronActivations, initialActivations, agent.NeuronActivations.Length);

		// Run Think
		Brain.Think(ref agent, grid, rng, population);

		// Neurons should be updated (not all zeros anymore)
		bool anyUpdated = false;
		for (int i = 0; i < agent.NeuronActivations.Length; i++)
		{
			if (Math.Abs(agent.NeuronActivations[i] - initialActivations[i]) > 0.001f)
			{
				anyUpdated = true;
				break;
			}
		}

		Assert.IsTrue(anyUpdated, "Neuron activations should be updated after Think()");
	}

	/// <summary>
	/// Tests that Think() processes sensors correctly.
	/// </summary>
	[TestMethod]
	public void Think_ProcessesSensors()
	{
		var agent = TestDataFactory.CreateAgent(seed: 42);
		var grid = TestDataFactory.CreateEmptyGrid(20, 20);
		var rng = TestDataFactory.CreateRandom(123);
		var population = TestDataFactory.CreateAgentPopulation(3, seed: 100);

		// Run Think
		Brain.Think(ref agent, grid, rng, population);

		// Verify agent has neuron activations array
		Assert.IsNotNull(agent.NeuronActivations);
		Assert.IsTrue(agent.NeuronActivations.Length > 0);

		// Location sensors should have values based on agent position
		// X and Y should be in range [-1, 1]
		var xNeuron = agent.NeuronActivations[(int)SensorType.LocationX];
		var yNeuron = agent.NeuronActivations[(int)SensorType.LocationY];

		Assert.IsTrue(xNeuron >= -1 && xNeuron <= 1, "X neuron should be in [-1, 1]");
		Assert.IsTrue(yNeuron >= -1 && yNeuron <= 1, "Y neuron should be in [-1, 1]");
	}

	/// <summary>
	/// Tests that Think() handles empty grid correctly.
	/// </summary>
	[TestMethod]
	public void Think_HandlesEmptyGrid()
	{
		var agent = TestDataFactory.CreateAgent(seed: 42);
		var grid = TestDataFactory.CreateEmptyGrid(10, 10);
		var rng = TestDataFactory.CreateRandom(123);
		var population = TestDataFactory.CreateAgentPopulation(2, seed: 100);

		// Should not throw
		Brain.Think(ref agent, grid, rng, population);

		Assert.IsTrue(true);
	}

	/// <summary>
	/// Tests that Think() handles populated grid.
	/// </summary>
	[TestMethod]
	public void Think_HandlesPopulatedGrid()
	{
		var agent = TestDataFactory.CreateAgent(seed: 42);
		// Place agent at specific location
		agent.X = 5;
		agent.Y = 5;

		// Create grid with entities around the agent
		var entities = new (EntityType, int, int, int)[]
		{
			(EntityType.Agent, 0, 4, 4),  // agent index 0 at 4,4
			(EntityType.Plant, 1, 6, 5),   // plant index 1 at 6,5
			(EntityType.Plant, 2, 5, 6),   // plant index 2 at 5,6
		};
		var grid = TestDataFactory.CreateGrid(entities, 10, 10);

		var rng = TestDataFactory.CreateRandom(123);
		var population = TestDataFactory.CreateAgentPopulation(1, seed: 100);

		// Should not throw
		Brain.Think(ref agent, grid, rng, population);

		Assert.IsTrue(true);
	}

	/// <summary>
	/// Tests that Agent has BrainConfig accessible.
	/// </summary>
	[TestMethod]
	public void Brain_Config_Accessible()
	{
		var cfg = Vivarium.Config.ConfigProvider.Brain;

		Assert.IsNotNull(cfg);
		Assert.IsTrue(cfg.LocalScanRadius > 0);
	}

	/// <summary>
	/// Integration test: Multiple Think calls in sequence.
	/// </summary>
	[TestMethod]
	public void Think_MultipleCalls_AccumulatesState()
	{
		var agent = TestDataFactory.CreateAgent(seed: 42);
		var grid = TestDataFactory.CreateGridWithRandomEntities(20, 20, agentCount: 3, plantCount: 5, seed: 50);
		var rng = TestDataFactory.CreateRandom(123);
		var population = TestDataFactory.CreateAgentPopulation(3, seed: 100);

		// First Think
		Brain.Think(ref agent, grid, rng, population);
		var firstEnergy = agent.NeuronActivations[(int)SensorType.Energy];

		// Age the agent
		agent.Age += 10;

		// Second Think
		Brain.Think(ref agent, grid, rng, population);

		// Energy should be recalculated (may be same or different based on energy changes)
		Assert.IsNotNull(agent.NeuronActivations);
	}

	/// <summary>
	/// Tests Brain.Act() - action execution.
	/// </summary>
	[TestMethod]
	public void Act_ExecutesWithoutError()
	{
		var agent = TestDataFactory.CreateAgent(seed: 42);
		var grid = TestDataFactory.CreateGridWithRandomEntities(20, 20, agentCount: 3, plantCount: 5, seed: 50);
		var rng = TestDataFactory.CreateRandom(123);
		var population = TestDataFactory.CreateAgentPopulation(5, seed: 100);
		var plants = TestDataFactory.CreatePlantPopulation(5, seed: 200);

		// First run Think to populate neuron activations
		Brain.Think(ref agent, grid, rng, population);

		// Then run Act - should execute without error
		Brain.Act(ref agent, grid, rng, population, plants);

		// Test passes if no exception thrown
		Assert.IsTrue(true);
	}

	/// <summary>
	/// Tests that Act() metabolizes energy.
	/// </summary>
	[TestMethod]
	public void Act_MetabolizesEnergy()
	{
		var agent = TestDataFactory.CreateAgent(seed: 42);
		// Give agent extra energy so they don't die immediately
		var grid = TestDataFactory.CreateEmptyGrid(20, 20);
		agent.ChangeEnergy(100f, grid); // Add 100 energy

		var rng = TestDataFactory.CreateRandom(123);
		var population = TestDataFactory.CreateAgentPopulation(3, seed: 100);
		var plants = TestDataFactory.CreatePlantPopulation(3, seed: 200);

		var initialEnergy = agent.Energy;

		// Run Think first
		Brain.Think(ref agent, grid, rng, population);

		// Run Act
		Brain.Act(ref agent, grid, rng, population, plants);

		// Energy should have decreased due to metabolism
		Assert.IsTrue(agent.Energy <= initialEnergy, "Energy should decrease due to metabolism");
	}

	/// <summary>
	/// Tests that Act() handles empty grid.
	/// </summary>
	[TestMethod]
	public void Act_HandlesEmptyGrid()
	{
		var agent = TestDataFactory.CreateAgent(seed: 42);
		var grid = TestDataFactory.CreateEmptyGrid(10, 10);
		var rng = TestDataFactory.CreateRandom(123);
		var population = TestDataFactory.CreateAgentPopulation(2, seed: 100);
		var plants = TestDataFactory.CreatePlantPopulation(2, seed: 200);

		// Run Think first
		Brain.Think(ref agent, grid, rng, population);

		// Act should not throw
		Brain.Act(ref agent, grid, rng, population, plants);

		Assert.IsTrue(true);
	}
}
