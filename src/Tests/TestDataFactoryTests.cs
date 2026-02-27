using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Biology;
using Vivarium.Entities;
using Vivarium.World;

namespace Vivarium.Tests;

[TestClass]
public class TestDataFactoryTests
{
	[TestMethod]
	public void CreateRandom_ReturnsSeededRandom()
	{
		var rng1 = TestDataFactory.CreateRandom(42);
		var rng2 = TestDataFactory.CreateRandom(42);

		// Same seed should produce same sequence
		Assert.AreEqual(rng1.Next(), rng2.Next());
	}

	[TestMethod]
	public void CreateAgent_CreatesValidAgent()
	{
		TestDataFactory.Reset();
		var agent = TestDataFactory.CreateAgent(index: 0, x: 5, y: 10, seed: 123);

		Assert.IsNotNull(agent);
		Assert.AreEqual(0, agent.Index);
		Assert.AreEqual(5, agent.X);
		Assert.AreEqual(10, agent.Y);
		Assert.IsTrue(agent.IsAlive);
		Assert.IsNotNull(agent.Genome);
		Assert.IsTrue(agent.Genome.Length > 0);
	}

	[TestMethod]
	public void CreateAgent_WithDifferentSeeds_ProducesDifferentAgents()
	{
		TestDataFactory.Reset();
		var agent1 = TestDataFactory.CreateAgent(seed: 100);
		var agent2 = TestDataFactory.CreateAgent(seed: 200);

		// Different seeds should produce different genomes
		CollectionAssert.AreNotEqual(agent1.Genome, agent2.Genome);
	}

	[TestMethod]
	public void CreateGenome_CreatesValidGenome()
	{
		TestDataFactory.Reset();
		var genome = TestDataFactory.CreateGenome(seed: 42);

		Assert.IsNotNull(genome);
		Assert.IsTrue(genome.Length > 0);
	}

	[TestMethod]
	public void CreateGenomeWithLength_CreatesSpecifiedLength()
	{
		TestDataFactory.Reset();
		var genome = TestDataFactory.CreateGenomeWithLength(100, seed: 42);

		Assert.AreEqual(100, genome.Length);
	}

	[TestMethod]
	public void CreateEmptyGrid_CreatesEmptyGrid()
	{
		var grid = TestDataFactory.CreateEmptyGrid(10, 20);

		Assert.AreEqual(10, grid.GetLength(0));
		Assert.AreEqual(20, grid.GetLength(1));

		// All cells should be empty
		for (int x = 0; x < 10; x++)
		{
			for (int y = 0; y < 20; y++)
			{
				Assert.AreEqual(EntityType.Empty, grid[x, y].Type);
			}
		}
	}

	[TestMethod]
	public void CreateGridWithEntities_CreatesPopulatedGrid()
	{
		var grid = TestDataFactory.CreateGridWithRandomEntities(
			width: 10, height: 10, 
			agentCount: 3, plantCount: 5, 
			seed: 42);

		int agentCount = 0;
		int plantCount = 0;

		for (int x = 0; x < 10; x++)
		{
			for (int y = 0; y < 10; y++)
			{
				if (grid[x, y].Type == EntityType.Agent) agentCount++;
				if (grid[x, y].Type == EntityType.Plant) plantCount++;
			}
		}

		// Note: Counts may be less due to overlap handling
		Assert.IsTrue(agentCount > 0);
		Assert.IsTrue(plantCount > 0);
	}

	[TestMethod]
	public void CreatePlant_CreatesValidPlant()
	{
		TestDataFactory.Reset();
		var plant = TestDataFactory.CreatePlant(index: 0, x: 5, y: 10, seed: 123);

		Assert.IsNotNull(plant);
		Assert.AreEqual(0, plant.Index);
		Assert.AreEqual(5, plant.X);
		Assert.AreEqual(10, plant.Y);
		Assert.IsTrue(plant.IsAlive);
	}

	[TestMethod]
	public void CreateStructure_CreatesValidStructure()
	{
		var structure = TestDataFactory.CreateStructure(index: 0, x: 5, y: 10);

		Assert.IsNotNull(structure);
		Assert.AreEqual(0, structure.Index);
		Assert.AreEqual(5, structure.X);
		Assert.AreEqual(10, structure.Y);
	}

	[TestMethod]
	public void CreateAgentPopulation_CreatesPopulation()
	{
		TestDataFactory.Reset();
		var agents = TestDataFactory.CreateAgentPopulation(5, seed: 42);

		Assert.AreEqual(5, agents.Length);

		// Each agent should be valid
		for (int i = 0; i < 5; i++)
		{
			Assert.IsTrue(agents[i].IsAlive);
			Assert.IsNotNull(agents[i].Genome);
		}
	}

	[TestMethod]
	public void CreateGene_CreatesValidGene()
	{
		var gene = TestDataFactory.CreateGene(source: 5, sink: 10, weight: 0.5f);

		Assert.AreEqual(5, gene.SourceId);
		Assert.AreEqual(10, gene.SinkId);
		Assert.AreEqual(0.5f, gene.Weight, 0.001f);
	}

	[TestMethod]
	public void CreateGenes_CreatesMultipleGenes()
	{
		var genes = TestDataFactory.CreateGenes(
			(1, 2, 0.1f),
			(3, 4, -0.5f),
			(5, 6, 1.0f)
		);

		Assert.AreEqual(3, genes.Length);
		Assert.AreEqual(1, genes[0].SourceId);
		Assert.AreEqual(-0.5f, genes[1].Weight, 0.001f);
	}

	[TestMethod]
	public void Reset_CanReinitialize()
	{
		// First initialization
		TestDataFactory.Reset();
		TestDataFactory.EnsureConfigInitialized(100);
		var agent1 = TestDataFactory.CreateAgent(seed: 100);

		// Reset and reinitialize with different seed
		TestDataFactory.Reset();
		TestDataFactory.EnsureConfigInitialized(200);
		var agent2 = TestDataFactory.CreateAgent(seed: 200);

		// Should work without issues
		Assert.IsNotNull(agent1);
		Assert.IsNotNull(agent2);
	}
}
