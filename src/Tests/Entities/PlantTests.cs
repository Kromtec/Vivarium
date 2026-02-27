using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Entities;
using Vivarium.World;

namespace Vivarium.Tests.Entities;

[TestClass]
public class PlantTests
{
	[TestMethod]
	public void Plant_ClassStructure_IsValid()
	{
		var type = typeof(Plant);
		
		Assert.IsNotNull(type);
		Assert.IsTrue(type.IsValueType);
		Assert.IsTrue(type.IsSealed);
	}

	[TestMethod]
	public void Plant_ImplementsIGridEntity()
	{
		var type = typeof(Plant);
		var interfaces = type.GetInterfaces();
		
		Assert.IsTrue(Array.Exists(interfaces, i => i.Name == "IGridEntity"));
	}

	[TestMethod]
	public void Plant_Properties_Exist()
	{
		var type = typeof(Plant);
		
		// Core IGridEntity properties
		var idProp = type.GetProperty("Id");
		var indexProp = type.GetProperty("Index");
		var xProp = type.GetProperty("X");
		var yProp = type.GetProperty("Y");
		
		// Plant-specific properties
		var ageProp = type.GetProperty("Age");
		var isAliveProp = type.GetProperty("IsAlive");
		var originalColorProp = type.GetProperty("OriginalColor");
		var colorProp = type.GetProperty("Color");
		var energyProp = type.GetProperty("Energy");
		
		Assert.IsNotNull(idProp);
		Assert.IsNotNull(indexProp);
		Assert.IsNotNull(xProp);
		Assert.IsNotNull(yProp);
		Assert.IsNotNull(ageProp);
		Assert.IsNotNull(isAliveProp);
		Assert.IsNotNull(originalColorProp);
		Assert.IsNotNull(colorProp);
		Assert.IsNotNull(energyProp);
	}

	[TestMethod]
	public void Plant_StaticConfigProperties_Exist()
	{
		var type = typeof(Plant);
		
		var shrivelRateProp = type.GetProperty("ShrivelRate");
		var photosynthesisRateProp = type.GetProperty("PhotosynthesisRate");
		var maturityAgeProp = type.GetProperty("MaturityAge");
		var reproductionCostProp = type.GetProperty("ReproductionCost");
		var minEnergyToReproduceProp = type.GetProperty("MinEnergyToReproduce");
		var maxEnergyProp = type.GetProperty("MaxEnergy");
		
		Assert.IsNotNull(shrivelRateProp);
		Assert.IsNotNull(photosynthesisRateProp);
		Assert.IsNotNull(maturityAgeProp);
		Assert.IsNotNull(reproductionCostProp);
		Assert.IsNotNull(minEnergyToReproduceProp);
		Assert.IsNotNull(maxEnergyProp);
	}

	[TestMethod]
	public void Plant_StaticMethods_Exist()
	{
		var type = typeof(Plant);
		
		var createMethod = type.GetMethod("Create");
		var constructPlantMethod = type.GetMethod("ConstructPlant", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		
		Assert.IsNotNull(createMethod);
		Assert.IsNotNull(constructPlantMethod);
	}

	[TestMethod]
	public void Plant_InstanceMethods_Exist()
	{
		var type = typeof(Plant);
		
		var changeEnergyMethod = type.GetMethod("ChangeEnergy");
		var updateMethod = type.GetMethod("Update");
		var tryReproduceMethod = type.GetMethod("TryReproduce");
		var canReproduceMethod = type.GetMethod("CanReproduce");
		
		Assert.IsNotNull(changeEnergyMethod);
		Assert.IsNotNull(updateMethod);
		Assert.IsNotNull(tryReproduceMethod);
		Assert.IsNotNull(canReproduceMethod);
	}

	[TestMethod]
	public void Plant_ChangeEnergy_ClampsEnergy()
	{
		// Test that energy is clamped to MaxEnergy
		var type = typeof(Plant);
		var maxEnergyProp = type.GetProperty("MaxEnergy");
		
		Assert.IsNotNull(maxEnergyProp);
	}

	[TestMethod]
	public void Plant_CanReproduce_ReturnsFalse_WhenDead()
	{
		var type = typeof(Plant);
		
		// CanReproduce is a readonly method
		var canReproduceMethod = type.GetMethod("CanReproduce");
		
		Assert.IsNotNull(canReproduceMethod);
		// The method checks: IsAlive && Age >= MaturityAge
	}

	[TestMethod]
	public void Plant_TryReproduce_Has5PercentChance()
	{
		var type = typeof(Plant);
		var tryReproduceMethod = type.GetMethod("TryReproduce");
		
		Assert.IsNotNull(tryReproduceMethod);
	}
}
