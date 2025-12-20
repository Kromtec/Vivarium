using System.ComponentModel.DataAnnotations;

namespace Vivarium.Config;

/// <summary>
/// Mutable plant configuration. These values can be tweaked at runtime
/// to balance plant behavior without restarting the simulation.
/// </summary>
public sealed class PlantConfig
{
    /// <summary>Energy lost per frame when plant is mature (aging).</summary>
    [Range(0.0f, 2.0f)]
    public float ShrivelRate { get; set; } = 0.4f;

    /// <summary>Energy gained per frame from photosynthesis.</summary>
    [Range(0.0f, 2.0f)]
    public float PhotosynthesisRate { get; set; } = 0.50f;

    /// <summary>Frames until plant can reproduce (maturity age).</summary>
    [Range(60, 3600)]
    public int MaturityAge { get; set; } = 60 * 10; // 10 seconds at 60 FPS

    /// <summary>Energy cost to reproduce.</summary>
    [Range(0.0f, 100.0f)]
    public float ReproductionCost { get; set; } = 20.0f;

    /// <summary>Minimum energy required to attempt reproduction.</summary>
    [Range(0.0f, 100.0f)]
    public float MinEnergyToReproduce { get; set; } = 30.0f;

    /// <summary>Maximum energy a plant can store.</summary>
    [Range(10.0f, 500.0f)]
    public float MaxEnergy { get; set; } = 100f;

    /// <summary>
    /// Creates the default plant configuration with current hardcoded values.
    /// </summary>
    public static PlantConfig CreateDefault() => new();
}
