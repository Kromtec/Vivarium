using System;
using System.IO;
using System.Runtime.InteropServices;
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
        }

        if (headless)
        {
            // Attempt to attach to the parent console to enable output
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
            }
            RunHeadless(duration, seed);
        }
        else
        {
            using var game = new VivariumGame(seed);
            game.Run();
        }
    }

    static void RunHeadless(int durationTicks, int seed)
    {
        Console.WriteLine($"Starting Headless Simulation for {durationTicks} ticks with seed {seed}...");

        var simulation = new Simulation(seed);
        simulation.Initialize();

        // Setup Logging
        string logDir = "Logs";
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string logFile = Path.Combine(logDir, $"simulation_run_{timestamp}_{seed}_{durationTicks}.csv");
        
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
                    Console.WriteLine($"Progress: {i}/{durationTicks} ticks ({(double)i / durationTicks * 100:F1}%)");
                }
            }
        }

        var endTime = DateTime.Now;
        var realDuration = endTime - startTime;
        var simulatedSeconds = durationTicks / 60.0;

        Console.WriteLine($"Simulation Complete.");
        Console.WriteLine($"Simulated {durationTicks} ticks ({simulatedSeconds} seconds) in {realDuration.TotalSeconds:F2} seconds real time.");
        Console.WriteLine($"Speedup: {durationTicks / 60.0 / realDuration.TotalSeconds:F2}x real-time.");
        Console.WriteLine($"Log saved to: {logFile}");
    }
}
