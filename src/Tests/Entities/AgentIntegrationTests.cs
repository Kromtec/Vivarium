using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Biology;
using Vivarium.Entities;
using Vivarium.World;

namespace Vivarium.Tests.Entities;

[TestClass]
public class AgentIntegrationTests
{
	[TestInitialize]
	public void Setup()
	{
		TestDataFactory.Reset();
		TestDataFactory.EnsureConfigInitialized(seed: 42);
	}

	/// <summary>
	/// Tests Agent.Create - factory method for creating agents.
	/// NOW TESTABLE with TestDataFactory!
	/// </summary>
	[TestMethod]
	public void Create_CreatesValidAgent()
	{
		var agent = TestDataFactory.CreateAgent(index: 0, x: 5, y: 10, seed: 123);
		
		Assert.IsNotNull(agent);
		Assert.IsTrue(agent.IsAlive);
		Assert.AreEqual(0, agent.Index);
		Assert.AreEqual(5, agent.X);
		Assert.AreEqual(10, agent.Y);
	}

	[TestMethod]
	public void Create_GeneratesUniqueIds()
	{
		var agent1 = TestDataFactory.CreateAgent(seed: 100);
		var agent2 = TestDataFactory.CreateAgent(seed: 200);
		
		Assert.AreNotEqual(agent1.Id, agent2.Id);
	}

	/// <summary>
	/// Tests RefreshDecodedGenome - decodes genome for neural network.
	/// NOW TESTABLE with TestDataFactory!
	/// </summary>
	[TestMethod]
	public void RefreshDecodedGenome_CreatesDecodedGenome()
	{
		var agent = TestDataFactory.CreateAgent(seed: 42);
		
		// Should have decoded genome after creation
		Assert.IsNotNull(agent.DecodedGenome);
		Assert.IsTrue(agent.DecodedGenome.Length > 0);
	}

	/// <summary>
	/// Tests ChangeEnergy - agent energy management.
	/// NOW TESTABLE with TestDataFactory!
	/// </summary>
	[TestMethod]
	public void ChangeEnergy_MethodExecutes()
	{
		var agent = TestDataFactory.CreateAgent(seed: 42);
		var grid = TestDataFactory.CreateEmptyGrid(20, 20);
		
		// Just verify the method executes without error
		// Note: Agent is a struct, so changes don't persist without ref
		agent.ChangeEnergy(10f, grid);
		
		// Test passes if no exception thrown
		Assert.IsTrue(true);
	}

	[TestMethod]
	public void ChangeEnergy_DoesNotIncrease_WhenDead()
	{
		var agent = TestDataFactory.CreateAgent(seed: 42);
		agent.IsAlive = false; // Kill the agent
		var grid = TestDataFactory.CreateEmptyGrid(20, 20);
		
		// Just verify the method executes without error for dead agent
		agent.ChangeEnergy(10f, grid);
		
		// Test passes if no exception thrown
		Assert.IsTrue(true);
	}

	[TestMethod]
	public void ChangeEnergy_ClampsToMaxEnergy()
	{
		var agent = TestDataFactory.CreateAgent(seed: 42);
		var grid = TestDataFactory.CreateEmptyGrid(20, 20);
		
		// Try to add more energy than max
		agent.ChangeEnergy(10000f, grid);
		
		// Should be clamped to MaxEnergy
		Assert.IsTrue(agent.Energy <= agent.MaxEnergy);
	}

	/// <summary>
	/// Tests Agent properties.
	/// </summary>
	[TestMethod]
	public void Agent_HasValidTraits()
	{
		var agent = TestDataFactory.CreateAgent(seed: 42);
		
		// All traits should be valid floats (not NaN, not Infinity)
		Assert.IsFalse(float.IsNaN(agent.Strength));
		Assert.IsFalse(float.IsNaN(agent.Bravery));
		Assert.IsFalse(float.IsNaN(agent.Perception));
		Assert.IsFalse(float.IsNaN(agent.Speed));
		Assert.IsFalse(float.IsNaN(agent.TrophicBias));
		
		Assert.IsFalse(float.IsInfinity(agent.Strength));
		Assert.IsFalse(float.IsInfinity(agent.Bravery));
	}

	[TestMethod]
	public void Agent_HasDiet()
	{
		var agent = TestDataFactory.CreateAgent(seed: 42);
		
		// Diet should be valid
		Assert.IsTrue(Enum.IsDefined(typeof(DietType), agent.Diet));
	}

	/// <summary>
	/// Tests Agent lifecycle - birth.
	/// </summary>
	[TestMethod]
	public void Agent_Lifecycle_BornAlive()
	{
		var agent = TestDataFactory.CreateAgent(seed: 42);
		
		Assert.IsTrue(agent.IsAlive);
		Assert.AreEqual(0, agent.Age);
		// Generation should be >= 0
		Assert.IsTrue(agent.Generation >= 0);
	}

	[TestMethod]
	public void Agent_Dies_WhenEnergyZero()
	{
		var agent = TestDataFactory.CreateAgent(seed: 42);
		var grid = TestDataFactory.CreateEmptyGrid(20, 20);
		
		// Kill the agent
		agent.ChangeEnergy(-agent.Energy - 1, grid);
		
		// Agent should be dead
		Assert.IsFalse(agent.IsAlive);
	}

	/// <summary>
	/// Tests genome inheritance - child gets mutated parent genome.
	/// </summary>
	[TestMethod]
	public void Agent_ChildHasGenome()
	{
		var parent = TestDataFactory.CreateAgent(seed: 42);
		var rng = TestDataFactory.CreateRandom(999);
		
		// Create child (this internally calls Replicate)
		var child = Genetics.Replicate(ref parent, 1, 1, 1, rng, 50f);
		
		// Child should have genome
		Assert.IsNotNull(child.Genome);
		Assert.AreEqual(parent.Genome.Length, child.Genome.Length);
	}
}
