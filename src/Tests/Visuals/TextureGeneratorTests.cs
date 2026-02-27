using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Visuals;

namespace Vivarium.Tests.Visuals;

[TestClass]
public class TextureGeneratorTests
{
	[TestMethod]
	public void TextureGenerator_IsStaticClass()
	{
		var type = typeof(TextureGenerator);
		
		Assert.IsNotNull(type);
		Assert.IsTrue(type.IsClass);
		Assert.IsTrue(type.IsSealed);
		Assert.IsTrue(type.IsAbstract);
	}

	[TestMethod]
	public void TextureGenerator_PublicStaticMethods_Exist()
	{
		var type = typeof(TextureGenerator);
		
		// Main texture creation methods
		var createCircleMethod = type.GetMethod("CreateCircle");
		var createStarMethod = type.GetMethod("CreateStar");
		var createRoundedRectMethod = type.GetMethod("CreateRoundedRect");
		var createTriangleMethod = type.GetMethod("CreateTriangle");
		var createRingMethod = type.GetMethod("CreateRing");
		var createStructureTextureMethod = type.GetMethod("CreateStructureTexture");
		var createOrganicShapeMethod = type.GetMethod("CreateOrganicShape");
		var createAgentTextureMethod = type.GetMethod("CreateAgentTexture");
		
		Assert.IsNotNull(createCircleMethod);
		Assert.IsNotNull(createStarMethod);
		Assert.IsNotNull(createRoundedRectMethod);
		Assert.IsNotNull(createTriangleMethod);
		Assert.IsNotNull(createRingMethod);
		Assert.IsNotNull(createStructureTextureMethod);
		Assert.IsNotNull(createOrganicShapeMethod);
		Assert.IsNotNull(createAgentTextureMethod);
	}

	[TestMethod]
	public void TextureGenerator_CreateCircle_HasCorrectParameters()
	{
		var type = typeof(TextureGenerator);
		var method = type.GetMethod("CreateCircle");
		
		Assert.IsNotNull(method);
		
		var parameters = method.GetParameters();
		Assert.AreEqual(2, parameters.Length);
		Assert.AreEqual("GraphicsDevice", parameters[0].ParameterType.Name);
		Assert.AreEqual(typeof(int), parameters[1].ParameterType);
	}

	[TestMethod]
	public void TextureGenerator_CreateStar_HasCorrectParameters()
	{
		var type = typeof(TextureGenerator);
		var method = type.GetMethod("CreateStar");
		
		Assert.IsNotNull(method);
		
		var parameters = method.GetParameters();
		Assert.AreEqual(3, parameters.Length);
		Assert.AreEqual("GraphicsDevice", parameters[0].ParameterType.Name);
		Assert.AreEqual(typeof(int), parameters[1].ParameterType);
	}

	[TestMethod]
	public void TextureGenerator_CreateRoundedRect_HasCorrectParameters()
	{
		var type = typeof(TextureGenerator);
		var method = type.GetMethod("CreateRoundedRect");
		
		Assert.IsNotNull(method);
		
		var parameters = method.GetParameters();
		Assert.AreEqual(4, parameters.Length);
	}

	[TestMethod]
	public void TextureGenerator_CreateTriangle_HasCorrectParameters()
	{
		var type = typeof(TextureGenerator);
		var method = type.GetMethod("CreateTriangle");
		
		Assert.IsNotNull(method);
		
		var parameters = method.GetParameters();
		Assert.AreEqual(2, parameters.Length);
	}

	[TestMethod]
	public void TextureGenerator_CreateRing_HasCorrectParameters()
	{
		var type = typeof(TextureGenerator);
		var method = type.GetMethod("CreateRing");
		
		Assert.IsNotNull(method);
		
		var parameters = method.GetParameters();
		Assert.AreEqual(3, parameters.Length);
	}

	[TestMethod]
	public void TextureGenerator_CreateStructureTexture_HasCorrectParameters()
	{
		var type = typeof(TextureGenerator);
		var method = type.GetMethod("CreateStructureTexture");
		
		Assert.IsNotNull(method);
		
		var parameters = method.GetParameters();
		Assert.AreEqual(8, parameters.Length);
		
		// GraphicsDevice graphicsDevice, int size, int cornerRadius, int borderThickness, bool top, bool right, bool bottom, bool left
		Assert.AreEqual("GraphicsDevice", parameters[0].ParameterType.Name);
		Assert.AreEqual(typeof(int), parameters[1].ParameterType);
		Assert.AreEqual(typeof(int), parameters[2].ParameterType);
		Assert.AreEqual(typeof(int), parameters[3].ParameterType);
		// bool top, bool right, bool bottom, bool left
		Assert.AreEqual(typeof(bool), parameters[4].ParameterType);
		Assert.AreEqual(typeof(bool), parameters[5].ParameterType);
		Assert.AreEqual(typeof(bool), parameters[6].ParameterType);
		Assert.AreEqual(typeof(bool), parameters[7].ParameterType);
	}

	[TestMethod]
	public void TextureGenerator_CreateOrganicShape_HasCorrectParameters()
	{
		var type = typeof(TextureGenerator);
		var method = type.GetMethod("CreateOrganicShape");
		
		Assert.IsNotNull(method);
		
		var parameters = method.GetParameters();
		Assert.AreEqual(5, parameters.Length);
	}

	[TestMethod]
	public void TextureGenerator_CreateAgentTexture_HasCorrectParameters()
	{
		var type = typeof(TextureGenerator);
		var method = type.GetMethod("CreateAgentTexture");
		
		Assert.IsNotNull(method);
		
		var parameters = method.GetParameters();
		Assert.AreEqual(4, parameters.Length);
		
		Assert.AreEqual("GraphicsDevice", parameters[0].ParameterType.Name);
		Assert.AreEqual(typeof(int), parameters[1].ParameterType);
		Assert.AreEqual("DietType", parameters[2].ParameterType.Name);
		Assert.AreEqual(typeof(int), parameters[3].ParameterType);
	}

	[TestMethod]
	public void TextureGenerator_PrivateMethods_Exist()
	{
		var type = typeof(TextureGenerator);
		
		// Private helper methods
		var getAgentSDFMethod = type.GetMethod("GetAgentSDF", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		var smoothMinMethod = type.GetMethod("SmoothMin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		
		Assert.IsNotNull(getAgentSDFMethod);
		Assert.IsNotNull(smoothMinMethod);
	}

	[TestMethod]
	public void TextureGenerator_CreateMethods_ReturnTexture2D()
	{
		var type = typeof(TextureGenerator);
		
		// Just verify the return types are Texture2D
		var createCircleMethod = type.GetMethod("CreateCircle");
		var createStarMethod = type.GetMethod("CreateStar");
		var createTriangleMethod = type.GetMethod("CreateTriangle");
		var createRingMethod = type.GetMethod("CreateRing");
		
		Assert.IsNotNull(createCircleMethod);
		Assert.IsNotNull(createStarMethod);
		Assert.IsNotNull(createTriangleMethod);
		Assert.IsNotNull(createRingMethod);
		
		Assert.AreEqual("Texture2D", createCircleMethod.ReturnType.Name);
		Assert.AreEqual("Texture2D", createStarMethod.ReturnType.Name);
		Assert.AreEqual("Texture2D", createTriangleMethod.ReturnType.Name);
		Assert.AreEqual("Texture2D", createRingMethod.ReturnType.Name);
	}
}
