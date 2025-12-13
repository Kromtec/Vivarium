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
    /// Generates a 5x5 Identicon texture based on the genome hash.
    /// </summary>
    public static Texture2D GenerateIdenticon(GraphicsDevice graphics, ulong hash, Color primaryColor)
    {
        int size = 5;
        var texture = new Texture2D(graphics, size, size);
        var colors = new Color[size * size];

        // Use the hash to determine the pattern
        // We only need 15 bits for a symmetric 5x5 grid (left half + center column)
        // 0 1 2 1 0
        // 3 4 5 4 3
        // 6 7 8 7 6
        // ...

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Calculate which bit controls this pixel
                // Mirror horizontally: 0,1,2,1,0
                int bitIndex = (y * 3) + Math.Min(x, size - 1 - x);
                
                // Check if the bit is set
                bool isSet = ((hash >> bitIndex) & 1) == 1;

                if (isSet)
                {
                    colors[y * size + x] = primaryColor;
                }
                else
                {
                    // Background color (transparent or dark)
                    colors[y * size + x] = new Color(30, 30, 35); 
                }
            }
        }

        texture.SetData(colors);
        return texture;
    }
}
