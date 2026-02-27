using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Biology;

namespace Vivarium.Tests.Biology;

[TestClass]
public class ScientificNameGeneratorTests
{
    [TestMethod]
    public void GenerateVariantName_First15_UseGreekLetters()
    {
        Assert.AreEqual("var. alpha", ScientificNameGenerator.GenerateVariantName(0));
        Assert.AreEqual("var. beta", ScientificNameGenerator.GenerateVariantName(1));
        Assert.AreEqual("var. gamma", ScientificNameGenerator.GenerateVariantName(2));
        Assert.AreEqual("var. delta", ScientificNameGenerator.GenerateVariantName(3));
        Assert.AreEqual("var. epsilon", ScientificNameGenerator.GenerateVariantName(4));
        Assert.AreEqual("var. zeta", ScientificNameGenerator.GenerateVariantName(5));
        Assert.AreEqual("var. eta", ScientificNameGenerator.GenerateVariantName(6));
        Assert.AreEqual("var. theta", ScientificNameGenerator.GenerateVariantName(7));
        Assert.AreEqual("var. iota", ScientificNameGenerator.GenerateVariantName(8));
        Assert.AreEqual("var. kappa", ScientificNameGenerator.GenerateVariantName(9));
        Assert.AreEqual("var. lambda", ScientificNameGenerator.GenerateVariantName(10));
        Assert.AreEqual("var. mu", ScientificNameGenerator.GenerateVariantName(11));
        Assert.AreEqual("var. nu", ScientificNameGenerator.GenerateVariantName(12));
        Assert.AreEqual("var. xi", ScientificNameGenerator.GenerateVariantName(13));
        Assert.AreEqual("var. omicron", ScientificNameGenerator.GenerateVariantName(14));
    }

    [TestMethod]
    public void GenerateVariantName_After15_UsesNumericIndex()
    {
        Assert.AreEqual("var. 16", ScientificNameGenerator.GenerateVariantName(15));
        Assert.AreEqual("var. 17", ScientificNameGenerator.GenerateVariantName(16));
        Assert.AreEqual("var. 100", ScientificNameGenerator.GenerateVariantName(99));
    }

    [TestMethod]
    public void GenerateVariantName_ZeroIndex_IsAlpha()
    {
        Assert.AreEqual("var. alpha", ScientificNameGenerator.GenerateVariantName(0));
    }
}
