namespace Vivarium.Config;

/// <summary>
/// Immutable world configuration. These values affect array sizes and data structures,
/// so they cannot be changed after simulation initialization.
/// </summary>
public sealed class WorldConfig
{
    /// <summary>Grid height in cells.</summary>
    public required int GridHeight { get; init; }

    /// <summary>Grid width in cells (calculated from height for 16:9 aspect ratio by default).</summary>
    public required int GridWidth { get; init; }

    /// <summary>Size of each cell in pixels (for rendering).</summary>
    public required int CellSize { get; init; }

    /// <summary>Maximum number of agents in the simulation (array pool size).</summary>
    public required int AgentPoolSize { get; init; }

    /// <summary>Maximum number of plants in the simulation (array pool size).</summary>
    public required int PlantPoolSize { get; init; }

    /// <summary>Maximum number of structures in the simulation (array pool size).</summary>
    public required int StructurePoolSize { get; init; }

    /// <summary>Random seed for deterministic simulation.</summary>
    public required int Seed { get; init; }

    /// <summary>
    /// Creates the default world configuration with current hardcoded values.
    /// </summary>
    public static WorldConfig CreateDefault(int? seed = null)
    {
        const int gridHeight = 96;
        const int gridWidth = (gridHeight / 9) * 16;
        const int cellSize = 1280 / gridHeight;
        int totalCells = gridWidth * gridHeight;

        return new WorldConfig
        {
            GridHeight = gridHeight,
            GridWidth = gridWidth,
            CellSize = cellSize,
            AgentPoolSize = totalCells / 18,
            PlantPoolSize = totalCells / 8,
            StructurePoolSize = totalCells / 32,
            Seed = seed ?? 64
        };
    }
}
