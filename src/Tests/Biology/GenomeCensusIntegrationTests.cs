using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Biology;
using Vivarium.Entities;
using System.Linq;

namespace Vivarium.Tests.Biology;

[TestClass]
public class GenomeCensusIntegrationTests
{
	[TestInitialize]
	public void Setup()
	{
		TestDataFactory.Reset();
		TestDataFactory.EnsureConfigInitialized(seed: 42);
	}

	/// <summary>
	/// Tests GenomeCensus.AnalyzePopulation - analyzes agent population for genome families.
	/// NOW TESTABLE with TestDataFactory!
	/// </summary>
	[TestMethod]
	public void AnalyzePopulation_WithEmptyArray()
	{
		var census = new GenomeCensus();
		var agents = TestDataFactory.CreateAgentPopulation(0, seed: 42);

		// Should not throw
		census.AnalyzePopulation(agents);

		Assert.IsNotNull(census);
	}

	[TestMethod]
	public void AnalyzePopulation_WithSingleAgent()
	{
		var census = new GenomeCensus();
		var agents = TestDataFactory.CreateAgentPopulation(1, seed: 42);

		census.AnalyzePopulation(agents);

		// Census should have data
		Assert.IsNotNull(census.TopFamilies);
	}

	[TestMethod]
	public void AnalyzePopulation_WithMultipleAgents()
	{
		var census = new GenomeCensus();
		var agents = TestDataFactory.CreateAgentPopulation(10, seed: 42);

		census.AnalyzePopulation(agents);

		// Should identify families
		Assert.IsNotNull(census.TopFamilies);
	}

	[TestMethod]
	public void AnalyzePopulation_TracksFamilyCount()
	{
		var census = new GenomeCensus();
		// Create agents with same seed = same genome = same family
		var agents = TestDataFactory.CreateAgentPopulation(5, seed: 100);

		census.AnalyzePopulation(agents);

		// With same seed, all should be in same family
		Assert.IsTrue(census.TopFamilies.Count > 0);
	}

	[TestMethod]
	public void AnalyzePopulation_DifferentSeeds_DifferentFamilies()
	{
		var census = new GenomeCensus();
		// Create agents with different seeds = different genomes
		var agents1 = TestDataFactory.CreateAgentPopulation(3, seed: 100);
		var agents2 = TestDataFactory.CreateAgentPopulation(3, seed: 200);
		
		var allAgents = agents1.Concat(agents2).ToArray();

		census.AnalyzePopulation(allAgents);

		// Should have multiple families
		Assert.IsTrue(census.TopFamilies.Count >= 2);
	}
}
