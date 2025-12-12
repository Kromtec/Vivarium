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
    // Reproduction Thermodynamics
    // Reproduction Thermodynamics
    public const float ReproductionOverheadPct = 0.30f; // 30% of MaxEnergy wasted as effort (Harder to breed)
    // Buffer to ensure parent survives the process
    public const float MinEnergyBuffer = 5.0f;

    private const float BaseAttackThreshold = 0.5f;
    private const float BaseMovementThreshold = 0.1f;
    private const float BaseMetabolismRate = 0.01f; // Energy lost per frame (Reduced 10x)
    private const int BaseMovementCooldown = 3; // Base cooldown for moving

    public const float MovementCost = 0.1f;   // Base cost for moving (Reduced 5x)
    public const float OrthogonalMovementCost = MovementCost; // Extra cost for non-cardinal moves
    public const float DiagonalMovementCost = MovementCost * 1.414f; // Extra cost for diagonal moves
    public const float FleeCost = 0.5f; // High cost for panic running (5x normal)

    public const int MaturityAge = 60 * 10; // Frames until agent can reproduce after birth (10 seconds at 60 FPS)
    private int ReproductionCooldown;       // Frames until next possible reproduction
    private int MovementCooldown;           // Frames until next possible movement
    public int AttackCooldown { get; set; } // Frames until next possible attack
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
            field = Math.Clamp(value, 0f, MaxEnergy);

            // If energy hits zero, the agent dies.
            if (field <= 0)
            {
                IsAlive = false;
            }
        }
    }


    public float Hunger
    {
        get;
        private set
        {
            field = Math.Clamp(value, 0f, 100f);
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

    public void Eat(float amount)
    {
        if (IsAlive)
        {
            Hunger -= amount;
            Energy += amount + (amount * MetabolicEfficiency * 0.5f);
        }
    }

    public bool TryMoveToLocation(GridCell[,] gridMap, int pendingX, int pendingY, int dx, int dy, float? overrideCost = null)
    {
        if (!IsAlive)
        {
            return false;
        }
        // Movement Cooldown Check
        if (MovementCooldown > 0)
        {
            return false;
        }

        // clear old location
        if (gridMap[X, Y].Type == EntityType.Agent && gridMap[X, Y].Index == Index)
        {
            gridMap[X, Y] = GridCell.Empty;
        }

        // 2. --- THE TRAP (Debug Trap) ---
        // We check BEFORE writing whether we're about to kill someone.
        GridCell target = gridMap[pendingX, pendingY];

        // If the target is an agent (and not ourselves)...
        if (target.Type == EntityType.Agent && target.Index != Index)
        {
            // ... then we've found a bug in Act()!
            // Act() told us "Go there" even though it's occupied.
            throw new Exception($"FATAL ERROR: Agent #{Index} is overwriting living Agent #{target.Index} at {pendingX},{pendingY}!\n" +
                                $"This means 'gridMap[{pendingX},{pendingY}] == Empty' was TRUE even though an agent was there.");
        }
        else if (target.Type == EntityType.Plant)
        {
            throw new Exception($"FATAL ERROR: Agent #{Index} is overwriting living Plant #{target.Index} at {pendingX},{pendingY}!\n" +
                                $"This means 'gridMap[{pendingX},{pendingY}] == Empty' was TRUE even though a plant was there.");
        }
        else if (target.Type == EntityType.Structure)
        {
            throw new Exception($"FATAL ERROR: Agent #{Index} is overwriting Structure #{target.Index} at {pendingX},{pendingY}!\n" +
                                $"This means 'gridMap[{pendingX},{pendingY}] == Empty' was TRUE even though a structure was there.");
        }
        // move to new location
        X = pendingX;
        Y = pendingY;
        gridMap[pendingX, pendingY] = new(EntityType.Agent, Index);

        // Calculate Movement Cost
        float cost;
        if (overrideCost.HasValue)
        {
            cost = overrideCost.Value;
        }
        else
        {
            // Orthogonal move (0,1) or (1,0) length is 1.
            // Diagonal move (1,1) length is approx 1.414.
            bool isDiagonal = (dx != 0 && dy != 0);
            cost = isDiagonal ? Agent.DiagonalMovementCost : Agent.OrthogonalMovementCost;
        }

        ChangeEnergy(-cost, gridMap);
        MovementCooldown = BaseMovementCooldown - (int)(Energy * 0.02 * Math.Clamp(Speed, 0d, 1d)); // - 0 to 2 frames
        return true;
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
    public float Constitution { get; private set; }
    public float MaxEnergy { get; private set; }

    public void Update(GridCell[,] gridMap)
    {
        if (!IsAlive)
        {
            return;
        }

        // Age the agent
        Age++;

        // Cooldown timers
        if (ReproductionCooldown > 0) ReproductionCooldown--;
        if (MovementCooldown > 0) MovementCooldown--;
        if (AttackCooldown > 0) AttackCooldown--;

        // Metabolize energy
        Hunger += MetabolismRate * 2;
        ChangeEnergy(-(MetabolismRate + (MetabolismRate * (Hunger / 100))), gridMap);

        // Color Update
        Color = Color.Lerp(Color.Black, OriginalColor, Math.Clamp(Energy / 100f, .25f, 1f));
    }

    public bool TryReproduce(Span<Agent> population, GridCell[,] gridMap, Random rng)
    {
        // 1. Biological Checks
        // Calculate costs based on physiology
        float childEnergy = MaxEnergy * 0.5f;
        float overhead = MaxEnergy * ReproductionOverheadPct;
        float totalCost = childEnergy + overhead;

        // Must have enough energy reserves (Cost + Safety Buffer)
        if (Energy < totalCost + MinEnergyBuffer)
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

        childSlot = Genetics.Replicate(ref parent, childIndex, childX, childY, rng, childEnergy);

        // 4. COST
        parent.ChangeEnergy(-totalCost, gridMap); // Giving birth is exhausting!

        // Update map so nobody else claims this spot this frame
        gridMap[childX, childY] = new(EntityType.Agent, childIndex);

        // Set reproduction cooldown (Prevent rapid-fire breeding)
        ReproductionCooldown = 600; // 10 seconds at 60 FPS
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
        float constitution = Genetics.ExtractTrait(genome, Genetics.TraitType.Constitution);

        DietType dietType = DetermineDiet(trophicBias);
        return new Agent()
        {
            Id = VivariumGame.NextEntityId++,
            // Initialize MaxEnergy FIRST so Energy clamp works correctly
            MaxEnergy = 100f * (1.0f + (constitution * 0.5f)),
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
            Resilience = 1.0f + (constitution * 0.5f),
            Bravery = bravery,
            AttackThreshold = BaseAttackThreshold * (1.0f + (bravery * 0.5f)),
            MetabolicEfficiency = metabolicEfficiency,
            MetabolismRate = BaseMetabolismRate * (1f - (metabolicEfficiency * 0.5f)),
            Perception = perception,
            Speed = speed,
            MovementThreshold = BaseMovementThreshold - (speed * BaseMovementThreshold),
            TrophicBias = trophicBias,
            Constitution = constitution
        };
    }

    public bool TryPerformAreaAttack(GridCell[,] gridMap, Span<Agent> agentPopulation, Span<Plant> plantPopulation, Random rng)
    {
        if (AttackCooldown > 0) return false;

        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

        // Single Target Attack instead of Area (Spin to Win)
        // Check 3x3 grid around attacker
        // 8 Neighbors. Pick one random valid target.

        int startDir = rng.Next(0, 8); // Random offset

        for (int i = 0; i < 8; i++)
        {
            int dirIndex = (startDir + i) % 8;
            
            // Map index 0..7 to dx,dy
            // 0: -1,-1 | 1: 0,-1 | 2: 1,-1
            // 3: -1, 0 |          | 4: 1, 0
            // 5: -1, 1 | 6: 0, 1 | 7: 1, 1
            
            int dx = 0, dy = 0;
            switch(dirIndex) {
                case 0: dx=-1; dy=-1; break;
                case 1: dx= 0; dy=-1; break;
                case 2: dx= 1; dy=-1; break;
                case 3: dx=-1; dy= 0; break;
                case 4: dx= 1; dy= 0; break;
                case 5: dx=-1; dy= 1; break;
                case 6: dx= 0; dy= 1; break;
                case 7: dx= 1; dy= 1; break;
            }

            int nx = X + dx;
            int ny = Y + dy;

            // Bounds check
            if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight)
            {
                // Is there a victim?
                if (gridMap[nx, ny].Type == EntityType.Agent)
                {
                    int victimIndex = gridMap[nx, ny].Index;
                    ref Agent victim = ref agentPopulation[victimIndex];
                    if (TryAttackAgent(ref victim, gridMap))
                    {
                        ChangeEnergy(-2.0f, gridMap);
                        AttackCooldown = 60;
                        return true; // Hit one target and stop
                    }
                }
                else if (gridMap[nx, ny].Type == EntityType.Plant)
                {
                    int plantIndex = gridMap[nx, ny].Index;
                    ref Plant plant = ref plantPopulation[plantIndex];
                    if (TryAttackPlant(ref plant, gridMap))
                    {
                        ChangeEnergy(-2.0f, gridMap);
                        AttackCooldown = 60;
                        return true; // Hit one target and stop
                    }
                }
            }
        }
        return false;
    }

    public bool TryAttackPlant(ref Plant plant, GridCell[,] gridMap)
    {
        if (!plant.IsAlive || !IsAlive)
        {
            return false;
        }
        const float baseDamage = 15f;
        var power = 1.0f + (Strength * 0.5f); // 0.5x to 1.5x damage based on Strength trait
        var damage = baseDamage * power;

        // Plant loses energy
        // Attacker gains energy (Herbivory!)
        if (Diet == DietType.Herbivore)
        {
            damage = Math.Min(damage, plant.Energy); // Cap damage to available energy
            plant.ChangeEnergy(-damage, gridMap);
            Eat(damage * 0.8f);
        }
        else if (Diet == DietType.Omnivore)
        {
            plant.ChangeEnergy(-damage * 0.25f, gridMap);
            Eat(damage * 0.1f);
        }
        else
        {
            plant.ChangeEnergy(-damage * 0.1f, gridMap);
        }
        return true;
    }

    public bool TryAttackAgent(ref Agent victim, GridCell[,] gridMap)
    {
        if (!victim.IsAlive || !IsAlive)
        {
            return false;
        }
        // Since we are inside the struct, 'this' is passed by value (read-only) unless we are careful.
        // But wait, we are modifying 'this' (Hunger, Energy).
        // C# instance methods on structs can modify state.
        
        // HOWEVER, IsDirectlyRelatedTo takes 'ref Agent'. We need to be careful.
        // 'this' is available. 
        if (IsDirectlyRelatedTo(ref victim))
        {
            return false; // No friendly fire
        }
        if (Bravery < victim.Bravery)
        {
            return false; // Too craven to attack an opponent that looks braver
        }

        const float baseDamage = 7.5f;
        var damage = baseDamage * Power / victim.Resilience;

        // Victim loses energy
        // Attacker gains energy (Carnivory!)
        if (Diet == DietType.Carnivore)
        {
            // Law of Thermodynamics: You cannot eat 20 energy if the victim only has 1.
            damage = Math.Min(damage, victim.Energy);

            victim.ChangeEnergy(-damage, gridMap);
            Eat(damage * 0.8f);
        }
        else if (Diet == DietType.Omnivore)
        {
            victim.ChangeEnergy(-damage * 0.25f, gridMap);
            Eat(damage * 0.05f);
        }
        else
        {
            victim.ChangeEnergy(-damage * 0.1f, gridMap);
        }

        if (victim.IsAlive)
        {
            // Retaliation chance
            float retaliationChance = (victim.Bravery + victim.Perception) * 0.5f;
            if (retaliationChance > 0.1f) // Minimum chance to retaliate
            {
                var retaliationDamage = baseDamage * victim.Power / Resilience;
                ChangeEnergy(-retaliationDamage * 0.2f, gridMap); // Retaliation is less effective
            }
        }
        // Removed Death Bonus (Double Dipping Loop). You eat what you kill via the damage dealt above.
        // If they die, they die. No extra candy.
        return true;
    }
}
