using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Vivarium.Entities;
using Vivarium.UI;

namespace Vivarium.Biology;

/// <summary>
/// Represents a family of genetically related organisms (cluster of similar genomes).
/// </summary>
public class GenomeFamily
{
    /// <summary>The seed variant that represents this family.</summary>
    public GenomeVariant Representative;
    /// <summary>All variants belonging to this family.</summary>
    public List<GenomeVariant> Variants = [];
    /// <summary>Total count of all agents in this family.</summary>
    public int TotalCount;
    /// <summary>Rank by population size (1 = most common).</summary>
    public int Rank;
    /// <summary>Diet type of this family.</summary>
    public DietType Diet;
    /// <summary>Visual identicon texture.</summary>
    public Texture2D Identicon;
    /// <summary>Agent texture for list view.</summary>
    public Texture2D AgentTexture;
    /// <summary>Scientific name (e.g., "Carnofortis vulgaris").</summary>
    public string ScientificName;
    /// <summary>Translation of the scientific name.</summary>
    public string ScientificNameTranslation;
}

/// <summary>
/// Represents a specific genome variant (exact genome hash match).
/// </summary>
public class GenomeVariant
{
    /// <summary>64-bit hash of the genome.</summary>
    public ulong Hash;
    /// <summary>Number of agents with this exact genome.</summary>
    public int Count;
    /// <summary>Example agent with this genome.</summary>
    public Agent Representative;
    /// <summary>Diet type.</summary>
    public DietType Diet;
    /// <summary>Visual identicon.</summary>
    public Texture2D Identicon;
    /// <summary>Grid visualization of the genome.</summary>
    public Texture2D GenomeGrid;
    /// <summary>Variant name (e.g., "var. alpha").</summary>
    public string VariantName;
}

/// <summary>
/// Analyzes the population to group agents into families based on genome similarity.
/// </summary>
public class GenomeCensus
{
    /// <summary>Top families sorted by population count.</summary>
    public List<GenomeFamily> TopFamilies { get; private set; } = [];

    public void AnalyzePopulation(Agent[] agents)
    {
        // 1. Group agents by exact genome hash (Variants)
        var variants = new Dictionary<ulong, GenomeVariant>();

        foreach (var agent in agents)
        {
            if (!agent.IsAlive) continue;

            ulong hash = Genetics.CalculateGenomeHash(agent.Genome);

            if (!variants.TryGetValue(hash, out GenomeVariant entry))
            {
                entry = new GenomeVariant
                {
                    Hash = hash,
                    Count = 0,
                    Representative = agent,
                    Diet = agent.Diet
                };
                variants[hash] = entry;
            }

            entry.Count++;
            variants[hash] = entry;
        }

        // Sort variants by Count (Most popular first)
        var sortedVariants = variants.Values
            .OrderByDescending(v => v.Count)
            .ToList();

        // 2. Cluster Variants into Families
        var families = new List<GenomeFamily>();
        var unassigned = new List<GenomeVariant>(sortedVariants);

        // Threshold: 90% similarity to be in the same family
        const float SimilarityThreshold = 0.90f;

        while (unassigned.Count > 0)
        {
            // Take the most popular remaining variant as the seed for a new family
            var seed = unassigned[0];
            unassigned.RemoveAt(0);

            var family = new GenomeFamily
            {
                Representative = seed, // The most popular variant represents the family
                Diet = seed.Diet
            };
            family.Variants.Add(seed);

            // Find all other variants that are similar to the seed
            for (int i = unassigned.Count - 1; i >= 0; i--)
            {
                var candidate = unassigned[i];

                // Optimization: Only check similarity if Diet matches (Diet is a hard filter usually)
                if (candidate.Diet == seed.Diet)
                {
                    float similarity = Genetics.CalculateSimilarity(seed.Representative.Genome, candidate.Representative.Genome);
                    if (similarity >= SimilarityThreshold)
                    {
                        family.Variants.Add(candidate);
                        unassigned.RemoveAt(i);
                    }
                }
            }

            // Calculate total count for the family
            family.TotalCount = family.Variants.Sum(v => v.Count);

            // Sort variants within family by count
            family.Variants = family.Variants.OrderByDescending(v => v.Count).ToList();

            // Generate Scientific Name for the Family
            var names = ScientificNameGenerator.GenerateFamilyName(family.Representative.Representative);
            family.ScientificName = names.ScientificName;
            family.ScientificNameTranslation = names.Translation;

            // Generate Variant Names
            for (int i = 0; i < family.Variants.Count; i++)
            {
                family.Variants[i].VariantName = ScientificNameGenerator.GenerateVariantName(i);
            }

            families.Add(family);
        }

        // 3. Sort Families by Total Count
        var allSortedFamilies = families
            .OrderByDescending(f => f.TotalCount)
            .ToList();

        // Assign Ranks
        for (int i = 0; i < allSortedFamilies.Count; i++)
        {
            allSortedFamilies[i].Rank = i + 1;
        }

        TopFamilies = allSortedFamilies;
    }

    public string GetVariantName(ulong genomeHash)
    {
        foreach (var family in TopFamilies)
        {
            foreach (var variant in family.Variants)
            {
                if (variant.Hash == genomeHash)
                {
                    return variant.VariantName;
                }
            }
        }
        return null;
    }
}
