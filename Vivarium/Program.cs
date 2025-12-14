using System;
using System.IO;
using Vivarium.Engine;

namespace Vivarium;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        bool headless = false;
        int duration = 3600; // Default 1 minute (60 fps * 60 sec)

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
        }

        if (headless)
        {
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
        Console.WriteLine($"Starting Headless Simulation for {durationTicks} ticks...");
        
        var simulation = new Simulation();
        simulation.Initialize();

        // Setup Logging
        string logDir = "Logs";
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string logFile = Path.Combine(logDir, $"simulation_run_{timestamp}.csv");
        
        // Header
        File.WriteAllText(logFile, "Tick,Agents,Herbivores,Omnivores,Carnivores,Plants,Structures" + Environment.NewLine);

        var startTime = DateTime.Now;

        for (long i = 0; i < durationTicks; i++)
        {
            simulation.Update();

            if (i % 60 == 0)
            {
                StatsLogger.LogStats(simulation, logFile);
                if (i % 600 == 0)
                {
                    Console.WriteLine($"Progress: {i}/{durationTicks} ticks ({(double)i/durationTicks*100:F1}%)");
                }
            }
        }

        var endTime = DateTime.Now;
        var realDuration = endTime - startTime;

        Console.WriteLine($"Simulation Complete.");
        Console.WriteLine($"Simulated {durationTicks} ticks in {realDuration.TotalSeconds:F2} seconds.");
        Console.WriteLine($"Speedup: {durationTicks / 60.0 / realDuration.TotalSeconds:F2}x real-time.");
        Console.WriteLine($"Log saved to: {logFile}");
    }
}
