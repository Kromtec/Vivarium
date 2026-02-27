using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Config;

namespace Vivarium.Tests.Config;

[TestClass]
public class ConfigProviderTests
{
    [TestInitialize]
    public void Setup()
    {
        ConfigProvider.Reset();
    }

    [TestCleanup]
    public void Cleanup()
    {
        ConfigProvider.Reset();
    }

    [TestMethod]
    public void IsInitialized_ReturnsFalse_BeforeInitialization()
    {
        Assert.IsFalse(ConfigProvider.IsInitialized);
    }

    [TestMethod]
    public void Initialize_SetsConfiguration()
    {
        var config = SimulationConfig.CreateDefault();
        
        ConfigProvider.Initialize(config);
        
        Assert.IsTrue(ConfigProvider.IsInitialized);
    }

    [TestMethod]
    public void Initialize_Throws_WhenAlreadyInitialized()
    {
        var config = SimulationConfig.CreateDefault();
        
        ConfigProvider.Initialize(config);
        
        Assert.ThrowsException<InvalidOperationException>(() => ConfigProvider.Initialize(config));
    }

    [TestMethod]
    public void Initialize_Throws_WhenConfigIsNull()
    {
        Assert.ThrowsException<ArgumentNullException>(() => ConfigProvider.Initialize(null!));
    }

    [TestMethod]
    public void Current_Throws_WhenNotInitialized()
    {
        Assert.ThrowsException<InvalidOperationException>(() => { var _ = ConfigProvider.Current; });
    }

    [TestMethod]
    public void Current_ReturnsConfiguration_WhenInitialized()
    {
        var config = SimulationConfig.CreateDefault();
        ConfigProvider.Initialize(config);
        
        var result = ConfigProvider.Current;
        
        Assert.AreSame(config, result);
    }

    [TestMethod]
    public void Reset_ClearsConfiguration()
    {
        var config = SimulationConfig.CreateDefault();
        ConfigProvider.Initialize(config);
        
        ConfigProvider.Reset();
        
        Assert.IsFalse(ConfigProvider.IsInitialized);
    }

    [TestMethod]
    public void ConvenienceAccessors_ReturnConfigs_WhenInitialized()
    {
        var config = SimulationConfig.CreateDefault();
        ConfigProvider.Initialize(config);
        
        Assert.IsNotNull(ConfigProvider.World);
        Assert.IsNotNull(ConfigProvider.Agent);
        Assert.IsNotNull(ConfigProvider.Plant);
        Assert.IsNotNull(ConfigProvider.Brain);
        Assert.IsNotNull(ConfigProvider.Genetics);
    }

    [TestMethod]
    public void FramesPerSecond_ReturnsValue()
    {
        var config = SimulationConfig.CreateDefault();
        ConfigProvider.Initialize(config);
        
        Assert.AreEqual(60.0, ConfigProvider.FramesPerSecond);
    }
}
