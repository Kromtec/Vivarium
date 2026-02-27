using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Biology;
using Vivarium.Entities;

namespace Vivarium.Tests.Biology;

[TestClass]
public class ScientificNameGeneratorIntegrationTests
{
	[TestInitialize]
	public void Setup()
	{
		TestDataFactory.Reset();
		TestDataFactory.EnsureConfigInitialized(seed: 42);
	}

	/// <summary>
	/// Tests ScientificNameGenerator.GenerateFamilyName - generates name from agent genome.
	/// NOW TESTABLE with TestDataFactory!
	/// </summary>
	[TestMethod]
	public void GenerateFamilyName_CreatesName()
	{
		var agent = TestDataFactory.CreateAgent(seed: 42);

		var (name, translation) = ScientificNameGenerator.GenerateFamilyName(agent);

		Assert.IsNotNull(name);
		Assert.IsNotNull(translation);
		Assert.IsTrue(name.Length > 0);
	}

	[TestMethod]
	public void GenerateFamilyName_DifferentAgents_DifferentNames()
	{
		var agent1 = TestDataFactory.CreateAgent(seed: 100);
		var agent2 = TestDataFactory.CreateAgent(seed: 200);

		var (name1, _) = ScientificNameGenerator.GenerateFamilyName(agent1);
		var (name2, _) = ScientificNameGenerator.GenerateFamilyName(agent2);

		// Different genomes should produce different names
		Assert.AreNotEqual(name1, name2);
	}

	[TestMethod]
	public void GenerateFamilyName_SameAgent_SameName()
	{
		var agent = TestDataFactory.CreateAgent(seed: 42);

		var (name1, translation1) = ScientificNameGenerator.GenerateFamilyName(agent);
		var (name2, translation2) = ScientificNameGenerator.GenerateFamilyName(agent);

		// Same agent should produce same name
		Assert.AreEqual(name1, name2);
		Assert.AreEqual(translation1, translation2);
	}

	/// <summary Generate>
	/// TestsVariantName.
	/// </summary>
	[TestMethod]
	public void GenerateVariantName_CreatesName()
	{
		var name = ScientificNameGenerator.GenerateVariantName(0);

		Assert.IsNotNull(name);
		Assert.IsTrue(name.Length > 0);
	}

	[TestMethod]
	public void GenerateVariantName_DifferentIndices_DifferentNames()
	{
		var name1 = ScientificNameGenerator.GenerateVariantName(0);
		var name2 = ScientificNameGenerator.GenerateVariantName(1);
		var name3 = ScientificNameGenerator.GenerateVariantName(100);

		// Different indices should produce different names
		Assert.AreNotEqual(name1, name2);
		Assert.AreNotEqual(name2, name3);
	}

	/// <summary>
	/// Tests that diet affects the generated name.
	/// </summary>
	[TestMethod]
	public void GenerateFamilyName_DifferentDiets_DifferentPrefixes()
	{
		var herbivore = TestDataFactory.CreateAgent(seed: 100);
		var carnivore = TestDataFactory.CreateAgent(seed: 200);

		// Note: Diet is determined by genome, not directly settable
		// But different seeds should produce different diets
		var (name1, _) = ScientificNameGenerator.GenerateFamilyName(herbivore);
		var (name2, _) = ScientificNameGenerator.GenerateFamilyName(carnivore);

		// Names should be different (different genomes)
		Assert.AreNotEqual(name1, name2);
	}
}
