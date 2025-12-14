using Microsoft.Xna.Framework;
using System;
using Vivarium.Biology;
using Vivarium.World;
using Vivarium.UI;

namespace Vivarium.Entities;

public struct Agent : IGridEntity
{
    // Reproduction Thermodynamics
    public const float ReproductionOverheadPct = 0.30f; // 30% of MaxEnergy wasted as effort (Harder to breed)
    // Buffer to ensure parent survives the process
    public const float MinEnergyBuffer = 5.0f;

    private const float BaseAttackThreshold = 0.5f;
    private const float BaseMovementThreshold = 0.1f;
    private const float BaseMetabolismRate = 0.01f; // Energy lost per frame (Reduced from 0.02f)
    private const int BaseMovementCooldown = 3; // Base cooldown for moving

    public const float MovementCost = 0.25f;   // Base cost for moving
    public const float OrthogonalMovementCost = MovementCost; // Extra cost for non-cardinal moves
    public const float DiagonalMovementCost = MovementCost * 1.414f; // Extra cost for diagonal moves
    public const float FleeCost = MovementCost * 2f; // High cost for panic running

    public const int MaturityAge = 60 * 10; // Frames until agent can reproduce after birth (10 seconds at 60 FPS)
    public int ReproductionCooldown;        // Frames until next possible reproduction
    public int MovementCooldown;            // Frames until next possible movement
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

    public void ChangeEnergy(float amount, GridCell[,] gridMap)
    {
        if (IsAlive)
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
    public float MovementThreshold { get; private set; }
    public float TrophicBias { get; private set; } // continuous diet axis: -1 carnivore .. +1 herbivore
    public float Constitution { get; private set; }
    public float MaxEnergy { get; private set; }

    // --- VISUAL FEEDBACK STATE ---
    public sbyte LastAttackDirX;
    public sbyte LastAttackDirY;
    public byte AttackVisualTimer;

    public sbyte LastFleeDirX;
    public sbyte LastFleeDirY;
    public byte FleeVisualTimer;

    public byte ReproductionVisualTimer;

    public void Update()
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
        if (AttackVisualTimer > 0) AttackVisualTimer--;
        if (FleeVisualTimer > 0) FleeVisualTimer--;
        if (ReproductionVisualTimer > 0) ReproductionVisualTimer--;

        // Color Update
        Color = Color.Lerp(Color.Black, OriginalColor, Math.Clamp(Energy / 100f, .25f, 1f));
    }

    /// <summary>
    /// Determines whether this agent is a direct parent or child of the specified agent.
    /// </summary>
    /// <param name="other">A reference to the agent to compare with the current agent.</param>
    /// <returns>true if this agent is the parent or child of the specified agent; otherwise, false.</returns>
    public readonly bool IsDirectlyRelatedTo(ref Agent other)
    {
        return ParentId == other.Id || Id == other.ParentId;
    }

    /// <summary>
    /// Determines whether the current agent is threatened by the specified agent based on kinship, diet, and bravery.
    /// </summary>
    /// <remarks>An agent is not considered threatened by another agent if they are directly related, if both
    /// are herbivores, if the current agent is a carnivore or omnivore and the other is a herbivore, or if the current
    /// agent's bravery exceeds that of the other agent.</remarks>
    /// <param name="other">A reference to the agent to evaluate as a potential threat.</param>
    /// <returns>true if the current agent is considered threatened by the specified agent; otherwise, false.</returns>
    public readonly bool IsThreatenedBy(ref Agent other)
    {
        // Constraint 1: Kinship (Don't flee from family)
        if (IsDirectlyRelatedTo(ref other)) return false;

        // Constraint 2: Diet (Herbivores don't flee from Herbivores)
        if (Diet == DietType.Herbivore && other.Diet == DietType.Herbivore) return false;

        // Constraint 3: Predation (Carnivores && Omnivores don't flee from Herbivores)
        if ((Diet == DietType.Carnivore || Diet == DietType.Omnivore) && other.Diet == DietType.Herbivore) return false;

        // Constraint 4: Bravery Check (Braver than threat, don't flee)
        if (Bravery > other.Bravery) return false;

        return true;
    }

