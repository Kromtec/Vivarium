using System;
using Vivarium.Entities;

namespace Vivarium.Biology;

public static class Genetics
{
    // The probability that a single gene will mutate.
    private const double MutationRate = 0.001d;

    public const int DietGeneIndex = 0;

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
    public static Agent Replicate(ref Agent parent, int index, int x, int y, Random rng, float initialEnergy)
    {
        // Copy Genome (Crucial: Arrays are reference types, so we need a manual copy)
        // In .NET 10, we can use Array.Clone() or copy manually.
        var genomeCopy = new Gene[parent.Genome.Length];
        Array.Copy(parent.Genome, genomeCopy, parent.Genome.Length);

        // Create new agent structure
        return Agent.CreateChild(index, x, y, rng, genomeCopy, ref parent, initialEnergy);
    }

    public static Gene[] CreateGenome(Random rng)
    {
        const int GenomeLength = 64;
        var initialGenome = new Gene[GenomeLength];

        for (int g = 0; g < GenomeLength; g++)
        {
            // 1. Random Topology (Any Sensor -> Any Neuron)
            int source = rng.Next(BrainConfig.NeuronCount);
            int sink = rng.Next(BrainConfig.NeuronCount);

            // 2. "Peaceful Start" Weights
            float weight = (float)(rng.NextDouble() * 2.0 - 1.0);

            initialGenome[g] = Gene.CreateConnection(source, sink, weight);
        }

        return initialGenome;
    }
}