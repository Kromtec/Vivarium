using System;

namespace Vivarium.Biology;

public static class BrainConfig
{
    // Dynamically calculate counts using .NET generics
    public static readonly int SensorCount = Enum.GetValues<SensorType>().Length;
    public static readonly int ActionCount = Enum.GetValues<ActionType>().Length;

    // --- BRAIN SIZE OPTIMIZATION ---
    // We force the specific Total size to 256.
    // Why? Because Genes store Source/Sink IDs as 8-bit bytes (0-255).
    // If the array is exactly 256, we don't need to do any Division/Modulo (%) math.
    // Index = Byte. Fast.
    public const int NeuronCount = 256;

    // The rest is just filling the space (approx 256 - 55 = 200 hidden neurons)
    public static readonly int HiddenCount = NeuronCount - (SensorCount + ActionCount);

    // --- OFFSETS (The "Map" of the brain array) ---
    // [ SENSORS ... | ACTIONS ... | HIDDEN ... ]
    public static readonly int SensorsStart = 0;
    public static readonly int ActionsStart = SensorCount;
    public static readonly int HiddenStart = SensorCount + ActionCount;

    // Helper to map Action Enum to Array Index
    public static int GetActionIndex(ActionType action) => ActionsStart + (int)action;
}
