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

        // Iterate over the square area defined by radius
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                // Skip the center cell (the agent itself)
                if (dx == 0 && dy == 0) continue;

                int nx = centerX + dx;
                int ny = centerY + dy;

                // Bounds check
                if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight)
                {
                    cellsChecked++;
                    var cell = gridMap[nx, ny];

                    switch (cell.Type)
                    {
                        case EntityType.Agent:
                            agentsFound++;
                            break;
                        case EntityType.Plant:
                            plantsFound++;
                            break;
                        case EntityType.Structure:
                            structuresFound++;
                            break;
                    }
                }
            }
        }

        // Avoid division by zero if radius is 0 or something went wrong
        if (cellsChecked == 0)
            return new DensityResult(0f, 0f, 0f);

        return new DensityResult(
            (float)agentsFound / cellsChecked,
            (float)plantsFound / cellsChecked,
            (float)structuresFound / cellsChecked
        );
    }
}