using System.ComponentModel.DataAnnotations;

namespace Vivarium.Config;

/// <summary>
/// Mutable brain/AI configuration. These values can be tweaked at runtime
/// to balance neural network behavior and instincts.
/// </summary>
public sealed class BrainAIConfig
{
    // --- Memory ---

    /// <summary>Decay factor for hidden neuron activations (0.0 = no memory, 1.0 = perfect memory).</summary>
    [Range(0.0f, 1.0f)]
    public float HiddenNeuronDecayFactor { get; set; } = 0.5f;

    // --- Perception ---

    /// <summary>Base radius for directional sensing.</summary>
    [Range(1, 10)]
    public int BasePerceptionRadius { get; set; } = 2;

    /// <summary>Extra radius gained from Perception trait at maximum.</summary>
    [Range(0, 10)]
    public int MaxExtraPerceptionRadius { get; set; } = 2;

    /// <summary>Local area scan radius for density sensors.</summary>
    [Range(1, 5)]
    public int LocalScanRadius { get; set; } = 2;

    // --- Instinct Biases ---

    /// <summary>Bias strength applied to dominant action during instinct activation.</summary>
    [Range(0.0f, 10.0f)]
    public float InstinctBiasStrength { get; set; } = 2.0f;

    /// <summary>Extra attack bias when carnivore is hunting (added on top of suppression recovery).</summary>
    [Range(0.0f, 10.0f)]
    public float HuntingAttackBias { get; set; } = 4.0f;

    // --- Instinct Thresholds ---

    /// <summary>Energy ratio below which feeding instinct activates (0.6 = 60% of max energy).</summary>
    [Range(0.0f, 1.0f)]
    public float FeedingInstinctThreshold { get; set; } = 0.6f;

    /// <summary>Energy ratio above which reproduction instinct activates (0.9 = 90% of max energy).</summary>
    [Range(0.0f, 1.0f)]
    public float ReproductionInstinctThreshold { get; set; } = 0.9f;

    /// <summary>Probability that omnivores choose plants over agents when hungry.</summary>
    [Range(0.0f, 1.0f)]
    public float OmnivorePlantPreference { get; set; } = 0.75f;

    // --- Action Thresholds ---

    /// <summary>Minimum activation for suicide action to trigger.</summary>
    [Range(0.0f, 1.0f)]
    public float SuicideActivationThreshold { get; set; } = 0.9f;

    /// <summary>Age multiplier for suicide eligibility (relative to MaturityAge).</summary>
    [Range(1.0f, 5.0f)]
    public float SuicideAgeMultiplier { get; set; } = 2.0f;

    /// <summary>
    /// Creates the default brain configuration with current hardcoded values.
    /// </summary>
    public static BrainAIConfig CreateDefault() => new();
}