    /// <summary>
    /// Determines whether this agent is considered to be preying on the specified agent.
    /// </summary>
    /// <remarks>An agent is not considered to be preying on another agent if they are directly related or if
    /// this agent is a herbivore.</remarks>
    /// <param name="other">The agent to evaluate as a potential prey. Passed by reference.</param>
    /// <returns>true if this agent is preying on the specified agent; otherwise, false.</returns>
    public readonly bool IsPreyingOn(ref Agent other)
    {
        // Constraint 1: Kinship (Don't eat family)
        if (IsDirectlyRelatedTo(ref other)) return false;

        // Constraint 2: Diet (Herbivores don't eat agents)
        if (Diet == DietType.Herbivore) return false;

        return true;
    }

    public static DietType DetermineDiet(float trophicBias)
    {
        return trophicBias switch
        {
            < -0.60f => DietType.Carnivore, // Increased initial population (was < -0.65f)
            > 0.0f => DietType.Herbivore,
            _ => DietType.Omnivore,
        };
    }

    public static Color GetColorBasedOnDietType(DietType dietType)
    {
        return dietType switch
        {
            DietType.Herbivore => Visuals.VivariumColors.Herbivore,
            DietType.Carnivore => Visuals.VivariumColors.Carnivore,
            DietType.Omnivore => Visuals.VivariumColors.Omnivore,
            _ => Visuals.VivariumColors.Agent
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

        // Metabolism Multipliers based on Diet
        // Carnivores: Efficient hunters (0.8x)
        // Herbivores: Standard (1.0x)
        // Omnivores: High maintenance (1.2x)
        float metabolismMultiplier = dietType switch
        {
            DietType.Carnivore => 0.6f, // Buffed from 0.7f to help survival
            DietType.Omnivore => 1.1f,  // Buffed from 1.2f (was too harsh)
            _ => 1.0f
        };

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
            MetabolismRate = BaseMetabolismRate * (1f - (metabolicEfficiency * 0.5f)) * metabolismMultiplier,
            Perception = perception,
            Speed = speed,
            MovementThreshold = BaseMovementThreshold - (speed * BaseMovementThreshold),
            TrophicBias = trophicBias,
            Constitution = constitution
        };
    }


