using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Biology;
using Vivarium.Entities;
using Vivarium.World;
using Vivarium.Config;
using System;

namespace Vivarium.Tests.Biology;

[TestClass]
public class BrainTests
{
    [TestInitialize]
    public void Setup()
    {
        // Initialize config for testing - only if not already initialized
        if (!ConfigProvider.IsInitialized)
        {
            var config = SimulationConfig.CreateDefault();
            ConfigProvider.Initialize(config);
        }
    }

    [TestMethod]
    public void BrainConfig_HasExpectedNeuronCount()
    {
        Assert.AreEqual(256, BrainConfig.NeuronCount);
    }

    [TestMethod]
    public void BrainConfig_HasExpectedSensorCount()
    {
        Assert.IsTrue(BrainConfig.SensorCount > 0);
    }

    [TestMethod]
    public void BrainConfig_HasExpectedActionCount()
    {
        Assert.AreEqual(8, BrainConfig.ActionCount);
    }

    [TestMethod]
    public void BrainConfig_GetActionIndex_ReturnsCorrectOffset()
    {
        // Actions start after sensors
        int expectedOffset = BrainConfig.SensorCount;
        
        Assert.AreEqual(expectedOffset + 0, BrainConfig.GetActionIndex(ActionType.MoveN));
        Assert.AreEqual(expectedOffset + 1, BrainConfig.GetActionIndex(ActionType.MoveS));
        Assert.AreEqual(expectedOffset + 2, BrainConfig.GetActionIndex(ActionType.MoveE));
        Assert.AreEqual(expectedOffset + 3, BrainConfig.GetActionIndex(ActionType.MoveW));
        Assert.AreEqual(expectedOffset + 4, BrainConfig.GetActionIndex(ActionType.Attack));
        Assert.AreEqual(expectedOffset + 5, BrainConfig.GetActionIndex(ActionType.Reproduce));
        Assert.AreEqual(expectedOffset + 6, BrainConfig.GetActionIndex(ActionType.Suicide));
        Assert.AreEqual(expectedOffset + 7, BrainConfig.GetActionIndex(ActionType.Flee));
    }

    [TestMethod]
    public void BrainConfig_OffsetsAreLogical()
    {
        Assert.IsTrue(BrainConfig.SensorsStart >= 0);
        Assert.IsTrue(BrainConfig.ActionsStart > BrainConfig.SensorsStart);
        Assert.IsTrue(BrainConfig.HiddenStart > BrainConfig.ActionsStart);
        Assert.AreEqual(BrainConfig.NeuronCount, BrainConfig.HiddenStart + BrainConfig.HiddenCount);
    }

    [TestMethod]
    public void BrainConfig_SourceMap_IsValid()
    {
        Assert.AreEqual(256, BrainConfig.SourceMap.Length);
        for (int i = 0; i < 256; i++)
        {
            int source = BrainConfig.SourceMap[i];
            Assert.IsTrue(source >= 0 && source < BrainConfig.NeuronCount);
        }
    }

    [TestMethod]
    public void BrainConfig_SinkMap_IsValid()
    {
        Assert.AreEqual(256, BrainConfig.SinkMap.Length);
        for (int i = 0; i < 256; i++)
        {
            int sink = BrainConfig.SinkMap[i];
            Assert.IsTrue(sink >= BrainConfig.ActionsStart && sink < BrainConfig.NeuronCount);
        }
    }

    [TestMethod]
    public void Agent_NeuronActivations_InitializedCorrectly()
    {
        // Test that an agent has the correct neuron array size
        var agent = Agent.Create(0, 10, 10, new Random(42));
        
        Assert.IsNotNull(agent.NeuronActivations);
        Assert.AreEqual(BrainConfig.NeuronCount, agent.NeuronActivations.Length);
    }
}
