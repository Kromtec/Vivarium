using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Config;

namespace Vivarium.Tests.Config;

[TestClass]
public class BrainAIConfigTests
{
    [TestMethod]
    public void CreateDefault_ReturnsValidConfig()
    {
        var config = BrainAIConfig.CreateDefault();
        
        Assert.IsNotNull(config);
    }

    [TestMethod]
    public void DefaultValues_AreSetCorrectly()
    {
        var config = BrainAIConfig.CreateDefault();
        
        // Memory
        Assert.AreEqual(0.5f, config.HiddenNeuronDecayFactor);
        
        // Perception
        Assert.AreEqual(2, config.BasePerceptionRadius);
        Assert.AreEqual(2, config.MaxExtraPerceptionRadius);
        Assert.AreEqual(2, config.LocalScanRadius);
        
        // Instinct Biases
        Assert.AreEqual(2.0f, config.InstinctBiasStrength);
        Assert.AreEqual(4.0f, config.HuntingAttackBias);
        
        // Instinct Thresholds
        Assert.AreEqual(0.6f, config.FeedingInstinctThreshold);
        Assert.AreEqual(0.9f, config.ReproductionInstinctThreshold);
        Assert.AreEqual(0.75f, config.OmnivorePlantPreference);
        
        // Action Thresholds
        Assert.AreEqual(0.9f, config.SuicideActivationThreshold);
        Assert.AreEqual(2.0f, config.SuicideAgeMultiplier);
    }

    [TestMethod]
    public void Properties_CanBeModified()
    {
        var config = BrainAIConfig.CreateDefault();
        
        config.HiddenNeuronDecayFactor = 0.8f;
        config.BasePerceptionRadius = 5;
        config.FeedingInstinctThreshold = 0.5f;
        
        Assert.AreEqual(0.8f, config.HiddenNeuronDecayFactor);
        Assert.AreEqual(5, config.BasePerceptionRadius);
        Assert.AreEqual(0.5f, config.FeedingInstinctThreshold);
    }

    [TestMethod]
    public void InstinctThresholds_AreValid()
    {
        var config = BrainAIConfig.CreateDefault();
        
        // Reproduction threshold should be higher than feeding
        Assert.IsTrue(config.ReproductionInstinctThreshold > config.FeedingInstinctThreshold);
    }
}
