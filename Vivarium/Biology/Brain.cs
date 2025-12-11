using System;
using Vivarium.Entities;
using Vivarium.World;

namespace Vivarium.Biology;

public static class Brain
{
    public static void Think(ref Agent agent, GridCell[,] gridMap, Random rng)
    {
        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

        // --- 1. MEMORY MANAGEMENT ---

        // A) Clear only the Output section (Actions)
        Array.Clear(agent.NeuronActivations, BrainConfig.ActionsStart, BrainConfig.ActionCount);

        // B) Decay Hidden Neurons:
        // Instead of keeping them 100% (Infinite Accumulation) or clearing them 0% (No Memory),
        // we multiply them by a factor.
        // 0.0 = No Memory (Reactive), 0.9 = Long Memory, 0.5 = Balanced
        const float decayFactor = 0.5f;
        // Iterate from HiddenStart to end of array
        for (int i = BrainConfig.HiddenStart; i < BrainConfig.NeuronCount; i++)
        {
            agent.NeuronActivations[i] *= decayFactor;
        }

        var neurons = agent.NeuronActivations;

        // --- 2. SENSORS (Inputs) ---
        // Direct mapping since Sensors start at index 0

        neurons[(int)SensorType.LocationX] = (float)agent.X / gridWidth;
        neurons[(int)SensorType.LocationY] = (float)agent.Y / gridHeight;
        neurons[(int)SensorType.Random] = (float)rng.NextDouble();
        neurons[(int)SensorType.Energy] = agent.Energy / 100f;
        neurons[(int)SensorType.Hunger] = agent.Hunger / 100f;
        neurons[(int)SensorType.Age] = Math.Min(agent.Age / 2000f, 1.0f);
        neurons[(int)SensorType.Oscillator] = MathF.Sin(agent.Age * 0.1f);

        var density = WorldSensor.ScanLocalArea(gridMap, agent.X, agent.Y, radius: 2);

        neurons[(int)SensorType.AgentDensity] = density.AgentDensity;
        neurons[(int)SensorType.PlantDensity] = density.PlantDensity;
        neurons[(int)SensorType.StructureDensity] = density.StructureDensity;

        // Trait sensors (derived from genome and precomputed on the Agent)
        neurons[(int)SensorType.Strength] = agent.Strength; // -1 .. +1
        neurons[(int)SensorType.Bravery] = agent.Bravery; // -1 .. +1
        neurons[(int)SensorType.MetabolicEfficiency] = agent.MetabolicEfficiency; // -1 .. +1
        neurons[(int)SensorType.Perception] = agent.Perception; // -1 .. +1
        neurons[(int)SensorType.Speed] = agent.Speed; // -1 .. +1
        neurons[(int)SensorType.TrophicBias] = agent.TrophicBias; // -1 .. +1 (carnivore->herbivore)

        // Directional sensors (8-way). Perception influences effective radius.
        const int baseRadius = 2;
        const int extraRange = 2; // max additional radius from perception
        int dirRadius = Math.Clamp(baseRadius + (int)MathF.Round(agent.Perception * extraRange), 1, 6);

        var dir = WorldSensor.ScanDirectional(gridMap, agent.X, agent.Y, dirRadius);

        // Slightly amplify signals for higher perception (but keep bounded)
        float amp = (float)Math.Clamp(1f + agent.Perception * 0.5f, 0.5f, 2f);

        // Agent densities N, NE, E, SE, S, SW, W, NW
        neurons[(int)SensorType.AgentDensity_N] = dir.AgentByDir[0] * amp;
        neurons[(int)SensorType.AgentDensity_NE] = dir.AgentByDir[1] * amp;
        neurons[(int)SensorType.AgentDensity_E] = dir.AgentByDir[2] * amp;
        neurons[(int)SensorType.AgentDensity_SE] = dir.AgentByDir[3] * amp;
        neurons[(int)SensorType.AgentDensity_S] = dir.AgentByDir[4] * amp;
        neurons[(int)SensorType.AgentDensity_SW] = dir.AgentByDir[5] * amp;
        neurons[(int)SensorType.AgentDensity_W] = dir.AgentByDir[6] * amp;
        neurons[(int)SensorType.AgentDensity_NW] = dir.AgentByDir[7] * amp;

        // Plant densities
        neurons[(int)SensorType.PlantDensity_N] = dir.PlantByDir[0] * amp;
        neurons[(int)SensorType.PlantDensity_NE] = dir.PlantByDir[1] * amp;
        neurons[(int)SensorType.PlantDensity_E] = dir.PlantByDir[2] * amp;
        neurons[(int)SensorType.PlantDensity_SE] = dir.PlantByDir[3] * amp;
        neurons[(int)SensorType.PlantDensity_S] = dir.PlantByDir[4] * amp;
        neurons[(int)SensorType.PlantDensity_SW] = dir.PlantByDir[5] * amp;
        neurons[(int)SensorType.PlantDensity_W] = dir.PlantByDir[6] * amp;
        neurons[(int)SensorType.PlantDensity_NW] = dir.PlantByDir[7] * amp;

        // Structure densities
        neurons[(int)SensorType.StructureDensity_N] = dir.StructureByDir[0] * amp;
        neurons[(int)SensorType.StructureDensity_NE] = dir.StructureByDir[1] * amp;
        neurons[(int)SensorType.StructureDensity_E] = dir.StructureByDir[2] * amp;
        neurons[(int)SensorType.StructureDensity_SE] = dir.StructureByDir[3] * amp;
        neurons[(int)SensorType.StructureDensity_S] = dir.StructureByDir[4] * amp;
        neurons[(int)SensorType.StructureDensity_SW] = dir.StructureByDir[5] * amp;
        neurons[(int)SensorType.StructureDensity_W] = dir.StructureByDir[6] * amp;
        neurons[(int)SensorType.StructureDensity_NW] = dir.StructureByDir[7] * amp;

        // --- 3. PROCESS GENOME ---
        foreach (var gene in agent.Genome)
        {
            // Safety modulo ensures we stay within valid array bounds [0..NeuronCount-1]
            int sourceIdx = gene.SourceId % BrainConfig.NeuronCount;
            int sinkIdx = gene.SinkId % BrainConfig.NeuronCount;

            // Feed Forward
            neurons[sinkIdx] += neurons[sourceIdx] * gene.Weight;
        }

        // --- 4. ACTIVATION ---
        // Apply Tanh to everything AFTER the sensors (Actions + Hidden)
        for (int i = BrainConfig.ActionsStart; i < neurons.Length; i++)
        {
            neurons[i] = MathF.Tanh(neurons[i]);
        }
    }

