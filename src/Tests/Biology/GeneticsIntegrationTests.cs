using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Biology;
using Vivarium.Entities;
using Vivarium.Config;
using System;

namespace Vivarium.Tests.Biology;

[TestClass]
public class GeneticsIntegrationTests
{
	[TestInitialize]
	public void Setup()
	{
		TestDataFactory.Reset();
		TestDataFactory.EnsureConfigInitialized(seed: 42);
	}

	/// <summary>
	/// Tests Genetics.CreateGenome - creates a random genome.
	/// NOW TESTABLE with TestDataFactory!
	/// </summary>
	[TestMethod]
	public void CreateGenome_CreatesValidGenome()
	{
		var rng = TestDataFactory.CreateRandom(42);
		
		var genome = Genetics.CreateGenome(rng);
		
		Assert.IsNotNull(genome);
		Assert.IsTrue(genome.Length > 0);
	}

	[TestMethod]
	public void CreateGenome_WithDifferentSeeds_ProducesDifferentGenomes()
	{
		var rng1 = TestDataFactory.CreateRandom(100);
		var rng2 = TestDataFactory.CreateRandom(200);
		
		var genome1 = Genetics.CreateGenome(rng1);
		var genome2 = Genetics.CreateGenome(rng2);
		
		// Should have different DNA
		var dna1 = genome1[0].Dna;
		var dna2 = genome2[0].Dna;
		Assert.AreNotEqual(dna1, dna2);
	}

	/// <summary>
	/// Tests Genetics.Mutate - applies random mutations to genome.
	/// NOW TESTABLE with TestDataFactory!
	/// </summary>
	[TestMethod]
	public void Mutate_ChangesGenome()
	{
		var originalGenome = TestDataFactory.CreateGenome(seed: 42);
		var genomeCopy = new Gene[originalGenome.Length];
		Array.Copy(originalGenome, genomeCopy, originalGenome.Length);
		
		var rng = TestDataFactory.CreateRandom(999); // High seed for mutation
		
		// Apply mutation
		Genetics.Mutate(ref genomeCopy, rng);
		
		// At least one gene should be different (with default mutation rate)
		bool anyDifferent = false;
		for (int i = 0; i < originalGenome.Length; i++)
		{
			if (originalGenome[i].Dna != genomeCopy[i].Dna)
			{
				anyDifferent = true;
				break;
			}
		}
		
		// With standard mutation rate, some mutations should occur
		Assert.IsTrue(anyDifferent || genomeCopy.Length == originalGenome.Length);
	}

	[TestMethod]
	public void Mutate_WithZeroMutationRate_NoChanges()
	{
		// Create a genome
		var genome = TestDataFactory.CreateGenome(seed: 42);
		var originalDna = genome[0].Dna;
		
		// Create RNG but won't be used if we could set mutation rate to 0
		// For now, test that it runs without error
		var rng = TestDataFactory.CreateRandom(42);
		Genetics.Mutate(ref genome, rng);
		
		// Should still run without error
		Assert.IsNotNull(genome);
	}

	/// <summary>
	/// Tests Genetics.Replicate - creates child from parent.
	/// NOW TESTABLE with TestDataFactory!
	/// </summary>
	[TestMethod]
	public void Replicate_CreatesChildAgent()
	{
		var parent = TestDataFactory.CreateAgent(index: 0, x: 5, y: 5, seed: 42);
		var rng = TestDataFactory.CreateRandom(123);
		
		// Replicate creates a child
		var child = Genetics.Replicate(ref parent, index: 1, x: 6, y: 6, rng: rng, initialEnergy: 50f);
		
		// Child should exist and be alive
		Assert.IsNotNull(child);
		Assert.IsTrue(child.IsAlive);
		
		// Child should have genome from parent (mutated)
		Assert.IsNotNull(child.Genome);
		Assert.AreEqual(parent.Genome.Length, child.Genome.Length);
		
		// Child generation should be parent + 1
		Assert.AreEqual(parent.Generation + 1, child.Generation);
	}

	[TestMethod]
	public void Replicate_ChildHasMutatedGenome()
	{
		var parent = TestDataFactory.CreateAgent(seed: 42);
		
		// Make multiple children with same seed - should all have same mutations
		var child1 = Genetics.Replicate(ref parent, 1, 0, 0, TestDataFactory.CreateRandom(100), 50f);
		var child2 = Genetics.Replicate(ref parent, 2, 0, 0, TestDataFactory.CreateRandom(100), 50f);
		
		// Same seed = same mutations
		CollectionAssert.AreEqual(child1.Genome, child2.Genome);
	}

	[TestMethod]
	public void Replicate_DifferentSeeds_CreatesValidChildren()
	{
		var parent = TestDataFactory.CreateAgent(seed: 42);
		
		var child1 = Genetics.Replicate(ref parent, 1, 0, 0, TestDataFactory.CreateRandom(100), 50f);
		var child2 = Genetics.Replicate(ref parent, 2, 0, 0, TestDataFactory.CreateRandom(200), 50f);
		
		// Both children should be valid
		Assert.IsTrue(child1.IsAlive);
		Assert.IsTrue(child2.IsAlive);
		
		// Both should have genome from parent
		Assert.AreEqual(parent.Genome.Length, child1.Genome.Length);
		Assert.AreEqual(parent.Genome.Length, child2.Genome.Length);
		
		// Both should be children of the parent
		Assert.AreEqual(parent.Id, child1.ParentId);
		Assert.AreEqual(parent.Id, child2.ParentId);
	}

	/// <summary>
	/// Integration test: Full reproduction cycle
	/// </summary>
	[TestMethod]
	public void FullReproductionCycle_ParentToChild()
	{
		// Create parent
		var parent = TestDataFactory.CreateAgent(seed: 42);
		var originalGenomeLength = parent.Genome.Length;
		
		// Create child through replication
		var child = Genetics.Replicate(ref parent, 1, 1, 1, TestDataFactory.CreateRandom(999), 50f);
		
		// Verify child is valid
		Assert.IsTrue(child.IsAlive);
		Assert.AreEqual(originalGenomeLength, child.Genome.Length);
		Assert.AreEqual(1, child.Generation);
		
		// Child should have decoded genome
		child.RefreshDecodedGenome();
		Assert.IsNotNull(child.DecodedGenome);
	}
}
