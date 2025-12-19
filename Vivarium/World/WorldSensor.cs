using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Vivarium.World;

public static class WorldSensor
{
    // Precomputed direction lookup table for dx, dy in range [-6, 6]
    // 13x13 array. Center is at [6, 6]
    private static readonly int[,] DirectionLookup = new int[13, 13];
    private const int LookupOffset = 6;

    static WorldSensor()
    {
        for (int dy = -6; dy <= 6; dy++)
        {
            for (int dx = -6; dx <= 6; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                DirectionLookup[dx + LookupOffset, dy + LookupOffset] = CalculateDirectionIndex(dx, dy);
            }
        }
    }

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
            int ny = centerY + dy;
            if (ny < 0) ny += gridHeight;
            else if (ny >= gridHeight) ny -= gridHeight;

            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = centerX + dx;
                if (nx < 0) nx += gridWidth;
                else if (nx >= gridWidth) nx -= gridWidth;

                cellsChecked++;
                // Direct struct access is fast, but we can avoid the copy if we use ref return or unsafe, 
                // but for now just avoiding modulo is good.
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

        // Pre-calculate inverse to multiply instead of divide? 
        // Compiler might optimize, but float division is fast enough here compared to memory access.
        float invCells = 1.0f / cellsChecked;
        return new DensityResult(agentsFound * invCells, plantsFound * invCells, structuresFound * invCells);
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
            // This modulo is unavoidable for random walk, but it's only called on spawn.
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
            int ny = centerY + dy;
            if (ny < 0) ny += gridHeight;
            else if (ny >= gridHeight) ny -= gridHeight;

            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = centerX + dx;
                if (nx < 0) nx += gridWidth;
                else if (nx >= gridWidth) nx -= gridWidth;

