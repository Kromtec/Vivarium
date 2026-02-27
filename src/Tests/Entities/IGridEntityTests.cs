using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Entities;

namespace Vivarium.Tests.Entities;

[TestClass]
public class IGridEntityTests
{
    [TestMethod]
    public void IGridEntity_IsInterface()
    {
        var type = typeof(IGridEntity);
        
        Assert.IsNotNull(type);
        Assert.IsTrue(type.IsInterface);
    }

    [TestMethod]
    public void IGridEntity_HasExpectedMembers()
    {
        var type = typeof(IGridEntity);
        
        // Check properties
        var idProp = type.GetProperty("Id");
        var indexProp = type.GetProperty("Index");
        var xProp = type.GetProperty("X");
        var yProp = type.GetProperty("Y");
        
        Assert.IsNotNull(idProp);
        Assert.IsNotNull(indexProp);
        Assert.IsNotNull(xProp);
        Assert.IsNotNull(yProp);
        
        // Check property types
        Assert.AreEqual(typeof(long), idProp.PropertyType);
        Assert.AreEqual(typeof(int), indexProp.PropertyType);
        Assert.AreEqual(typeof(int), xProp.PropertyType);
        Assert.AreEqual(typeof(int), yProp.PropertyType);
    }

    [TestMethod]
    public void Agent_ImplementsIGridEntity()
    {
        var agentType = typeof(Agent);
        var interfaces = agentType.GetInterfaces();
        
        Assert.IsTrue(Array.Exists(interfaces, i => i == typeof(IGridEntity)));
    }

    [TestMethod]
    public void Plant_ImplementsIGridEntity()
    {
        var plantType = typeof(Plant);
        var interfaces = plantType.GetInterfaces();
        
        Assert.IsTrue(Array.Exists(interfaces, i => i == typeof(IGridEntity)));
    }

    [TestMethod]
    public void Structure_ImplementsIGridEntity()
    {
        var structureType = typeof(Structure);
        var interfaces = structureType.GetInterfaces();
        
        Assert.IsTrue(Array.Exists(interfaces, i => i == typeof(IGridEntity)));
    }
}
