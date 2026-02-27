namespace Vivarium.Biology;

/// <summary>
/// Represents the diet type of an agent in the simulation.
/// </summary>
public enum DietType
{
    /// <summary>Feeds on other agents (meat).</summary>
    Carnivore, // (-1.0 to -0.3)
    /// <summary>Can feed on both plants and other agents.</summary>
    Omnivore,  // (-0.3 to +0.3)
    /// <summary>Feeds on plants only.</summary>
    Herbivore  // (+0.3 to +1.0)
}