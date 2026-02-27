using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.World;

namespace Vivarium.Tests.World;

[TestClass]
public class WorldSensorTests
{
	[TestMethod]
	public void WorldSensor_IsStaticClass()
	{
		var type = typeof(WorldSensor);
		
		Assert.IsNotNull(type);
		Assert.IsTrue(type.IsClass);
		Assert.IsTrue(type.IsSealed);
		Assert.IsTrue(type.IsAbstract);
	}

	[TestMethod]
	public void WorldSensor_StaticMethods_Exist()
	{
		var type = typeof(WorldSensor);
		
		var scanLocalAreaMethod = type.GetMethod("ScanLocalArea");
		var tryGetRandomEmptySpotMethod = type.GetMethod("TryGetRandomEmptySpot");
		var populateDirectionalSensorsMethod = type.GetMethod("PopulateDirectionalSensors");
		var scanSensorsMethod = type.GetMethod("ScanSensors");
		
		Assert.IsNotNull(scanLocalAreaMethod);
		Assert.IsNotNull(tryGetRandomEmptySpotMethod);
		Assert.IsNotNull(populateDirectionalSensorsMethod);
		Assert.IsNotNull(scanSensorsMethod);
	}

	[TestMethod]
	public void WorldSensor_ScanLocalArea_HasCorrectParameters()
	{
		var type = typeof(WorldSensor);
		var method = type.GetMethod("ScanLocalArea");
		
		Assert.IsNotNull(method);
		
		var parameters = method.GetParameters();
		Assert.AreEqual(4, parameters.Length);
		
		Assert.AreEqual("GridCell[,]", parameters[0].ParameterType.Name);
		Assert.AreEqual(typeof(int), parameters[1].ParameterType);
		Assert.AreEqual(typeof(int), parameters[2].ParameterType);
		Assert.AreEqual(typeof(int), parameters[3].ParameterType);
	}

	[TestMethod]
	public void WorldSensor_ScanLocalArea_ReturnsDensityResult()
	{
		var type = typeof(WorldSensor);
		var method = type.GetMethod("ScanLocalArea");
		
		Assert.IsNotNull(method);
		
		Assert.AreEqual("DensityResult", method.ReturnType.Name);
	}

	[TestMethod]
	public void WorldSensor_TryGetRandomEmptySpot_HasCorrectParameters()
	{
		var type = typeof(WorldSensor);
		var method = type.GetMethod("TryGetRandomEmptySpot");
		
		Assert.IsNotNull(method);
		
		var parameters = method.GetParameters();
		Assert.AreEqual(4, parameters.Length);
		
		Assert.AreEqual("GridCell[,]", parameters[0].ParameterType.Name);
	}

	[TestMethod]
	public void WorldSensor_PopulateDirectionalSensors_HasCorrectParameters()
	{
		var type = typeof(WorldSensor);
		var method = type.GetMethod("PopulateDirectionalSensors");
		
		Assert.IsNotNull(method);
		
		var parameters = method.GetParameters();
		Assert.AreEqual(8, parameters.Length);
	}

	[TestMethod]
	public void WorldSensor_ScanSensors_HasCorrectParameters()
	{
		var type = typeof(WorldSensor);
		var method = type.GetMethod("ScanSensors");
		
		Assert.IsNotNull(method);
		
		var parameters = method.GetParameters();
		Assert.AreEqual(15, parameters.Length);
	}

	[TestMethod]
	public void WorldSensor_PrivateFields_Exist()
	{
		var type = typeof(WorldSensor);
		
		var directionLookupField = type.GetField("DirectionLookup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		var lookupOffsetField = type.GetField("LookupOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		
		Assert.IsNotNull(directionLookupField);
		Assert.IsNotNull(lookupOffsetField);
	}

	[TestMethod]
	public void WorldSensor_PrivateMethods_Exist()
	{
		var type = typeof(WorldSensor);
		
		var calculateDirectionIndexMethod = type.GetMethod("CalculateDirectionIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		
		Assert.IsNotNull(calculateDirectionIndexMethod);
	}
}
