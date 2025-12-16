namespace Vivarium.Config;

/// <summary>
/// Mutable plant configuration. These values can be tweaked at runtime
/// to balance plant behavior without restarting the simulation.
/// </summary>
public sealed class PlantConfig
{
    /// <summary>Energy lost per frame when plant is mature (aging).</summary>
    public float ShrivelRate { get; set; } = 0.4f;

    /// <summary>Energy gained per frame from photosynthesis.</summary>
    public float PhotosynthesisRate { get; set; } = 0.50f;

    /// <summary>Frames until plant can reproduce (maturity age).</summary>
    public int MaturityAge { get; set; } = 60 * 10; // 10 seconds at 60 FPS

    /// <summary>Energy cost to reproduce.</summary>
    public float ReproductionCost { get; set; } = 20.0f;

    /// <summary>Minimum energy required to attempt reproduction.</summary>
    public float MinEnergyToReproduce { get; set; } = 30.0f;

    /// <summary>Maximum energy a plant can store.</summary>
    public float MaxEnergy { get; set; } = 100f;

    /// <summary>
    /// Creates the default plant configuration with current hardcoded values.
    /// </summary>
    public static PlantConfig CreateDefault() => new();
}
