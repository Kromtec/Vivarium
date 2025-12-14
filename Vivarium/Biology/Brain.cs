using System;
using Vivarium.Entities;
using Vivarium.World;
using Vivarium.UI;

namespace Vivarium.Biology;

public static class Brain
{
    public static void Think(ref Agent agent, GridCell[,] gridMap, Random rng, Agent[] agentPopulation)
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

        neurons[(int)SensorType.LocationX] = ((float)(agent.X / gridWidth) * 2) - 1.0f; // -1 .. +1
        neurons[(int)SensorType.LocationY] = ((float)agent.Y / gridHeight * 2) - 1.0f;  // -1 .. +1
        neurons[(int)SensorType.Random] = (float)rng.NextDouble();                      //  0 .. +1
        neurons[(int)SensorType.Energy] = agent.Energy / agent.MaxEnergy;               //  0 .. +1
        neurons[(int)SensorType.Age] = Math.Min(agent.Age / 2000f, 1.0f);
        neurons[(int)SensorType.Oscillator] = MathF.Sin(agent.Age * 0.1f);

        var density = WorldSensor.ScanLocalArea(gridMap, agent.X, agent.Y, radius: 2);

        neurons[(int)SensorType.AgentDensity] = density.AgentDensity;                   //  0 .. +1
        neurons[(int)SensorType.PlantDensity] = density.PlantDensity;                   //  0 .. +1
        neurons[(int)SensorType.StructureDensity] = density.StructureDensity;           //  0 .. +1

        // Directional sensors (8-way). Perception influences effective radius.
        const int baseRadius = 2;
        const int extraRange = 2; // max additional radius from perception
        int dirRadius = Math.Clamp(baseRadius + (int)MathF.Round(agent.Perception * extraRange), 1, 6);

        // --- OPTIMIZATION: Zero-Alloc Direct Write ---
        // We pass the offsets for the N (North) sensor. The function assumes N, NE, E... follow mainly.
        // Based on SensorType enum, the directions are sequential:
        // AgentDensity_N, AgentDensity_NE...
        WorldSensor.PopulateDirectionalSensors(                                         //  0 .. +1
            gridMap,
            agent.X,
            agent.Y,
            dirRadius,
            neurons,
            (int)SensorType.AgentDensity_N,
            (int)SensorType.PlantDensity_N,
            (int)SensorType.StructureDensity_N
        );

        // Trait sensors (derived from genome and precomputed on the Agent)
        neurons[(int)SensorType.Strength] = agent.Strength;                             // -1 .. +1
        neurons[(int)SensorType.Bravery] = agent.Bravery;                               // -1 .. +1
        neurons[(int)SensorType.MetabolicEfficiency] = agent.MetabolicEfficiency;       // -1 .. +1
        neurons[(int)SensorType.Perception] = agent.Perception;                         // -1 .. +1
        neurons[(int)SensorType.Speed] = agent.Speed;                                   // -1 .. +1
        neurons[(int)SensorType.TrophicBias] = agent.TrophicBias;                       // -1 .. +1 (carnivore->herbivore)
        neurons[(int)SensorType.Constitution] = agent.Constitution;                     // -1 .. +1

        // --- 3. PROCESS GENOME ---
        foreach (var gene in agent.Genome)
        {
            // Safety modulo ensures we stay within valid array bounds [0..NeuronCount-1]
            int sourceIdx = gene.SourceId % BrainConfig.NeuronCount;

            // Wrap Sink to Actions + Hidden only
            int sinkIdx = (gene.SinkId % (BrainConfig.NeuronCount - BrainConfig.ActionsStart)) + BrainConfig.ActionsStart;

            // Feed Forward
            neurons[sinkIdx] += neurons[sourceIdx] * gene.Weight;
        }

        // --- 3.5 INSTINCTS (Biological Overrides)
        // We apply strong biases based on critical survival needs.
        // These are applied BEFORE activation, so they can be modulated by the network but provide a strong baseline.
        ApplyInstincts(ref agent, gridMap, rng, neurons, agentPopulation);

