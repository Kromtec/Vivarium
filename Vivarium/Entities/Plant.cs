using Microsoft.Xna.Framework;
using System;
using Vivarium.World;

namespace Vivarium.Entities;

public struct Plant : IGridEntity
{
    public const float ShrivelRate = 0.4f; // Energy lost per frame (Aging)
    public const float PhotosynthesisRate = 0.35f; // Energy gained per frame from sun (Buffed from 0.30f)
    public const int MaturityAge = 60 * 10; // 10 Seconds to mature

    public const float ReproductionCost = 20.0f; // Reduced cost (was 30.0f)
    public const float MinEnergyToReproduce = 30.0f; // Easier to reproduce (was 40.0f)

    public long Id { get; set; } // Unique identifier for tracking across generations
    public int Index { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public long Age { get; set; }
    public bool IsAlive { get; set; }

    private Color originalColor;
    public Color OriginalColor
    {
        get => originalColor;
        set
        {
            originalColor = value;
            Color = value;
        }
    }
    public Color Color { get; private set; }

    public float Energy
    {
        get;
        private set
        {
            // C# 14 field keyword
            field = Math.Clamp(value, 0f, 100f);

            // If energy hits zero, the plant dies.
            if (field <= 0)
            {
                IsAlive = false;
            }
        }
    }

    public void ChangeEnergy(float amount, GridCell[,] gridMap)
    {
        if (IsAlive)
        {
            Energy += amount;
        }
        if (!IsAlive)
        {
            if (gridMap[X, Y].Type == EntityType.Plant && gridMap[X, Y].Index == Index)
            {
                gridMap[X, Y] = GridCell.Empty;
            }
        }
    }

    public void Update(GridCell[,] gridMap, Random rng)
    {
        if (!IsAlive)
        {
            return;
        }
        // Age the agent
        Age++;

        if (Age > MaturityAge && rng.Next(-1, 2) > 0)
        {
            ChangeEnergy(-ShrivelRate, gridMap);
        }

        // Photosynthesis
        // Grow if not full
        if (Energy < 100f)
        {
            ChangeEnergy(PhotosynthesisRate, gridMap);
        }

        // Color Update
        Color = Color.Lerp(Color.Black, OriginalColor, Math.Clamp(Energy / 100f, .25f, 1f));
    }

    public static Plant Create(int index, int x, int y, Random rng)
    {
        var plant = ConstructPlant(index, x, y);
        // Randomize age for initial spawn to avoid synchronization
        plant.Age = rng.Next(0, MaturityAge);
        plant.Energy = rng.Next(50, 100);
        return plant;
    }

    public readonly void TryReproduce(Span<Plant> population, GridCell[,] gridMap, Random rng)
    {
        if (rng.NextDouble() > 0.05) // 5% chance to attempt reproduction each frame
        {
            return;
        }
        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

        ref Plant parent = ref population[Index];

        // 0. CHECK: Am I strong enough?
        if (parent.Energy < MinEnergyToReproduce) return;

        // 1. Find an empty spot nearby in the WORLD
        int childX = -1;
        int childY = -1;
        int neighborCount = 0; // Check for overcrowding

        // Check 3x3 grid around parent
        // We shuffle checking order to avoid bias (always breeding North-West), 
        // but for performance, a simple loop with a random start is okay.

        // Quick random attempt logic:
        for (int attempt = 0; attempt < 8; attempt++)
        {
            int dx = rng.Next(-1, 2);
            int dy = rng.Next(-1, 2);

            // Skip the center (parent's own position)
            if (dx == 0 && dy == 0) continue;

            int tx = (parent.X + dx + gridWidth) % gridWidth;
            int ty = (parent.Y + dy + gridHeight) % gridHeight;

            // Count neighbors (Plants only) for density check
            if (gridMap[tx, ty].Type == EntityType.Plant)
            {
                neighborCount++;
            }

            // Check if empty using our GridMap
            if (childX == -1 && gridMap[tx, ty] == GridCell.Empty)
            {
                childX = tx;
                childY = ty;
                // Don't break, we need to count neighbors!
            }
        }

        // Overcrowding Check: If too many neighbors, do not reproduce.
        // This encourages spreading to edges rather than filling blocks.
        if (neighborCount >= 5) return;

        // No space found? No baby.
        if (childX == -1) return;

        // 2. Find an empty slot in the ARRAY (Dead agent memory slot)
        int childIndex = -1;
        // We search linearly. In huge simulations, you'd keep a list of "free indices".
        for (int j = 0; j < population.Length; j++)
        {
            if (!population[j].IsAlive)
            {
                childIndex = j;
                break;
            }
        }

        // No array memory left? Population cap reached.
        if (childIndex == -1) return;

        // 3. CREATE BABY
        // Create the child using our Genetics helper
        ref Plant childSlot = ref population[childIndex];

        childSlot = ConstructPlant(childIndex, childX, childY);

        // 4. COST
        parent.ChangeEnergy(-ReproductionCost, gridMap); // Giving birth is exhausting dramatically

        // Update map so nobody else claims this spot this frame
        gridMap[childX, childY] = new(EntityType.Plant, childIndex);
    }

    public readonly bool CanReproduce()
    {
        return IsAlive && Age >= MaturityAge;
    }

    private static Plant ConstructPlant(int index, int x, int y)
    {
        return new Plant()
        {
            Id = VivariumGame.NextEntityId++,
            Index = index,
            X = x,
            Y = y,
            OriginalColor = Visuals.VivariumColors.Plant,
            IsAlive = true,
            Age = 0,
            Energy = 100f
        };
    }
}
