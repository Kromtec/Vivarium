using System;

namespace Vivarium.World;

public static class WorldSensor
{
    /// <summary>
    /// Scans a square area around the center point and calculates the density of entities.
    /// Returns values between 0.0 (empty) and 1.0 (completely full).
    /// </summary>
    public static DensityResult ScanLocalArea(GridCell[,] gridMap, int centerX, int centerY, int radius)
    {
        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

        int agentsFound = 0;
        int plantsFound = 0;
        int structuresFound = 0;
        int cellsChecked = 0;

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = (centerX + dx + gridWidth) % gridWidth;
                int ny = (centerY + dy + gridHeight) % gridHeight;

                cellsChecked++;
                var cell = gridMap[nx, ny];
                switch (cell.Type)
                {
                    case EntityType.Agent: agentsFound++; break;
                    case EntityType.Plant: plantsFound++; break;
                    case EntityType.Structure: structuresFound++; break;
                }
            }
        }

        if (cellsChecked == 0) return new DensityResult(0f, 0f, 0f);

        return new DensityResult((float)agentsFound / cellsChecked, (float)plantsFound / cellsChecked, (float)structuresFound / cellsChecked);
    }

    public static bool TryGetRandomEmptySpot(GridCell[,] gridMap, out int x, out int y, Random rng)
    {
        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

        for (int i = 0; i < 5; i++)
        {
            int rx = rng.Next(0, gridWidth);
            int ry = rng.Next(0, gridHeight);

            if (gridMap[rx, ry] == GridCell.Empty)
            {
                x = rx;
                y = ry;
                return true;
            }
        }

        int totalCells = gridWidth * gridHeight;
        int startIndex = rng.Next(totalCells);

        for (int i = 0; i < totalCells; i++)
        {
            int currentIndex = (startIndex + i) % totalCells;

            int cx = currentIndex % gridWidth;
            int cy = currentIndex / gridWidth;

            if (gridMap[cx, cy] == GridCell.Empty)
            {
                x = cx;
                y = cy;
                return true;
            }
        }

        x = -1;
        y = -1;
        return false;
    }

    public static bool DetectThreats(GridCell[,] gridMap, Entities.Agent[] agentPopulation, int centerX, int centerY, int radius, ref Entities.Agent self)
    {
        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

        // Safety check
        if (agentPopulation == null) return false;

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = (centerX + dx + gridWidth) % gridWidth;
                int ny = (centerY + dy + gridHeight) % gridHeight;

                var cell = gridMap[nx, ny];
                if (cell.Type == EntityType.Agent)
                {
                    // Bounds check to prevent crashes if the map is desynced
                    if (cell.Index >= 0 && cell.Index < agentPopulation.Length)
                    {
                        ref Entities.Agent other = ref agentPopulation[cell.Index];
                        if (other.IsAlive && self.IsThreatenedBy(ref other))
                        {
                            return true; // Found at least one threat
                        }
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Scans a square area and populates the neuron input outputs directly.
    /// Uses stackalloc to avoid heap allocations.
    /// </summary>
    public static void PopulateDirectionalSensors(
        GridCell[,] gridMap,
        int centerX,
        int centerY,
        int radius,
        Span<float> neuronOutput,
        int agentOffset,
        int plantOffset,
        int structOffset)
    {
        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

        // StackAlloc for zero GC pressure
        Span<int> agentCounts = stackalloc int[8];
        Span<int> plantCounts = stackalloc int[8];
        Span<int> structureCounts = stackalloc int[8];
        Span<int> cellsPerBucket = stackalloc int[8];

        // Clear stack memory (safety, though stackalloc usually zeroed in recent .NET)
        agentCounts.Clear();
        plantCounts.Clear();
        structureCounts.Clear();
        cellsPerBucket.Clear();

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = (centerX + dx + gridWidth) % gridWidth;
                int ny = (centerY + dy + gridHeight) % gridHeight;

                int dir = GetDirectionIndex(dx, dy);
                cellsPerBucket[dir]++;

                var cell = gridMap[nx, ny];
                switch (cell.Type)
                {
                    case EntityType.Agent: agentCounts[dir]++; break;
                    case EntityType.Plant: plantCounts[dir]++; break;
                    case EntityType.Structure: structureCounts[dir]++; break;
                }
            }
        }

        // Write directly to the brain's neuron inputs
        for (int i = 0; i < 8; i++)
        {
            if (cellsPerBucket[i] > 0)
            {
                float count = (float)cellsPerBucket[i];
                neuronOutput[agentOffset + i] = (agentCounts[i] / count);
                neuronOutput[plantOffset + i] = (plantCounts[i] / count);
                neuronOutput[structOffset + i] = (structureCounts[i] / count);
            }
            else
            {
                neuronOutput[agentOffset + i] = 0f;
                neuronOutput[plantOffset + i] = 0f;
                neuronOutput[structOffset + i] = 0f;
            }
        }
    }

    private static int GetDirectionIndex(int dx, int dy)
    {
        // Map to angle with 0 = North, clockwise
        // Atan2 returns angle from X axis (East). North (-Y) is -PI/2.
        float angle = MathF.Atan2(dy, dx);

        // Rotate so North (-PI/2) becomes 0.
        float rotated = angle + MathF.PI / 2f;

        // Wrap to 0..2PI range
        if (rotated < 0) rotated += 2f * MathF.PI;

        // Map 0..2PI to 0..8
        // We use Round to center the sectors on the cardinal directions
        int idx = (int)MathF.Round((rotated / (2f * MathF.PI)) * 8f);

        // Wrap index 8 back to 0 (North)
        return idx % 8;
    }
}
