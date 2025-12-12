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