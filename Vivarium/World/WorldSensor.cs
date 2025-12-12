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

                int nx = centerX + dx;
                int ny = centerY + dy;

                if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight)
                {
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
        }

        if (cellsChecked == 0) return new DensityResult(0f, 0f, 0f);

        return new DensityResult((float)agentsFound / cellsChecked, (float)plantsFound / cellsChecked, (float)structuresFound / cellsChecked);
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
        int structOffset,
        float amplification)
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
                int nx = centerX + dx;
                int ny = centerY + dy;
                
                // Bounds check
                if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight) continue;

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
                neuronOutput[agentOffset + i] = (agentCounts[i] / count) * amplification;
                neuronOutput[plantOffset + i] = (plantCounts[i] / count) * amplification;
                neuronOutput[structOffset + i] = (structureCounts[i] / count) * amplification;
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
        // Precomputed lookup for small radi? No, math is fast enough compared to memory access.
        float angle = MathF.Atan2(dy, dx); // -PI..PI, 0 = +X (east)
        float rotated = angle + MathF.PI / 2f; // rotate so 0 points to north
        
        // Wrap angle
        if (rotated <= -MathF.PI) rotated += 2f * MathF.PI;
        if (rotated > MathF.PI) rotated -= 2f * MathF.PI;
        
        float sector = (rotated + MathF.PI) / (2f * MathF.PI); // 0..1
        int idx = (int)MathF.Floor(sector * 8f) % 8;
        if (idx < 0) idx += 8;
        return idx;
    }
}
