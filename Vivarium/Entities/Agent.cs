using Microsoft.Xna.Framework;
using System;
using Vivarium.Biology;
using Vivarium.World;

namespace Vivarium.Entities;

// --- C# 14 / .NET 10 UPDATE ---
// Using a struct for memory efficiency (Stack allocated / packed in arrays)
public struct Agent
{
    public const float MetabolismRate = 0.2f; // Energy lost per frame
    public const int MaturityAge = 60 * 4; // Frames until agent can reproduce after birth (4 seconds at 60 FPS)
    private Color originalColor;

    public int Index { get; set; }
    public int X { get; set; }
    public int Y { get; set; }

    public int ParentIndex { get; set; }

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

    // Counts how many frames this agent has lived
    public long Age { get; set; }

    public long Generation { get; set; }

    // Helps us track active slots in the population array
    public bool IsAlive { get; set; }

    public float Energy
    {
        get;
        private set
        {
            // C# 14 field keyword
            field = Math.Clamp(value, 0f, 100f);

            // If energy hits zero, the agent dies.
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

    public Gene[] Genome { get; set; }
    public float[] NeuronActivations { get; set; }

    public void Update(GridCell[,] gridMap)
    {
        if (!IsAlive)
        {
            return;
        }

        // Age the agent
        Age++;
        // Metabolize energy
        ChangeEnergy(-MetabolismRate, gridMap);

        // Color Update
        if (Energy <= 50)
        {
            Color = Color.Lerp(Color.Black, OriginalColor, Energy / 50f);
        }
    }

    public readonly void TryReproduce(Span<Agent> population, GridCell[,] gridMap, Random rng)
    {
        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

        ref Agent parent = ref population[Index];

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
        ref Agent childSlot = ref population[childIndex];

        childSlot = Genetics.Replicate(ref parent, childIndex, childX, childY, rng);

        // 4. COST
        parent.ChangeEnergy(-2f, gridMap); // Giving birth is exhausting

        // Update map so nobody else claims this spot this frame
        gridMap[childX, childY] = new(EntityType.Agent, childIndex);
    }

    public readonly bool CanReproduce()
    {
        return IsAlive && Age >= MaturityAge;
    }

    public static Agent Create(int index, int x, int y, Random rng)
    {
        Gene[] initialGenome = Genetics.CreateGenome(rng);

        return ConstructAgent(index, x, y, initialGenome);
    }

    public static Agent CreateChild(int index, int x, int y, Random rng, Gene[] genome, ref Agent parent)
    {
        // Apply Mutation to the genome of the parent to create the child's genome
        Genetics.Mutate(ref genome, rng);

        Agent child = ConstructAgent(index, x, y, genome, parent.Index);
        child.Generation = parent.Generation + 1;
        return child;
    }

    private static Agent ConstructAgent(int index, int x, int y, Gene[] genome, int parentIndex = -1)
    {
        return new Agent()
        {
            Index = index,
            X = x,
            Y = y,
            ParentIndex = parentIndex,
            OriginalColor = Genetics.ComputePhenotypeColor(genome),
            IsAlive = true,
            Energy = 100f,
            Genome = genome,
            NeuronActivations = new float[BrainConfig.NeuronCount]
        };
    }
}
