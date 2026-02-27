# Configuration System

The simulation uses a modular configuration system that allows balancing parameters to be tweaked without code changes.

## Configuration Categories

| Category | Mutability | Description |
|----------|------------|-------------|
| `WorldConfig` | Immutable | Grid size, entity pool sizes, seed |
| `AgentConfig` | Runtime | Agent balance (metabolism, reproduction, movement, combat) |
| `PlantConfig` | Runtime | Plant balance (photosynthesis, reproduction, aging) |
| `BrainAIConfig` | Runtime | Neural network and instinct parameters |
| `GeneticsConfig` | Partial | Mutation rate (mutable), genome structure (immutable) |

## Example Configuration

```json
{
  "world": {
    "gridHeight": 96,
    "gridWidth": 170,
    "cellSize": 13,
    "agentPoolSize": 906,
    "plantPoolSize": 2040,
    "structurePoolSize": 510,
    "seed": 64
  },
  "agent": {
    "reproductionOverheadPct": 0.30,
    "baseMetabolismRate": 0.01,
    "maturityAge": 600,
    "reproductionCooldownFrames": 600
  },
  "plant": {
    "photosynthesisRate": 0.50,
    "shrivelRate": 0.40,
    "maturityAge": 600
  },
  "brain": {
    "hiddenNeuronDecayFactor": 0.5,
    "feedingInstinctThreshold": 0.6,
    "reproductionInstinctThreshold": 0.9
  },
  "genetics": {
    "genomeLength": 512,
    "traitGeneCount": 14,
    "mutationRate": 0.001
  },
  "framesPerSecond": 60.0
}
```

## Loading Custom Config

```bash
dotnet run --project src/Simulation/Vivarium.csproj -c Release -- --config myconfig.json
```
