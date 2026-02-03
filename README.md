# 🟨 Granular - [**Play it on Itch.io**](https://alinlucian.itch.io/granular)
> **A real-time falling-sand simulation sandbox with emergent elemental behavior and high-performance multi-threaded updates.**

---

## ⚡ About the Project
**Granular** is a real-time **cellular material simulation** inspired by classic *falling-sand* experiments, focused on **emergent behavior**, **element interaction**, and **performance-oriented simulation**.

The world is represented as a large 2D grid divided into chunks, where elements such as **sand, water, stone, and smoke** interact according to simple physical rules, producing complex and organic results.  
Simulation updates are handled using **Unity Jobs + Burst**, allowing tens or hundreds of thousands of cells to update each frame efficiently.

The project serves both as a **technical exploration of cellular automata and data-oriented design** and as a sandbox foundation for future gameplay ideas, experiments, or visual effects.

---

## 🎮 Controls

### 🟧 World Interaction
- **Left Click** — Place selected element  
- **Right Click** — Remove element  
- **Mouse Wheel** — Adjust brush size  
- **1** — Smoke  
- **2** — Water  
- **3** — Sand  
- **4** — Stone  

---

## 🧠 Core Features
- **Cellular Sandbox Simulation** — Large-scale grid-based world with elemental interactions  
- **Element Physics** — Sand falls and piles, water flows and pools, stone remains solid, smoke rises  
- **Chunked World Layout** — World divided into chunks for cache-friendly memory access  
- **Unity Jobs + Burst** — Highly optimized, multi-threaded simulation updates  
- **Deterministic Update Phases** — Checkerboard-style updates to avoid race conditions  
- **Real-Time Editing** — Add, remove, and modify elements directly with the mouse  
- **Procedural Initialization** — Noise-based world generation for varied starting states  
- **GPU-Based Rendering** — Compute shader–driven rendering to a RenderTexture  

---

## 📜 License

**Creative Commons Attribution–NonCommercial 4.0 International (CC BY-NC 4.0)**  

This work, including all source code, assets, and materials of *“Granular”*  
by **Alin Lucian Brebulet**, is licensed under CC BY-NC 4.0.

You are free to:  
- **Share** — copy and redistribute the material in any medium or format  
- **Adapt** — remix, transform, and build upon the material  

Under the following terms:  
- **Attribution** — You must give appropriate credit and indicate if changes were made.  
- **NonCommercial** — You may not use the material for commercial purposes.  
- **No additional restrictions** — You may not apply legal terms or technological measures that legally restrict others from doing anything the license permits.

🔗 Full license text:  
https://creativecommons.org/licenses/by-nc/4.0/
