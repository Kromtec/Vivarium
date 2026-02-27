using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Config;

namespace Vivarium.Tests.Config;

[TestClass]
public class GeneticsConfigTests
{
    [TestMethod]
    public void CreateDefault_ReturnsValidConfig()
    {
        var config = GeneticsConfig.CreateDefault();
        
        Assert.AreEqual(512, config.GenomeLength);
        Assert.AreEqual(14, config.TraitGeneCount);
    }

    [TestMethod]
    public void CreateDefault_SetsDefaultValues()
    {
        var config = GeneticsConfig.CreateDefault();
        
        Assert.AreEqual(0.001, config.MutationRate);
        Assert.AreEqual(4.0f, config.InitialWeightRange);
        Assert.AreEqual(4.0f, config.TraitNormalizer);
    }

    [TestMethod]
    public void TraitStartIndex_IsCalculatedCorrectly()
    {
        var config = new GeneticsConfig
        {
            GenomeLength = 512,
            TraitGeneCount = 14
        };
        
        Assert.AreEqual(498, config.TraitStartIndex);
    }

    [TestMethod]
    public void TraitStartIndex_ZeroTraitCount()
    {
        var config = new GeneticsConfig
        {
            GenomeLength = 100,
            TraitGeneCount = 0
        };
        
        Assert.AreEqual(100, config.TraitStartIndex);
    }

    [TestMethod]
    public void MutationRate_CanBeModified()
    {
        var config = GeneticsConfig.CreateDefault();
        
        config.MutationRate = 0.05;
        
        Assert.AreEqual(0.05, config.MutationRate);
    }

    [TestMethod]
    public void InitialWeightRange_CanBeModified()
    {
        var config = GeneticsConfig.CreateDefault();
        
        config.InitialWeightRange = 2.0f;
        
        Assert.AreEqual(2.0f, config.InitialWeightRange);
    }

    [TestMethod]
    public void TraitNormalizer_CanBeModified()
    {
        var config = GeneticsConfig.CreateDefault();
        
        config.TraitNormalizer = 5.0f;
        
        Assert.AreEqual(5.0f, config.TraitNormalizer);
    }

    [TestMethod]
    public void RequiredProperties_MustBeSet()
    {
        // Using init only properties - this tests the required behavior
        var config = new GeneticsConfig
        {
            GenomeLength = 256,
            TraitGeneCount = 10,
            MutationRate = 0.01
        };
        
        Assert.AreEqual(256, config.GenomeLength);
        Assert.AreEqual(10, config.TraitGeneCount);
    }
}
