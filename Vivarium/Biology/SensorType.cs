namespace Vivarium.Biology;

// --- 2. SENSORS (Inputs) ---
// Direct mapping since Sensors start at index 0

public enum SensorType
{
    LocationX,
    LocationY,
    Random,
    Energy,
    Age,
    Oscillator,
    AgentDensity,
    PlantDensity,
    StructureDensity,

    // Directional Sensors (4 * 3 = 12)
    AgentDensity_N, AgentDensity_E, AgentDensity_S, AgentDensity_W,
    PlantDensity_N, PlantDensity_E, PlantDensity_S, PlantDensity_W,
    StructureDensity_N, StructureDensity_E, StructureDensity_S, StructureDensity_W,

    // Traits (7)
    Strength,
    Bravery,
    MetabolicEfficiency,
    Perception,
    Speed,
    TrophicBias,
    Constitution
}