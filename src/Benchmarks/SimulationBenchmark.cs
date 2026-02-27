using BenchmarkDotNet.Attributes;
using System.Diagnostics.CodeAnalysis;
using Vivarium.Config;

namespace Vivarium.Benchmarks;

[IterationCount(1)]
[WarmupCount(1)]
[MemoryDiagnoser]
public class SimulationBenchmark
{
    public SimulationBenchmark()
    {
        const int seed = 64;
        SimulationConfig config = SimulationConfig.CreateDefault(seed);

        ConfigProvider.Initialize(config);
    }

    [Benchmark]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Needs to not be static by BenchmarkDotNet")]
    public void RunSimulationStep()
    {
        const int duration = 3600; // Default 1 minute (60 fps * 60 sec)
        Program.RunHeadless(duration);
    }
}
