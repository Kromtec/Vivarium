using Microsoft.Xna.Framework;
using System;
using Vivarium.Enums;

namespace Vivarium;

public struct Plant
{
    public const float ShrivelRate = 0.5f; // Energy lost per frame
    public const int MaturityAge = 60 * 2; // Frames until agent can reproduce after birth (2 seconds at 60 FPS)

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
        Energy += amount;
        if (!IsAlive)
        {
            gridMap[X, Y] = GridCell.Empty;
        }
    }

    public void Update(GridCell[,] gridMap)
    {
        if (!IsAlive)
        {
            return;
        }
        // Age the agent
        Age++;

        if (Age > MaturityAge)
        {
            ChangeEnergy(-ShrivelRate, gridMap);
        }
        // Color Update
        Color = Color.Lerp(Color.Black, OriginalColor, Energy / 100f);
    }

    public static Plant Create(int index, int x, int y)
    {
        return ConstructPlant(index, x, y);
    }

    public readonly void TryReproduce(Span<Plant> population, GridCell[,] gridMap, Random rng)
    {
        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

        ref Plant parent = ref population[Index];

        // 1. Find an empty spot nearby in the WORLD
        int childX = -1;
        int childY = -1;

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

            int tx = parent.X + dx;
            int ty = parent.Y + dy;

            // Bounds check
            if (tx >= 0 && tx < gridWidth && ty >= 0 && ty < gridHeight)
            {
                // Check if empty using our GridMap
                if (gridMap[tx, ty] == GridCell.Empty)
                {
                    childX = tx;
                    childY = ty;
                    break; // Found a spot!
                }
            }
        }

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

        childSlot = ConstructPlant(childSlot.Index, childX, childY);

        // 4. COST
        parent.ChangeEnergy(-2f, gridMap); // Giving birth is exhausting

        // Update map so nobody else claims this spot this frame
        gridMap[childX, childY] = new(EntityType.Plant, childSlot.Index);
    }

    public readonly bool CanReproduce()
    {
        return IsAlive && Age >= MaturityAge;
    }

    private static Plant ConstructPlant(int index, int x, int y)
    {
        return new Plant()
        {
            Index = index,
            X = x,
            Y = y,
            OriginalColor = Color.LimeGreen,
            IsAlive = true,
            Age = 0,
            Energy = 100f
        };
    }
}
