## Solution Design

### 1\. High-Level Strategy

We will treat this as a "Batch Processing System."

1.  **Configuration:** Define the parameter grid.
2.  **Orchestration:** Map parameters -\> Async Tasks -\> Parallel Execution.
3.  **Visualization:** Transform the raw list of results into the specific matrix/array format required by Plotly.

### 2\. Architecture (Modular)

Even though it's one file, we use `module` keywords to enforce boundaries:

  * `Domain`: Types (`Parameter`, `Result`).
  * `Core`: The "slow" simulation logic.
  * `Tests`: A function to sanity-check the Core.
  * `Runner`: The parallel execution logic.
  * `Viz`: Plotly plotting logic.

### 3\. The "Black Box" Formula

We will use a mathematical peak function so the heatmap looks interesting (e.g., a 2D Gaussian or Sine wave).
