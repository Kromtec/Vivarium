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
            agent.Bravery,
            agent.MetabolicEfficiency,
            agent.Perception,
            agent.Speed,
            agent.TrophicBias,
            agent.Constitution
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
            
            // Desaturated colors
            Color bondColor = new Color(200, 200, 200);
            if (val > 0.3f) bondColor = new Color(110, 190, 110); // Muted Green
            else if (val < -0.3f) bondColor = new Color(210, 90, 90); // Muted Red

            float angle = (x * frequency) + phaseShift;
            float y1 = centerY + MathF.Sin(angle) * amplitude;
            float y2 = centerY + MathF.Sin(angle + MathF.PI) * amplitude;

            // We attach to Strand 1 (y1)
            // Consistent gap calculation
            float gapSize = 14f; // Fixed gap in pixels
            float dist = Math.Abs(y2 - y1);
            
            // Ensure we have enough space
            if (dist <= gapSize) continue;

            float dir = Math.Sign(y2 - y1);
            // End point is gapSize away from the opposing strand
            float yEnd = y2 - (dir * gapSize);

            // Draw rounded bond (Capsule)
            int bondRadius = 4; // Increased width to ensure consistent rendering
            DrawVerticalCapsule(colors, width, height, x, (int)y1, (int)yEnd, bondColor, bondRadius);
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

    private static void DrawVerticalCapsule(Color[] colors, int w, int h, int cx, int yStart, int yEnd, Color c, int radius)
    {
        int yMin = Math.Min(yStart, yEnd);
        int yMax = Math.Max(yStart, yEnd);

        // Draw rect
        for (int y = yMin; y <= yMax; y++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                int px = cx + dx;
                if (px >= 0 && px < w && y >= 0 && y < h)
                {
                    colors[y * w + px] = c;
                }
            }
        }
        
        // Draw caps
        DrawCircle(colors, w, h, cx, yMin, c, radius);
        DrawCircle(colors, w, h, cx, yMax, c, radius);
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
