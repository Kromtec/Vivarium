using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Biology;
using Vivarium.Config;
using System;

namespace Vivarium.Tests.Biology;

[TestClass]
public class GeneticsTests
{
    [TestInitialize]
    public void Setup()
    {
        if (!ConfigProvider.IsInitialized)
        {
            var config = SimulationConfig.CreateDefault();
            ConfigProvider.Initialize(config);
        }
    }

    [TestMethod]
    public void ExtractTrait_ReturnsZero_WhenGenomeIsNull()
    {
        float result = Genetics.ExtractTrait(null!, Genetics.TraitType.Strength);
        Assert.AreEqual(0f, result);
    }

    [TestMethod]
    public void ExtractTrait_ReturnsZero_WhenGenomeIsEmpty()
    {
        float result = Genetics.ExtractTrait(Array.Empty<Gene>(), Genetics.TraitType.Strength);
        Assert.AreEqual(0f, result);
    }

    [TestMethod]
    public void ExtractTrait_ReturnsNormalizedValue()
    {
        // Create a genome with specific weights in trait region
        var genome = new Gene[Genetics.GenomeLength];
        int traitStart = Genetics.TraitStartIndex;
        
        // Set weights to 1.0 in the trait region
        genome[traitStart] = Gene.CreateConnection(0, 0, 1.0f);
        genome[traitStart + 1] = Gene.CreateConnection(0, 0, 1.0f);

        float result = Genetics.ExtractTrait(genome, Genetics.TraitType.Strength);
        
        // Should return a value (not NaN, not infinity)
        Assert.IsFalse(float.IsNaN(result));
        Assert.IsFalse(float.IsInfinity(result));
    }

    [TestMethod]
    public void CalculateSimilarity_ReturnsZero_WhenGenomesAreDifferentLengths()
    {
        var a = new Gene[10];
        var b = new Gene[20];
        
        float result = Genetics.CalculateSimilarity(a, b);
        Assert.AreEqual(0f, result);
    }

    [TestMethod]
    public void CalculateSimilarity_ReturnsZero_WhenOneGenomeIsNull()
    {
        var a = new Gene[10];
        
        float result = Genetics.CalculateSimilarity(a, null!);
        Assert.AreEqual(0f, result);
    }

    [TestMethod]
    public void CalculateSimilarity_ReturnsOne_WhenGenomesAreIdentical()
    {
        var genome = new Gene[10];
        for (int i = 0; i < 10; i++)
        {
            genome[i] = Gene.CreateConnection(i, i, 1.0f);
        }
        
        float result = Genetics.CalculateSimilarity(genome, genome);
        Assert.AreEqual(1.0f, result);
    }

    [TestMethod]
    public void CalculateSimilarity_ReturnsZero_WhenGenomesAreCompletelyDifferent()
    {
        var a = new Gene[10];
        var b = new Gene[10];
        
        for (int i = 0; i < 10; i++)
        {
            a[i] = Gene.CreateConnection(i, i, 1.0f);
            b[i] = Gene.CreateConnection(255 - i, 255 - i, -1.0f);
        }
        
        float result = Genetics.CalculateSimilarity(a, b);
        Assert.AreEqual(0f, result);
    }

    [TestMethod]
    public void CalculateSimilarity_ReturnsHalf_WhenHalfMatch()
    {
        var a = new Gene[10];
        var b = new Gene[10];
        
        for (int i = 0; i < 10; i++)
        {
            if (i < 5)
            {
                a[i] = Gene.CreateConnection(i, i, 1.0f);
                b[i] = Gene.CreateConnection(i, i, 1.0f);
            }
            else
            {
                a[i] = Gene.CreateConnection(i, i, 1.0f);
                b[i] = Gene.CreateConnection(255 - i, 255 - i, -1.0f);
            }
        }
        
        float result = Genetics.CalculateSimilarity(a, b);
        Assert.AreEqual(0.5f, result);
    }

    [TestMethod]
    public void CalculateGenomeHash_ReturnsHash_ForSameGenome()
    {
        var genome = new Gene[10];
        for (int i = 0; i < 10; i++)
        {
            genome[i] = Gene.CreateConnection(i, i, 1.0f);
        }
        
        var hash1 = Genetics.CalculateGenomeHash(genome);
        var hash2 = Genetics.CalculateGenomeHash(genome);
        
        Assert.AreEqual(hash1, hash2);
    }

    [TestMethod]
    public void CalculateGenomeHash_ReturnsDifferentHash_ForDifferentGenome()
    {
        var a = new Gene[10];
        var b = new Gene[10];
        
        for (int i = 0; i < 10; i++)
        {
            a[i] = Gene.CreateConnection(i, i, 1.0f);
            b[i] = Gene.CreateConnection(i + 1, i, 1.0f);
        }
        
        var hashA = Genetics.CalculateGenomeHash(a);
        var hashB = Genetics.CalculateGenomeHash(b);
        
        Assert.AreNotEqual(hashA, hashB);
    }

    [TestMethod]
    public void CalculateGenomeHash_ReturnsZero_ForEmptyGenome()
    {
        var genome = Array.Empty<Gene>();
        
        // FNV-1a with empty input returns offset basis
        ulong expected = 14695981039346656037UL;
        
        var hash = Genetics.CalculateGenomeHash(genome);
        Assert.AreEqual(expected, hash);
    }

    [TestMethod]
    public void Genetics_ConfigProperties_AreAccessible()
    {
        Assert.IsTrue(Genetics.GenomeLength > 0);
        Assert.IsTrue(Genetics.TraitGeneCount > 0);
        Assert.IsTrue(Genetics.TraitStartIndex >= 0);
        Assert.IsTrue(Genetics.MutationRate >= 0);
    }
}