        // --- 4. ACTIVATION ---
        // Apply Tanh to everything AFTER the sensors (Actions + Hidden)
        for (int i = BrainConfig.ActionsStart; i < neurons.Length; i++)
        {
            neurons[i] = MathF.Tanh(neurons[i]);
        }
    }

    private static void ApplyInstincts(ref Agent agent, GridCell[,] gridMap, Random rng, float[] neurons, Agent[] agentPopulation)
    {
        // Helper to add bias to one action and suppress all others
        void ApplyDominantBias(ActionType targetType, float amount)
        {
            int targetIndex = BrainConfig.GetActionIndex(targetType);
            int start = BrainConfig.ActionsStart;
            int end = start + BrainConfig.ActionCount;

            for (int i = start; i < end; i++)
            {
                if (i == targetIndex)
                {
                    neurons[i] += amount;
                }
                else
                {
                    neurons[i] -= amount;
                }
            }
        }

        // 1. SURVIVAL INSTINCT (Panic)
        // If threats are nearby, run away!
        // We do a quick scan for threats.
        if (WorldSensor.DetectThreats(gridMap, agentPopulation, agent.X, agent.Y, 2, ref agent))
        {
            ApplyDominantBias(ActionType.Flee, 2.0f);
            ActivityLog.Log(agent.Id, "Instinct: Panic! Threat detected nearby. Urge to flee.");
            return; // Priority 1: Survival overrides everything
        }

        // 2. FEEDING INSTINCT (Energy)
        // If low energy, seek food.
        if (agent.Energy < agent.MaxEnergy * 0.6f)
        {
            var dietDecision = rng.NextDouble();
            if (agent.Diet == DietType.Herbivore || (agent.Diet == DietType.Omnivore && dietDecision >= 0.5f))
            {
                // Move towards plants
                // We check the directional sensors we just populated.
                // Note: This assumes the sensors are populated in the standard order (N, NE, E...)
                // We find the direction with the highest plant density.

                int bestDir = -1;
                float maxDensity = 0f;

                // Check cardinal directions first for simple movement
                // N=0, E=2, S=4, W=6 in the 8-way array
                float n = neurons[(int)SensorType.PlantDensity_N];
                float e = neurons[(int)SensorType.PlantDensity_E];
                float s = neurons[(int)SensorType.PlantDensity_S];
                float w = neurons[(int)SensorType.PlantDensity_W];

                if (n > maxDensity) { maxDensity = n; bestDir = 0; }
                if (e > maxDensity) { maxDensity = e; bestDir = 1; }
                if (s > maxDensity) { maxDensity = s; bestDir = 2; }
                if (w > maxDensity) { maxDensity = w; bestDir = 3; }

                string biasDir = "Randomly";
                if (bestDir == -1)
                {
                    bestDir = rng.Next(0, 4); // Random direction if no food detected
                }
                else
                {
                    // Map bestDir index to cardinal direction name
                    if (bestDir == 0) biasDir = "North";
                    else if (bestDir == 1) biasDir = "East";
                    else if (bestDir == 2) biasDir = "South";
                    else if (bestDir == 3) biasDir = "West";
                }

                if (bestDir == 0) ApplyDominantBias(ActionType.MoveN, 2.0f);
                else if (bestDir == 1) ApplyDominantBias(ActionType.MoveE, 2.0f);
                else if (bestDir == 2) ApplyDominantBias(ActionType.MoveS, 2.0f);
                else if (bestDir == 3) ApplyDominantBias(ActionType.MoveW, 2.0f);

                ActivityLog.Log(agent.Id, $"Instinct: Low Energy. Seeking Plants towards {biasDir}.");
                return; // Priority 2: Food
            }
            else if (agent.Diet == DietType.Carnivore || (agent.Diet == DietType.Omnivore && dietDecision < 0.5f))
            {
                // Move towards prey (Agents)
                // Similar logic but for AgentDensity
                float n = neurons[(int)SensorType.AgentDensity_N];
                float e = neurons[(int)SensorType.AgentDensity_E];
                float s = neurons[(int)SensorType.AgentDensity_S];
                float w = neurons[(int)SensorType.AgentDensity_W];

                int bestDir = -1;
                float maxDensity = 0f;

                if (n > maxDensity) { maxDensity = n; bestDir = 0; }
                if (e > maxDensity) { maxDensity = e; bestDir = 1; }
                if (s > maxDensity) { maxDensity = s; bestDir = 2; }
                if (w > maxDensity) { maxDensity = w; bestDir = 3; }

                string biasDir = "Randomly";
                if (bestDir == -1)
                {
                    bestDir = rng.Next(0, 4); // Random direction if no plants detected
                }
                else
                {
                    if (bestDir == 0) biasDir = "North";
                    else if (bestDir == 1) biasDir = "East";
                    else if (bestDir == 2) biasDir = "South";
                    else if (bestDir == 3) biasDir = "West";
                }

                if (bestDir == 0) ApplyDominantBias(ActionType.MoveN, 2.0f);
                else if (bestDir == 1) ApplyDominantBias(ActionType.MoveE, 2.0f);
                else if (bestDir == 2) ApplyDominantBias(ActionType.MoveS, 2.0f);
                else if (bestDir == 3) ApplyDominantBias(ActionType.MoveW, 2.0f);

                // Also encourage attacking if we smell food
                // We need to counteract the suppression (-2.0) and add bias (+2.0) -> +4.0 total
                neurons[BrainConfig.GetActionIndex(ActionType.Attack)] += 4.0f;

                ActivityLog.Log(agent.Id, $"Instinct: Low Energy. Hunting Prey towards {biasDir}.");
                return; // Priority 2: Food
            }
        }

        // 3. REPRODUCTION INSTINCT (Libido)
        // If healthy and mature, try to reproduce.
        if (agent.Energy > agent.MaxEnergy * 0.9f && agent.Age > Agent.MaturityAge && agent.ReproductionCooldown == 0)
        {
            ApplyDominantBias(ActionType.Reproduce, 2.0f);
            ActivityLog.Log(agent.Id, $"Instinct: Libido. Healthy & Mature. Urge to reproduce.");
        }
    }

    // Executes the actions based on the brain's output
    public static void Act(ref Agent agent, GridCell[,] gridMap, Random rng,
        Span<Agent> agentPopulationSpan,
        Span<Plant> plantPopulationSpan)
    {

        // Metabolize energy (Entropy)
        // Constant energy loss per frame
        agent.ChangeEnergy(-agent.MetabolismRate, gridMap);
        ActivityLog.Log(agent.Id, $"Entropy: Metabolized {agent.MetabolismRate:F2} Energy.");

        var neurons = agent.NeuronActivations;
        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

        // --- ACCESSING ACTIONS VIA OFFSET ---
        // Helper function for cleaner reading
        float GetAction(ActionType type) => neurons[BrainConfig.GetActionIndex(type)];

        // --- 1. REPRODUCTION DECISION ---
        // The agent decides if it wants to invest energy in offspring.
        // We use a threshold (0.0 means "neutral", so > 0 is "yes").
        if (GetAction(ActionType.Reproduce) > 0.0f && agent.TryReproduce(agentPopulationSpan, gridMap, rng))
        {
            ActivityLog.Log(agent.Id, "Action: Reproduced successfully.");
            return;
        }

        // 2. SPECIAL ACTIONS
        if (GetAction(ActionType.Suicide) > 0.9f && agent.Age > Agent.MaturityAge * 2)
        {
            agent.ChangeEnergy(-100f, gridMap);
            ActivityLog.Log(agent.Id, "Action: Committed Suicide (Old Age).");
            return;
        }

        // Attack Logic
        if (GetAction(ActionType.Attack) > agent.AttackThreshold)
        {
            var result = agent.TryAreaAttack(gridMap, agentPopulationSpan, plantPopulationSpan, rng);
            if (result.Success)
            {
                // Log for Attacker
                ActivityLog.Log(agent.Id, $"Action: Attacked {result.TargetType} #{result.TargetId} in an uncoordinated swing for {result.DamageDealt:F1} damage.");

                // Log for Victim (if it was an agent)
                if (result.TargetType == "Agent")
                {
                    ActivityLog.Log(result.TargetId, $"Event: Attacked by Agent #{agent.Id} in an uncoordinated swing for {result.DamageDealt:F1} damage.");
                }

                // Log Retaliation
                if (result.SelfDamage > 0)
                {
                    ActivityLog.Log(agent.Id, $"Event: Took {result.SelfDamage:F1} retaliation damage.");
                    if (result.TargetType == "Agent")
                    {
                        ActivityLog.Log(result.TargetId, $"Action: Retaliated against Agent #{agent.Id} for {result.SelfDamage:F1} damage.");
                    }
                }
                return;
            }
        }

        // 3. SPECIAL DECISION: FLEE
        // If Flee neuron fires strongly, we override normal movement to evade threats.
        // Rules:
        // - Ignore Parents/Children (Kinship)
        // - Ignore Herbivores if I am a Herbivore (Diet)
        // - Ignore PREY if I am a Predator (Don't run from food!)
        if (GetAction(ActionType.Flee) > agent.MovementThreshold)
        {
            if (agent.TryToFlee(gridMap, agentPopulationSpan, rng, out int fleeX, out int fleeY))
            {
                string dirText = GetDirectionText(fleeX, fleeY);
                ActivityLog.Log(agent.Id, $"Action: Fled from threat to {dirText}.");
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

            string dirText = GetDirectionText(moveX, moveY);

            // is within grid and target cell is empty
            if (gridMap[pendingX, pendingY] == GridCell.Empty)
            {
                if (agent.TryMoveToLocation(gridMap, pendingX, pendingY, moveX, moveY))
                {
                    ActivityLog.Log(agent.Id, $"Action: Moved {dirText}.");
                    return;
                }
            }
            // a plant occupies the target cell
            else if (gridMap[pendingX, pendingY].Type == EntityType.Plant)
            {
                ref Plant plant = ref plantPopulationSpan[gridMap[pendingX, pendingY].Index];

                float damageDealt;
                agent.TryAttackPlant(ref plant, gridMap, moveX, moveY, out damageDealt);
                var actionText = agent.Diet != DietType.Carnivore ? "Ate" : "Trampled";
                ActivityLog.Log(agent.Id, $"Action: {actionText} Plant #{plant.Id} in the {dirText} and made {damageDealt:F1} damage.");

                if (!plant.IsAlive)
                {
                    if (agent.TryMoveToLocation(gridMap, pendingX, pendingY, moveX, moveY))
                    {
                        ActivityLog.Log(agent.Id, $"Action: Moved {dirText} after the Plant #{plant.Id} was completely destroyed.");
                        return;
                    }
                }
            }
            // cell occupied by another agent - attack
            else if (gridMap[pendingX, pendingY].Type == EntityType.Agent)
            {
                int victimIndex = gridMap[pendingX, pendingY].Index;
                ref Agent victim = ref agentPopulationSpan[victimIndex];

                float damageDealt, selfDamage;
                agent.TryAttackAgent(ref victim, gridMap, moveX, moveY, out damageDealt, out selfDamage);

                // Log for Victim
                ActivityLog.Log(victim.Id, $"Event: Attacked by Agent #{agent.Id} for {damageDealt:F1} damage.");
                if (selfDamage > 0)
                {
                    ActivityLog.Log(victim.Id, $"Action: Retaliated against Agent #{agent.Id} for {selfDamage:F1} damage.");
                }

                if (!victim.IsAlive)
                {
                    if (agent.TryMoveToLocation(gridMap, pendingX, pendingY, moveX, moveY))
                    {
                        ActivityLog.Log(agent.Id, $"Action: Killed Agent #{victim.Id} and Moved {dirText}.");
                        return;
                    }
                }

                string retalMsg = selfDamage > 0 ? $" (Took {selfDamage:F1} retaliation damage)" : "";
                ActivityLog.Log(agent.Id, $"Action: Attacked {dirText} Agent #{victim.Id} for {damageDealt:F1} damage{retalMsg}.");
            }
            else if (gridMap[pendingX, pendingY].Type == EntityType.Structure)
            {
                // Slamming into a structure costs energy
                bool isDiagonal = (moveX != 0 && moveY != 0);
                float cost = isDiagonal ? Agent.DiagonalMovementCost : Agent.OrthogonalMovementCost; // cost * sqrt(2) approx
                agent.ChangeEnergy(-cost, gridMap);
                ActivityLog.Log(agent.Id, $"Action: Hit a wall in the {dirText} and took {cost:F2} damage!");
                return;
            }
        }

        // Regenerate if no action taken and properly fed (Energy > 90%)
        if (agent.Energy > agent.MaxEnergy * 0.9f)
        {
            agent.ChangeEnergy(agent.MetabolismRate * 0.5f, gridMap);
            ActivityLog.Log(agent.Id, $"Action: Resting. Regenerated {agent.MetabolismRate:F2} Energy.");
        }
        else
        {
            ActivityLog.Log(agent.Id, "Action: Idle.");
        }
    }

    private static string GetDirectionText(int dx, int dy)
    {
        if (dx == 0 && dy == -1) return "North";
        if (dx == 0 && dy == 1) return "South";
        if (dx == -1 && dy == 0) return "West";
        if (dx == 1 && dy == 0) return "East";
        if (dx == -1 && dy == -1) return "North-West";
        if (dx == 1 && dy == -1) return "North-East";
        if (dx == -1 && dy == 1) return "South-West";
        if (dx == 1 && dy == 1) return "South-East";
        return "Unknown";
    }
}