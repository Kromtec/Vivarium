using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.UI;

namespace Vivarium.Tests.UI;

[TestClass]
public class ActivityLogTests
{
	[TestMethod]
	public void ActivityLog_IsStaticClass()
	{
		var type = typeof(ActivityLog);
		
		Assert.IsNotNull(type);
		Assert.IsTrue(type.IsClass);
		Assert.IsTrue(type.IsSealed);
		Assert.IsTrue(type.IsAbstract);
	}

	[TestMethod]
	public void ActivityLog_Properties_Exist()
	{
		var type = typeof(ActivityLog);
		
		var isLoggingEnabledProp = type.GetProperty("IsLoggingEnabled");
		var targetAgentIdProp = type.GetProperty("TargetAgentId");
		
		Assert.IsNotNull(isLoggingEnabledProp);
		Assert.IsNotNull(targetAgentIdProp);
	}

	[TestMethod]
	public void ActivityLog_StaticMethods_Exist()
	{
		var type = typeof(ActivityLog);
		
		// Log methods
		var logMethod1 = type.GetMethod("Log", [typeof(long), typeof(string)]);
		var logMethod2 = type.GetMethod("Log", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static, null, [typeof(long), type.Module.GetType("Vivarium.UI.ActivityLog+LogHandler")!], null);
		
		// Control methods
		var setTargetMethod = type.GetMethod("SetTarget");
		var enableMethod = type.GetMethod("Enable");
		var disableMethod = type.GetMethod("Disable");
		
		// Draw method
		var drawMethod = type.GetMethod("Draw");
		
		Assert.IsNotNull(logMethod1);
		Assert.IsNotNull(setTargetMethod);
		Assert.IsNotNull(enableMethod);
		Assert.IsNotNull(disableMethod);
		Assert.IsNotNull(drawMethod);
	}

	[TestMethod]
	public void ActivityLog_LogHandler_IsNestedType()
	{
		var type = typeof(ActivityLog);
		var nestedTypes = type.GetNestedTypes();
		
		var logHandlerType = Array.Find(nestedTypes, t => t.Name == "LogHandler");
		
		Assert.IsNotNull(logHandlerType);
		Assert.IsTrue(logHandlerType.IsValueType);
	}

	[TestMethod]
	public void ActivityLog_LogEntry_IsPrivateNestedType()
	{
		var type = typeof(ActivityLog);
		var nestedTypes = type.GetNestedTypes(System.Reflection.BindingFlags.NonPublic);
		
		var logEntryType = Array.Find(nestedTypes, t => t.Name == "LogEntry");
		
		Assert.IsNotNull(logEntryType);
		Assert.IsTrue(logEntryType.IsValueType);
	}

	[TestMethod]
	public void ActivityLog_MaxEntries_Is8()
	{
		var type = typeof(ActivityLog);
		var maxEntriesField = type.GetField("MaxEntries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		
		Assert.IsNotNull(maxEntriesField);
		
		var value = maxEntriesField.GetValue(null);
		Assert.AreEqual(8, value);
	}

	[TestMethod]
	public void ActivityLog_InitialState_IsDisabled()
	{
		var type = typeof(ActivityLog);
		
		var isLoggingEnabledProp = type.GetProperty("IsLoggingEnabled");
		var targetAgentIdProp = type.GetProperty("TargetAgentId");
		
		Assert.IsNotNull(isLoggingEnabledProp);
		Assert.IsNotNull(targetAgentIdProp);
		
		// Static properties should return default values
		var isEnabled = (bool)isLoggingEnabledProp.GetValue(null)!;
		var targetId = (long)targetAgentIdProp.GetValue(null)!;
		
		Assert.IsFalse(isEnabled);
		Assert.AreEqual(-1L, targetId);
	}

	[TestMethod]
	public void ActivityLog_DrawMethod_HasCorrectParameters()
	{
		var type = typeof(ActivityLog);
		var drawMethod = type.GetMethod("Draw");
		
		Assert.IsNotNull(drawMethod);
		
		var parameters = drawMethod.GetParameters();
		Assert.AreEqual(3, parameters.Length);
		
		Assert.AreEqual("SpriteBatch", parameters[0].ParameterType.Name);
		Assert.AreEqual("SpriteFont", parameters[1].ParameterType.Name);
		Assert.AreEqual("GraphicsDevice", parameters[2].ParameterType.Name);
	}
}
