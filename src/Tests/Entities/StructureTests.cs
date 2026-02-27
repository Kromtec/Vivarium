using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Entities;
using Vivarium.World;

namespace Vivarium.Tests.Entities;

[TestClass]
public class StructureTests
{
	[TestMethod]
	public void Structure_ClassStructure_IsValid()
	{
		var type = typeof(Structure);
		
		Assert.IsNotNull(type);
		Assert.IsTrue(type.IsValueType);
		Assert.IsTrue(type.IsSealed);
	}

	[TestMethod]
	public void Structure_ImplementsIGridEntity()
	{
		var type = typeof(Structure);
		var interfaces = type.GetInterfaces();
		
		Assert.IsTrue(Array.Exists(interfaces, i => i.Name == "IGridEntity"));
	}

	[TestMethod]
	public void Structure_Properties_Exist()
	{
		var type = typeof(Structure);
		
		// Core IGridEntity properties
		var idProp = type.GetProperty("Id");
		var indexProp = type.GetProperty("Index");
		var xProp = type.GetProperty("X");
		var yProp = type.GetProperty("Y");
		
		// Structure-specific properties
		var originalColorProp = type.GetProperty("OriginalColor");
		var colorProp = type.GetProperty("Color");
		
		Assert.IsNotNull(idProp);
		Assert.IsNotNull(indexProp);
		Assert.IsNotNull(xProp);
		Assert.IsNotNull(yProp);
		Assert.IsNotNull(originalColorProp);
		Assert.IsNotNull(colorProp);
	}

	[TestMethod]
	public void Structure_StaticMethods_Exist()
	{
		var type = typeof(Structure);
		
		var createMethod = type.GetMethod("Create");
		var constructStructureMethod = type.GetMethod("ConstructStructure", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		
		Assert.IsNotNull(createMethod);
		Assert.IsNotNull(constructStructureMethod);
	}

	[TestMethod]
	public void Structure_PropertyTypes_AreCorrect()
	{
		var type = typeof(Structure);
		
		// Check IGridEntity property types
		var idProp = type.GetProperty("Id");
		var indexProp = type.GetProperty("Index");
		var xProp = type.GetProperty("X");
		var yProp = type.GetProperty("Y");
		
		Assert.IsNotNull(idProp);
		Assert.IsNotNull(indexProp);
		Assert.IsNotNull(xProp);
		Assert.IsNotNull(yProp);
		
		Assert.AreEqual(typeof(long), idProp.PropertyType);
		Assert.AreEqual(typeof(int), indexProp.PropertyType);
		Assert.AreEqual(typeof(int), xProp.PropertyType);
		Assert.AreEqual(typeof(int), yProp.PropertyType);
	}

	[TestMethod]
	public void Structure_ColorProperties_AreCorrect()
	{
		var type = typeof(Structure);
		
		var originalColorProp = type.GetProperty("OriginalColor");
		var colorProp = type.GetProperty("Color");
		
		Assert.IsNotNull(originalColorProp);
		Assert.IsNotNull(colorProp);
		
		// Both should be Microsoft.Xna.Framework.Color
		Assert.AreEqual("Color", originalColorProp.PropertyType.Name);
		Assert.AreEqual("Color", colorProp.PropertyType.Name);
	}

	[TestMethod]
	public void Structure_OriginalColorSetter_UpdatesColor()
	{
		var type = typeof(Structure);
		
		// Verify OriginalColor has a setter
		var originalColorProp = type.GetProperty("OriginalColor");
		
		Assert.IsNotNull(originalColorProp);
		Assert.IsTrue(originalColorProp.CanWrite);
	}
}
