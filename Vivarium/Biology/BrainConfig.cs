using System;

namespace Vivarium.Biology;

public static class BrainConfig
{
    // Dynamically calculate counts using .NET generics
    public static readonly int SensorCount = Enum.GetValues<SensorType>().Length;
    public static readonly int ActionCount = Enum.GetValues<ActionType>().Length;

    // Define how many internal memory neurons we want
    public const int HiddenCount = 20;

    // --- OFFSETS (The "Map" of the brain array) ---
    // [ SENSORS ... | ACTIONS ... | HIDDEN ... ]

    public static readonly int SensorsStart = 0;
    public static readonly int ActionsStart = SensorCount;
    public static readonly int HiddenStart = SensorCount + ActionCount;

    // Total size of the brain array
    public static readonly int NeuronCount = SensorCount + ActionCount + HiddenCount;

    // Helper to map Action Enum to Array Index
    public static int GetActionIndex(ActionType action) => ActionsStart + (int)action;
}