using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vivarium.Config;

/// <summary>
/// Root configuration container for the entire simulation.
/// Combines immutable world settings with mutable balance parameters.
/// </summary>
public sealed class SimulationConfig
{
    /// <summary>Immutable world configuration (grid size, pool sizes, seed).</summary>
    public required WorldConfig World { get; init; }

    /// <summary>Mutable agent balance parameters.</summary>
    public AgentConfig Agent { get; set; } = AgentConfig.CreateDefault();

    /// <summary>Mutable plant balance parameters.</summary>
    public PlantConfig Plant { get; set; } = PlantConfig.CreateDefault();

    /// <summary>Mutable brain/AI balance parameters.</summary>
    public BrainAIConfig Brain { get; set; } = BrainAIConfig.CreateDefault();

    /// <summary>Genetics configuration (partially immutable).</summary>
    public required GeneticsConfig Genetics { get; init; }

    /// <summary>Target frames per second for simulation timing.</summary>
    public double FramesPerSecond { get; init; } = 60.0;

    /// <summary>
    /// Creates the default simulation configuration with all current hardcoded values.
    /// </summary>
    /// <param name="seed">Optional random seed (defaults to 64).</param>
    public static SimulationConfig CreateDefault(int? seed = null) => new()
    {
        World = WorldConfig.CreateDefault(seed),
        Agent = AgentConfig.CreateDefault(),
        Plant = PlantConfig.CreateDefault(),
        Brain = BrainAIConfig.CreateDefault(),
        Genetics = GeneticsConfig.CreateDefault(),
        FramesPerSecond = 60.0
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Saves the configuration to a JSON file.
    /// </summary>
    public void SaveToFile(string filePath)
    {
        string json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Loads a configuration from a JSON file.
    /// </summary>
    /// <exception cref="FileNotFoundException">If the file does not exist.</exception>
    /// <exception cref="JsonException">If the JSON is invalid.</exception>
    public static SimulationConfig LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}");
        }

        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<SimulationConfig>(json, JsonOptions)
            ?? throw new JsonException("Failed to deserialize configuration");
    }

    /// <summary>
    /// Tries to load a configuration from a JSON file, returning default if it fails.
    /// </summary>
    public static SimulationConfig LoadFromFileOrDefault(string filePath, int? seed = null)
    {
        try
        {
            if (File.Exists(filePath))
            {
                return LoadFromFile(filePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load config from {filePath}: {ex.Message}");
            Console.WriteLine("Using default configuration.");
        }

        return CreateDefault(seed);
    }
}
