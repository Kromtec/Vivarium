namespace Vivarium.Config;

/// <summary>
/// Mutable brain/AI configuration. These values can be tweaked at runtime
/// to balance neural network behavior and instincts.
/// </summary>
public sealed class BrainAIConfig
{
    // --- Memory ---

    /// <summary>Decay factor for hidden neuron activations (0.0 = no memory, 1.0 = perfect memory).</summary>
    public float HiddenNeuronDecayFactor { get; set; } = 0.5f;

    // --- Perception ---

    /// <summary>Base radius for directional sensing.</summary>
    public int BasePerceptionRadius { get; set; } = 2;

    /// <summary>Extra radius gained from Perception trait at maximum.</summary>
    public int MaxExtraPerceptionRadius { get; set; } = 2;

    /// <summary>Local area scan radius for density sensors.</summary>
    public int LocalScanRadius { get; set; } = 2;

    // --- Instinct Biases ---

    /// <summary>Bias strength applied to dominant action during instinct activation.</summary>
    public float InstinctBiasStrength { get; set; } = 2.0f;

    /// <summary>Extra attack bias when carnivore is hunting (added on top of suppression recovery).</summary>
    public float HuntingAttackBias { get; set; } = 4.0f;

    // --- Instinct Thresholds ---

    /// <summary>Energy ratio below which feeding instinct activates (0.6 = 60% of max energy).</summary>
    public float FeedingInstinctThreshold { get; set; } = 0.6f;

    /// <summary>Energy ratio above which reproduction instinct activates (0.9 = 90% of max energy).</summary>
    public float ReproductionInstinctThreshold { get; set; } = 0.9f;

    /// <summary>Probability that omnivores choose plants over agents when hungry.</summary>
    public float OmnivorePlantPreference { get; set; } = 0.75f;

    // --- Action Thresholds ---

    /// <summary>Minimum activation for suicide action to trigger.</summary>
    public float SuicideActivationThreshold { get; set; } = 0.9f;

    /// <summary>Age multiplier for suicide eligibility (relative to MaturityAge).</summary>
    public float SuicideAgeMultiplier { get; set; } = 2.0f;

    /// <summary>
    /// Creates the default brain configuration with current hardcoded values.
    /// </summary>
    public static BrainAIConfig CreateDefault() => new();
}