                // Use lookup table
                int dir = DirectionLookup[dx + LookupOffset, dy + LookupOffset];
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
                // Multiplication is faster than division
                float invCount = 1.0f / count;
                neuronOutput[agentOffset + i] = (agentCounts[i] * invCount);
                neuronOutput[plantOffset + i] = (plantCounts[i] * invCount);
                neuronOutput[structOffset + i] = (structureCounts[i] * invCount);
            }
            else
            {
                neuronOutput[agentOffset + i] = 0f;
                neuronOutput[plantOffset + i] = 0f;
                neuronOutput[structOffset + i] = 0f;
            }
        }
    }

    /// <summary>
    /// Combined scan for both local area density (fixed radius) and directional sensors (variable radius).
    /// Optimizes performance by iterating the grid only once.
    /// </summary>
    public static void ScanSensors(
        GridCell[,] gridMap,
        int centerX,
        int centerY,
        int localRadius,
        int dirRadius,
        Span<float> neuronOutput,
        int agentOffset,
        int plantOffset,
        int structOffset,
        int localAgentIdx,
        int localPlantIdx,
        int localStructIdx,
        ref Entities.Agent self,
        Entities.Agent[] agentPopulation,
        out bool threatDetected)
    {
        threatDetected = false;
        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

        // StackAlloc for zero GC pressure
        Span<int> agentCounts = stackalloc int[8];
        Span<int> plantCounts = stackalloc int[8];
        Span<int> structureCounts = stackalloc int[8];
        Span<int> cellsPerBucket = stackalloc int[8];

        // Clear stack memory
        agentCounts.Clear();
        plantCounts.Clear();
        structureCounts.Clear();
        cellsPerBucket.Clear();

        int localAgents = 0;
        int localPlants = 0;
        int localStructs = 0;
        int localCells = 0;

        int maxRadius = Math.Max(localRadius, dirRadius);

        // Optimization: Get references to array data to skip bounds checks
        ref GridCell gridBase = ref gridMap[0, 0];
        ref int dirLookupBase = ref DirectionLookup[0, 0];
        const int dirLookupWidth = 13; // DirectionLookup.GetLength(1)

        ref Entities.Agent populationBase = ref Unsafe.NullRef<Entities.Agent>();
        if (agentPopulation != null)
        {
            populationBase = ref MemoryMarshal.GetArrayDataReference(agentPopulation);
        }

        for (int dy = -maxRadius; dy <= maxRadius; dy++)
        {
            int ny = centerY + dy;
            if (ny < 0) ny += gridHeight;
            else if (ny >= gridHeight) ny -= gridHeight;

            for (int dx = -maxRadius; dx <= maxRadius; dx++)
            {
                if (dx == 0 && dy == 0) continue;

                // Chebyshev distance for square radius
                int dist = (dx < 0 ? -dx : dx); // Math.Abs
                int distY = (dy < 0 ? -dy : dy);
                if (distY > dist) dist = distY;

                int nx = centerX + dx;
                if (nx < 0) nx += gridWidth;
                else if (nx >= gridWidth) nx -= gridWidth;

                // var cell = gridMap[nx, ny];
                var cell = Unsafe.Add(ref gridBase, nx * gridHeight + ny);

                // Local Scan
                if (dist <= localRadius)
                {
                    localCells++;
                    switch (cell.Type)
                    {
                        case EntityType.Agent:
                            localAgents++;
                            // Threat Detection (integrated into scan)
                            if (!threatDetected && agentPopulation != null)
                            {
                                if (cell.Index >= 0 && cell.Index < agentPopulation.Length)
                                {
                                    ref Entities.Agent other = ref Unsafe.Add(ref populationBase, cell.Index);
                                    if (other.IsAlive && self.IsThreatenedBy(ref other))
                                    {
                                        threatDetected = true;
                                    }
                                }
                            }
                            break;
                        case EntityType.Plant: localPlants++; break;
                        case EntityType.Structure: localStructs++; break;
                    }
                }

                // Directional Scan
                if (dist <= dirRadius)
                {
                    int dir = Unsafe.Add(ref dirLookupBase, (dx + LookupOffset) * dirLookupWidth + (dy + LookupOffset));
                    cellsPerBucket[dir]++;
                    switch (cell.Type)
                    {
                        case EntityType.Agent: agentCounts[dir]++; break;
                        case EntityType.Plant: plantCounts[dir]++; break;
                        case EntityType.Structure: structureCounts[dir]++; break;
                    }
                }
            }
        }

        // Write Local Density
        if (localCells > 0)
        {
            float invLocal = 1.0f / localCells;
            neuronOutput[localAgentIdx] = localAgents * invLocal;
            neuronOutput[localPlantIdx] = localPlants * invLocal;
            neuronOutput[localStructIdx] = localStructs * invLocal;
        }
        else
        {
            neuronOutput[localAgentIdx] = 0f;
            neuronOutput[localPlantIdx] = 0f;
            neuronOutput[localStructIdx] = 0f;
        }

        // Write Directional Density
        for (int i = 0; i < 8; i++)
        {
            if (cellsPerBucket[i] > 0)
            {
                float invCount = 1.0f / cellsPerBucket[i];
                neuronOutput[agentOffset + i] = (agentCounts[i] * invCount);
                neuronOutput[plantOffset + i] = (plantCounts[i] * invCount);
                neuronOutput[structOffset + i] = (structureCounts[i] * invCount);
            }
            else
            {
                neuronOutput[agentOffset + i] = 0f;
                neuronOutput[plantOffset + i] = 0f;
                neuronOutput[structOffset + i] = 0f;
            }
        }
    }

    private static int CalculateDirectionIndex(int dx, int dy)
    {
        // Map to angle with 0 = North, clockwise
        // Atan2 returns angle from X axis (East). North (-Y) is -PI/2.
        float angle = MathF.Atan2(dy, dx);

        // Rotate so North (-PI/2) becomes 0.
        float rotated = angle + (MathF.PI / 2f);

        // Wrap to 0..2PI range
        if (rotated < 0) rotated += 2f * MathF.PI;

        // Map 0..2PI to 0..8
        // We use Round to center the sectors on the cardinal directions
        int idx = (int)MathF.Round((rotated / (2f * MathF.PI)) * 8f);

        // Wrap index 8 back to 0 (North)
        return idx % 8;
    }
}
