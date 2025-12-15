using System;

namespace Vivarium.Biology;

// A Gene is essentially a connection in the neural network.
// It is encoded into a single 32-bit integer for maximum performance and easy mutation.
public readonly struct Gene(uint dna)
{
    // The raw genetic data
    public readonly uint Dna = dna;

    // --- DECODING THE DNA ---

    // Bits 0-7: The ID of the input sensor (0 to 255)
    // We use a bitmask (0xFF is 11111111 in binary) to isolate the first 8 bits.
    public int SourceId => (int)(Dna & 0xFF);

    // Bits 8-15: The ID of the output action (0 to 255)
    // We shift right by 8 to discard the SourceId, then mask the next 8 bits.
    public int SinkId => (int)((Dna >> 8) & 0xFF);

    // Bits 16-31: The weight of the connection (signed 16-bit integer)
    // We shift right by 16. Since we cast to 'short', the sign bit is preserved properly.
    // This gives us a raw value between -32768 and 32767.
    private short RawWeight => (short)(Dna >> 16);

    // Helper to get the weight as a float (e.g., between -4.0 and 4.0)
    // Neural networks work better with small float values.
    public float Weight => RawWeight / 8192.0f;

    public static Gene CreateConnection(int source, int sink, float weight)
    {
        // Clamp weight to prevent overflow logic errors
        float clampedWeight = Math.Clamp(weight, -4.0f, 4.0f);

        // Encode float to 16-bit signed integer (short)
        // Multiplier 8192.0f gives us precision of approx 0.0001
        int weightInt = (int)(clampedWeight * 8192.0f);

        // Pack bits: [Weight 16][Sink 8][Source 8]
        uint dna = (uint)((weightInt << 16) | (sink << 8) | source);

        return new Gene(dna);
    }

    public override string ToString()
    {
        return $"In:{SourceId} -> Out:{SinkId} (W:{Weight:F2})";
    }
}