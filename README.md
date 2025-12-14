# Vivarium

**Vivarium** is a high-performance artificial life simulation written in **C# (.NET 10)** using the **MonoGame** framework. 

The project simulates an ecosystem of autonomous agents ("Creatures") interacting within a dynamic grid-based world containing plants and structures.

## 🧬 Key Features

* **Evolutionary Neural Networks:** Agents possess a genome encoding a Recurrent Neural Network (RNN), allowing for memory and complex decision-making.
* **Complex Phenotypes:** Genes determine visual traits (color based on behavior) and neural wiring.
* **Emergent Behavior:** Watch as agents evolve strategies for hunting, grazing, swarming, and combat without hard-coded rules.
* **High Performance:** Optimized using `Span<T>` and Struct-based architecture to support thousands of agents at 60 FPS.
* **Interactive Camera:** Full 2D camera support with zoom and panning to observe the simulation at macro and micro levels.

## 🛠️ Tech Stack

* **Language:** C# 14 / .NET 10
* **Engine:** MonoGame (DesktopGL)
* **Architecture:** Entity Component System (ECS) inspired, struct-based arrays for memory efficiency.