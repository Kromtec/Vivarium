using System;
using System.IO;
using System.Runtime.InteropServices;
using Vivarium.Config;
using Vivarium.Engine;

namespace Vivarium;

public static class Program
{
    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);
    private const int ATTACH_PARENT_PROCESS = -1;

    [STAThread]
    static void Main(string[] args)
    {
        bool headless = false;
        int duration = 3600; // Default 1 minute (60 fps * 60 sec)
        int seed = 64;
        string configFile = string.Empty;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--headless")
            {
                headless = true;
            }
            else if (args[i] == "--duration" && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], out int d))
                {
                    duration = d;
                    i++;
                }
            }
            else if (args[i] == "--seed" && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], out int s))
                {
                    seed = s;
                    i++;
                }
            }
            else if (args[i] == "--config" && i + 1 < args.Length)
            {
                configFile = args[i + 1];
                i++;
            }
        }

        // Initialize configuration
        SimulationConfig config;
        if (!string.IsNullOrEmpty(configFile))
        {
            config = SimulationConfig.LoadFromFileOrDefault(configFile, seed);
            // Override seed from command line if specified and different from file
            // Note: We create a new config with the seed override using the factory
            if (config.World.Seed != seed)
            {
                config = SimulationConfig.CreateDefault(seed);
                // Try to reload mutable settings from file
                var fileConfig = SimulationConfig.LoadFromFileOrDefault(configFile, seed);
                config.Agent = fileConfig.Agent;
                config.Plant = fileConfig.Plant;
                config.Brain = fileConfig.Brain;
            }
        }
        else
        {
            config = SimulationConfig.CreateDefault(seed);
        }

        ConfigProvider.Initialize(config);

        if (headless)
        {
            // Attempt to attach to the parent console to enable output
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
            }
            RunHeadless(duration);
        }
        else
        {
            using var game = new VivariumGame();
            game.Run();
        }
    }

    static void RunHeadless(int durationTicks)
    {
        int seed = ConfigProvider.World.Seed;

        Console.WriteLine($"Starting Headless Simulation for {durationTicks} ticks with seed {seed}...");

        var simulation = new Simulation();
        simulation.Initialize();

        // Setup Logging
        const string logDir = "Logs";
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string logFile = Path.Combine(logDir, $"simulation_run_{timestamp}_{seed}_{durationTicks}.csv");
        string summaryFile = Path.Combine(logDir, $"summary_{timestamp}_{seed}_{durationTicks}.txt");

        // Header
        File.WriteAllText(logFile, "Tick,Agents,Herbivores,Omnivores,Carnivores,Plants,Structures" + Environment.NewLine);

        var startTime = DateTime.Now;

        // Track min/max/final for each diet type
        int minHerb = int.MaxValue, maxHerb = 0, finalHerb = 0;
        int minOmni = int.MaxValue, maxOmni = 0, finalOmni = 0;
        int minCarni = int.MaxValue, maxCarni = 0, finalCarni = 0;

        for (long i = 0; i < durationTicks; i++)
        {
            simulation.Update();

            if (i % 60 == 0)
            {
                StatsLogger.LogStats(simulation, logFile);

                // Count diets for tracking
                int herbs = 0, omnis = 0, carnis = 0;
                foreach (var agent in simulation.AgentPopulation)
                {
                    if (agent.IsAlive)
                    {
                        switch (agent.Diet)
                        {
                            case Biology.DietType.Herbivore: herbs++; break;
                            case Biology.DietType.Omnivore: omnis++; break;
                            case Biology.DietType.Carnivore: carnis++; break;
                        }
                    }
                }

                minHerb = Math.Min(minHerb, herbs); maxHerb = Math.Max(maxHerb, herbs); finalHerb = herbs;
                minOmni = Math.Min(minOmni, omnis); maxOmni = Math.Max(maxOmni, omnis); finalOmni = omnis;
                minCarni = Math.Min(minCarni, carnis); maxCarni = Math.Max(maxCarni, carnis); finalCarni = carnis;
            }
            if (i % 600 == 0)
            {
                Console.WriteLine(durationTicks > 600 ? $"Progress: {i * 100 / durationTicks}%" : $"Tick: {i}");
            }
        }

        var endTime = DateTime.Now;
        var realDuration = endTime - startTime;
        var simulatedSeconds = durationTicks / 60.0;

        // Write summary to file
        var summary = new System.Text.StringBuilder();
        summary.AppendLine("=== SIMULATION COMPLETE ===");
        summary.AppendLine($"Seed: {seed}");
        summary.AppendLine($"Duration: {durationTicks} ticks ({simulatedSeconds} seconds)");
        summary.AppendLine($"Real time: {realDuration.TotalSeconds:F2} seconds");
        summary.AppendLine($"Speedup: {durationTicks / 60.0 / realDuration.TotalSeconds:F2}x real-time.");
        summary.AppendLine($"Log saved to: {logFile}");
        summary.AppendLine();
        summary.AppendLine("===  POPULATION SUMMARY  ===");
        summary.AppendLine($"Herbivores: Min={minHerb}, Max={maxHerb}, Final={finalHerb}");
        summary.AppendLine($"Omnivores:  Min={minOmni}, Max={maxOmni}, Final={finalOmni}");
        summary.AppendLine($"Carnivores: Min={minCarni}, Max={maxCarni}, Final={finalCarni}");
        summary.AppendLine($"Total Final: {finalHerb + finalOmni + finalCarni}");

        // Indicate stability
        bool herbStable = finalHerb > 10;
        bool omniStable = finalOmni > 10;
        bool carniStable = finalCarni > 10;
        summary.AppendLine();
        summary.AppendLine($"Stability: Herb={herbStable}, Omni={omniStable}, Carni={carniStable}");
        if (herbStable && omniStable && carniStable)
        {
            summary.AppendLine("SUCCESS: All three diet types are stable!");
        }
        else
        {
            summary.AppendLine("NEEDS BALANCING: One or more diet types went extinct or near-extinct.");
        }

        File.WriteAllText(summaryFile, summary.ToString());
        Console.WriteLine(summary.ToString());
        Console.WriteLine($"Summary saved to: {summaryFile}");
    }
}
