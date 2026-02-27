using System;
using Vivarium.Config;
using Vivarium.Entities;

namespace Vivarium.Biology;

/// <summary>
/// Provides genetic operations for the simulation including genome creation, mutation, replication, and analysis.
/// </summary>
public static partial class Genetics
{
    /// <summary>Mutation rate from configuration (probability of each gene mutating).</summary>
    public static double MutationRate => ConfigProvider.Genetics.MutationRate;
    /// <summary>Length of the genome array from configuration.</summary>
    public static int GenomeLength => ConfigProvider.Genetics.GenomeLength;
    /// <summary>Number of genes reserved for traits.</summary>
    public static int TraitGeneCount => ConfigProvider.Genetics.TraitGeneCount;
    /// <summary>Starting index of trait genes in the genome array.</summary>
    public static int TraitStartIndex => ConfigProvider.Genetics.TraitStartIndex;

    /// <summary>
    /// Applies mutations to an agent's genome in place.
    /// Each gene has a probability of mutation based on MutationRate.
    /// </summary>
    /// <param name="genome">The genome array to mutate.</param>
    /// <param name="rng">Random number generator.</param>
    public static void Mutate(ref Gene[] genome, Random rng)
    {
        double mutationRate = MutationRate;

        // Use Span for performance when iterating the genome array
        Span<Gene> genomeSpan = genome.AsSpan();

        for (int i = 0; i < genomeSpan.Length; i++)
        {
            // Check if this specific gene should mutate
            if (rng.NextDouble() < mutationRate)
            {
                // 1. Pick a random bit to flip (0 to 31)
                int bitIndex = rng.Next(0, 32);

                // 2. Create a bitmask (e.g., 0000...00100...0000)
                // We use '1u' to ensure it's treated as an unsigned int
                uint mask = 1u << bitIndex;

                // 3. Apply XOR to flip the bit
                // Since Gene struct is immutable/readonly, we create a new one.
                // We read the old DNA, flip the bit, and store the new Gene.
                genomeSpan[i] = new Gene(genomeSpan[i].Dna ^ mask);
            }
        }
    }

    /// <summary>
    /// Creates a deep copy of an agent (Replication).
    /// Used when a parent spawns a child.
    /// </summary>
    /// <param name="parent">The parent agent.</param>
    /// <param name="index">Index for the new agent.</param>
    /// <param name="x">X position for the new agent.</param>
    /// <param name="y">Y position for the new agent.</param>
    /// <param name="rng">Random number generator.</param>
    /// <param name="initialEnergy">Initial energy for the child.</param>
    /// <returns>A new Agent representing the child.</returns>
    public static Agent Replicate(ref Agent parent, int index, int x, int y, Random rng, float initialEnergy)
    {
        // Copy Genome (Crucial: Arrays are reference types, so we need a manual copy)
        // In .NET 10, we can use Array.Clone() or copy manually.
        var genomeCopy = new Gene[parent.Genome.Length];
        Array.Copy(parent.Genome, genomeCopy, parent.Genome.Length);

        // Create new agent structure
        return Agent.CreateChild(index, x, y, rng, genomeCopy, ref parent, initialEnergy);
    }

    /// <summary>
    /// Creates a new genome with random neural network connections.
    /// </summary>
    /// <param name="rng">Random number generator.</param>
    /// <returns>A new gene array representing the genome.</returns>
    public static Gene[] CreateGenome(Random rng)
    {
        int genomeLength = GenomeLength;
        float weightRange = ConfigProvider.Genetics.InitialWeightRange;

        var initialGenome = new Gene[genomeLength];

        for (int g = 0; g < genomeLength; g++)
        {
            // 1. Random Topology (Any Sensor -> Any Neuron)
            int source = rng.Next(BrainConfig.NeuronCount);
            int sink = rng.Next(BrainConfig.NeuronCount);

            // 2. "Evenly Distributed" Weights
            float weight = (float)((rng.NextDouble() * weightRange * 2) - weightRange);

            initialGenome[g] = Gene.CreateConnection(source, sink, weight);
        }

        return initialGenome;
    }

    /// <summary>
    /// Extracts a single trait value from the genome using the reserved trait gene region.
    /// TraitIndex 0 corresponds to the last pair of genes, 1 to the previous pair, etc.
    /// Returns a normalized float in approx range [-1, +1].
    /// </summary>
    /// <param name="genome">The genome to extract from.</param>
    /// <param name="trait">The trait type to extract.</param>
    /// <returns>A normalized trait value between -1 and 1.</returns>
    public static float ExtractTrait(Gene[] genome, TraitType trait)
    {
        if (genome == null || genome.Length == 0)
            return 0f;

        int traitGeneCount = TraitGeneCount;
        int traitStartIndex = TraitStartIndex;
        float normalizer = ConfigProvider.Genetics.TraitNormalizer;

        int pairs = traitGeneCount / 2;
        int traitIndex = (int)trait;

        // Clamp traitIndex
        traitIndex = Math.Clamp(traitIndex, 0, pairs - 1);

        // Map traitIndex 0 -> last pair. Compute base index accordingly.
        int baseIdx = traitStartIndex + ((pairs - 1 - traitIndex) * 2);

        int idxA = Math.Clamp(baseIdx, 0, genome.Length - 1);
        int idxB = Math.Clamp(baseIdx + 1, 0, genome.Length - 1);

        float a = genome[idxA].Weight;
        float b = genome[idxB].Weight;

        float avg = (a + b) * 0.5f;

        return Math.Clamp(avg / normalizer, -1f, 1f);
    }

    /// <summary>
    /// Calculates the similarity between two genomes.
    /// Returns a value between 0.0 (completely different) and 1.0 (identical).
    /// </summary>
    /// <param name="a">First genome.</param>
    /// <param name="b">Second genome.</param>
    /// <returns>Similarity value between 0.0 and 1.0.</returns>
    public static float CalculateSimilarity(Gene[] a, Gene[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return 0f;

        int matches = 0;
        for (int i = 0; i < a.Length; i++)
        {
            // Exact DNA match
            if (a[i].Dna == b[i].Dna)
            {
                matches++;
            }
        }

        return (float)matches / a.Length;
    }

    /// <summary>
    /// Calculates a 64-bit hash of the genome.
    /// Uses FNV-1a algorithm for consistent hashing.
    /// </summary>
    /// <param name="genome">The genome to hash.</param>
    /// <returns>A 64-bit hash value.</returns>
    public static ulong CalculateGenomeHash(Gene[] genome)
    {
        const ulong fnvPrime = 1099511628211;
        const ulong offsetBasis = 14695981039346656037;

        ulong hash = offsetBasis;

        // We iterate over the raw DNA values
        for (int i = 0; i < genome.Length; i++)
        {
            uint dna = genome[i].Dna;

            // Process 4 bytes of the uint
            hash ^= (dna & 0xFF);
            hash *= fnvPrime;

            hash ^= ((dna >> 8) & 0xFF);
            hash *= fnvPrime;

            hash ^= ((dna >> 16) & 0xFF);
            hash *= fnvPrime;

            hash ^= ((dna >> 24) & 0xFF);
            hash *= fnvPrime;
        }

        return hash;
    }
}