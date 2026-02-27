using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Engine;

namespace Vivarium.Tests.Engine;

[TestClass]
public class StatsLoggerTests
{
    [TestMethod]
    public void StatsLogger_ClassStructure_IsValid()
    {
        var type = typeof(StatsLogger);
        
        Assert.IsNotNull(type);
        Assert.IsTrue(type.IsClass);
        Assert.IsTrue(type.IsSealed);
    }

    [TestMethod]
    public void DietCounts_Struct_HasExpectedFields()
    {
        var type = typeof(StatsLogger.DietCounts);
        
        Assert.IsNotNull(type);
        Assert.IsTrue(type.IsValueType); // Structs are value types
        
        // Check fields
        var herbivoresField = type.GetField("Herbivores");
        var omnivoresField = type.GetField("Omnivores");
        var carnivoresField = type.GetField("Carnivores");
        
        Assert.IsNotNull(herbivoresField);
        Assert.IsNotNull(omnivoresField);
        Assert.IsNotNull(carnivoresField);
    }

    [TestMethod]
    public void DietCounts_DefaultValues_AreZero()
    {
        var counts = new StatsLogger.DietCounts();
        
        Assert.AreEqual(0, counts.Herbivores);
        Assert.AreEqual(0, counts.Omnivores);
        Assert.AreEqual(0, counts.Carnivores);
    }

    [TestMethod]
    public void DietCounts_CanBeModified()
    {
        var counts = new StatsLogger.DietCounts
        {
            Herbivores = 10,
            Omnivores = 5,
            Carnivores = 3
        };
        
        Assert.AreEqual(10, counts.Herbivores);
        Assert.AreEqual(5, counts.Omnivores);
        Assert.AreEqual(3, counts.Carnivores);
    }

    [TestMethod]
    public void LogStats_MethodExists()
    {
        var type = typeof(StatsLogger);
        var method = type.GetMethod("LogStats");
        
        Assert.IsNotNull(method);
        Assert.IsTrue(method.IsStatic);
    }
}
