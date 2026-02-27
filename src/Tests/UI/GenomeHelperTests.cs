using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.UI;

namespace Vivarium.Tests.UI;

[TestClass]
public class GenomeHelperTests
{
	[TestMethod]
	public void GenomeHelper_IsStaticClass()
	{
		var type = typeof(GenomeHelper);
		
		Assert.IsNotNull(type);
		Assert.IsTrue(type.IsClass);
		Assert.IsTrue(type.IsSealed);
		Assert.IsTrue(type.IsAbstract);
	}

	[TestMethod]
	public void GenomeHelper_PublicMethods_Exist()
	{
		var type = typeof(GenomeHelper);
		
		var generateHelixTextureMethod = type.GetMethod("GenerateHelixTexture");
		var generateGenomeGridTextureMethod = type.GetMethod("GenerateGenomeGridTexture");
		
		Assert.IsNotNull(generateHelixTextureMethod);
		Assert.IsNotNull(generateGenomeGridTextureMethod);
	}

	[TestMethod]
	public void GenomeHelper_GenerateHelixTexture_HasCorrectParameters()
	{
		var type = typeof(GenomeHelper);
		var method = type.GetMethod("GenerateHelixTexture");
		
		Assert.IsNotNull(method);
		
		var parameters = method.GetParameters();
		Assert.AreEqual(2, parameters.Length);
		
		Assert.AreEqual("GraphicsDevice", parameters[0].ParameterType.Name);
		Assert.AreEqual("Agent", parameters[1].ParameterType.Name);
	}

	[TestMethod]
	public void GenomeHelper_GenerateHelixTexture_ReturnsTexture2D()
	{
		var type = typeof(GenomeHelper);
		var method = type.GetMethod("GenerateHelixTexture");
		
		Assert.IsNotNull(method);
		
		Assert.AreEqual("Texture2D", method.ReturnType.Name);
	}

	[TestMethod]
	public void GenomeHelper_GenerateGenomeGridTexture_HasCorrectParameters()
	{
		var type = typeof(GenomeHelper);
		var method = type.GetMethod("GenerateGenomeGridTexture");
		
		Assert.IsNotNull(method);
		
		var parameters = method.GetParameters();
		Assert.AreEqual(2, parameters.Length);
		
		Assert.AreEqual("GraphicsDevice", parameters[0].ParameterType.Name);
		Assert.AreEqual("Agent", parameters[1].ParameterType.Name);
	}

	[TestMethod]
	public void GenomeHelper_GenerateGenomeGridTexture_ReturnsTexture2D()
	{
		var type = typeof(GenomeHelper);
		var method = type.GetMethod("GenerateGenomeGridTexture");
		
		Assert.IsNotNull(method);
		
		Assert.AreEqual("Texture2D", method.ReturnType.Name);
	}

	[TestMethod]
	public void GenomeHelper_PrivateMethods_Exist()
	{
		var type = typeof(GenomeHelper);
		
		var drawVerticalCapsuleMethod = type.GetMethod("DrawVerticalCapsule", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		var drawCircleMethod = type.GetMethod("DrawCircle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		
		Assert.IsNotNull(drawVerticalCapsuleMethod);
		Assert.IsNotNull(drawCircleMethod);
	}
}
