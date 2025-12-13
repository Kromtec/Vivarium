using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Vivarium.Biology;
using Vivarium.Entities;

namespace Vivarium.UI;

public static class GenomeHelper
{
    /// <summary>
    /// Calculates a 64-bit hash of the genome.
    /// Uses FNV-1a algorithm.
    /// </summary>
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

    /// <summary>
    /// Generates a horizontal double helix texture representing the agent's genome.
    /// </summary>
    public static Texture2D GenerateHelixTexture(GraphicsDevice graphics, Agent agent)
    {
        int width = 256; // Increased resolution
        int height = 128;
        var texture = new Texture2D(graphics, width, height);
        var colors = new Color[width * height];

        // Clear to transparent
        Array.Fill(colors, Color.Transparent);

        Color strandColor = Agent.GetColorBasedOnDietType(agent.Diet);

        // Traits to visualize as bonds
        // Added TrophicBias back to have 7 traits for the new layout
        float[] traits = new float[] {
            agent.Strength,
            agent.Speed,
            agent.Perception,
            agent.MetabolicEfficiency,
            agent.Bravery,
            agent.Constitution,
            agent.TrophicBias
        };

        float amplitude = (height / 2f) - 16; // More padding
        float centerY = height / 2f;
        
        // Range: PI/2 to 5PI/2 (2 PI total width)
        // This gives: Open Start -> Twist -> Bubble -> Twist -> Open End
        float totalPhase = 2f * MathF.PI;
        float frequency = totalPhase / width; 
        float phaseShift = MathF.PI / 2;

        // 1. Draw Bonds (Rungs)
        // 2 in Start, 3 in Middle, 2 in End
        float[] bondPhases = new float[] {
            // Start (Open)
            0.66f * MathF.PI,
            0.83f * MathF.PI,
            // Middle (Bubble)
            1.25f * MathF.PI,
            1.50f * MathF.PI,
            1.75f * MathF.PI,
            // End (Open)
            2.16f * MathF.PI,
            2.33f * MathF.PI
        };

        for (int i = 0; i < traits.Length; i++)
        {
            float targetPhase = bondPhases[i];
            int x = (int)((targetPhase - phaseShift) / frequency);
            
            if (x < 0 || x >= width) continue;

            float val = traits[i];
            Color bondColor = Color.LightGray;
            if (val > 0.3f) bondColor = Color.LimeGreen;
            else if (val < -0.3f) bondColor = Color.Red;

            float angle = (x * frequency) + phaseShift;
            float y1 = centerY + MathF.Sin(angle) * amplitude;
            float y2 = centerY + MathF.Sin(angle + MathF.PI) * amplitude;

            // We attach to Strand 1 (y1)
            // Calculate end point (65% of the way to y2) to create depth/gap
            float bondLengthPct = 0.65f;
            float yEnd = y1 + (y2 - y1) * bondLengthPct;

            int startY = (int)Math.Min(y1, yEnd);
            int endY = (int)Math.Max(y1, yEnd);

            startY = Math.Clamp(startY, 0, height - 1);
            endY = Math.Clamp(endY, 0, height - 1);

            // Draw thicker bond (6px width)
            int bondWidth = 6;
            for (int bx = x - bondWidth/2; bx <= x + bondWidth/2; bx++)
            {
                if (bx < 0 || bx >= width) continue;
                for (int y = startY; y <= endY; y++)
                {
                    colors[y * width + bx] = bondColor;
                }
            }
        }

        // 2. Draw Strands (Sine Waves) - Thicker
        int thicknessRadius = 3; // ~7px thick
        for (int x = 0; x < width; x++)
        {
            float angle = (x * frequency) + phaseShift;

            // Strand 1
            float y1 = centerY + MathF.Sin(angle) * amplitude;
            DrawCircle(colors, width, height, x, (int)y1, strandColor, thicknessRadius);

            // Strand 2
            float y2 = centerY + MathF.Sin(angle + MathF.PI) * amplitude;
            DrawCircle(colors, width, height, x, (int)y2, strandColor, thicknessRadius);
        }

        texture.SetData(colors);
        return texture;
    }

    private static void DrawCircle(Color[] colors, int w, int h, int cx, int cy, Color c, int radius)
    {
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx*dx + dy*dy <= radius*radius)
                {
                    int x = cx + dx;
                    int y = cy + dy;
                    if (x >= 0 && x < w && y >= 0 && y < h)
                    {
                        colors[y * w + x] = c;
                    }
                }
            }
        }
    }
}
