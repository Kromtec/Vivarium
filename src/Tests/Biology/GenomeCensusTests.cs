using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Biology;
using System.Collections.Generic;

namespace Vivarium.Tests.Biology;

[TestClass]
public class GenomeCensusTests
{
    [TestMethod]
    public void GenomeFamily_DefaultValues_AreCorrect()
    {
        var family = new GenomeFamily();
        
        Assert.IsNotNull(family.Variants);
        Assert.AreEqual(0, family.TotalCount);
        Assert.AreEqual(0, family.Rank);
    }

    [TestMethod]
    public void GenomeVariant_DefaultValues_AreCorrect()
    {
        var variant = new GenomeVariant();
        
        Assert.AreEqual(0UL, variant.Hash);
        Assert.AreEqual(0, variant.Count);
    }

    [TestMethod]
    public void GenomeCensus_DefaultTopFamilies_IsEmpty()
    {
        var census = new GenomeCensus();
        
        Assert.IsNotNull(census.TopFamilies);
        Assert.AreEqual(0, census.TopFamilies.Count);
    }

    [TestMethod]
    public void GenomeCensus_GetVariantName_ReturnsNull_WhenEmpty()
    {
        var census = new GenomeCensus();
        
        // GetVariantName should return null when TopFamilies is empty
        var result = census.GetVariantName(12345);
        
        Assert.IsNull(result);
    }
}
