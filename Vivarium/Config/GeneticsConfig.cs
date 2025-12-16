namespace Vivarium.Config;

/// <summary>
/// Genetics configuration. GenomeLength and TraitGeneCount are immutable
/// as they affect array sizes. MutationRate can be tweaked at runtime.
/// </summary>
public sealed class GeneticsConfig
{
    // --- Immutable (affect genome array structure) ---

    /// <summary>Total number of genes in the genome.</summary>
    public required int GenomeLength { get; init; }

    /// <summary>Number of genes reserved for trait encoding.</summary>
    public required int TraitGeneCount { get; init; }

    // --- Mutable ---

    /// <summary>Probability that a single gene will mutate during reproduction.</summary>
    public double MutationRate { get; set; } = 0.001;

    /// <summary>Weight range for initial genome connections (±WeightRange).</summary>
    public float InitialWeightRange { get; set; } = 4.0f;

    /// <summary>Normalizer for trait extraction from gene weights.</summary>
    public float TraitNormalizer { get; set; } = 4.0f;

    /// <summary>
    /// Creates the default genetics configuration with current hardcoded values.
    /// </summary>
    public static GeneticsConfig CreateDefault() => new()
    {
        GenomeLength = 512,
        TraitGeneCount = 14
    };

    /// <summary>
    /// Calculated index where trait genes begin in the genome.
    /// </summary>
    public int TraitStartIndex => GenomeLength - TraitGeneCount;
}
