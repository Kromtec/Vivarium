using System;
using System.IO;
using Vivarium.Biology;
using Vivarium.Entities;

namespace Vivarium.Engine;

public static class StatsLogger
{
    public static void LogStats(Simulation simulation, string logFilePath)
    {
        int herbs = 0;
        int omnis = 0;
        int carnis = 0;

        Span<Agent> agents = simulation.AgentPopulation.AsSpan();
        for (int i = 0; i < agents.Length; i++)
        {
            if (agents[i].IsAlive)
            {
                switch (agents[i].Diet)
                {
                    case DietType.Herbivore: herbs++; break;
                    case DietType.Omnivore: omnis++; break;
                    case DietType.Carnivore: carnis++; break;
                }
            }
        }

        string line = $"{simulation.TickCount},{simulation.AliveAgents},{herbs},{omnis},{carnis},{simulation.AlivePlants},{simulation.AliveStructures}";

        // Append to file
        File.AppendAllText(logFilePath, line + Environment.NewLine);
    }
}
