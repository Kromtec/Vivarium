using System;
using Vivarium.Biology;
using Vivarium.Entities;

namespace Vivarium.Engine;

public static class StatsLogger
{
    public struct DietCounts
    {
        public int Herbivores;
        public int Omnivores;
        public int Carnivores;
    }

    public static DietCounts LogStats(Simulation simulation)
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
        return new DietCounts { Herbivores = herbs, Omnivores = omnis, Carnivores = carnis };
    }
}
