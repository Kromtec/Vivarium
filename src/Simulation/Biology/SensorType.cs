namespace Vivarium.Biology;

/// <summary>
/// Sensor types that provide input to the neural network.
/// Sensors map directly to brain input neurons (index 0 onwards).
/// </summary>
public enum SensorType
{
    /// <summary>X coordinate of the agent's position.</summary>
    LocationX,
    /// <summary>Y coordinate of the agent's position.</summary>
    LocationY,
    /// <summary>Random noise input for exploration.</summary>
    Random,
    /// <summary>Current energy level of the agent.</summary>
    Energy,
    /// <summary>Age of the agent in simulation steps.</summary>
    Age,
    /// <summary>Oscillating signal based on simulation time.</summary>
    Oscillator,
    /// <summary>Density of agents in the vicinity.</summary>
    AgentDensity,
    /// <summary>Density of plants in the vicinity.</summary>
    PlantDensity,
    /// <summary>Density of structures in the vicinity.</summary>
    StructureDensity,

    // Directional Sensors (4 * 3 = 12)
    /// <summary>Agent density to the North.</summary>
    AgentDensity_N, 
    /// <summary>Agent density to the East.</summary>
    AgentDensity_E, 
    /// <summary>Agent density to the South.</summary>
    AgentDensity_S, 
    /// <summary>Agent density to the West.</summary>
    AgentDensity_W,
    /// <summary>Plant density to the North.</summary>
    PlantDensity_N, 
    /// <summary>Plant density to the East.</summary>
    PlantDensity_E, 
    /// <summary>Plant density to the South.</summary>
    PlantDensity_S, 
    /// <summary>Plant density to the West.</summary>
    PlantDensity_W,
    /// <summary>Structure density to the North.</summary>
    StructureDensity_N, 
    /// <summary>Structure density to the East.</summary>
    StructureDensity_E, 
    /// <summary>Structure density to the South.</summary>
    StructureDensity_S, 
    /// <summary>Structure density to the West.</summary>
    StructureDensity_W,

    // Traits (7)
    /// <summary>Physical strength trait.</summary>
    Strength,
    /// <summary>Courage trait.</summary>
    Bravery,
    /// <summary>Energy efficiency trait.</summary>
    MetabolicEfficiency,
    /// <summary>Perception trait.</summary>
    Perception,
    /// <summary>Movement speed trait.</summary>
    Speed,
    /// <summary>Trophic level preference (-1 = carnivore, +1 = herbivore).</summary>
    TrophicBias,
    /// <summary>Health and endurance trait.</summary>
    Constitution
}