using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Config;

namespace Vivarium.Tests.Config;

[TestClass]
public class WorldConfigTests
{
    [TestMethod]
    public void CreateDefault_ReturnsValidConfig()
    {
        var config = WorldConfig.CreateDefault();
        
        Assert.AreEqual(96, config.GridHeight);
        Assert.AreEqual(160, config.GridWidth); // (96/9)*16 with integer division = 160
        Assert.IsTrue(config.CellSize > 0);
        Assert.IsTrue(config.AgentPoolSize > 0);
        Assert.IsTrue(config.PlantPoolSize > 0);
        Assert.IsTrue(config.StructurePoolSize > 0);
    }

    [TestMethod]
    public void CreateDefault_WithSeed_SetsSeed()
    {
        var config = WorldConfig.CreateDefault(12345);
        
        Assert.AreEqual(12345, config.Seed);
    }

    [TestMethod]
    public void CreateDefault_DefaultSeed_Is64()
    {
        var config = WorldConfig.CreateDefault();
        
        Assert.AreEqual(64, config.Seed);
    }

    [TestMethod]
    public void PoolSizes_AreReasonable()
    {
        var config = WorldConfig.CreateDefault();
        
        int totalCells = config.GridWidth * config.GridHeight;
        
        // AgentPoolSize = totalCells / 18
        Assert.IsTrue(config.AgentPoolSize < totalCells / 10);
        
        // PlantPoolSize = totalCells / 8 (larger than agents)
        Assert.IsTrue(config.PlantPoolSize > config.AgentPoolSize);
        
        // StructurePoolSize = totalCells / 32 (smallest)
        Assert.IsTrue(config.StructurePoolSize < config.AgentPoolSize);
    }

    [TestMethod]
    public void GridWidth_UsesIntegerDivision()
    {
        var config = WorldConfig.CreateDefault();
        
        // In C#, 96/9 = 10 (integer division)
        // Then 10 * 16 = 160
        Assert.AreEqual(160, config.GridWidth);
    }
}
