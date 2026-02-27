using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Config;

namespace Vivarium.Tests.Config;

[TestClass]
public class PlantConfigTests
{
    [TestMethod]
    public void CreateDefault_ReturnsValidConfig()
    {
        var config = PlantConfig.CreateDefault();
        
        Assert.IsNotNull(config);
    }

    [TestMethod]
    public void DefaultValues_AreSetCorrectly()
    {
        var config = PlantConfig.CreateDefault();
        
        Assert.AreEqual(0.4f, config.ShrivelRate);
        Assert.AreEqual(0.50f, config.PhotosynthesisRate);
        Assert.AreEqual(600, config.MaturityAge);
        Assert.AreEqual(20.0f, config.ReproductionCost);
        Assert.AreEqual(30.0f, config.MinEnergyToReproduce);
        Assert.AreEqual(100f, config.MaxEnergy);
    }

    [TestMethod]
    public void Properties_CanBeModified()
    {
        var config = PlantConfig.CreateDefault();
        
        config.ShrivelRate = 0.2f;
        config.PhotosynthesisRate = 1.0f;
        config.MaxEnergy = 200f;
        
        Assert.AreEqual(0.2f, config.ShrivelRate);
        Assert.AreEqual(1.0f, config.PhotosynthesisRate);
        Assert.AreEqual(200f, config.MaxEnergy);
    }

    [TestMethod]
    public void MinEnergyToReproduce_IsLessThanMaxEnergy()
    {
        var config = PlantConfig.CreateDefault();
        
        Assert.IsTrue(config.MinEnergyToReproduce < config.MaxEnergy);
    }
}
