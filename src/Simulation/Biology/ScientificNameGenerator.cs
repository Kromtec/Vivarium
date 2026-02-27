using System;
using System.Collections.Generic;
using System.Linq;
using Vivarium.Entities;
using Vivarium.UI;

namespace Vivarium.Biology;

/// <summary>
/// Generates scientific names for genome families and variants based on genetic traits.
/// </summary>
public static class ScientificNameGenerator
{
    private static readonly Dictionary<DietType, (string[] Roots, string Meaning)> DietPrefixes = new()
    {
        { DietType.Herbivore, (new[] { "Phyto", "Herbi", "Flori", "Viridi", "Folio" }, "Plant") },
        { DietType.Carnivore, (new[] { "Carno", "Sarco", "Besti", "Vena", "Cruento" }, "Flesh") },
        { DietType.Omnivore, (new[] { "Omni", "Pan", "Ambi", "Poly", "Vora" }, "All") }
    };

    // Trait mappings: (Positive Root, Negative Root, Positive Meaning, Negative Meaning)
    private static readonly Dictionary<string, (string[] Pos, string[] Neg, string PosMeaning, string NegMeaning)> TraitSuffixes = new()
    {
        { "Strength", (new[] { "fortis", "validus", "potens", "titanus", "robustus" }, new[] { "debilis", "tener", "pusillus", "gracilis" }, "Strong", "Weak") },
        { "Bravery", (new[] { "ferox", "audax", "bellus", "intrepidus" }, new[] { "timidus", "pavidus", "fugax", "cautus" }, "Brave", "Timid") },
        { "Metabolism", (new[] { "parcus", "durus", "efficiens" }, new[] { "vorax", "edax", "gulosus", "consumens" }, "Efficient", "Hungry") },
        { "Perception", (new[] { "sapiens", "vigil", "argus", "sensilis" }, new[] { "caecus", "obtusus", "ignarus" }, "Wise", "Blind") },
        { "Speed", (new[] { "celer", "velox", "rapidus", "agilis", "dromus" }, new[] { "tardus", "lentus", "segnis", "pigrus" }, "Fast", "Slow") },
        { "Constitution", (new[] { "aeternus", "solidus", "perennis" }, new[] { "fragilis", "caducus", "mortalis" }, "Enduring", "Fragile") }
    };

    private static readonly string[] GenericSpeciesNames = [
        "vulgaris", "communis", "simplex", "variabilis", "mirabilis", "notabilis", "modestus"
    ];

    /// <summary>
    /// Generates a scientific name for a genome family based on the agent's diet and traits.
    /// </summary>
    /// <param name="agent">The agent to generate a name for.</param>
    /// <returns>Tuple of (ScientificName, Translation).</returns>
    public static (string ScientificName, string Translation) GenerateFamilyName(Agent agent)
    {
        // Deterministic RNG based on genome hash
        ulong hash = Genetics.CalculateGenomeHash(agent.Genome);
        Random rng = new((int)(hash & 0xFFFFFFFF));

        // 1. Genus (Diet + Primary Trait)
        var dietData = DietPrefixes[agent.Diet];
        string prefix = dietData.Roots[rng.Next(dietData.Roots.Length)];
        string dietMeaning = dietData.Meaning;

        var (traitName, isPositive, intensity) = GetDominantTrait(agent, 0); // 0 = Most dominant

        string suffix = "morphus";
        string traitMeaning = "Form";

        if (TraitSuffixes.TryGetValue(traitName, out (string[] Pos, string[] Neg, string PosMeaning, string NegMeaning) traitDataPrimary))
        {
            var roots = isPositive ? traitDataPrimary.Pos : traitDataPrimary.Neg;
            suffix = roots[rng.Next(roots.Length)];
            traitMeaning = isPositive ? traitDataPrimary.PosMeaning : traitDataPrimary.NegMeaning;
        }

        // Combine to form Genus (e.g., Carnovelox)
        string genus = prefix + suffix.ToLower();

        // 2. Species (Secondary Trait)
        string species;
        string speciesMeaning = "Common";

        var (secTraitName, secIsPositive, secIntensity) = GetDominantTrait(agent, 1); // 1 = Second most dominant

        if (secIntensity > 0.1f && TraitSuffixes.TryGetValue(secTraitName, out (string[] Pos, string[] Neg, string PosMeaning, string NegMeaning) traitDataSecondary))
        {
            var roots = secIsPositive ? traitDataSecondary.Pos : traitDataSecondary.Neg;
            species = roots[rng.Next(roots.Length)];
            speciesMeaning = secIsPositive ? traitDataSecondary.PosMeaning : traitDataSecondary.NegMeaning;
        }
        else
        {
            species = GenericSpeciesNames[rng.Next(GenericSpeciesNames.Length)];
        }

        string scientificName = $"{genus} {species}";
        string translation = $"The {speciesMeaning} {traitMeaning} {dietMeaning}-Eater";

        return (scientificName, translation);
    }

    /// <summary>
    /// Generates a variant name using Greek letters for first 15 variants, then numeric.
    /// </summary>
    /// <param name="variantIndex">The variant index (0-based).</param>
    /// <returns>Variant name (e.g., "var. alpha", "var. 16").</returns>
    public static string GenerateVariantName(int variantIndex)
    {
        string[] greekLetters = [
            "alpha", "beta", "gamma", "delta", "epsilon",
            "zeta", "eta", "theta", "iota", "kappa",
            "lambda", "mu", "nu", "xi", "omicron"
        ];

        if (variantIndex < greekLetters.Length)
        {
            return $"var. {greekLetters[variantIndex]}";
        }
        return $"var. {variantIndex + 1}";
    }

    private static (string name, bool positive, float intensity) GetDominantTrait(Agent agent, int rank)
    {
        var traits = new List<(string Name, float Value)>
        {
            ("Strength", agent.Strength),
            ("Bravery", agent.Bravery),
            ("Metabolism", agent.MetabolicEfficiency),
            ("Perception", agent.Perception),
            ("Speed", agent.Speed),
            ("Constitution", agent.Constitution)
        };

        // Sort by absolute intensity
        traits.Sort((a, b) => Math.Abs(b.Value).CompareTo(Math.Abs(a.Value)));

        if (rank >= traits.Count) rank = traits.Count - 1;

        var selected = traits[rank];
        return (selected.Name, selected.Value > 0, Math.Abs(selected.Value));
    }
}
