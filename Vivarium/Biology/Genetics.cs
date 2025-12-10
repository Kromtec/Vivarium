using Microsoft.Xna.Framework;
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
        const int GenomeLength = 60;
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
    public static Color ComputePhenotypeColor(Gene[] genome)
    {
        float aggressionSum = 0f;
        float movementSum = 0f;
        float complexitySum = 0f;

        foreach (var gene in genome)
        {
            // Use absolute values so negative weights also contribute to visibility
            float weight = Math.Abs(gene.Weight);
            int sink = gene.SinkId;

            // 1. SUM UP WEIGHTS (Don't average yet)
            if (sink == BrainConfig.GetActionIndex(ActionType.Attack) ||
                sink == BrainConfig.GetActionIndex(ActionType.KillSelf))
            {
                aggressionSum += weight;
            }
            else if (sink == BrainConfig.GetActionIndex(ActionType.MoveNorth) ||
                     sink == BrainConfig.GetActionIndex(ActionType.MoveEast) ||
                     sink == BrainConfig.GetActionIndex(ActionType.MoveSouth) ||
                     sink == BrainConfig.GetActionIndex(ActionType.MoveWest))
            {
                movementSum += weight;
            }
            else
            {
                complexitySum += weight;
            }
        }

        // 2. VISUALIZATION LOGIC

        // A) Base Brightness (Body)
        // Even with 0 weights, the agent should be visible (Dark Grey = 0.25f).
        // Complexity adds to brightness up to a max of 0.6f.
        float baseBrightness = 0.25f + (MathF.Tanh(complexitySum * 0.5f) * 0.35f);

        // B) Tints (Red/Blue)
        // We use Tanh to boost small values (0.1 -> 0.1) but cap high values (10.0 -> 1.0).
        // We multiply by 2.0 inside Tanh to make the "Peaceful Start" genes more visible.
        float r = MathF.Tanh(aggressionSum * 2.0f);
        float b = MathF.Tanh(movementSum * 2.0f);

        // 3. CONTRAST ("Winner takes all")
        // If one trait dominates, suppress the other to prevent "Muddy Purple".
        if (r > b * 1.2f) b *= 0.5f;
        if (b > r * 1.2f) r *= 0.5f;

        // 4. COMPOSE FINAL COLOR
        // We add the tint to the base brightness.
        float finalR = Math.Clamp(baseBrightness + r, 0f, 1f);
        float finalB = Math.Clamp(baseBrightness + b, 0f, 1f);

        // Green is used to desaturate (whiten) highly complex or balanced agents.
        // It should never exceed R or B to avoid looking like a plant.
        float finalG = Math.Clamp(baseBrightness, 0f, 0.8f);

        // If both R and B are high (Hybrid), we boost G slightly to make it white/magenta
        if (r > 0.5f && b > 0.5f) finalG += 0.2f;

        return new Color(finalR, finalG, finalB, 1.0f);
    }
}