namespace Vivarium.Biology;

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
    // Trait sensors (derived from genome)
    Strength,
    Bravery,
    MetabolicEfficiency,
    Perception,
    Speed,
    TrophicBias,
}