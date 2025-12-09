using Microsoft.Xna.Framework;
using System;
using Vivarium.Entities;

namespace Vivarium.Biology;

public static class Genetics
{
    // The probability that a single gene will mutate.
    private const double MutationRate = 0.001;

    /// <summary>
    /// Applies mutations to an agent's genome in place.
    /// </summary>
    public static void Mutate(ref Gene[] genome, Random rng)
    {
        // Use Span for performance when iterating the genome array
        Span<Gene> genomeSpan = genome.AsSpan();

        for (int i = 0; i < genomeSpan.Length; i++)
        {
            // Check if this specific gene should mutate
            if (rng.NextDouble() < MutationRate)
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
    public static Agent Replicate(ref Agent parent, int index, int x, int y, Random rng)
    {
        // Copy Genome (Crucial: Arrays are reference types, so we need a manual copy)
        // In .NET 10, we can use Array.Clone() or copy manually.
        var genomeCopy = new Gene[parent.Genome.Length];
        Array.Copy(parent.Genome, genomeCopy, parent.Genome.Length);

        // Create new agent structure
        return Agent.CreateChild(index, x, y, rng, genomeCopy, ref parent);
    }

    /// <summary>
    /// Creates a new genome consisting of randomly generated genes using the specified random number generator.
    /// </summary>
    /// <remarks>Each gene in the returned genome is initialized with a random 32-bit value. The method always
    /// returns an array of length 24.</remarks>
    /// <param name="rng">The random number generator used to produce gene values. Cannot be null.</param>
    /// <returns>An array of 24 randomly generated genes representing a new genome.</returns>
    public static Gene[] CreateGenome(Random rng)
    {
        // Define how many genes/connections each brain has
        const int GenomeLength = 24;

        var initialGenome = new Gene[GenomeLength];
        for (int g = 0; g < GenomeLength; g++)
        {
            // Create a completely random gene (random input, random output, random weight)
            // We use (uint)_rng.Next() to get a full 32-bit random number.
            // However, Random.Next() only returns non-negative Int32 (31 bits).
            // Better full 32-bit random:
            uint randomDna = (uint)(rng.Next(1 << 30)) << 2 | (uint)(rng.Next(1 << 2));

            initialGenome[g] = new Gene(randomDna);
        }

        return initialGenome;
    }

    public static Color ComputePhenotypeColor(Gene[] genome)
    {
        float aggressionScore = 0f;
        float movementScore = 0f;
        float complexityScore = 0f; // Represents internal wiring/intelligence

        foreach (var gene in genome)
        {
            float power = Math.Abs(gene.Weight);
            int sink = gene.SinkId;

            // 1. CALCULATE SCORES
            if (sink == BrainConfig.GetActionIndex(ActionType.Attack) ||
                sink == BrainConfig.GetActionIndex(ActionType.KillSelf))
            {
                aggressionScore += power;
            }
            else if (sink == BrainConfig.GetActionIndex(ActionType.MoveNorth) ||
                     sink == BrainConfig.GetActionIndex(ActionType.MoveWest) ||
                     sink == BrainConfig.GetActionIndex(ActionType.MoveSouth) ||
                     sink == BrainConfig.GetActionIndex(ActionType.MoveEast))
            {
                movementScore += power;
            }
            else
            {
                // Internal connections contribute to "complexity" but NOT color tint
                complexityScore += power;
            }
        }

        // 2. NORMALIZE SCORES (Average per gene to be independent of genome size)
        // We use a safe divisor to avoid /0
        float count = Math.Max(1, genome.Length);
        aggressionScore /= count;
        movementScore /= count;
        complexityScore /= count;

        // 3. DETERMINE COLOR CHANNELS
        // We amplify the scores to make them visible (genes often have small weights)
        float r = aggressionScore * 5.0f;
        float b = movementScore * 5.0f;

        // Base brightness from complexity (Grey/White)
        // This ensures agents are visible even if they don't move or attack much.
        float baseGrey = Math.Min(complexityScore * 0.5f, 0.3f);

        // 4. APPLY "WINNER TAKES ALL" CONTRAST
        // If one trait is dominant, we suppress the other color to avoid Purple.
        if (r > b)
        {
            b *= 0.3f; // Suppress Blue if Aggressive
        }
        else if (b > r)
        {
            r *= 0.3f; // Suppress Red if Mover
        }

        // 5. FINAL ASSEMBLY
        // We add the baseGrey to all channels so "neutral" agents look Grey/White, not black.
        float finalR = Math.Clamp(r + baseGrey, 0f, 1f);
        float finalB = Math.Clamp(b + baseGrey, 0f, 1f);

        // Green channel is used ONLY for desaturation (making it whiter/greyer),
        // but kept low to avoid looking like a plant.
        float finalG = Math.Clamp(baseGrey, 0f, 0.5f);

        return new Color(finalR, finalG, finalB, 1.0f);
    }
}