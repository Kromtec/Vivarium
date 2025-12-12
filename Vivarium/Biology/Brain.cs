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
        neurons[(int)SensorType.Constitution] = agent.Constitution; // -1 .. +1

        // Directional sensors (8-way). Perception influences effective radius.
        const int baseRadius = 2;
        const int extraRange = 2; // max additional radius from perception
        int dirRadius = Math.Clamp(baseRadius + (int)MathF.Round(agent.Perception * extraRange), 1, 6);

        // Slightly amplify signals for higher perception (but keep bounded)
        float amp = (float)Math.Clamp(1f + agent.Perception * 0.5f, 0.5f, 2f);

        // --- OPTIMIZATION: Zero-Alloc Direct Write ---
        // We pass the offsets for the N (North) sensor. The function assumes N, NE, E... follow mainly.
        // Based on SensorType enum, the directions are sequential:
        // AgentDensity_N, AgentDensity_NE...
        
        WorldSensor.PopulateDirectionalSensors(
            gridMap, 
            agent.X, 
            agent.Y, 
            dirRadius, 
            neurons, 
            (int)SensorType.AgentDensity_N,
            (int)SensorType.PlantDensity_N,
            (int)SensorType.StructureDensity_N,
            amp
        );

        // --- 3. PROCESS GENOME ---
        // OPTIMIZATION: Manual Inlining
        // We unpack the DNA manually to avoid Property Access overhead in Debug builds.
        foreach (var gene in agent.Genome)
        {
            uint dna = gene.Dna;

            // Unpack directly (Matches Gene.cs logic)
            // Source = Dna & 0xFF
            // Sink = (Dna >> 8) & 0xFF
            int sourceIdx = (int)(dna & 0xFF);
            int sinkIdx = (int)((dna >> 8) & 0xFF);

            // Weight = (short)(Dna >> 16) / 8192.0f
            float weight = ((short)(dna >> 16)) / 8192.0f;

            // Feed Forward
            neurons[sinkIdx] += neurons[sourceIdx] * weight;
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
        if (GetAction(ActionType.Suicide) > 0.9f && agent.Age > Agent.MaturityAge * 2)
        {
            agent.ChangeEnergy(-100f, gridMap);
            return;
        }

        if (GetAction(ActionType.Attack) > agent.AttackThreshold &&
            agent.TryPerformAreaAttack(gridMap, agentPopulationSpan, plantPopulationSpan, rng))
        {
            return;
        }

        // 3. SPECIAL DECISION: FLEE
        // If Flee neuron fires strongly, we override normal movement to evade threats.
        // Rules:
        // - Ignore Parents/Children (Kinship)
        // - Ignore Herbivores if I am a Herbivore (Diet)
        // - Ignore PREY if I am a Predator (Don't run from food!)
        if (GetAction(ActionType.Flee) > agent.MovementThreshold) 
        {
            int vecX = 0;
            int vecY = 0;
            bool fleeing = false;

            // Scan neighborhood (radius 2 for hysteresis)
            // Radius 2 ensures we keep running until we have a 1-tile buffer
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = agent.X + dx;
                    int ny = agent.Y + dy;

                    if (nx < 0 || ny < 0 || nx >= gridWidth || ny >= gridHeight) continue;

                    GridCell cell = gridMap[nx, ny];
                    if (cell.Type == EntityType.Agent)
                    {
                        ref Agent other = ref agentPopulationSpan[cell.Index];
                        if (!other.IsAlive) continue;

                        // Constraint 1: Kinship (Don't flee from family)
                        if (agent.IsDirectlyRelatedTo(ref other)) continue;

                        // Constraint 2: Diet (Herbivores don't flee from Herbivores)
                        if (agent.Diet == DietType.Herbivore && other.Diet == DietType.Herbivore) continue;

                        // Constraint 3: Predation (Carnivores && Omnivores don't flee from Herbivores)
                        if ((agent.Diet == DietType.Carnivore || agent.Diet == DietType.Omnivore) && other.Diet == DietType.Herbivore) continue;

                        // Constraint 4: Bravery Check (Braver than threat, don't flee)
                        if (agent.Bravery > other.Bravery)  continue;

                        // EVADE!
                        // Add vector pointing AWAY from neighbor.
                        // We use Sign() so distance doesn't increase repulsion (which would be backwards).
                        // A threat at dist 1 and dist 2 both contribute "1 unit" of fear direction.
                        vecX -= Math.Sign(dx);
                        vecY -= Math.Sign(dy);
                        fleeing = true;
                    }
                }
            }

            if (fleeing)
            {
                // Add "Panic Jitter" (RNG) to prevent predictable loops
                // This converts strict orthogonal fleeing into potential diagonal fleeing
                vecX += rng.Next(-1, 2);
                vecY += rng.Next(-1, 2);

                // Normalize and Execute
                int fleeMoveX = Math.Clamp(vecX, -1, 1);
                int fleeMoveY = Math.Clamp(vecY, -1, 1);
                
                if (fleeMoveX != 0 || fleeMoveY != 0)
                {
                    // Calculate target position (Pac-Man Wrap)
                    int pendingX = (agent.X + fleeMoveX + gridWidth) % gridWidth;
                    int pendingY = (agent.Y + fleeMoveY + gridHeight) % gridHeight;

                    // Only move if empty (Don't attack while fleeing)
                    if (gridMap[pendingX, pendingY] == GridCell.Empty)
                    {
                        agent.TryMoveToLocation(gridMap, pendingX, pendingY, fleeMoveX, fleeMoveY, Agent.FleeCost);
                    }
                }
                return;
            }
        }

        // 3. MOVEMENT
        int moveX = 0;
        int moveY = 0;

        if (GetAction(ActionType.MoveN) > agent.MovementThreshold) moveY--;

        if (GetAction(ActionType.MoveS) > agent.MovementThreshold) moveY++;

        if (GetAction(ActionType.MoveW) > agent.MovementThreshold) moveX--;

        if (GetAction(ActionType.MoveE) > agent.MovementThreshold) moveX++;

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
                if(agent.TryMoveToLocation(gridMap, pendingX, pendingY, moveX, moveY))
                {
                    return;
                }
            }
            // a plant occupies the target cell
            else if (gridMap[pendingX, pendingY].Type == EntityType.Plant)
            {
                ref Plant plant = ref plantPopulationSpan[gridMap[pendingX, pendingY].Index];
                agent.TryAttackPlant(ref plant, gridMap);

                if (!plant.IsAlive)
                {
                    if (agent.TryMoveToLocation(gridMap, pendingX, pendingY, moveX, moveY))
                    {
                        return;
                    }
                }
            }
            // cell occupied by another agent - attack
            else if (gridMap[pendingX, pendingY].Type == EntityType.Agent)
            {
                int victimIndex = gridMap[pendingX, pendingY].Index;
                ref Agent victim = ref agentPopulationSpan[victimIndex];
                agent.TryAttackAgent(ref victim, gridMap);

                if (!victim.IsAlive)
                {
                    if (agent.TryMoveToLocation(gridMap, pendingX, pendingY, moveX, moveY))
                    {
                        return;
                    }
                }
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


}