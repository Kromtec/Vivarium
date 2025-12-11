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

    public readonly record struct DirectionalDensityResult(float[] AgentByDir, float[] PlantByDir, float[] StructureByDir);

    /// <summary>
    /// Scans a square area and buckets entities into 8 directional sectors around the center.
    /// Directions order: N, NE, E, SE, S, SW, W, NW
    /// Values are normalized 0..1 per bucket (fraction of occupied cells in that sector).
    /// </summary>
    public static DirectionalDensityResult ScanDirectional(GridCell[,] gridMap, int centerX, int centerY, int radius)
    {
        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

        int[] agentCounts = new int[8];
        int[] plantCounts = new int[8];
        int[] structureCounts = new int[8];
        int[] cellsPerBucket = new int[8];

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = centerX + dx;
                int ny = centerY + dy;
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

        float[] agentByDir = new float[8];
        float[] plantByDir = new float[8];
        float[] structureByDir = new float[8];

        for (int i = 0; i < 8; i++)
        {
            if (cellsPerBucket[i] == 0)
            {
                agentByDir[i] = 0f;
                plantByDir[i] = 0f;
                structureByDir[i] = 0f;
            }
            else
            {
                agentByDir[i] = (float)agentCounts[i] / cellsPerBucket[i];
                plantByDir[i] = (float)plantCounts[i] / cellsPerBucket[i];
                structureByDir[i] = (float)structureCounts[i] / cellsPerBucket[i];
            }
        }

        return new DirectionalDensityResult(agentByDir, plantByDir, structureByDir);
    }

    private static int GetDirectionIndex(int dx, int dy)
    {
        // Map to angle with 0 = North, clockwise
        float angle = MathF.Atan2(dy, dx); // -PI..PI, 0 = +X (east)
        float rotated = angle + MathF.PI / 2f; // rotate so 0 points to north
        if (rotated <= -MathF.PI) rotated += 2f * MathF.PI;
        if (rotated > MathF.PI) rotated -= 2f * MathF.PI;
        float sector = (rotated + MathF.PI) / (2f * MathF.PI); // 0..1
        int idx = (int)MathF.Floor(sector * 8f) % 8;
        if (idx < 0) idx += 8;
        return idx;
    }
}