    // Executes the actions based on the brain's output
    public static void Act(ref Agent agent, GridCell[,] gridMap, Random rng,
        Span<Agent> agentPopulationSpan,
        Span<Plant> plantPopulationSpan)
    {
        var neurons = agent.NeuronActivations;
        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

        // --- ACCESSING ACTIONS VIA OFFSET ---
        // Helper function for cleaner reading
        float GetAction(ActionType type) => neurons[BrainConfig.GetActionIndex(type)];

        // --- 1. REPRODUCTION DECISION ---
        // The agent decides if it wants to invest energy in offspring.
        // We use a threshold (0.0 means "neutral", so > 0 is "yes").
        if(GetAction(ActionType.Reproduce) > 0.0f && agent.TryReproduce(agentPopulationSpan, gridMap, rng))
        {
            return;
        }

        // 2. SPECIAL ACTIONS
        if (GetAction(ActionType.KillSelf) > 0.9f && agent.Age > Agent.MaturityAge * 2)
        {
            agent.ChangeEnergy(-100f, gridMap);
            return;
        }

        if (GetAction(ActionType.Attack) > agent.AttackThreshold &&
            TryPerformAreaAttack(ref agent, gridMap, agentPopulationSpan, plantPopulationSpan))
        {
            agent.ChangeEnergy(-2.0f, gridMap);
            return;
        }

        // 3. MOVEMENT
        int moveX = 0;
        int moveY = 0;

        if (GetAction(ActionType.MoveNorth) > agent.MovementThreshold) moveY--;

        if (GetAction(ActionType.MoveSouth) > agent.MovementThreshold) moveY++;

        if (GetAction(ActionType.MoveWest) > agent.MovementThreshold) moveX--;

        if (GetAction(ActionType.MoveEast) > agent.MovementThreshold) moveX++;

        moveX = Math.Clamp(moveX, -1, 1);
        moveY = Math.Clamp(moveY, -1, 1);

        // Apply Movement
        if (moveX != 0 || moveY != 0)
        {
            // PAC-MAN LOGIC: Wrap around edges
            // We add gridWidth before modulo to handle negative numbers correctly in C#
            // -1 becomes (width - 1)
            int pendingX = (agent.X + moveX + gridWidth) % gridWidth;
            int pendingY = (agent.Y + moveY + gridHeight) % gridHeight;


            // is within grid and target cell is empty
            if (gridMap[pendingX, pendingY] == GridCell.Empty)
            {
                MoveToLocation(ref agent, gridMap, pendingX, pendingY, moveX, moveY);
                return;
            }
            // a plant occupies the target cell
            else if (gridMap[pendingX, pendingY].Type == EntityType.Plant)
            {
                ref Plant plant = ref plantPopulationSpan[gridMap[pendingX, pendingY].Index];
                TryAttackPlant(ref agent, ref plant, gridMap);

                if (!plant.IsAlive)
                {
                    MoveToLocation(ref agent, gridMap, pendingX, pendingY, moveX, moveY);
                }
                return;
            }
            // cell occupied by another agent - attack
            else if (gridMap[pendingX, pendingY].Type == EntityType.Agent)
            {
                int victimIndex = gridMap[pendingX, pendingY].Index;
                ref Agent victim = ref agentPopulationSpan[victimIndex];
                TryAttackAgent(ref agent, ref victim, gridMap);

                if (!victim.IsAlive)
                {
                    MoveToLocation(ref agent, gridMap, pendingX, pendingY, moveX, moveY);
                }
                return;
            }
            else if (gridMap[pendingX, pendingY].Type == EntityType.Structure)
            {
                // Slamming into a structure costs energy
                bool isDiagonal = (moveX != 0 && moveY != 0);
                float cost = isDiagonal ? Agent.DiagonalMovementCost : Agent.OrthogonalMovementCost; // cost * sqrt(2) approx
                agent.ChangeEnergy(-cost, gridMap);
                return;
            }
        }
        // Regenerate if no action taken and properly fed
        if (agent.Hunger < 10f)
        {
            agent.ChangeEnergy(agent.MetabolismRate, gridMap);
        }
    }