    // Methods called from Brain.Act()

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
            cost = isDiagonal ? DiagonalMovementCost : OrthogonalMovementCost;
        }

        ChangeEnergy(-cost, gridMap);
        MovementCooldown = BaseMovementCooldown - (int)(Energy * 0.02 * Math.Clamp(Speed, 0d, 1d)); // - 0 to 2 frames
        return true;
    }

    public bool TryReproduce(Span<Agent> population, GridCell[,] gridMap, Random rng)
    {
        // 1. Biological Checks
        // Calculate costs based on physiology
        float childEnergy = MaxEnergy * 0.5f;
        float overhead = MaxEnergy * ReproductionOverheadPct;

        // Omnivores have higher reproduction overhead (Population Control)
        if (Diet == DietType.Omnivore)
        {
            overhead *= 1.2f; // Reduced penalty from 1.5f
        }
        // Herbivores have lower reproduction overhead (Buff)
        else if (Diet == DietType.Herbivore)
        {
            overhead *= 0.5f; // Significant buff to help them recover numbers
        }
        // Carnivores have lower reproduction overhead (Buff)
        else if (Diet == DietType.Carnivore)
        {
            overhead *= 0.5f; // Buff to help them recover numbers
        }

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

            int tx = (parent.X + dx + gridWidth) % gridWidth;
            int ty = (parent.Y + dy + gridHeight) % gridHeight;

            // Check if empty using our GridMap
            if (gridMap[tx, ty] == GridCell.Empty)
            {
                childX = tx;
                childY = ty;
                break; // Found a spot!
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

        // Visual Feedback
        ReproductionVisualTimer = 30; // Show for 0.5 seconds (longer than attack)

        return true;
    }

    public AttackResult TryAreaAttack(GridCell[,] gridMap, Span<Agent> agentPopulation, Span<Plant> plantPopulation, Random rng)
    {
        if (AttackCooldown > 0) return new AttackResult { Success = false };

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
            switch (dirIndex)
            {
                case 0: dx = -1; dy = -1; break;
                case 1: dx = 0; dy = -1; break;
                case 2: dx = 1; dy = -1; break;
                case 3: dx = -1; dy = 0; break;
                case 4: dx = 1; dy = 0; break;
                case 5: dx = -1; dy = 1; break;
                case 6: dx = 0; dy = 1; break;
                case 7: dx = 1; dy = 1; break;
            }

            int nx = (X + dx + gridWidth) % gridWidth;
            int ny = (Y + dy + gridHeight) % gridHeight;

            // Is there a victim?
            if (gridMap[nx, ny].Type == EntityType.Agent)
            {
                int victimIndex = gridMap[nx, ny].Index;
                ref Agent victim = ref agentPopulation[victimIndex];

                float damageDealt, selfDamage;
                if (TryAttackAgent(ref victim, gridMap, dx, dy, out damageDealt, out selfDamage))
                {
                    ChangeEnergy(-0.5f, gridMap); // Reduced cost for hunting (was 2.0f)
                    AttackCooldown = 30; // Slightly slower attacks to give prey a chance (was 20)
                    return new AttackResult
                    {
                        Success = true,
                        DamageDealt = damageDealt,
                        TargetId = victim.Id,
                        TargetType = "Agent",
                        SelfDamage = selfDamage
                    };
                }
            }
            else if (gridMap[nx, ny].Type == EntityType.Plant)
            {
                int plantIndex = gridMap[nx, ny].Index;
                ref Plant plant = ref plantPopulation[plantIndex];

                float damageDealt;
                if (TryAttackPlant(ref plant, gridMap, dx, dy, out damageDealt))
                {
                    ChangeEnergy(-2.0f, gridMap);
                    AttackCooldown = 60;
                    return new AttackResult
                    {
                        Success = true,
                        DamageDealt = damageDealt,
                        TargetId = plant.Id,
                        TargetType = "Plant",
                        SelfDamage = 0
                    };
                }
            }
        }
        return new AttackResult { Success = false };
    }

    public bool TryToFlee(GridCell[,] gridMap, Span<Agent> agentPopulationSpan, Random rng, out int fleeX, out int fleeY)
    {
        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

        int vecX = 0;
        int vecY = 0;
        bool fleeing = false;

        // Scan neighborhood (radius 2 for hysteresis)
        for (int dy = -2; dy <= 2; dy++)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = (X + dx + gridWidth) % gridWidth;
                int ny = (Y + dy + gridHeight) % gridHeight;

                GridCell cell = gridMap[nx, ny];
                if (cell.Type == EntityType.Agent)
                {
                    ref Agent other = ref agentPopulationSpan[cell.Index];
                    if (!other.IsAlive) continue;

                    if (!IsThreatenedBy(ref other)) continue;

                    // EVADE!
                    vecX -= Math.Sign(dx);
                    vecY -= Math.Sign(dy);
                    fleeing = true;
                }
            }
        }

        if (fleeing)
        {
            // Add "Panic Jitter" (RNG) to prevent predictable loops
            vecX += rng.Next(-1, 2);
            vecY += rng.Next(-1, 2);

            // Normalize and Execute
            int fleeMoveX = Math.Clamp(vecX, -1, 1);
            int fleeMoveY = Math.Clamp(vecY, -1, 1);

            if (fleeMoveX != 0 || fleeMoveY != 0)
            {
                // Calculate target position (Pac-Man Wrap)
                int pendingX = (X + fleeMoveX + gridWidth) % gridWidth;
                int pendingY = (Y + fleeMoveY + gridHeight) % gridHeight;

                // Only move if empty (Don't attack while fleeing)
                if (gridMap[pendingX, pendingY] == GridCell.Empty)
                {
                    TryMoveToLocation(gridMap, pendingX, pendingY, fleeMoveX, fleeMoveY, FleeCost);

                    // Visual Feedback for Fleeing
                    // We want to show where we are fleeing FROM.
                    // If we move (1, 0), we are fleeing from (-1, 0).
                    LastFleeDirX = (sbyte)-fleeMoveX;
                    LastFleeDirY = (sbyte)-fleeMoveY;
                    FleeVisualTimer = 15;

                    fleeX = fleeMoveX;
                    fleeY = fleeMoveY;

                    return true;
                }
            }
        }
        fleeX = 0;
        fleeY = 0;
        return false;
    }

    public bool TryAttackPlant(ref Plant plant, GridCell[,] gridMap, int dx, int dy, out float damageDealt)
    {
        damageDealt = 0;
        if (!plant.IsAlive || !IsAlive)
        {
            return false;
        }

        // Appetite Check (Thermodynamic)
        // Don't eat if full (Energy > 95%)
        bool isFull = Energy >= MaxEnergy * 0.95f;

        const float baseDamage = 10f; // Reduced damage to plants (was 15f) to prevent over-grazing
        var power = 1.0f + (Strength * 0.5f);
        var damage = baseDamage * power;

        // Plant loses energy
        // Attacker gains energy (Herbivory!)
        if (Diet == DietType.Herbivore)
        {
            damage = Math.Min(damage, plant.Energy);
            plant.ChangeEnergy(-damage, gridMap);

            // Only eat if not full
            // Increased efficiency (0.8f) so they get more energy per bite, needing to eat less often
            if (!isFull) ChangeEnergy(damage * 1.0f, gridMap); // Buffed to 1.0f (100% efficiency)
        }
        else if (Diet == DietType.Omnivore)
        {
            // Omnivores are now more efficient at eating plants (was 0.25/0.1)
            plant.ChangeEnergy(-damage * 0.5f, gridMap);
            // Reduced efficiency for Omnivores (was 0.4f) to curb population explosion
            if (!isFull) ChangeEnergy(damage * 0.3f, gridMap);
        }
        else
        {
            plant.ChangeEnergy(-damage * 0.1f, gridMap);
        }

        damageDealt = damage;

        // Visual Feedback
        LastAttackDirX = (sbyte)dx;
        LastAttackDirY = (sbyte)dy;
        AttackVisualTimer = 15; // Show for 1/4th of a second

        return true;
    }

    public bool TryAttackAgent(ref Agent victim, GridCell[,] gridMap, int dx, int dy, out float damageDealt, out float selfDamage)
    {
        damageDealt = 0;
        selfDamage = 0;

        if (!victim.IsAlive || !IsAlive)
        {
            return false;
        }

        // Motivation Check: Is it food or a threat?
        if (!IsPreyingOn(ref victim) && !IsThreatenedBy(ref victim))
        {
            return false;
        }

        const float baseDamage = 10.0f; // Buffed damage (was 7.5f)
        var damage = baseDamage * Power / victim.Resilience;

        // Victim loses energy
        // Attacker gains energy (Carnivory!)
        if (Diet == DietType.Carnivore)
        {
            // Law of Thermodynamics: You cannot eat 20 energy if the victim only has 1.
            damage = Math.Min(damage, victim.Energy);

            victim.ChangeEnergy(-damage, gridMap);
            ChangeEnergy(damage * 1.0f, gridMap); // 100% Efficiency for specialized carnivores
        }
        else if (Diet == DietType.Omnivore)
        {
            // Omnivores are now more efficient at hunting (was 0.25/0.1)
            victim.ChangeEnergy(-damage * 0.5f, gridMap);
            // Reduced efficiency for Omnivores (was 0.3f) to curb predation on herbivores
            ChangeEnergy(damage * 0.2f, gridMap);
        }
        else
        {
            victim.ChangeEnergy(-damage * 0.1f, gridMap);
        }

        damageDealt = damage;

        if (victim.IsAlive)
        {
            // Retaliation chance
            float retaliationChance = (victim.Bravery + victim.Perception) * 0.5f;
            if (retaliationChance > 0.1f) // Minimum chance to retaliate
            {
                var retaliationDamage = baseDamage * victim.Power / Resilience;
                ChangeEnergy(-retaliationDamage * 0.2f, gridMap); // Retaliation is less effective
                selfDamage = retaliationDamage * 0.2f;

                // Visual Feedback for Retaliation (Victim hits back!)
                // The victim is attacking US (the attacker).
                // Direction is -dx, -dy relative to the victim.
                victim.LastAttackDirX = (sbyte)-dx;
                victim.LastAttackDirY = (sbyte)-dy;
                victim.AttackVisualTimer = 15;
            }
        }

        // Visual Feedback for Attacker
        LastAttackDirX = (sbyte)dx;
        LastAttackDirY = (sbyte)dy;
        AttackVisualTimer = 15;

        return true;
    }

    public struct AttackResult
    {
        public bool Success;
        public float DamageDealt;
        public long TargetId;
        public string TargetType;
        public float SelfDamage; // Retaliation
    }
}
