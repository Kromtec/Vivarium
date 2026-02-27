using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Entities;

namespace Vivarium.Tests.Entities;

[TestClass]
public class AgentTests
{
    [TestMethod]
    public void Agent_ClassStructure_IsValid()
    {
        var type = typeof(Agent);
        
        Assert.IsNotNull(type);
        Assert.IsTrue(type.IsValueType); // It's a struct
        Assert.IsTrue(type.IsSealed);
    }

    [TestMethod]
    public void Agent_ImplementsIGridEntity()
    {
        var type = typeof(Agent);
        var interfaces = type.GetInterfaces();
        
        Assert.IsTrue(Array.Exists(interfaces, i => i.Name == "IGridEntity"));
    }

    [TestMethod]
    public void Agent_Properties_Exist()
    {
        var type = typeof(Agent);
        
        // Core properties
        var idProp = type.GetProperty("Id");
        var indexProp = type.GetProperty("Index");
        var xProp = type.GetProperty("X");
        var yProp = type.GetProperty("Y");
        var lastXProp = type.GetProperty("LastX");
        var lastYProp = type.GetProperty("LastY");
        var parentIdProp = type.GetProperty("ParentId");
        var dietProp = type.GetProperty("Diet");
        var originalColorProp = type.GetProperty("OriginalColor");
        var colorProp = type.GetProperty("Color");
        var ageProp = type.GetProperty("Age");
        var generationProp = type.GetProperty("Generation");
        var isAliveProp = type.GetProperty("IsAlive");
        var energyProp = type.GetProperty("Energy");
        var genomeProp = type.GetProperty("Genome");
        var decodedGenomeProp = type.GetProperty("DecodedGenome");
        var neuronActivationsProp = type.GetProperty("NeuronActivations");
        var reproductionCooldownField = type.GetField("ReproductionCooldown");
        var movementCooldownField = type.GetField("MovementCooldown");
        var attackCooldownProp = type.GetProperty("AttackCooldown");
        
        Assert.IsNotNull(idProp);
        Assert.IsNotNull(indexProp);
        Assert.IsNotNull(xProp);
        Assert.IsNotNull(yProp);
        Assert.IsNotNull(lastXProp);
        Assert.IsNotNull(lastYProp);
        Assert.IsNotNull(parentIdProp);
        Assert.IsNotNull(dietProp);
        Assert.IsNotNull(originalColorProp);
        Assert.IsNotNull(colorProp);
        Assert.IsNotNull(ageProp);
        Assert.IsNotNull(generationProp);
        Assert.IsNotNull(isAliveProp);
        Assert.IsNotNull(energyProp);
        Assert.IsNotNull(genomeProp);
        Assert.IsNotNull(decodedGenomeProp);
        Assert.IsNotNull(neuronActivationsProp);
        Assert.IsNotNull(reproductionCooldownField);
        Assert.IsNotNull(movementCooldownField);
        Assert.IsNotNull(attackCooldownProp);
    }

    [TestMethod]
    public void Agent_StaticConfigProperties_Exist()
    {
        var type = typeof(Agent);
        
        // Static properties from config
        var reproOverheadProp = type.GetProperty("ReproductionOverheadPct");
        var minEnergyBufferProp = type.GetProperty("MinEnergyBuffer");
        var baseAttackThresholdProp = type.GetProperty("BaseAttackThreshold");
        var baseMovementThresholdProp = type.GetProperty("BaseMovementThreshold");
        var baseMetabolismRateProp = type.GetProperty("BaseMetabolismRate");
        var baseMovementCooldownProp = type.GetProperty("BaseMovementCooldown");
        var movementCostProp = type.GetProperty("MovementCost");
        var maturityAgeProp = type.GetProperty("MaturityAge");
        
        Assert.IsNotNull(reproOverheadProp);
        Assert.IsNotNull(minEnergyBufferProp);
        Assert.IsNotNull(baseAttackThresholdProp);
        Assert.IsNotNull(baseMovementThresholdProp);
        Assert.IsNotNull(baseMetabolismRateProp);
        Assert.IsNotNull(baseMovementCooldownProp);
        Assert.IsNotNull(movementCostProp);
        Assert.IsNotNull(maturityAgeProp);
    }

    [TestMethod]
    public void Agent_StaticMethods_Exist()
    {
        var type = typeof(Agent);
        
        var createMethod = type.GetMethod("Create");
        var createChildMethod = type.GetMethod("CreateChild");
        
        Assert.IsNotNull(createMethod);
        Assert.IsNotNull(createChildMethod);
    }
}
