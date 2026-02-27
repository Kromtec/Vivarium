# Vivarium User Documentation

> A high-performance artificial life ecosystem simulation

## Table of Contents

1. [Introduction](#introduction)
2. [Core Concepts](#core-concepts)
3. [The Agents](#the-agents)
4. [The Neural Network](#the-neural-network)
5. [Genetics & Evolution](#genetics--evolution)
6. [The Ecosystem](#the-ecosystem)
7. [The World](#the-world)
8. [Configuration](#configuration)
9. [Running the Simulation](#running-the-simulation)
10. [Tips & Best Practices](#tips--best-practices)

---

## Introduction

**Vivarium** is an artificial life simulation that models a complete ecosystem with autonomous agents, plants, and structures. The simulation is built on one fundamental principle: **behaviors emerge from neural networks, not hard-coded rules**.

Unlike traditional games where creature behaviors are explicitly programmed, Vivarium's agents ("Creatures") possess brains that process sensory information through neural networks encoded in their DNA. Over time, natural selection favors agents that are better adapted to their environment.

### What Makes Vivarium Unique

| Feature | Description |
|---------|-------------|
| **Evolutionary Neural Networks** | Agents have genomes that encode RNN (Recurrent Neural Network) connections |
| **No Hard-Coded Behavior** | All actions emerge from the neural network processing |
| **Real-Time Simulation** | Runs at 60 FPS on modern hardware |
| **Emergent Complexity** | Watch ecosystems develop: predation, cooperation, migration |
| **Scientific Visualization** | Track genome evolution, brain activity, population dynamics |

---

## Core Concepts

### The Simulation Loop

Every frame (typically 60 times per second), the simulation executes:

```
For each agent (in random order):
    1. SENSORS → Read environment (location, energy, nearby entities)
    2. THINK → Process through neural network
    3. ACT → Execute chosen action (move, eat, reproduce, attack)
    4. METABOLIZE → Consume energy based on activity
    5. DIE or SURVIVE → Check if energy depleted or age exceeded
```

### Energy Flow

The entire ecosystem runs on **solar energy**:

```
Sunlight → Plants (photosynthesis) → Herbivores → Carnivores
                          ↓
                      Decomposition (when entities die)
```

- **Plants** gain energy from "sunlight" automatically
- **Agents** must eat to gain energy
- **All entities** lose energy over time (metabolism)
- **Death** occurs when energy reaches zero

---

## The Agents

Agents are the autonomous creatures in your simulation. Each agent is defined by:

### Properties

| Property | Description | Range |
|----------|-------------|-------|
| `Energy` | Current energy level | 0 - MaxEnergy |
| `Age` | Ticks since birth | 0 - ∞ |
| `Position` | X, Y coordinates on grid | 0 - GridSize |
| `Diet` | What the agent eats | Herbivore, Omnivore, Carnivore |
| `Generation` | How many ancestors | 1, 2, 3... |
| `ParentId` | ID of parent (for lineage tracking) | long |

### Physical Traits (Derived from Genome)

These traits are decoded from the agent's genome and affect behavior:

| Trait | Effect |
|-------|--------|
| **Strength** | Damage dealt in attacks |
| **Bravery** | Willingness to fight vs flee |
| **MetabolicEfficiency** | Energy conservation |
| **Perception** | How far the agent can "see" |
| **Speed** | Movement cooldown duration |
| **TrophicBias** | Plant vs meat preference (for omnivores) |
| **Constitution** | Resistance to damage |

### Actions

Agents can perform these actions (determined by neural network output):

| Action | Description | Energy Cost |
|--------|-------------|-------------|
| `MoveN/S/E/W` | Move one cell north/south/east/west | Base + Distance |
| `Attack` | Attack adjacent agent | High |
| `Reproduce` | Create offspring | Very High |
| `Suicide` | End own life (rare) | N/A |
| `Flee` | Emergency movement away from threats | High |

### Diet Types

```
┌─────────────────────────────────────────────────────┐
│                   TROPHIC LEVELS                    │
├─────────────────────────────────────────────────────┤
│  CARNIVORE    → Eats only agents                    │
│  OMNIVORE     → Eats plants AND agents              │
│  HERBIVORE    → Eats only plants                    │
│  PLANT        → Photosynthesis (passive energy)     │
└─────────────────────────────────────────────────────┘
```

---

## The Neural Network

This is the heart of Vivarium. Each agent has a brain composed of:

### Architecture

```
┌────────────────────────────────────────────────────────────┐
│                    NEURAL NETWORK ARCHITECTURE             │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  INPUT LAYER (SENSORS)         28 neurons                  │
│  ┌───────────────────────────────────────────────────────┐ │
│  │ LocationX, LocationY, Random, Energy, Age, Oscillator │ │
│  │ Directional Sensors (8-way): Agent/Plant/Structure    │ │
│  │ Local Density: Agent/Plant/Structure                  │ │
│  │ Trait Sensors: Strength, Bravery, etc.                │ │
│  └───────────────────────────────────────────────────────┘ │
│                         ↓                                  │
│  HIDDEN LAYER (PROCESSING)     128 neurons                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Fully connected to inputs via genome-defined genes  │   │
│  │ Recurrent connections for memory                    │   │
│  │ Tanh activation function                            │   │
│  └─────────────────────────────────────────────────────┘   │
│                         ↓                                  │
│  OUTPUT LAYER (ACTIONS)         8 neurons                  │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ MoveN, MoveS, MoveE, MoveW, Attack, Reproduce,      │   │
│  │ Suicide, Flee                                       │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

### How It Works

1. **Sensors** read the environment and set input neuron values (-1 to +1)
2. **Genome** defines connection weights between neurons (which input connects to which output)
3. **Processing** flows: Inputs → Hidden → Outputs
4. **Action Selection**: The output neuron with highest value determines action

### Instincts

Agents have biological "overrides" that bias behavior in critical situations:

- **Survival Instinct**: If threats detected nearby → strongly favor `Flee`
- **Feeding Instinct**: If energy low → strongly favor movement toward food
- **Reproduction Instinct**: If healthy and mature → strongly favor `Reproduce`

These instincts work WITH the neural network (they add bias), not AGAINST it. An agent can override instincts if its brain decides otherwise.

### Memory

The hidden layer has **recurrent connections** (self-connections) that decay over time:

- `HiddenNeuronDecayFactor = 0.5` (configurable)
- Agents "remember" what happened ~2-3 ticks ago
- This enables simple planning and decision-making

---

## Genetics & Evolution

### The Genome

Each agent's genome is an array of **512 genes** (configurable). Each gene is a 32-bit integer encoding:

```
┌────────────────────────────────────────┐
│              GENE STRUCTURE            │
├────────────────────────────────────────┤
│  Bits 0-7:   Source Neuron ID (0-255)  │
│  Bits 8-15:  Sink Neuron ID (0-255)    │
│  Bits 16-31: Weight (-4.0 to +4.0)     │
└────────────────────────────────────────┘
```

The first 14 genes are reserved for **traits** (Strength, Bravery, etc.), while the remaining genes define neural network connections.

### Mutation

When an agent reproduces, there's a chance (`MutationRate`, default 0.1%) that each gene will mutate:

1. Pick a random bit to flip (0-31)
2. XOR the gene with a mask (e.g., `00000100`)
3. New gene = old gene with one bit changed

This simple mechanism creates:

- New neural connections
- Strengthened/weakened existing connections
- Completely novel brain architectures

### Natural Selection

The environment applies selective pressure:

| Pressure | Effect |
|----------|--------|
| **Energy Scarcity** | Agents with efficient metabolism survive longer |
| **Predation** | Fast/brave agents survive attacks |
| **Competition** | Successful reproducers pass genes |
| **Overcrowding** | Agents must disperse or die |

Over time (typically 1000-5000 ticks), you'll observe:

- 🧬 **Speciation**: Different body plans emerge
- 🏃 **Arms Races**: Speed vs perception
- 🐾 **Behavioral Evolution**: Hunting strategies, foraging patterns
- 🌍 **Ecosystem Dynamics**: Predator-prey cycles

---

## The Ecosystem

### Population Dynamics

Vivarium simulates a **food web**:

```
         ┌─────────┐
         │  SUN    │  (unlimited energy source)
         └────┬────┘
              ↓
         ┌─────────┐
         │ PLANTS  │  (produce energy via photosynthesis)
         └────┬────┘
              ├──────────────┬──────────────┐
              ↓              ↓              ↓
    ┌──────────────┐  ┌─────────────┐  ┌───────────┐
    │  HERBIVORES  │  │  OMNIVORES  │  │ CARNIVORES│
    │ (eat plants) │  │ (both)      │  │ (agents)  │
    └──────┬───────┘  └──────┬──────┘  └─────┬─────┘
           │                 │               │
           └─────────────────┴───────────────┘
                         ↓
                   DEATH (energy = 0)
```

### Trophic Interactions

| Interaction | Result |
|-------------|--------|
| Herbivore eats Plant | Herbivore gains energy, Plant dies |
| Carnivore eats Agent | Attacker gains strength×energy, Defender dies |
| Omnivore eats Plant | Same as herbivore |
| Omnivore eats Agent | Same as carnivore |

### Carrying Capacity

The ecosystem self-regulates:

- **Too many plants** → Herbivores thrive → Plants decline
- **Too many herbivores** → Plants depleted → Herbivores starve
- **Carnivores emerge** → Herbivores controlled → Plants recover

This creates **oscillations** - the classic Lotka-Volterra predator-prey cycle!

---

## The World

### Grid System

The world is a 2D grid (default 170×96 cells, configurable):

```
┌────────────────────────────────────────────────────────────────┐
│                                                                │
│   (0,0) ───────────────────────────→ X                         │
│    │                                                           │
│    │                                                           │
│    │              🦎 🦎 🦎                                   │
│    │           🌿      🦎                                     │
│    │     🌿  🌿   🏰                                         │
│    │   🌿     🌿                                              │
│    │                                                           │
│    ↓                                                           │
│   Y                                                            │
│                                                                │
└────────────────────────────────────────────────────────────────┘

Legend:
  🦎 = Agent (creature)
  🌿 = Plant
  🏰 = Structure
```

### Sensors

Each agent "sees" its environment through sensors:

| Sensor | Description | Range |
|--------|-------------|-------|
| `LocationX/Y` | Current position | -1 to +1 (normalized) |
| `Random` | Noise for stochastic behavior | -1 to +1 |
| `Energy` | Current energy level | -1 to +1 |
| `Age` | How old | 0 to MaturityAge |
| `Oscillator` | Time-dependent wave | -1 to +1 |
| `Directional Density` | Agents/Plants/Structures in 8 directions | -1 to +1 |
| `Local Density` | Entities in 3×3 area | -1 to +1 |
| `Trait Sensors` | Self-perception of own traits | -1 to +1 |

### Perception Radius

An agent's **Perception** trait affects how far directional sensors reach:

- `Perception = 0` → 1 cell range
- `Perception = 1` → 2 cell range
- `Perception = max` → 6 cell range

---

## Configuration

### Configuration Files

Vivarium uses JSON configuration files:

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
    "instinctBiasStrength": 1.5
  },
  "genetics": {
    "genomeLength": 512,
    "traitGeneCount": 14,
    "mutationRate": 0.001
  }
}
```

### Key Parameters

#### World Settings

| Parameter | Description | Recommended Range |
|-----------|-------------|-------------------|
| `gridWidth` | World width in cells | 100-200 |
| `gridHeight` | World height in cells | 60-120 |
| `agentPoolSize` | Max concurrent agents | 500-1500 |
| `plantPoolSize` | Max concurrent plants | 1000-3000 |
| `seed` | Random seed (same = reproducible) | Any integer |

#### Agent Settings

| Parameter | Description | Effect |
|-----------|-------------|--------|
| `baseMetabolismRate` | Energy loss per tick | Higher = harder survival |
| `maturityAge` | Ticks to reach adulthood | Higher = slower reproduction |
| `reproductionCooldown` | Ticks between reproductions | Higher = slower population growth |
| `reproductionOverheadPct` | % energy given to child | Higher = stronger children, weaker parents |

#### Brain Settings

| Parameter | Description | Effect |
|-----------|-------------|--------|
| `hiddenNeuronDecayFactor` | Memory retention (0-1) | 0.9 = long memory, 0.1 = reactive |
| `instinctBiasStrength` | How strongly instincts override brain | Higher = more biological, less learned |

#### Genetics Settings

| Parameter | Description | Effect |
|-----------|-------------|--------|
| `genomeLength` | Number of genes | More = more complex brains |
| `mutationRate` | Probability per gene | Higher = faster evolution, less stability |

---

## Running the Simulation

### Command Line

```bash
# Run with default config
dotnet run --project src/Simulation/Vivarium.csproj

# Run with custom config
dotnet run --project src/Simulation/Vivarium.csproj -- --config myconfig.json

# Run in headless mode (no graphics)
dotnet run --project src/Simulation/Vivarium.csproj -- --headless

# Run headless with custom config and output
dotnet run --project src/Simulation/Vivarium.csproj -- --headless --config myconfig.json --output results.json
```

### Controls

| Key | Action |
|-----|--------|
| `Space` | Pause/Resume simulation |
| `Escape` | Open menu / Exit |
| `Click` | Select entity for inspection |
| `Tab` | Toggle statistics overlay |

### Output Files

When running in headless mode:

| File | Description |
|------|-------------|
| `genome_census.json` | Population genetics snapshot |
| `simulation_stats.json` | Population counts over time |

---

## Tips & Best Practices

### For Interesting Simulations

1. **Start with diversity**: Use high `mutationRate` initially, then lower it once stable species emerge

2. **Balance your ecosystem**: 
   - Too few plants → herbivores die out
   - Too many plants → herbivores explode, then crash
   
3. **Watch for equilibrium**: The simulation is most interesting in the first 5000 ticks when evolution is fastest

4. **Use the inspection tools**: Select individual agents to see their brain activity and genome

### Common Issues

| Problem | Solution |
|---------|----------|
| Everyone dies immediately | Increase `photosynthesisRate` or decrease `baseMetabolismRate` |
| Population explodes (lag) | Decrease `reproductionOverheadPct` or increase `metabolismRate` |
| No evolution observed | Increase `mutationRate` |
| All agents look the same | Wait longer, or introduce a crisis (reduce food) |

### Performance

- **Optimal FPS**: 60 (configured via `framesPerSecond`)
- **Max Agents**: ~1000 on modern hardware
- **Memory**: ~100MB for typical simulation
- **Headless Mode**: 10-100x faster than graphics mode

---

## Further Reading

- [API Documentation](api/Vivarium.html) - Detailed code reference
- [Configuration Reference](configuration.html) - All config options
- [Source Code](https://github.com/Kromtec/Vivarium) - GitHub repository

---

*This documentation was generated for Vivarium - An Artificial Life Ecosystem Simulation*
