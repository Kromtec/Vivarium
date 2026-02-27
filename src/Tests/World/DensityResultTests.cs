using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.World;

namespace Vivarium.Tests.World;

[TestClass]
public class DensityResultTests
{
	[TestMethod]
	public void DensityResult_IsReadOnlyRecordStruct()
	{
		var type = typeof(DensityResult);
		
		Assert.IsNotNull(type);
		Assert.IsTrue(type.IsValueType);
		Assert.IsTrue(type.IsSealed);
	}

	[TestMethod]
	public void DensityResult_IsPublic()
	{
		var type = typeof(DensityResult);
		
		Assert.IsTrue(type.IsPublic);
	}

	[TestMethod]
	public void DensityResult_Properties_Exist()
	{
		var type = typeof(DensityResult);
		
		var agentDensityProp = type.GetProperty("AgentDensity");
		var plantDensityProp = type.GetProperty("PlantDensity");
		var structureDensityProp = type.GetProperty("StructureDensity");
		
		Assert.IsNotNull(agentDensityProp);
		Assert.IsNotNull(plantDensityProp);
		Assert.IsNotNull(structureDensityProp);
	}

	[TestMethod]
	public void DensityResult_Properties_AreFloat()
	{
		var type = typeof(DensityResult);
		
		var agentDensityProp = type.GetProperty("AgentDensity");
		var plantDensityProp = type.GetProperty("PlantDensity");
		var structureDensityProp = type.GetProperty("StructureDensity");
		
		Assert.IsNotNull(agentDensityProp);
		Assert.IsNotNull(plantDensityProp);
		Assert.IsNotNull(structureDensityProp);
		
		Assert.AreEqual(typeof(float), agentDensityProp.PropertyType);
		Assert.AreEqual(typeof(float), plantDensityProp.PropertyType);
		Assert.AreEqual(typeof(float), structureDensityProp.PropertyType);
	}

	[TestMethod]
	public void DensityResult_CanBeInstantiated()
	{
		var result = new DensityResult(0.5f, 0.3f, 0.2f);
		
		Assert.AreEqual(0.5f, result.AgentDensity);
		Assert.AreEqual(0.3f, result.PlantDensity);
		Assert.AreEqual(0.2f, result.StructureDensity);
	}

	[TestMethod]
	public void DensityResult_IsImmutable()
	{
		var result1 = new DensityResult(1.0f, 2.0f, 3.0f);
		var result2 = new DensityResult(1.0f, 2.0f, 3.0f);
		
		// Record structs are value types and should compare by value
		Assert.AreEqual(result1, result2);
	}

	[TestMethod]
	public void DensityResult_ToString_ContainsValues()
	{
		var result = new DensityResult(0.5f, 0.3f, 0.2f);
		var str = result.ToString();
		
		Assert.IsNotNull(str);
		StringAssert.Contains(str, "0.5");
		StringAssert.Contains(str, "0.3");
		StringAssert.Contains(str, "0.2");
	}
}
