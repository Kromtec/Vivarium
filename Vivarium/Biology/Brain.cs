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
        neurons[(int)SensorType.Age] = Math.Min(agent.Age / 2000f, 1.0f);
        neurons[(int)SensorType.Oscillator] = MathF.Sin(agent.Age * 0.1f);

        var density = WorldSensor.ScanLocalArea(gridMap, agent.X, agent.Y, radius: 2);

        neurons[(int)SensorType.AgentDensity] = density.AgentDensity;
        neurons[(int)SensorType.PlantDensity] = density.PlantDensity;
        neurons[(int)SensorType.StructureDensity] = density.StructureDensity;

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
    public static void Act(ref Agent agent, GridCell[,] gridMap, Span<Agent> agentPopulationSpan, Span<Plant> plantPopulationSpan)
    {
        var neurons = agent.NeuronActivations;
        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

        // --- ACCESSING ACTIONS VIA OFFSET ---
        // Helper function for cleaner reading
        float GetAction(ActionType type) => neurons[BrainConfig.GetActionIndex(type)];


        // 1. SPECIAL ACTIONS
        if (GetAction(ActionType.KillSelf) > 0.9f)
        {
            agent.ChangeEnergy(-100f, gridMap);
            return;
        }

        const float attackThreshold = 0.5f;
        if (GetAction(ActionType.Attack) > attackThreshold)
        {
            PerformAreaAttack(ref agent, gridMap, agentPopulationSpan, plantPopulationSpan);
            agent.ChangeEnergy(-2.0f, gridMap);
        }

        // 2. MOVEMENT
        const float moveThreshold = 0.1f;
        int moveX = 0;
        int moveY = 0;

        if (GetAction(ActionType.MoveNorth) > moveThreshold) moveY--;
        if (GetAction(ActionType.MoveNorth) < -moveThreshold) moveY++; // Bi-directional neuron support

        if (GetAction(ActionType.MoveSouth) > moveThreshold) moveY++;
        if (GetAction(ActionType.MoveSouth) < -moveThreshold) moveY--;

        if (GetAction(ActionType.MoveWest) > moveThreshold) moveX--;
        if (GetAction(ActionType.MoveWest) < -moveThreshold) moveX++;

        if (GetAction(ActionType.MoveEast) > moveThreshold) moveX++;
        if (GetAction(ActionType.MoveEast) < -moveThreshold) moveX--;

        moveX = Math.Clamp(moveX, -1, 1);
        moveY = Math.Clamp(moveY, -1, 1);

        // Apply Movement
        if (moveX != 0 || moveY != 0)
        {
            int pendingX = agent.X + moveX;
            int pendingY = agent.Y + moveY;

            // is within grid and target cell is empty
            if (pendingX >= 0 && pendingX < gridWidth &&
                pendingY >= 0 && pendingY < gridHeight)
            {

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

                    if (plant.Energy > 0)
                    {
                        plant.ChangeEnergy(-10.0f, gridMap);

                        // Eat the plant (Herbivory)
                        if (GetAction(ActionType.Attack) < attackThreshold)
                        {
                            agent.ChangeEnergy(+10.0f, gridMap);
                        }
                    }

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
                    if (victim.Energy > 0 && victim.Index != agent.ParentIndex && victim.ParentIndex != agent.Index)
                    {
                        const float damage = 15f;
                        // Victim loses energy
                        victim.ChangeEnergy(-damage, gridMap);

                        // Attacker gains energy (Carnivory!)
                        if (GetAction(ActionType.Attack) > attackThreshold)
                        {
                            agent.ChangeEnergy(+damage * 0.8f, gridMap);
                        }
                    }
                    if (!victim.IsAlive)
                    {
                        MoveToLocation(ref agent, gridMap, pendingX, pendingY, moveX, moveY);
                    }
                    return;
                }
                else if (gridMap[pendingX, pendingY].Type == EntityType.Structure)
                {
                    // Slamming into a structure costs energy
                    agent.ChangeEnergy(-1.0f, gridMap);
                    return;
                }
            }
        }
        // Resting recovers a tiny bit of energy
        agent.ChangeEnergy(+0.01f, gridMap);
    }

    private static void PerformAreaAttack(ref Agent attacker, GridCell[,] gridMap, Span<Agent> agentPopulation, Span<Plant> plantPopulation)
    {
        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

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

                        if (victim.IsAlive)
                        {
                            const float damage = 25f;

                            // Victim loses energy
                            // We use the ChangeEnergy helper to handle death logic inside agent if needed,
                            // but usually better to just reduce energy and let Update loop handle death.
                            victim.ChangeEnergy(-damage, gridMap);

                            // Attacker gains energy (Carnivory!)
                            // Thermodynamics: Gain is less than damage (e.g., 80% efficiency)
                            attacker.ChangeEnergy(+damage * 0.8f, gridMap);
                        }
                    }
                    else if (gridMap[nx, ny].Type == EntityType.Plant)
                    {
                        int plantIndex = gridMap[nx, ny].Index;
                        ref Plant plant = ref plantPopulation[plantIndex];
                        if (plant.IsAlive)
                        {
                            const float damage = 25f;
                            // Plant loses energy
                            plant.ChangeEnergy(-damage, gridMap);
                            // Attacker gains energy (Herbivory!)
                            attacker.ChangeEnergy(+damage * 0.5f, gridMap);
                        }
                    }
                }
            }
        }
    }

    private static void MoveToLocation(ref Agent agent, GridCell[,] gridMap, int pendingX, int pendingY, int dx, int dy)
    {
        // clear old location
        gridMap[agent.X, agent.Y] = GridCell.Empty;
        // move to new location
        agent.X = pendingX;
        agent.Y = pendingY;
        gridMap[pendingX, pendingY] = new(EntityType.Agent, agent.Index);

        // Calculate Movement Cost
        // Orthogonal move (0,1) or (1,0) length is 1.
        // Diagonal move (1,1) length is approx 1.414.
        // We penalize diagonal movement correctly to preserve physics.
        bool isDiagonal = (dx != 0 && dy != 0);
        float cost = isDiagonal ? 0.25f : 0.2f; // cost * sqrt(2) approx

        agent.ChangeEnergy(-cost, gridMap);
    }
}