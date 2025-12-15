# Vivarium

**Vivarium** is a high-performance artificial life simulation written in **C# (.NET 10)** using the **MonoGame** framework. 

The project simulates an ecosystem of autonomous agents ("Creatures") interacting within a dynamic grid-based world containing plants and structures.

## 🧬 Key Features

* **Evolutionary Neural Networks:** Agents possess a genome encoding a Recurrent Neural Network (RNN), allowing for memory and complex decision-making.
* **Complex Phenotypes:** Genes determine physical traits (Strength, Speed, Perception, etc.) and neural wiring.
* **Dynamic Ecosystem:**
    * **Herbivores:** Graze on plants, efficient metabolism, high resilience.
    * **Carnivores:** Hunt other agents, high energy cost, aggressive.
    * **Omnivores:** Adaptable diet, balanced traits.
* **Emergent Behavior:** Watch as agents evolve strategies for hunting, grazing, swarming, and combat without hard-coded rules.
* **High Performance:** Optimized using `Span<T>` and Struct-based architecture to support thousands of agents at 60 FPS.
* **Interactive Camera:** Full 2D camera support with zoom and panning to observe the simulation at macro and micro levels.
* **Headless Mode:** Run simulations without graphics for rapid training and data collection.

## 🎮 Controls

* **WASD / Arrow Keys:** Pan Camera
* **Scroll Wheel:** Zoom In/Out
* **Left Click:** Select Agent/Entity (View details in Inspector)
* **Right Click:** Deselect
* **Space:** Pause/Resume Simulation
* **.(Dot):** Step Forward One Tick (when paused)

## 🚀 Running the Simulation

### Standard Mode (Visual)
Run the project normally through your IDE or terminal:
```bash
dotnet run --project Vivarium/Vivarium.csproj -c Release
```
*Note: Using `Release` configuration significantly improves performance.*

### Headless Mode (Fast Simulation)
For rapid evolution or data gathering, run in headless mode. This disables rendering and runs the simulation as fast as the CPU allows.
```bash
dotnet run --project Vivarium/Vivarium.csproj -c Release -- --headless --duration 36000
```
*   `--headless`: Enables headless mode.
*   `--duration <ticks>`: Sets the simulation duration (60 ticks = 1 second). Default is 3600 (1 minute).
*   `--seed <int>`: (Optional) Set a specific random seed for reproducibility.

Logs are saved to the `Logs/` directory in CSV format. Log file names include timestamps and seed information for easy identification.
```Logs/simulation_run_<timestamp>_<seed>_<duration>.csv```

## 🛠️ Tech Stack

* **Language:** C# 14 / .NET 10
* **Engine:** MonoGame (DesktopGL)
* **Architecture:** Entity Component System (ECS) inspired, struct-based arrays for memory efficiency.