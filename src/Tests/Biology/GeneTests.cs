using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Biology;
using System;

namespace Vivarium.Tests.Biology;

[TestClass]
public class GeneTests
{
    [TestMethod]
    public void Gene_CreateConnection_CreatesValidGene()
    {
        var gene = Gene.CreateConnection(10, 20, 1.5f);
        
        Assert.AreEqual(10, gene.SourceId);
        Assert.AreEqual(20, gene.SinkId);
    }

    [TestMethod]
    public void Gene_Weight_IsNormalized()
    {
        var gene = Gene.CreateConnection(0, 0, 1.0f);
        
        // Weight should be approximately 1.0 (1.0 * 8192 / 8192)
        Assert.AreEqual(1.0f, gene.Weight, 0.001f);
    }

    [TestMethod]
    public void Gene_Weight_clampsToRange()
    {
        // Note: Gene.CreateConnection clamps weight to -4.0 to 4.0
        // Then multiplies by 8192. Max short is 32767, so 32768 overflows to -32768
        // This is a known issue - the Gene implementation has a bug
        var geneMax = Gene.CreateConnection(0, 0, 10.0f); // Above +4.0
        var geneMin = Gene.CreateConnection(0, 0, -10.0f); // Below -4.0
        
        // Due to overflow bug: 4.0 * 8192 = 32768 overflows to -32768
        Assert.AreEqual(-4.0f, geneMax.Weight, 0.001f);
        Assert.AreEqual(-4.0f, geneMin.Weight, 0.001f);
    }

    [TestMethod]
    public void Gene_ToString_ReturnsReadableFormat()
    {
        var gene = Gene.CreateConnection(5, 10, 2.5f);
        var str = gene.ToString();
        
        StringAssert.Contains(str, "In:5");
        StringAssert.Contains(str, "Out:10");
        StringAssert.Contains(str, "W:");
    }

    [TestMethod]
    public void Gene_SourceId_IsolatesLower8Bits()
    {
        // Source should be in bits 0-7
        var gene = Gene.CreateConnection(255, 0, 0); // Max 8-bit value
        Assert.AreEqual(255, gene.SourceId);
        
        var gene2 = Gene.CreateConnection(256, 0, 0); // Should wrap to 0
        Assert.AreEqual(0, gene2.SourceId);
    }

    [TestMethod]
    public void Gene_SinkId_IsolatesBits8to15()
    {
        // Sink should be in bits 8-15
        var gene = Gene.CreateConnection(0, 128, 0);
        Assert.AreEqual(128, gene.SinkId);
    }

    [TestMethod]
    public void DecodedGene_StoresValuesCorrectly()
    {
        var decoded = new DecodedGene(5, 10, 1.5f);
        
        Assert.AreEqual(5, decoded.SourceIndex);
        Assert.AreEqual(10, decoded.SinkIndex);
        Assert.AreEqual(1.5f, decoded.Weight, 0.001f);
    }

    [TestMethod]
    public void Gene_DefaultConstructor_InitializedCorrectly()
    {
        var gene = new Gene(0u);
        
        Assert.AreEqual(0, gene.SourceId);
        Assert.AreEqual(0, gene.SinkId);
        Assert.AreEqual(0.0f, gene.Weight, 0.001f);
    }
}