    private static bool TryPerformAreaAttack(ref Agent attacker, GridCell[,] gridMap, Span<Agent> agentPopulation, Span<Plant> plantPopulation)
    {
        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

        bool attackedSomething = false;
        // Iterate over 3x3 grid centered on attacker
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue; // Don't attack self

                int nx = attacker.X + dx;
                int ny = attacker.Y + dy;

                // Bounds check
                if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight)
                {
                    // Is there a victim?
                    if (gridMap[nx, ny].Type == EntityType.Agent)
                    {
                        int victimIndex = gridMap[nx, ny].Index;
                        ref Agent victim = ref agentPopulation[victimIndex];
                        if (TryAttackAgent(ref attacker, ref victim, gridMap))
                        {
                            attackedSomething = true;
                        }
                    }
                    else if (gridMap[nx, ny].Type == EntityType.Plant)
                    {
                        int plantIndex = gridMap[nx, ny].Index;
                        ref Plant plant = ref plantPopulation[plantIndex];
                        if (TryAttackPlant(ref attacker, ref plant, gridMap))
                        {
                            attackedSomething = true;
                        }
                    }
                }
            }
        }
        return attackedSomething;
    }

    private static bool TryAttackPlant(ref Agent attacker, ref Plant plant, GridCell[,] gridMap)
    {
        if (!plant.IsAlive || !attacker.IsAlive)
        {
            return false;
        }
        const float baseDamage = 15f;
        var power = 1.0f + (attacker.Strength * 0.5f); // 0.5x to 1.5x damage based on Strength trait
        var damage = baseDamage * power;

        // Plant loses energy
        // Attacker gains energy (Herbivory!)
        if (attacker.Diet == DietType.Herbivore)
        {
            plant.ChangeEnergy(-damage, gridMap);
            attacker.Eat(damage * 0.8f);
        }
        else if (attacker.Diet == DietType.Omnivore)
        {
            plant.ChangeEnergy(-damage * 0.25f, gridMap);
            attacker.Eat(damage * 0.1f);
        }
        else
        {
            plant.ChangeEnergy(-damage * 0.1f, gridMap);
        }
        return true;
    }

    private static bool TryAttackAgent(ref Agent attacker, ref Agent victim, GridCell[,] gridMap)
    {
        if (!victim.IsAlive || !attacker.IsAlive)
        {
            return false;
        }
        if (attacker.IsDirectlyRelatedTo(ref victim))
        {
            return false; // No friendly fire
        }
        if (attacker.Bravery < victim.Bravery)
        {
            return false; // Too craven to attack an opponent that looks braver
        }

        const float baseDamage = 7.5f;
        var damage = baseDamage * attacker.Power / victim.Resilience;

        // Victim loses energy
        // Attacker gains energy (Carnivory!)
        if (attacker.Diet == DietType.Carnivore)
        {
            victim.ChangeEnergy(-damage, gridMap);
            attacker.Eat(damage * 0.8f);
        }
        else if (attacker.Diet == DietType.Omnivore)
        {
            victim.ChangeEnergy(-damage * 0.25f, gridMap);
            attacker.Eat(damage * 0.05f);
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
                var retaliationDamage = baseDamage * victim.Power / attacker.Resilience;
                attacker.ChangeEnergy(-retaliationDamage * 0.2f, gridMap); // Retaliation is less effective

                if(!attacker.IsAlive)
                {
                    // Attacker died from retaliation - victim gets bonus energy
                    if (victim.Diet == DietType.Carnivore)
                        victim.Eat(retaliationDamage * 0.8f);
                    else if (victim.Diet == DietType.Omnivore)
                        victim.Eat(retaliationDamage * 0.05f);
                }
            }
        }
        else
        {
            // Victim died - attacker gets bonus energy
            if (attacker.Diet == DietType.Carnivore)
                attacker.Eat(damage * 0.8f);
            else if (attacker.Diet == DietType.Omnivore)
                attacker.Eat(damage * 0.05f);
        }
        return true;
    }

    private static void MoveToLocation(ref Agent agent, GridCell[,] gridMap, int pendingX, int pendingY, int dx, int dy)
    {
        if (!agent.IsAlive)
        {
            return;
        }

        // clear old location
        if (gridMap[agent.X, agent.Y].Type == EntityType.Agent && gridMap[agent.X, agent.Y].Index == agent.Index)
        {
            gridMap[agent.X, agent.Y] = GridCell.Empty;
        }

        // 2. --- THE TRAP (Debug Trap) ---
        // We check BEFORE writing whether we're about to kill someone.
        GridCell target = gridMap[pendingX, pendingY];

        // If the target is an agent (and not ourselves)...
        if (target.Type == EntityType.Agent && target.Index != agent.Index)
        {
            // ... then we've found a bug in Act()!
            // Act() told us "Go there" even though it's occupied.
            throw new Exception($"FATAL ERROR: Agent #{agent.Index} is overwriting living Agent #{target.Index} at {pendingX},{pendingY}!\n" +
                                $"This means 'gridMap[{pendingX},{pendingY}] == Empty' was TRUE even though an agent was there.");
        }
        else if (target.Type == EntityType.Plant)
        {
            throw new Exception($"FATAL ERROR: Agent #{agent.Index} is overwriting living Plant #{target.Index} at {pendingX},{pendingY}!\n" +
                                $"This means 'gridMap[{pendingX},{pendingY}] == Empty' was TRUE even though an plant was there.");
        }
        else if (target.Type == EntityType.Structure)
        {
            throw new Exception($"FATAL ERROR: Agent #{agent.Index} is overwriting Structure #{target.Index} at {pendingX},{pendingY}!\n" +
                                $"This means 'gridMap[{pendingX},{pendingY}] == Empty' was TRUE even though a structure was there.");
        }
        // move to new location
        agent.X = pendingX;
        agent.Y = pendingY;
        gridMap[pendingX, pendingY] = new(EntityType.Agent, agent.Index);

        // Calculate Movement Cost
        // Orthogonal move (0,1) or (1,0) length is 1.
        // Diagonal move (1,1) length is approx 1.414.
        // We penalize diagonal movement correctly to preserve physics.
        bool isDiagonal = (dx != 0 && dy != 0);
        float cost = isDiagonal ? Agent.DiagonalMovementCost : Agent.OrthogonalMovementCost; // cost * sqrt(2) approx

        agent.ChangeEnergy(-cost, gridMap);
    }
}