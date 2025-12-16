namespace Vivarium.Config;

/// <summary>
/// Mutable agent configuration. These values can be tweaked at runtime
/// to balance agent behavior without restarting the simulation.
/// </summary>
public sealed class AgentConfig
{
    // --- Reproduction ---

    /// <summary>Percentage of MaxEnergy lost as overhead when reproducing (0.30 = 30%).</summary>
    public float ReproductionOverheadPct { get; set; } = 0.30f;

    /// <summary>Minimum energy buffer required to survive reproduction.</summary>
    public float MinEnergyBuffer { get; set; } = 5.0f;

    /// <summary>Frames until agent can reproduce after birth (maturity).</summary>
    public int MaturityAge { get; set; } = 60 * 10; // 10 seconds at 60 FPS

    /// <summary>Frames until next possible reproduction after breeding.</summary>
    public int ReproductionCooldownFrames { get; set; } = 600; // 10 seconds at 60 FPS

    /// <summary>Reproduction overhead multiplier for Herbivores.</summary>
    public float HerbivoreReproductionMultiplier { get; set; } = 0.4f;

    /// <summary>Reproduction overhead multiplier for Carnivores.</summary>
    public float CarnivoreReproductionMultiplier { get; set; } = 0.3f; // Reduced to help carnivores breed

    /// <summary>Reproduction overhead multiplier for Omnivores.</summary>
    public float OmnivoreReproductionMultiplier { get; set; } = 0.6f; // Middle ground

    // --- Movement ---

    /// <summary>Base threshold for movement action activation.</summary>
    public float BaseMovementThreshold { get; set; } = 0.1f;

    /// <summary>Base cooldown frames between movements.</summary>
    public int BaseMovementCooldown { get; set; } = 10;

    /// <summary>Base energy cost for orthogonal movement.</summary>
    public float MovementCost { get; set; } = 0.25f;

    /// <summary>Multiplier for diagonal movement cost (sqrt(2) ? 1.414).</summary>
    public float DiagonalMovementMultiplier { get; set; } = 1.414f;

    /// <summary>Multiplier for flee action energy cost.</summary>
    public float FleeMovementMultiplier { get; set; } = 2.0f;

    // --- Combat ---

    /// <summary>Base threshold for attack action activation.</summary>
    public float BaseAttackThreshold { get; set; } = 0.5f;

    /// <summary>Cooldown frames after attacking.</summary>
    public int AttackCooldownFrames { get; set; } = 60;

    // --- Metabolism ---

    /// <summary>Base energy lost per frame from metabolism.</summary>
    public float BaseMetabolismRate { get; set; } = 0.01f;

    /// <summary>Metabolism multiplier for Herbivores.</summary>
    public float HerbivoreMetabolismMultiplier { get; set; } = 0.70f;

    /// <summary>Metabolism multiplier for Carnivores.</summary>
    public float CarnivoreMetabolismMultiplier { get; set; } = 0.65f; // Reduced to help carnivores survive

    /// <summary>Metabolism multiplier for Omnivores.</summary>
    public float OmnivoreMetabolismMultiplier { get; set; } = 0.90f; // Balanced

    // --- Diet Classification ---

    /// <summary>TrophicBias threshold below which agent is classified as Carnivore.</summary>
    public float CarnivoreTrophicThreshold { get; set; } = -0.55f;

    /// <summary>TrophicBias threshold above which agent is classified as Herbivore.</summary>
    public float HerbivoreTrophicThreshold { get; set; } = 0.0f;

    // --- Trait Scaling ---

    /// <summary>Base max energy for agents (scaled by Constitution).</summary>
    public float BaseMaxEnergy { get; set; } = 100f;

    /// <summary>Constitution influence on max energy (0.5 = ±50% at extremes).</summary>
    public float ConstitutionEnergyScale { get; set; } = 0.5f;

    /// <summary>Strength influence on power (0.5 = ±50% at extremes).</summary>
    public float StrengthPowerScale { get; set; } = 0.5f;

    /// <summary>Resilience multiplier for Herbivores.</summary>
    public float HerbivoreResilienceMultiplier { get; set; } = 1.5f;

    /// <summary>
    /// Creates the default agent configuration with current hardcoded values.
    /// </summary>
    public static AgentConfig CreateDefault() => new();
}
