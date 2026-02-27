using System;

namespace Vivarium.Biology;

/// <summary>
/// Represents a gene (neural network connection) encoded as a 32-bit integer.
/// Uses bit manipulation for efficient storage and mutation.
/// </summary>
public readonly struct Gene(uint dna)
{
    /// <summary>The raw genetic data (32-bit unsigned integer).</summary>
    public readonly uint Dna = dna;

    // --- DECODING THE DNA ---

    /// <summary>Bits 0-7: The ID of the input sensor (0 to 255).</summary>
    public int SourceId => (int)(Dna & 0xFF);

    /// <summary>Bits 8-15: The ID of the output action (0 to 255).</summary>
    public int SinkId => (int)((Dna >> 8) & 0xFF);

    /// <summary>Bits 16-31: The raw weight as a signed 16-bit integer.</summary>
    private short RawWeight => (short)(Dna >> 16);

    /// <summary>Gets the weight as a normalized float value (approximately -4.0 to 4.0).</summary>
    public float Weight => RawWeight / 8192.0f;

    /// <summary>
    /// Creates a new gene with the specified source, sink, and weight.
    /// </summary>
    /// <param name="source">The source sensor ID (0-255).</param>
    /// <param name="sink">The sink/action ID (0-255).</param>
    /// <param name="weight">The connection weight (-4.0 to 4.0).</param>
    /// <returns>A new Gene instance.</returns>
    public static Gene CreateConnection(int source, int sink, float weight)
    {
        float clampedWeight = Math.Clamp(weight, -4.0f, 4.0f);
        int weightInt = (int)(clampedWeight * 8192.0f);
        uint dna = (uint)((weightInt << 16) | (sink << 8) | source);
        return new Gene(dna);
    }

    /// <summary>Returns a string representation of the gene.</summary>
    public override string ToString()
    {
        return $"In:{SourceId} -> Out:{SinkId} (W:{Weight:F2})";
    }
}

/// <summary>
/// Represents a decoded gene with direct values for neural network processing.
/// </summary>
public readonly struct DecodedGene(int source, int sink, float weight)
{
    /// <summary>The source index.</summary>
    public readonly int SourceIndex = source;
    /// <summary>The sink/action index.</summary>
    public readonly int SinkIndex = sink;
    /// <summary>The connection weight.</summary>
    public readonly float Weight = weight;
}