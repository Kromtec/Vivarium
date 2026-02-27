using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Config;

namespace Vivarium.Tests.Config;

[TestClass]
public class AgentConfigTests
{
    [TestMethod]
    public void CreateDefault_ReturnsValidConfig()
    {
        var config = AgentConfig.CreateDefault();
        
        Assert.IsNotNull(config);
    }

    [TestMethod]
    public void DefaultValues_AreSetCorrectly()
    {
        var config = AgentConfig.CreateDefault();
        
        // Reproduction
        Assert.AreEqual(0.30f, config.ReproductionOverheadPct);
        Assert.AreEqual(5.0f, config.MinEnergyBuffer);
        Assert.AreEqual(600, config.MaturityAge);
        Assert.AreEqual(600, config.ReproductionCooldownFrames);
        Assert.AreEqual(0.4f, config.HerbivoreReproductionMultiplier);
        Assert.AreEqual(0.3f, config.CarnivoreReproductionMultiplier);
        Assert.AreEqual(0.6f, config.OmnivoreReproductionMultiplier);
        
        // Movement
        Assert.AreEqual(0.1f, config.BaseMovementThreshold);
        Assert.AreEqual(10, config.BaseMovementCooldown);
        Assert.AreEqual(0.25f, config.MovementCost);
        Assert.AreEqual(1.414f, config.DiagonalMovementMultiplier);
        Assert.AreEqual(2.0f, config.FleeMovementMultiplier);
        
        // Combat
        Assert.AreEqual(0.5f, config.BaseAttackThreshold);
        Assert.AreEqual(60, config.AttackCooldownFrames);
        
        // Metabolism
        Assert.AreEqual(0.01f, config.BaseMetabolismRate);
        Assert.AreEqual(0.70f, config.HerbivoreMetabolismMultiplier);
        Assert.AreEqual(0.65f, config.CarnivoreMetabolismMultiplier);
        Assert.AreEqual(0.90f, config.OmnivoreMetabolismMultiplier);
        
        // Diet
        Assert.AreEqual(-0.55f, config.CarnivoreTrophicThreshold);
        Assert.AreEqual(0.0f, config.HerbivoreTrophicThreshold);
        
        // Traits
        Assert.AreEqual(100f, config.BaseMaxEnergy);
        Assert.AreEqual(0.5f, config.ConstitutionEnergyScale);
        Assert.AreEqual(0.5f, config.StrengthPowerScale);
        Assert.AreEqual(1.5f, config.HerbivoreResilienceMultiplier);
    }

    [TestMethod]
    public void Properties_CanBeModified()
    {
        var config = AgentConfig.CreateDefault();
        
        config.ReproductionOverheadPct = 0.5f;
        config.MaturityAge = 1200;
        config.BaseMetabolismRate = 0.05f;
        config.BaseMaxEnergy = 200f;
        
        Assert.AreEqual(0.5f, config.ReproductionOverheadPct);
        Assert.AreEqual(1200, config.MaturityAge);
        Assert.AreEqual(0.05f, config.BaseMetabolismRate);
        Assert.AreEqual(200f, config.BaseMaxEnergy);
    }

    [TestMethod]
    public void DietThresholds_AreValid()
    {
        var config = AgentConfig.CreateDefault();
        
        // Carnivore threshold should be lower than Herbivore
        Assert.IsTrue(config.CarnivoreTrophicThreshold < config.HerbivoreTrophicThreshold);
    }
}
