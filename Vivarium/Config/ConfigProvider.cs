using System;

namespace Vivarium.Config;

/// <summary>
/// Provides global access to the current simulation configuration.
/// The configuration is set once at startup and the mutable sections
/// can be modified at runtime for live balancing.
/// </summary>
public static class ConfigProvider
{
    private static SimulationConfig _current;

    /// <summary>
    /// Gets the current simulation configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">If configuration has not been initialized.</exception>
    public static SimulationConfig Current => _current
        ?? throw new InvalidOperationException("ConfigProvider has not been initialized. Call Initialize() first.");

    /// <summary>
    /// Returns true if the configuration has been initialized.
    /// </summary>
    public static bool IsInitialized => _current != null;

    /// <summary>
    /// Initializes the global configuration. Should be called once at application startup.
    /// </summary>
    /// <param name="config">The configuration to use.</param>
    /// <exception cref="InvalidOperationException">If already initialized.</exception>
    public static void Initialize(SimulationConfig config)
    {
        if (_current != null)
        {
            throw new InvalidOperationException("ConfigProvider has already been initialized.");
        }
        _current = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Resets the configuration (primarily for testing purposes).
    /// </summary>
    public static void Reset()
    {
        _current = null;
    }

    // --- Convenience Accessors ---

    /// <summary>Quick access to World config.</summary>
    public static WorldConfig World => Current.World;

    /// <summary>Quick access to Agent config.</summary>
    public static AgentConfig Agent => Current.Agent;

    /// <summary>Quick access to Plant config.</summary>
    public static PlantConfig Plant => Current.Plant;

    /// <summary>Quick access to Brain config.</summary>
    public static BrainAIConfig Brain => Current.Brain;

    /// <summary>Quick access to Genetics config.</summary>
    public static GeneticsConfig Genetics => Current.Genetics;

    /// <summary>Quick access to FramesPerSecond.</summary>
    public static double FramesPerSecond => Current.FramesPerSecond;
}
