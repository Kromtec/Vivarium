using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.Config;
using System;
using System.IO;

namespace Vivarium.Tests.Config;

[TestClass]
public class SimulationConfigTests
{
    [TestMethod]
    public void CreateDefault_ReturnsValidConfig()
    {
        var config = SimulationConfig.CreateDefault();
        
        Assert.IsNotNull(config.World);
        Assert.IsNotNull(config.Agent);
        Assert.IsNotNull(config.Plant);
        Assert.IsNotNull(config.Brain);
        Assert.IsNotNull(config.Genetics);
        Assert.AreEqual(60.0, config.FramesPerSecond);
    }

    [TestMethod]
    public void CreateDefault_WithSeed_SetsSeed()
    {
        var config = SimulationConfig.CreateDefault(12345);
        
        Assert.IsNotNull(config.World);
    }

    [TestMethod]
    public void SaveToFile_CreatesFile()
    {
        var config = SimulationConfig.CreateDefault();
        string tempPath = Path.GetTempFileName();
        
        try
        {
            config.SaveToFile(tempPath);
            
            Assert.IsTrue(File.Exists(tempPath));
            
            string content = File.ReadAllText(tempPath);
            Assert.IsTrue(content.Contains("\"world\""));
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [TestMethod]
    public void LoadFromFile_RoundTrips()
    {
        var original = SimulationConfig.CreateDefault(42);
        string tempPath = Path.GetTempFileName();
        
        try
        {
            original.SaveToFile(tempPath);
            
            var loaded = SimulationConfig.LoadFromFile(tempPath);
            
            Assert.IsNotNull(loaded.World);
            Assert.IsNotNull(loaded.Agent);
            Assert.IsNotNull(loaded.Plant);
            Assert.IsNotNull(loaded.Brain);
            Assert.IsNotNull(loaded.Genetics);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [TestMethod]
    public void LoadFromFile_Throws_WhenFileNotFound()
    {
        Assert.ThrowsException<FileNotFoundException>(() => 
            SimulationConfig.LoadFromFile("/nonexistent/path/config.json"));
    }

    [TestMethod]
    public void LoadFromFileOrDefault_ReturnsDefault_WhenFileNotFound()
    {
        var result = SimulationConfig.LoadFromFileOrDefault("/nonexistent/path/config.json");
        
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void LoadFromFileOrDefault_ReturnsLoaded_WhenFileExists()
    {
        var original = SimulationConfig.CreateDefault(999);
        string tempPath = Path.GetTempFileName();
        
        try
        {
            original.SaveToFile(tempPath);
            
            var result = SimulationConfig.LoadFromFileOrDefault(tempPath);
            
            Assert.IsNotNull(result);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}
