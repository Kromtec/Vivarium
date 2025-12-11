using Microsoft.Xna.Framework;
using System;
using Vivarium.Biology;
using Vivarium.World;

namespace Vivarium.Entities;

// --- C# 14 / .NET 10 UPDATE ---
// Using a struct for memory efficiency (Stack allocated / packed in arrays)
public struct Agent : IGridEntity
{
    // Reproduction Thermodynamics
    public const float ReproductionCost = 20.0f;       // Wasted energy (effort)
    public const float ChildStartingEnergy = 75.0f;    // Transfer to child
    // Buffer to ensure parent survives the process
    public const float MinEnergyToReproduce = ReproductionCost + 5f;

    private const float BaseAttackThreshold = 0.5f;
    private const float BaseMovementThreshold = 0.1f;
    private const float BaseMetabolismRate = 0.1f; // Energy lost per frame

    public const float MovementCost = 1.0f;   // Base cost for moving
    public const float OrthogonalMovementCost = MovementCost; // Extra cost for non-cardinal moves
    public const float DiagonalMovementCost = MovementCost * 1.414f; // Extra cost for diagonal moves

    public const int MaturityAge = 60 * 10; // Frames until agent can reproduce after birth (10 seconds at 60 FPS)
    private int ReproductionCooldown;       // Frames until next possible reproduction
    private Color originalColor;

    public long Id { get; set; } // Unique identifier for tracking across generations
    public int Index { get; set; }
    public int X { get; set; }
    public int Y { get; set; }

    public long ParentId { get; set; }

    public DietType Diet { get; private set; }

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
        if(IsAlive)
        {
            Energy += amount;
        }

        if (!IsAlive)
        {
            if (gridMap[X, Y].Type == EntityType.Agent && gridMap[X, Y].Index == Index)
            {
                gridMap[X, Y] = GridCell.Empty;
            }
        }
    }

    public Gene[] Genome { get; set; }
    public float[] NeuronActivations { get; set; }

    // Traits derived from genome (normalized to [-1, +1])
    public float Strength { get; private set; }

    public float Power { get; private set; }
    public float Resilience { get; private set; }

    public float Bravery { get; private set; }
    public float AttackThreshold { get; private set; }
    public float MetabolicEfficiency { get; private set; }
    public float MetabolismRate { get; private set; }

    public float Perception { get; private set; }
    public float Speed { get; private set; }
    public float MovementThreshold {  get; private set; }
    public float TrophicBias { get; private set; } // continuous diet axis: -1 carnivore .. +1 herbivore

    public void Update(GridCell[,] gridMap)
    {
        if (!IsAlive)
        {
            return;
        }

        // Age the agent
        Age++;

        // Cooldown reproduction timer
        if (ReproductionCooldown > 0)
        {
            ReproductionCooldown--;
        }

        // Metabolize energy
        ChangeEnergy(-MetabolismRate, gridMap);

        // Color Update
        Color = Color.Lerp(Color.Black, OriginalColor, Math.Clamp(Energy / 100f, .25f, 1f));
    }

    public bool TryReproduce(Span<Agent> population, GridCell[,] gridMap, Random rng)
    {
        // 1. Biological Checks
        // Must have enough energy reserves
        if (Energy < MinEnergyToReproduce)
        {
            return false;
        }
        // Must be mature enough
        if (Age < MaturityAge)
        {
            return false;
        }
        // Must not be on reproduction cooldown
        if (ReproductionCooldown > 0)
        {
            return false;
        }

        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

        ref Agent parent = ref population[Index];

        // Find an empty spot nearby in the WORLD
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
        if (childX == -1) return false;

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
        if (childIndex == -1) return false;

        // 3. CREATE BABY
        // Create the child using our Genetics helper
        ref Agent childSlot = ref population[childIndex];

        childSlot = Genetics.Replicate(ref parent, childIndex, childX, childY, rng, ChildStartingEnergy);

        // 4. COST
        parent.ChangeEnergy(-ReproductionCost, gridMap); // Giving birth is exhausting

        // Update map so nobody else claims this spot this frame
        gridMap[childX, childY] = new(EntityType.Agent, childIndex);

        // Set reproduction cooldown (simple fixed cooldown for now)
        ReproductionCooldown = 60; // 1 second at 60 FPS
        return true;
    }

    public bool IsDirectlyRelatedTo(ref Agent other)
    {
        return ParentId == other.Id || Id == other.ParentId;
    }

    public static DietType DetermineDiet(float trophicBias)
    {
        return trophicBias switch
        {
            < -0.3f => DietType.Carnivore,
            > 0.3f => DietType.Herbivore,
            _ => DietType.Omnivore,
        };
    }

    public static Color GetColorBasedOnDietType(DietType dietType)
    {
        return dietType switch
        {
            DietType.Herbivore => Color.Turquoise,
            DietType.Carnivore => Color.Crimson,
            DietType.Omnivore => Color.Plum,
            _ => Color.White
        };
    }

    public static Agent Create(int index, int x, int y, Random rng)
    {
        Gene[] initialGenome = Genetics.CreateGenome(rng);

        return ConstructAgent(index, x, y, initialGenome);
    }

    public static Agent CreateChild(int index, int x, int y, Random rng, Gene[] genome, ref Agent parent, float initialEnergy)
    {
        // Apply Mutation to the genome of the parent to create the child's genome
        Genetics.Mutate(ref genome, rng);

        Agent child = ConstructAgent(index, x, y, genome, parent, initialEnergy);
        child.Generation = parent.Generation + 1;
        return child;
    }

    private static Agent ConstructAgent(int index, int x, int y, Gene[] genome, Agent? parent = null, float? initialEnergy = null)
    {
        // Extract traits and assign individually
        float strength = Genetics.ExtractTrait(genome, Genetics.TraitType.Strength);
        float bravery = Genetics.ExtractTrait(genome, Genetics.TraitType.Bravery);
        float metabolicEfficiency = Genetics.ExtractTrait(genome, Genetics.TraitType.MetabolicEfficiency);
        float perception = Genetics.ExtractTrait(genome, Genetics.TraitType.Perception);
        float speed = Genetics.ExtractTrait(genome, Genetics.TraitType.Speed);
        float trophicBias = Genetics.ExtractTrait(genome, Genetics.TraitType.TrophicBias);

        DietType dietType = DetermineDiet(trophicBias);
        return new Agent()
        {
            Id = VivariumGame.NextEntityId++,
            Index = index,
            X = x,
            Y = y,
            ParentId = parent?.Id ?? -1,
            OriginalColor = GetColorBasedOnDietType(dietType),
            IsAlive = true,
            Diet = dietType,
            Energy = initialEnergy ?? 100f,
            Genome = genome,
            NeuronActivations = new float[BrainConfig.NeuronCount],
            Strength = strength,
            Power = 1.0f + (strength * 0.5f),
            Resilience = 1.0f + (strength * 0.5f),
            Bravery = bravery,
            AttackThreshold = BaseAttackThreshold * (1.0f + (bravery * 0.5f)),
            MetabolicEfficiency = metabolicEfficiency,
            MetabolismRate = BaseMetabolismRate * (1f - (metabolicEfficiency * 0.5f)),
            Perception = perception,
            Speed = speed,
            MovementThreshold = BaseMovementThreshold - (speed * BaseMovementThreshold),
            TrophicBias = trophicBias
        };
    }
}
