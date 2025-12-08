# 🚀 Module 3 Mini-Project: The "GridRunner" Hyperparameter Tuner

## Project Brief

**Context:**
You have developed a new theoretical model called `BioSim-X`. It takes two parameters: `Pressure` and `Temperature`. The model outputs a "Yield" score.

However, the model is computationally expensive (it takes simulated time to run). You need to find the "sweet spot" of parameters that maximizes Yield. Running these sequentially (one after another) would take too long. You need to run a **Parallel Grid Search**.

**Your Goal:**
Write an F\# script (`.fsx`) that:

1.  Generates a grid of 100+ parameter combinations.
2.  Runs the simulation for all combinations **in parallel** using F\# Async workflows.
3.  Aggregates the results.
4.  Generates an interactive **Heatmap** using `Plotly.NET` and saves it as an HTML report.

### Input Data

You will generate the data programmatically:

  * **Pressure:** 0.0 to 10.0 (step 1.0)
  * **Temperature:** 0.0 to 10.0 (step 1.0)
  * **The "Black Box" Function:** I will provide a function that simulates the math and the delay.

### Requirements & Constraints

1.  **Async & Parallel (Chapter 9):**
      * You must wrap the simulation function in an `async { ... }` block.
      * Use `Async.Sleep` to simulate computational work (e.g., 50ms per run).
      * Use `Async.Parallel` to execute the batch. **Do not** run them sequentially.
2.  **Visualization (Chapter 10):**
      * Use the **Plotly.NET** library (via NuGet).
      * Create a Heatmap: X-axis = Pressure, Y-axis = Temperature, Z-axis = Yield.
      * Save the result to `ExperimentReport.html`.
3.  **Architecture (Chapter 11):**
      * Organize your script into explicit **modules** (e.g., `module Domain`, `module Simulation`, `module Reporting`). This mimics a real production codebase structure.
      * Include a simple "Unit Test" function that verifies the logic before the big run starts.

### Hints

  * To use Plotly in a script: `#r "nuget: Plotly.NET"`.
  * Remember that `Async.Parallel` takes a *sequence of async computations* and returns an *async array of results*. You still need to pass it to `Async.RunSynchronously` to actually start the engine.
  * For the heatmap, you will need to separate your results into three arrays (X values, Y values, Z values).

## Solution

[See this](solution.md)